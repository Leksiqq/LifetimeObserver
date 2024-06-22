using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Net.Leksi.System;

public class LifetimeObserver : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly ConditionalWeakTable<object, Tracer> _tracers = [];
    private readonly ObservableCollection<CountHolder> _counts = [];
    internal IServiceCollection? ServiceDescriptors { get; set; };
    public void Trace(Type type)
    {
        if(ServiceDescriptors is null)
        {
            throw new InvalidOperationException();
        }
        ServiceDescriptor[] descriptors = ServiceDescriptors!.Where(sd => sd.ServiceType == type).ToArray();
        if (descriptors.Length == 0)
        {
            throw new InvalidOperationException($"No service for type {type} found!");
        }
        ServiceDescriptors.AddKeyedScoped()
        foreach (ServiceDescriptor sd in descriptors)
        {
            ServiceDescriptors.Remove(sd);
            if (sd.ImplementationType is { })
            {
                ServiceDescriptors.Add(
                    new ServiceDescriptor(
                        sd.ServiceType, 
                        this, 
                        (sp, sk) => {
                            return Activator.CreateInstance(sd.ImplementationType)!;
                        }, 
                        sd.Lifetime
                    )
                );
            }
            else if(sd.ImplementationFactory is { })
            {
                ServiceDescriptors.Add(
                    new ServiceDescriptor(
                        sd.ServiceType,
                        this,
                        (sp, sk) => {
                            return sd.ImplementationFactory.Invoke(sp);
                        },
                        sd.Lifetime
                    )
                );
            }
            else if (sd.ImplementationInstance is { })
            {
                ServiceDescriptors.Add(
                    new ServiceDescriptor(
                        sd.ServiceType,
                        this,
                        (sp, sk) => {
                            return sd.ImplementationInstance;
                        },
                        sd.Lifetime
                    )
                );
            }
            else if (sd.KeyedImplementationType is { })
            {
                ServiceDescriptors.Add(
                    new ServiceDescriptor(
                        sd.ServiceType,
                        this,
                        (sp, sk) => {
                            return sd.ImplementationInstance;
                        },
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
    internal void ReportFinalized(Type type)
    {
        throw new NotImplementedException();
    }
}
