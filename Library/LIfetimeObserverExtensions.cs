using Microsoft.Extensions.DependencyInjection;
namespace Net.Leksi.Util;
public static class LIfetimeObserverExtensions
{
    public static IServiceCollection AddLIfetimeObserver(this IServiceCollection services, Action<LifetimeObserver> config)
    {
        LifetimeObserver.Add(services, config);
        return services;
    }
}
