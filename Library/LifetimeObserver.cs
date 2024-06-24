using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
namespace Net.Leksi.Util;
public class LifetimeObserver
{
    public event LifetimeEventHandler? LifetimeEventOccured;
    private readonly ConditionalWeakTable<object, Tracer> _tracers = [];
    private readonly Dictionary<object, Key> _keys = [];
    private readonly Key _noKey = new();
    private readonly List<ServiceDescriptor> _replacements = [];
    private readonly object _lock = new();
    private readonly HashSet<Type> _tracedTypes = [];
    private IServiceCollection? _serviceDescriptors;
    public void Trace(Type type)
    {
        if (_serviceDescriptors is null)
        {
            throw new InvalidOperationException();
        }
        _tracedTypes.Add(type);
        ServiceDescriptor[] descriptors = _serviceDescriptors!.Where(sd => sd.ServiceType == type).ToArray();
        if (descriptors.Length == 0)
        {
            throw new InvalidOperationException($"No service for type {type} found!");
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
                        (s, k) => GetService(s, sd.ServiceType, key),
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
                        s => GetService(s, sd.ServiceType, key),
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
            throw new InvalidOperationException();
        }
        return _tracedTypes.Select(t => t);
    }
    internal void Start(IServiceCollection services)
    {
        _serviceDescriptors = services;
        _replacements.Clear();
    }
    internal void Finish()
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
    private object GetService(IServiceProvider serviceProvider, Type serviceType, Key serviceKey)
    {
        object result = serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
        if (!_tracers.TryGetValue(result, out _))
        {
            lock (_lock)
            {
                if (!_tracers.TryGetValue(result, out _))
                {
                    _tracers.Add(result, new Tracer(this, result));
                }
            }
        }
        return result;
    }
}