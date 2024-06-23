using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestProject1
{
    public class Tests
    {
        [Test]
        public void TestWithoutLifetimeObserver()
        {
            HostApplicationBuilder builder = BuildImplementationType();
            IHost host = builder.Build();
            AssertThatAllOk(
                host,
                [1,1,2,2,3,3,4,5,6,7,8,8,9,9,10,10,11,11,12,12,13,13,14,15,16,17,18,19,20,21]
            );
            builder = BuildImplementationInstance();
            host = builder.Build();
            AssertThatAllOk(
                host, 
                [1, 1, 2, 2, 3, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 15, 16, 17, 18, 19, 20, 21]
            );
        }

        private HostApplicationBuilder BuildImplementationInstance()
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

        private static HostApplicationBuilder BuildImplementationType()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<SingletonModel>();
            builder.Services.AddScoped<ScopedModel>();
            builder.Services.AddTransient<TransientModel>();
            builder.Services.AddKeyedSingleton<SingletonModel>(1);
            builder.Services.AddKeyedSingleton<SingletonModel>(2);
            builder.Services.AddKeyedScoped<ScopedModel>(3);
            builder.Services.AddKeyedScoped<ScopedModel>(4);
            builder.Services.AddKeyedTransient<TransientModel>(5);
            builder.Services.AddKeyedTransient<TransientModel>(6);
            return builder;
        }
        private void AssertThatAllOk(IHost host, int[] expected)
        {
            Assert.That(host.Services.GetRequiredService<SingletonModel>().Id, Is.EqualTo(expected[0]));
            Assert.That(host.Services.GetRequiredService<SingletonModel>().Id, Is.EqualTo(expected[1]));
            Assert.That(host.Services.GetRequiredService<ScopedModel>().Id, Is.EqualTo(expected[2]));
            Assert.That(host.Services.GetRequiredService<ScopedModel>().Id, Is.EqualTo(expected[3]));
            IServiceScope serviceScope = host.Services.CreateScope();
            Assert.That(serviceScope.ServiceProvider.GetRequiredService<ScopedModel>().Id, Is.EqualTo(expected[4]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredService<ScopedModel>().Id, Is.EqualTo(expected[5]));
            Assert.That(host.Services.GetRequiredService<TransientModel>().Id, Is.EqualTo(expected[6]));
            Assert.That(host.Services.GetRequiredService<TransientModel>().Id, Is.EqualTo(expected[7]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredService<TransientModel>().Id, Is.EqualTo(expected[8]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredService<TransientModel>().Id, Is.EqualTo(expected[9]));
            Assert.That(host.Services.GetRequiredKeyedService<SingletonModel>(1).Id, Is.EqualTo(expected[10]));
            Assert.That(host.Services.GetRequiredKeyedService<SingletonModel>(1).Id, Is.EqualTo(expected[11]));
            Assert.That(host.Services.GetRequiredKeyedService<SingletonModel>(2).Id, Is.EqualTo(expected[12]));
            Assert.That(host.Services.GetRequiredKeyedService<SingletonModel>(2).Id, Is.EqualTo(expected[13]));
            Assert.That(host.Services.GetRequiredKeyedService<ScopedModel>(3).Id, Is.EqualTo(expected[14]));
            Assert.That(host.Services.GetRequiredKeyedService<ScopedModel>(3).Id, Is.EqualTo(expected[15]));
            Assert.That(host.Services.GetRequiredKeyedService<ScopedModel>(4).Id, Is.EqualTo(expected[16]));
            Assert.That(host.Services.GetRequiredKeyedService<ScopedModel>(4).Id, Is.EqualTo(expected[17]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(3).Id, Is.EqualTo(expected[18]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(3).Id, Is.EqualTo(expected[19]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(4).Id, Is.EqualTo(expected[20]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<ScopedModel>(4).Id, Is.EqualTo(expected[21]));
            Assert.That(host.Services.GetRequiredKeyedService<TransientModel>(5).Id, Is.EqualTo(expected[22]));
            Assert.That(host.Services.GetRequiredKeyedService<TransientModel>(5).Id, Is.EqualTo(expected[23]));
            Assert.That(host.Services.GetRequiredKeyedService<TransientModel>(6).Id, Is.EqualTo(expected[24]));
            Assert.That(host.Services.GetRequiredKeyedService<TransientModel>(6).Id, Is.EqualTo(expected[25]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(5).Id, Is.EqualTo(expected[26]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(5).Id, Is.EqualTo(expected[27]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(6).Id, Is.EqualTo(expected[28]));
            Assert.That(serviceScope.ServiceProvider.GetRequiredKeyedService<TransientModel>(6).Id, Is.EqualTo(expected[29]));
        }
    }
}