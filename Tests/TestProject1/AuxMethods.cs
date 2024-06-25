using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestProject1;
public class AuxMethods
{
    public static HostApplicationBuilder Build()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ISingletonByType>();
        builder.Services.AddSingleton<ISingletonByInstance>(new SingletonModel());
        builder.Services.AddSingleton<ISingletonByFactory>(s => new SingletonModel());
        builder.Services.AddScoped<IScopedByType>();
        builder.Services.AddScoped<IScopedByFactory>(s => new ScopedModel());
        builder.Services.AddTransient<ITransientByType>();
        builder.Services.AddTransient<ITransientByFactory>(s => new TransientModel());
        builder.Services.AddKeyedSingleton<ISingletonByType>(null);
        builder.Services.AddKeyedSingleton<ISingletonByType>(1);
        builder.Services.AddKeyedSingleton<ISingletonByType>(2);
        builder.Services.AddKeyedSingleton<ISingletonByInstance>(null, new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByInstance>(3, new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByInstance>(4, new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByFactory>(null, (s, k) => new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByFactory>(5, (s, k) => new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByFactory>(6, (s, k) => new SingletonModel());
        builder.Services.AddKeyedScoped<IScopedByType>(null);
        builder.Services.AddKeyedScoped<IScopedByType>(1);
        builder.Services.AddKeyedScoped<IScopedByType>(2);
        builder.Services.AddKeyedScoped<IScopedByFactory>(null, (s, k) => new ScopedModel());
        builder.Services.AddKeyedScoped<IScopedByFactory>(3, (s, k) => new ScopedModel());
        builder.Services.AddKeyedScoped<IScopedByFactory>(4, (s, k) => new ScopedModel());
        builder.Services.AddKeyedScoped<ITransientByType>(null);
        builder.Services.AddKeyedScoped<ITransientByType>(1);
        builder.Services.AddKeyedScoped<ITransientByType>(2);
        builder.Services.AddKeyedScoped<ITransientByType>(null, (s, k) => new TransientModel());
        builder.Services.AddKeyedScoped<ITransientByType>(3, (s, k) => new TransientModel());
        builder.Services.AddKeyedScoped<ITransientByType>(4, (s, k) => new TransientModel());
        return builder;
    }
}
