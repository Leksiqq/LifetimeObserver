using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.LifetimeObserverDemo;
using Net.Leksi.Util;
using System.Diagnostics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace TestProject1;

public class Tests
{
    private readonly bool _printServiceDescriptors = false;
    [SetUp]
    public static void Setup()
    {
        Model.ResetIdGen();
    }
    [Test]
    [TestCase(-1, 1000)]
    public void TestObserver(int seed, int requestCounts)
    {
        DirectoryInfo? dir = new(Environment.CurrentDirectory);
        while(dir is { } && !dir.EnumerateFiles().Any(f => f.Extension == ".csproj"))
        {
            dir = dir.Parent;
        }
        string? demoProjectPath = null;
        if (dir is { })
        {
            Process dotnet = new()
            {
                StartInfo = new()
                {
                    FileName = "dotnet",
                    Arguments = "list reference",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = dir.FullName,
                }
            };
            dotnet.OutputDataReceived += (s, e) =>
            {
                if (e.Data?.Contains("LifetimeObserverDemo") ?? false)
                {
                    demoProjectPath = Path.Combine(dir.FullName, e.Data.Trim());
                }
            };
            dotnet.ErrorDataReceived += (s, e) =>
            {
                Console.Error.WriteLine(e.Data);
            };
            dotnet.Start();
            dotnet.BeginErrorReadLine();
            dotnet.BeginOutputReadLine();

            dotnet.WaitForExit();
            dotnet.CancelErrorRead();
            dotnet.CancelOutputRead();

            if(demoProjectPath is { })
            {
                dotnet = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --seed {seed} --requests-count {requestCounts} --use-passed 1 --no-build",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        WorkingDirectory = demoProjectPath,
                    }
                };
                dotnet.OutputDataReceived += (s, e) =>
                {
                    Console.WriteLine(e.Data);
                };
                dotnet.ErrorDataReceived += (s, e) =>
                {
                    Console.Error.WriteLine(e.Data);
                };
                dotnet.Start();
                dotnet.BeginErrorReadLine();
                dotnet.BeginOutputReadLine();

                dotnet.WaitForExit();
                dotnet.CancelErrorRead();
                dotnet.CancelOutputRead();
            }
        }
    }
    [Test]
    [TestCase(-1)]
    public void TestSequence(int seed)
    {
        int count = 100;
        double newScopeRatio = 0.1;
        double scopeRatio = 0.5;

        List<int> expexted = [];
        Dictionary<int, CreatedAndPassedEntry> passed = [];
        int prevSeed = seed;
        List<ScriptEntry> script = AuxMethods.GetScript(ref seed, newScopeRatio, scopeRatio).Take(count).ToList();
        if(prevSeed != seed)
        {
            Console.WriteLine($"Generated seed: {seed}");
        }
        for (int i = 0; i < 2; ++i)
        {
            Setup();
            if (i == 1)
            {
                Model.LifetimeEventOccured += Model_LifetimeEventOccured;
            }
            HostApplicationBuilder builder = AuxMethods.CreateBuilder();
            if (i == 1)
            {
                AuxMethods.AddTraces(builder.Services);
            }
            using IHost host = builder.Build();
            if(i == 1)
            {
                host.Services.GetRequiredService<LifetimeObserver>().ProxyPassthroughOccured += Tests_ProxyPassthroughOccured;
            }
            FillExpectedOrAssertThatAllOk(host, script, expexted, i == 0);
            if(i == 1)
            {
                foreach(var e in passed.Where(e => e.Value.Created != e.Value.Passed))
                {
                    Console.WriteLine($"{e.Value.Type}@{e.Key}: {e.Value.Created}, {e.Value.Passed}");
                }
                //Assert.That(passed.Where(e => e.Value.Created && !e.Value.Passed), Is.Empty);
            }
        }
        void Tests_ProxyPassthroughOccured(object? sender, ProxyPassthroughEventArgs args)
        {
            if (passed.TryGetValue(args.Hash, out CreatedAndPassedEntry? cape))
            {
                cape.Passed = true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        void Model_LifetimeEventOccured(object? sender, LifetimeEventArgs args)
        {
            if (!passed.TryGetValue(args.Hash, out CreatedAndPassedEntry? cape))
            {
                cape = new CreatedAndPassedEntry { Type = args.Type, Created = true };
                passed.Add(args.Hash, cape);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
    private static void FillExpectedOrAssertThatAllOk(IHost host, List<ScriptEntry> script, List<int> expected, bool isFillingStep)
    {
        List<int> got = AuxMethods.PlayScript(host, script).Select(m => m!.Id).ToList();
        if (isFillingStep)
        {
            expected.AddRange(got);
        }
        else
        {
            Assert.That(got.ToArray(), Is.EqualTo(expected));
        }
    }
}