using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.LifetimeObserverDemo;
using Net.Leksi.Util;

const string s_referencedCount = "referenced-count";
const string s_requestsCount = "requests-count";
const string s_seed = "seed";
const string s_askKey = "/?";
const string s_helpKey = "--help";
const string s_clearLine = "                                                                                                         ";
const string s_usePassed = "use-passed";

if (args.Contains(s_askKey) || args.Contains(s_helpKey))
{
    Usage();
    return;
}

void Usage()
{
    Console.WriteLine(
        string.Format(
            "Usage:\n {0} [--{1}} <number>] [--{2} <number>] [--{3} {{1|0}}]",
            Path.GetFileName(Environment.ProcessPath),
            s_referencedCount,
            s_seed,
            s_usePassed
        )
    );
}

Dictionary<Type, int> counts = [];

bool isRunning = true;
Console.CancelKeyPress += Console_CancelKeyPress;

IConfiguration bootstrapConfig = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

int referencedCount = 0;
if (bootstrapConfig[s_referencedCount] is string s1 && int.TryParse(s1, out referencedCount)) { }
int requestsCount = 0;
if (bootstrapConfig[s_requestsCount] is string s2 && int.TryParse(s2, out requestsCount)) { }
int seed = -1;
if (bootstrapConfig[s_seed] is string s3 && int.TryParse(s3, out seed)) { }
bool usePassed = bootstrapConfig[s_usePassed] is string s4 && s4 == "1";
int prevSeed = seed;
IEnumerable<ScriptEntry> script = AuxMethods.GetScript(ref seed, 0.1, 0.5).Select(e =>
    {
        if (!isRunning)
        {
            throw new BreakStreamException();
        }
        return e;
    }
);
if (requestsCount > 0)
{
    script = script.Take(requestsCount);
}

if (!usePassed)
{
    Console.WriteLine($"{nameof(referencedCount)}: {referencedCount}");
    Console.WriteLine($"{(prevSeed != seed ? "generated " : string.Empty)}{nameof(seed)}: {seed}");
    if (requestsCount > 0)
    {
        Console.WriteLine($"{nameof(requestsCount)}: {requestsCount}");
    }
    else
    {
        Console.WriteLine("Press Ctrl-C to finish");
    }
    Console.WriteLine();
}
else
{
    Console.WriteLine($"log: {nameof(referencedCount)} {referencedCount}");
    Console.WriteLine($"log: {nameof(seed)} {seed}{(prevSeed != seed ? " generated" : string.Empty)}");
    if (requestsCount > 0)
    {
        Console.WriteLine($"log: {nameof(requestsCount)} {requestsCount}");
    }
}
int headerLinesCount = 5;

Model.LifetimeEventOccured += Model_LifetimeEventOccured;

HostApplicationBuilder builder = AuxMethods.CreateBuilder();

AuxMethods.AddTraces(builder.Services);
HashSet<IModel>? references = null;

using (IHost host = builder.Build())
{
    LifetimeObserver? lifetimeObserver = null;
    lifetimeObserver = host.Services.GetRequiredService<LifetimeObserver>();
    lifetimeObserver.LifetimeEventOccured += Model_LifetimeEventOccured;
    if (usePassed)
    {
        lifetimeObserver.ProxyPassthroughOccured += LifetimeObserver_ProxyPassthroughOccured;
    }
    int i = 0;
    IEnumerable<IModel?> stream = AuxMethods.PlayScript(host, script);
    try
    {
        references = referencedCount > 0 ? [] : null;
        foreach (IModel? model in stream)
        {
            if (references is { })
            {
                references.Add(model!);
                while (references.Count > referencedCount)
                {
                    references.Remove(references.First());
                }
            }
            else
            {
                int id = model!.Id;
            }
            Console.WriteLine($"log: Variant {model!.GetType()} {model.GetHashCode()} {model.Variant}");
            if (++i % 100 == 0)
            {
                PrintInfo();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
        }
    }
    catch (BreakStreamException) { }
}
references?.Clear();
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
GC.WaitForPendingFinalizers();
PrintInfo();
Console.WriteLine("done");
void LifetimeObserver_ProxyPassthroughOccured(object? sender, ProxyPassthroughEventArgs args)
{
    Console.WriteLine($"log: Passed {args.Type} {args.Hash}");
}
void Model_ConstructorEventOccured(object? sender, LifetimeEventArgs args)
{
    Console.WriteLine($"log: {args.Kind} {args.Type} {args.Hash}");
}
void PrintInfo()
{
    if (!usePassed)
    {
        int i = headerLinesCount - 1;
        int total = 0;
        lock (counts)
        {
            foreach (var it in counts)
            {
                Console.SetCursorPosition(0, ++i);
                Console.Write(s_clearLine);
                Console.SetCursorPosition(0, i);
                total += it.Value;
                Console.Write($"{it.Key}: {it.Value}");
            }
        }
        Console.SetCursorPosition(0, ++i);
        Console.Write(s_clearLine);
        Console.SetCursorPosition(0, i);
        Console.WriteLine($"total: {total}");
    }
}
void Model_LifetimeEventOccured(object? sender, LifetimeEventArgs args)
{
    if (usePassed)
    {
        Console.WriteLine($"log: {args.Kind} {args.Type} {args.Hash}");
    }
    lock (counts)
    {
        if (args.Kind is LifetimeEventKind.Created)
        {
            if (!counts.TryGetValue(args.Type, out int value))
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
}
void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    isRunning = false;
    e.Cancel = true;
}
class BreakStreamException : Exception { }

