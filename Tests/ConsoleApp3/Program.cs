using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.Util;
using TestProject1;

Random rand = new();

Dictionary<Type, int> counts = [];
bool withLifetimeObserver = false;

for(int step = 1; step <= 3; ++step)
{
    Tests.Setup();
    HostApplicationBuilder builder = step switch
    {
        1 => Tests.BuildImplementationType(),
        2 => Tests.BuildImplementationInstance(),
        _ => Tests.BuildImplementationFactory()
    };
    if (withLifetimeObserver)
    {
        Tests.AddTraces(builder.Services);
    }
    using (IHost host = builder.Build())
    {
        if (withLifetimeObserver)
        {
            host.Services.GetRequiredService<LifetimeObserver>().LifetimeEventOccured += LifetimeObserver_LifetimeEventOccured;
        }
        else
        {
            Model.LifetimeEventOccured += LifetimeObserver_LifetimeEventOccured;
        }
        IServiceScope serviceScope = host.Services.CreateScope();
        for (int i = 0; i < 1000000; ++i)
        {
            if(Tests.ModelRequest(serviceScope, host, rand.Next(1, 26)) is Model model)
            {
                int id = model.Id;
            }
            else
            {
                serviceScope = host.Services.CreateScope();
            }
            if (i % 100 == 0)
            {
                PrintInfo(step);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
        }

    }
    for (int i = 0; i < 1000000; ++i)
    {
        if (i % 100 == 0)
        {
            PrintInfo(step);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }
    PrintInfo(step);
}

void PrintInfo(int step)
{
    Console.SetCursorPosition(0, 0);
    for (int i = 0; i < 1 + counts.Count; ++i)
    {
        Console.WriteLine("                                              ");
    }
    Console.SetCursorPosition(0, 0);
    switch (step)
    {
        case 1:
            Console.WriteLine("BuildImplementationType");
            break;
        case 2:
            Console.WriteLine("BuildImplementationInstance");
            break;
        case 3:
            Console.WriteLine("BuildImplementationFactory");
            break;
    }
    foreach(var it in counts)
    {
        Console.WriteLine($"{it.Key}: {it.Value}");
    }
}

void LifetimeObserver_LifetimeEventOccured(object? sender, LifetimeEventArgs args)
{
    if(args.Kind is LifetimeEventKind.Created)
    {
        if(!counts.TryGetValue(args.Type, out int value))
        {
            counts.Add(args.Type, 1);
        }
        else
        {
            counts[args.Type] = ++value;
        }
    }
    else
    {
        --counts[args.Type];
    }
}
