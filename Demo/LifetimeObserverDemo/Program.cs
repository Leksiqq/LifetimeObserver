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
const string s_withLogStyle = "with-log-style";
const string s_nextTracedCount = "next-traced-count";

if (args.Contains(s_askKey) || args.Contains(s_helpKey))
{
    Usage();
    return;
}

void Usage()
{
    Console.WriteLine(
        string.Format(
            "Usage:\n {0} [--{1}} <number>] [--{2}} <number>] [--{3} <number>] [--{4} {{1|0}}] [--{5} <number>]",
            Path.GetFileName(Environment.ProcessPath),
            s_referencedCount,
            s_requestsCount,
            s_seed,
            s_withLogStyle,
            s_nextTracedCount
        )
    );
}

Dictionary<Type, int> counts = [];
object locker = new();

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
int nextTracedCount = 0;
if (bootstrapConfig[s_nextTracedCount] is string s4 && int.TryParse(s4, out nextTracedCount)) { }
bool withLogStyle = bootstrapConfig[s_withLogStyle] is string s5 && s5 == "1";

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

Random changeInfoRandom = new(seed * 37);
double changeInfoRatio = 0.1;

int headerLinesCount = 0;
if (withLogStyle)
{
    Console.WriteLine($"log: {nameof(referencedCount)} {referencedCount}");
    Console.WriteLine($"log: {nameof(seed)} {seed}{(prevSeed != seed ? " generated" : string.Empty)}");
    if (requestsCount > 0)
    {
        Console.WriteLine($"log: {nameof(requestsCount)} {requestsCount}");
    }
}
else
{
    Console.WriteLine($"{nameof(referencedCount)}: {referencedCount}");
    ++headerLinesCount;
    Console.WriteLine($"{(prevSeed != seed ? "generated " : string.Empty)}{nameof(seed)}: {seed}");
    ++headerLinesCount;
    if (requestsCount > 0)
    {
        Console.WriteLine($"{nameof(requestsCount)}: {requestsCount}");
    }
    else
    {
        Console.WriteLine("Press Ctrl-C to finish");
    }
    ++headerLinesCount;
    Console.WriteLine();
    ++headerLinesCount;
}

Model.LifetimeEventOccured += OnLifetimeEventOccured;

HostApplicationBuilder builder = AuxMethods.CreateBuilder();

AuxMethods.AddTraces(builder.Services);
HashSet<IModel>? references = null;

using (IHost host = builder.Build())
{
    LifetimeObserver? lifetimeObserver = null;
    lifetimeObserver = host.Services.GetRequiredService<LifetimeObserver>();
    lifetimeObserver.LifetimeEventOccured += OnLifetimeEventOccured;
    if(nextTracedCount > 0)
    {
        lifetimeObserver.CountTracedForRaisingEvent = nextTracedCount;
    }
    lifetimeObserver.NextTracedCount += OnNextTracedCount;
    if (withLogStyle)
    {
        lifetimeObserver.ProxyPassthroughOccured += OnProxyPassthroughOccured;
    }
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
            if (withLogStyle)
            {
                Console.WriteLine($"log: Variant {model!.GetType()} {model.GetHashCode()} {model.Variant}");
            }
            if(withLogStyle && changeInfoRandom.NextDouble() < changeInfoRatio)
            {
                string info = Guid.NewGuid().ToString();
                bool res = lifetimeObserver.ChangeInfo(model!, info);
                Console.WriteLine($"log: Info {model!.GetType()} {model.GetHashCode()} {info} {res}");
            }
        }
    }
    catch (BreakStreamException) { }
}

references?.Clear();
if (withLogStyle)
{
    Console.WriteLine($"log: Finished");
}
CollectGarbageAndPrintInfo();
Console.WriteLine("done");
void OnProxyPassthroughOccured(object? sender, ProxyPassthroughEventArgs args)
{
    Console.WriteLine($"log: Passed {args.Type} {args.Hash}");
}
void PrintInfo()
{
    if (!withLogStyle)
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
void OnLifetimeEventOccured(object? sender, LifetimeEventArgs args)
{
    if (withLogStyle)
    {
        if(sender is LifetimeObserver)
        {
            Console.WriteLine($"log: {(args.Kind is LifetimeEventKind.Created ? "Traced" : "Untraced")} {args.Type} {args.Hash} {args.Info}");
        }
        else
        {
            Console.WriteLine($"log: {args.Kind} {args.Type} {args.Hash}");
        }
    }
    if(sender is LifetimeObserver)
    {
        lock (locker)
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
}
void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    isRunning = false;
    e.Cancel = true;
}
void OnNextTracedCount(object? sender, EventArgs e)
{
    CollectGarbageAndPrintInfo();
}
void CollectGarbageAndPrintInfo()
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
    GC.WaitForPendingFinalizers();
    PrintInfo();
}
class BreakStreamException : Exception { }

