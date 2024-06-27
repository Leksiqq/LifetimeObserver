using Microsoft.Extensions.DependencyInjection;
namespace Net.Leksi.Util;
public static class LifetimeObserverExtensions
{
    public static IServiceCollection AddLifetimeObserver(this IServiceCollection services, Action<LifetimeObserver> config)
    {
        LifetimeObserver.Add(services, config);
        return services;
    }
}
