using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.Util;
namespace Net.Leksi.LifetimeObserverDemo;
public static class AuxMethods
{
    public const int s_ModelRequestsCount = 28;
    public static HostApplicationBuilder CreateBuilder()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ISingletonByType, SingletonModel>();
        builder.Services.AddSingleton<ISingletonByInstance>(new SingletonModel());
        builder.Services.AddSingleton<ISingletonByFactory>(s => new SingletonModel());
        builder.Services.AddScoped<IScopedByType, ScopedModel>();
        builder.Services.AddScoped<IScopedByFactory>(s => new ScopedModel());
        builder.Services.AddTransient<ITransientByType, TransientModel>();
        builder.Services.AddTransient<ITransientByFactory>(s => new TransientModel());
        builder.Services.AddKeyedSingleton<ISingletonByType, SingletonModel>(1);
        builder.Services.AddKeyedSingleton<ISingletonByType, SingletonModel>(2);
        builder.Services.AddKeyedSingleton<ISingletonByInstance>(3, new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByInstance>(4, new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByFactory>(5, (s, k) => new SingletonModel());
        builder.Services.AddKeyedSingleton<ISingletonByFactory>(6, (s, k) => new SingletonModel());
        builder.Services.AddKeyedScoped<IScopedByType, ScopedModel>(1);
        builder.Services.AddKeyedScoped<IScopedByType, ScopedModel>(2);
        builder.Services.AddKeyedScoped<IScopedByFactory>(3, (s, k) => new ScopedModel());
        builder.Services.AddKeyedScoped<IScopedByFactory>(4, (s, k) => new ScopedModel());
        builder.Services.AddKeyedScoped<ITransientByType, TransientModel>(1);
        builder.Services.AddKeyedScoped<ITransientByType, TransientModel>(2);
        builder.Services.AddKeyedScoped<ITransientByFactory>(3, (s, k) => new TransientModel());
        builder.Services.AddKeyedScoped<ITransientByFactory>(4, (s, k) => new TransientModel());
        return builder;
    }
    public static void AddTraces(IServiceCollection services)
    {
        services.AddLIfetimeObserver(lto =>
        {
            lto.Trace(typeof(SingletonModel));
            lto.Trace(typeof(ScopedModel));
            lto.Trace(typeof(TransientModel));
        });
    }
    public static IModel? ModelRequest(IServiceProvider services, int variant)
    {
        IModel? result = variant switch
        {
            0 => services.GetRequiredService<ISingletonByType>(),
            1 => services.GetRequiredService<ISingletonByInstance>(),
            2 => services.GetRequiredService<ISingletonByFactory>(),
            3 => services.GetRequiredService<IScopedByType>(),
            4 => services.GetRequiredService<IScopedByFactory>(),
            5 => services.GetRequiredService<ITransientByType>(),
            6 => services.GetRequiredService<ITransientByFactory>(),
            7 => services.GetRequiredKeyedService<ISingletonByType>(null),
            8 => services.GetRequiredKeyedService<ISingletonByType>(1),
            9 => services.GetRequiredKeyedService<ISingletonByType>(2),
            10 => services.GetRequiredKeyedService<ISingletonByInstance>(null),
            11 => services.GetRequiredKeyedService<ISingletonByInstance>(3),
            12 => services.GetRequiredKeyedService<ISingletonByInstance>(4),
            13 => services.GetRequiredKeyedService<ISingletonByFactory>(null),
            14 => services.GetRequiredKeyedService<ISingletonByFactory>(5),
            15 => services.GetRequiredKeyedService<ISingletonByFactory>(6),
            16 => services.GetRequiredKeyedService<IScopedByType>(null),
            17 => services.GetRequiredKeyedService<IScopedByType>(1),
            18 => services.GetRequiredKeyedService<IScopedByType>(2),
            19 => services.GetRequiredKeyedService<IScopedByFactory>(null),
            20 => services.GetRequiredKeyedService<IScopedByFactory>(3),
            21 => services.GetRequiredKeyedService<IScopedByFactory>(4),
            22 => services.GetRequiredKeyedService<ITransientByType>(null),
            23 => services.GetRequiredKeyedService<ITransientByType>(1),
            24 => services.GetRequiredKeyedService<ITransientByType>(2),
            25 => services.GetRequiredKeyedService<ITransientByFactory>(null),
            26 => services.GetRequiredKeyedService<ITransientByFactory>(3),
            27 => services.GetRequiredKeyedService<ITransientByFactory>(4),
            _ => null
        };
        if(result is { })
        {
            result.Variant = variant;
        }
        return result;
    }
    public static void PrintServiceDescriptors(IServiceCollection services)
    {
        foreach (
            ServiceDescriptor sd in
            services.Where(
                sd => typeof(IModel).IsAssignableFrom(sd.ServiceType)
                    || sd.ServiceType == typeof(ScopedModel)
            )
        )
        {
            if (sd.IsKeyedService)
            {
                Console.WriteLine($"keyed: {sd.ServiceType}, {sd.Lifetime}, k: {sd.ServiceKey}, t: {sd.KeyedImplementationType}, i: {sd.KeyedImplementationInstance}, f: {sd.KeyedImplementationFactory}");
            }
            else
            {
                Console.WriteLine($"not keyed: {sd.ServiceType}, {sd.Lifetime}, t: {sd.ImplementationType}, i: {sd.ImplementationInstance}, f: {sd.ImplementationFactory}");
            }
        }
    }
    public static IEnumerable<IModel?> PlayScript(IHost host, IEnumerable<ScriptEntry> script)
    {
        IServiceProvider services = host.Services;
        IServiceScope serviceScope = services.CreateScope();
        foreach (ScriptEntry step in script)
        {
            if (step.CreateNewScope)
            {
                serviceScope = host.Services.CreateScope();
            }
            if (step.UseScope)
            {
                services = serviceScope.ServiceProvider;
            }
            else
            {
                services = host.Services;
            }
            yield return ModelRequest(services, step.Variant);
        }
    }
    public static IEnumerable<ScriptEntry> GetScript(ref int seed, double newScopeRatio, double scopeRatio)
    {
        if (seed == -1)
        {
            seed = (int)(long.Parse(
                new string(
                    DateTime.UtcNow.Ticks.ToString().Reverse().ToArray()
                )
            ) % int.MaxValue);
        }
        Random random = new(seed);
        return GenerateScript(random, newScopeRatio, scopeRatio);
    }
    private static IEnumerable<ScriptEntry> GenerateScript(Random random, double newScopeRatio, double scopeRatio)
    {
        if (scopeRatio <= newScopeRatio)
        {
            throw new ArgumentException($"{nameof(newScopeRatio)} must be less than {nameof(scopeRatio)}");
        }
        bool needCreateNewScope = false;
        while (true)
        {
            double ratio = random.NextDouble();
            if (ratio < newScopeRatio)
            {
                needCreateNewScope = true;
            }
            else
            {
                yield return new ScriptEntry(random.Next(0, s_ModelRequestsCount), needCreateNewScope, ratio < scopeRatio);
                needCreateNewScope = false;
            }
        }
    }
}
