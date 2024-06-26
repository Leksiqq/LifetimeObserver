using Microsoft.Extensions.DependencyInjection;
namespace Net.Leksi.Util;
public static class LIfetimeObserverExtensions
{
    public static IServiceCollection AddLIfetimeObserver(this IServiceCollection services, Action<LifetimeObserver> config)
    {
        LifetimeObserver lifetimeObserver = new();
        lifetimeObserver.StartConfiguring(services);
        config.Invoke(lifetimeObserver);
        services.AddSingleton(lifetimeObserver);
        lifetimeObserver.FinishConfiguring();
        return services;
    }
}
