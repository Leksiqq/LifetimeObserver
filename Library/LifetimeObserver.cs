using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
namespace Net.Leksi.Util;
public class LifetimeObserver
{
    public event LifetimeEventHandler? LifetimeEventOccured;
    public event ProxyPassthroughEventHandler? ProxyPassthroughOccured;
    public event EventHandler? NextTracedCount;
    private readonly ConditionalWeakTable<object, Tracer> _tracers = [];
    private readonly Dictionary<object, Key> _keys = [];
    private readonly Key _noKey = new();
    private readonly List<ServiceDescriptor> _replacements = [];
    private readonly object _lock = new();
    private readonly HashSet<Type> _tracedTypes = [];
    private readonly EventArgs _nextTracedCount = new();
    private IServiceCollection? _serviceDescriptors;
    private int _tracedCount = 0;
    public int CountTracedForRaisingEvent { get; set; } = 100;
    public static void Add(IServiceCollection services, Action<LifetimeObserver> config)
    {
        LifetimeObserver lifetimeObserver = new();
        lifetimeObserver.StartConfiguring(services);
        config.Invoke(lifetimeObserver);
        services.AddSingleton(lifetimeObserver);
        lifetimeObserver.FinishConfiguring();
    }
    public void Trace(Type type)
    {
        if (_serviceDescriptors is null)
        {
            throw new LifetimeObserverException("Must be called inside of configuring action.")
            {
                KindOfError = LifetimeObserverException.Kind.CalledOutsideOfConfiguring
            };
        }
        _tracedTypes.Add(type);
        ServiceDescriptor[] descriptors = _serviceDescriptors!.Where(sd => 
            sd.ServiceType == type
            || (
                !sd.IsKeyedService 
                && (
                    sd.ImplementationType == type
                    || sd.ImplementationInstance?.GetType() == type
                    || (sd.ImplementationFactory?.Method.ReturnType.IsAssignableFrom(type) ?? false)
                )
            )
            || (
                sd.IsKeyedService
                && (
                    sd.KeyedImplementationType == type
                    || sd.KeyedImplementationInstance?.GetType() == type
                    || (sd.KeyedImplementationFactory?.Method.ReturnType.IsAssignableFrom(type) ?? false)
                )
            )
        ).ToArray();
        if (descriptors.Length == 0)
        {
            throw new LifetimeObserverException($"No service for type {type} found!") 
            { 
                Type = type, 
                KindOfError = LifetimeObserverException.Kind.NoServiceRegisteredForType
            };
        }
        foreach (ServiceDescriptor sd in descriptors)
        {
            Key key = _noKey;
            if (sd.IsKeyedService)
            {
                if (!_keys.TryGetValue(sd.ServiceKey!, out Key? savedKey))
                {
                    savedKey = new Key(sd.ServiceKey);
                    _keys.Add(sd.ServiceKey!, savedKey);
                }
                key = savedKey;
            }
            _serviceDescriptors.Remove(sd);
            if (sd.IsKeyedService)
            {
                if (sd.KeyedImplementationType is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            sd.KeyedImplementationType,
                            sd.Lifetime
                        )
                    );
                }
                else if (sd.KeyedImplementationInstance is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            sd.KeyedImplementationInstance
                        )
                    );
                }
                else if (sd.KeyedImplementationFactory is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            (s, k) => sd.KeyedImplementationFactory.Invoke(s, key.SourceKey),
                            sd.Lifetime
                        )
                    );
                }
                _replacements.Add(
                    new ServiceDescriptor(
                        sd.ServiceType,
                        sd.ServiceKey,
                        (s, k) => GetService(s, sd.ServiceType, key, type),
                        sd.Lifetime
                    )
                );
            }
            else
            {
                if (sd.ImplementationType is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            sd.ImplementationType,
                            sd.Lifetime
                        )
                    );
                }
                else if (sd.ImplementationInstance is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            sd.ImplementationInstance
                        )
                    );
                }
                else if (sd.ImplementationFactory is { })
                {
                    _replacements.Add(
                        new ServiceDescriptor(
                            sd.ServiceType,
                            key,
                            (s, k) => sd.ImplementationFactory.Invoke(s),
                            sd.Lifetime
                        )
                    );
                }
                _replacements.Add(
                    new ServiceDescriptor(
                        sd.ServiceType,
                        s => GetService(s, sd.ServiceType, key, type),
                        sd.Lifetime
                    )
                );
            }
        }
    }
    public void Trace<T>()
    {
        Trace(typeof(T));
    }
    public IEnumerable<Type> GetTracedTypes()
    {
        if(_serviceDescriptors is { })
        {
            throw new LifetimeObserverException("Must be called after the host is built.")
            {
                KindOfError = LifetimeObserverException.Kind.CalledBeforeHostIsBuilt
            };
        }
        return _tracedTypes.Select(t => t);
    }
    internal void StartConfiguring(IServiceCollection services)
    {
        _serviceDescriptors = services;
        _replacements.Clear();
    }
    internal void FinishConfiguring()
    {
        foreach (ServiceDescriptor sd in _replacements)
        {
            _serviceDescriptors!.Add(sd);
        }
        _serviceDescriptors = null;
        _replacements.Clear();
    }
    internal void ReportLifetimeEvent(LifetimeEventKind kind, Type type, int hash, string info)
    {
        LifetimeEventOccured?.Invoke(this, new LifetimeEventArgs { Kind = kind, Type = type, Hash = hash, Info = info });
    }
    private object GetService(IServiceProvider serviceProvider, Type serviceType, Key serviceKey, Type expectedType)
    {
        object result = serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
        if(result.GetType() == expectedType)
        {
            if(ProxyPassthroughOccured is { })
            {
                ProxyPassthroughOccured.Invoke(this, new ProxyPassthroughEventArgs { 
                    ServiceType = serviceType, 
                    ServiceKey = serviceKey, 
                    Type = expectedType, 
                    Hash = result.GetHashCode() 
                });
            }
            if (!_tracers.TryGetValue(result, out _))
            {
                lock (_lock)
                {
                    if (!_tracers.TryGetValue(result, out _))
                    {
                        _tracers.Add(result, new Tracer(this, result));
                        ++_tracedCount;
                        if(_tracedCount >= CountTracedForRaisingEvent)
                        {
                            NextTracedCount?.Invoke(this, _nextTracedCount);
                            _tracedCount = 0;
                        }
                    }
                }
            }
        }
        return result;
    }
}