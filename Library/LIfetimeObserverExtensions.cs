using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.System;

public static class LIfetimeObserverExtensions
{
    public static IServiceCollection AddLIfetimeObserver(this IServiceCollection services, Action<LifetimeObserver> config)
    {
        LifetimeObserver lifetimeObserver = new();
        lifetimeObserver.ServiceDescriptors = services;
        config.Invoke(lifetimeObserver);
        services.AddSingleton(lifetimeObserver);
        lifetimeObserver.ServiceDescriptors = null;
        return services;
    }
}
