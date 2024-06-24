using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.Util;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;

namespace TestProject1
{
    public class Tests
    {
        [SetUp]
        public static void Setup()
        {
            Model._idGen = 0;
        }
        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TestSequence1(int step)
        {
            List<int> requests = [];
            List<int> results = [];
            HostApplicationBuilder builder;
            IHost? host;
            Dictionary<Type, int> counts = [];
            Dictionary<Type, HashSet<int>> ids = [];
            Random rand = new();
            builder = step switch
            {
                1 => Tests.BuildImplementationType(),
                2 => Tests.BuildImplementationInstance(),
                _ => Tests.BuildImplementationFactory()
            };
            host = builder.Build();
            IServiceScope serviceScope = host.Services.CreateScope();
            for (int i = 0; i < 1000000; ++i)
            {
                int request = rand.Next(1, 26);
                requests.Add(request);
                if(ModelRequest(serviceScope, host, request) is Model model)
                {
                    results.Add(model.Id);
                }
                else
                {
                    serviceScope = host.Services.CreateScope();
                    results.Add(0);
                }
            }
            Setup();
            builder = step switch
            {
                1 => Tests.BuildImplementationType(),
                2 => Tests.BuildImplementationInstance(),
                _ => Tests.BuildImplementationFactory()
            };
            AddTraces(builder.Services);
            host = builder.Build();
            //LifetimeObserver lifetimeObserver = host.Services.GetRequiredService<LifetimeObserver>();
            //lifetimeObserver.LifetimeEventOccured += (s, e) =>
            //{
            //    if(e.Kind is LifetimeEventKind.Created)
            //    {
            //        if (counts.TryGetValue(e.Type, out int value))
            //        {
            //            counts[e.Type] = ++value;
            //        }
            //        else
            //        {
            //            counts.Add(e.Type, 1);
            //        }
            //    }
            //};
            serviceScope = host.Services.CreateScope();
            for (int i = 0; i < requests.Count; ++i)
            {
                if(ModelRequest(serviceScope, host, requests[i]) is Model model)
                {
                    if (!ids.TryGetValue(model.GetType(), out HashSet<int>? value))
                    {
                        value = new HashSet<int>();
                        ids.Add(model.GetType(), value);
                    }
                    value.Add(model.Id);
                    Assert.That(model.Id, Is.EqualTo(results[i]));
                }
                else
                {
                    serviceScope = host.Services.CreateScope();
                    Assert.That(0, Is.EqualTo(results[i]));
                }
            }
            foreach(var it in ids)
            {
                Console.WriteLine($"{it.Key}: {it.Value.Count}");
            }
        }
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestSequence(bool withLifetimeObserver)
        {
            HostApplicationBuilder builder;
            IHost? host;

            builder = BuildImplementationType();
            if (withLifetimeObserver)
            {
                AddTraces(builder.Services);
            }
            //PrintServiceDescriptors(builder.Services);
            host = builder.Build();
            AssertThatAllOk(
                host,
                [1, 1, 2, 2, 3, 4, 1, 1, 5, 5, 6, 7, 1, 1, 8, 8, 9, 9, 2, 2, 10, 10, 11, 11, 12, 13, 14, 15, 16, 17, 1, 1, 8, 8, 9, 9, 5, 5, 18, 18, 19, 19, 20, 21, 22, 23, 24, 25]
            );
            Setup();
            builder = BuildImplementationInstance();
            if (withLifetimeObserver)
            {
                AddTraces(builder.Services);
            }
            //PrintServiceDescriptors(builder.Services);
            host = builder.Build();
            AssertThatAllOk(
                host,
                [1, 1, 4, 4, 5, 6, 1, 1, 7, 7, 8, 9, 1, 1, 2, 2, 3, 3, 4, 4, 10, 10, 11, 11, 12, 13, 14, 15, 16, 17, 1, 1, 2, 2, 3, 3, 7, 7, 18, 18, 19, 19, 20, 21, 22, 23, 24, 25]
            );
            Setup();
            builder = BuildImplementationFactory();
            if (withLifetimeObserver)
            {
                AddTraces(builder.Services);
            }
            //PrintServiceDescriptors(builder.Services);
            host = builder.Build();
            AssertThatAllOk(
                host,
                [1, 1, 2, 2, 3, 4, 1, 1, 5, 5, 6, 7, 1, 1, 8, 8, 9, 9, 2, 2, 10, 10, 11, 11, 12, 13, 14, 15, 16, 17, 1, 1, 8, 8, 9, 9, 5, 5, 18, 18, 19, 19, 20, 21, 22, 23, 24, 25]
            );
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

        private void PrintServiceDescriptors(IServiceCollection services)
        {
            foreach (
                ServiceDescriptor sd in 
                services.Where(
                    sd => sd.ServiceType == typeof(SingletonModel) 
                        || sd.ServiceType == typeof(ScopedModel) 
                        || sd.ServiceType == typeof(TransientModel)
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

        public static HostApplicationBuilder BuildImplementationFactory()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(s => new SingletonModel());
            builder.Services.AddScoped<ScopedModel>(s => new ScopedModel());
            builder.Services.AddTransient<TransientModel>(s => new TransientModel());
            builder.Services.AddKeyedSingleton(1, (s, key) => new SingletonModel());
            builder.Services.AddKeyedSingleton(2, (s, key) => new SingletonModel());
            builder.Services.AddKeyedScoped<ScopedModel>(3, (s, key) => new ScopedModel());
            builder.Services.AddKeyedScoped<ScopedModel>(4, (s, key) => new ScopedModel());
            builder.Services.AddKeyedTransient<TransientModel>(5, (s, key) => new TransientModel());
            builder.Services.AddKeyedTransient<TransientModel>(6, (s, key) => new TransientModel());
            return builder;
        }

        public static HostApplicationBuilder BuildImplementationInstance()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(new SingletonModel());
            builder.Services.AddScoped<ScopedModel>();
            builder.Services.AddTransient<TransientModel>();
            builder.Services.AddKeyedSingleton(1, new SingletonModel());
            builder.Services.AddKeyedSingleton(2, new SingletonModel());
            builder.Services.AddKeyedScoped<ScopedModel>(3);
            builder.Services.AddKeyedScoped<ScopedModel>(4);
            builder.Services.AddKeyedTransient<TransientModel>(5);
            builder.Services.AddKeyedTransient<TransientModel>(6);
            return builder;
        }

        public static HostApplicationBuilder BuildImplementationType()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<SingletonModel>();
            builder.Services.AddScoped<ScopedModel>();
            builder.Services.AddTransient<TransientModel>();
            builder.Services.AddKeyedSingleton<SingletonModel>(null);
            builder.Services.AddKeyedSingleton<SingletonModel>(1);
            builder.Services.AddKeyedSingleton<SingletonModel>(2);
            builder.Services.AddKeyedScoped<ScopedModel>(null);
            builder.Services.AddKeyedScoped<ScopedModel>(3);
            builder.Services.AddKeyedScoped<ScopedModel>(4);
            builder.Services.AddKeyedTransient<TransientModel>(null);
            builder.Services.AddKeyedTransient<TransientModel>(5);
            builder.Services.AddKeyedTransient<TransientModel>(6);
            return builder;
        }
        public static Model? ModelRequest(IServiceScope serviceScope, IHost host, int variant)
        {
            return variant switch
            {
                1 => host.Services.GetRequiredService<SingletonModel>(),
                2 => host.Services.GetRequiredService<ScopedModel>(),
                3 => host.Services.GetRequiredService<TransientModel>(),
                4 => host.Services.GetRequiredKeyedService<SingletonModel>(null),
                5 => host.Services.GetRequiredKeyedService<SingletonModel>(1),
                6 => host.Services.GetRequiredKeyedService<SingletonModel>(2),
                7 => host.Services.GetRequiredKeyedService<ScopedModel>(null),
                8 => host.Services.GetRequiredKeyedService<ScopedModel>(3),
                9 => host.Services.GetRequiredKeyedService<ScopedModel>(4),
                10 => host.Services.GetRequiredKeyedService<TransientModel>(null),
                11 => host.Services.GetRequiredKeyedService<TransientModel>(5),
                12 => host.Services.GetRequiredKeyedService<TransientModel>(6),
                13 => serviceScope.ServiceProvider.GetRequiredService<SingletonModel>(),
                14 => serviceScope.ServiceProvider.GetRequiredService<ScopedModel>(),
                15 => serviceScope.ServiceProvider.GetRequiredService<TransientModel>(),
                16 => serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(null),
                17 => serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(1),
                18 => serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(2),
                19 => serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(null),
                20 => serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(3),
                21 => serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(4),
                22 => serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(null),
                23 => serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(5),
                24 => serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(6),
                _ => null
            };
        }
        private void AssertThatAllOk(IHost host, int[] expected)
        {
            IServiceScope serviceScope = host.Services.CreateScope();
            List<int> got = [];
            got.Add(host.Services.GetRequiredService<SingletonModel>().Id);
            got.Add(host.Services.GetRequiredService<SingletonModel>().Id);
            got.Add(host.Services.GetRequiredService<ScopedModel>().Id);
            got.Add(host.Services.GetRequiredService<ScopedModel>().Id);
            got.Add(host.Services.GetRequiredService<TransientModel>().Id);
            got.Add(host.Services.GetRequiredService<TransientModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<SingletonModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<SingletonModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<ScopedModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<ScopedModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<TransientModel>().Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredService<TransientModel>().Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(1).Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(1).Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(2).Id);
            got.Add(host.Services.GetRequiredKeyedService<SingletonModel>(2).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(3).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(3).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(4).Id);
            got.Add(host.Services.GetRequiredKeyedService<ScopedModel>(4).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(null).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(5).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(5).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(6).Id);
            got.Add(host.Services.GetRequiredKeyedService<TransientModel>(6).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(1).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(1).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(2).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<SingletonModel>(2).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(3).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(3).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(4).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(4).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(null).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(5).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(5).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(6).Id);
            got.Add(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(6).Id);
            //Console.WriteLine($"exp: [{string.Join(", ", expected)}]");
            //Console.WriteLine($"got: [{string.Join(", ", got)}]");
            Assert.That(got.ToArray(), Is.EqualTo(expected));
        }
    }
}