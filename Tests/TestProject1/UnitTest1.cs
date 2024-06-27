using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.LifetimeObserverDemo;
using Net.Leksi.Util;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Web;
namespace TestProject1;
public class Tests
{
    [SetUp]
    public static void Setup()
    {
        Model.ResetIdGen();
    }
    [Test]
    [TestCase(-1, 1000, 10)]
    [TestCase(-1, 1000000, 1000)]
    public void TestObserver(int seed, int requestCounts, int referencedCount)
    {
        string? demoProjectPath = null;
        StringBuilder sbErr = new();
        Dictionary<int, LifetimeEntry> lifetimeEntries = [];
        int tracedCount = 0;
        bool isRunning = true;
        AssemblyConfigurationAttribute? assemblyConfigurationAttribute = GetType().Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        string? buildConfigurationName = assemblyConfigurationAttribute?.Configuration;

        Console.WriteLine(buildConfigurationName);

        DirectoryInfo? dir = new(Environment.CurrentDirectory);

        while (dir is { } && !dir.EnumerateFiles().Any(f => f.Extension == ".csproj"))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null);

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
            if (demoProjectPath is null && (e.Data?.Contains("LifetimeObserverDemo.csproj") ?? false))
            {
                Uri uri = new($"{dir.FullName}/_");
                string relPath = e.Data.Trim().Replace(Path.DirectorySeparatorChar, '/');
                Uri demoProjectPathUri = new(uri, relPath);
                demoProjectPath = Path.GetDirectoryName(HttpUtility.UrlDecode(demoProjectPathUri.AbsolutePath));
            }
        };
        dotnet.ErrorDataReceived += OnErrorData;
        dotnet.Start();
        dotnet.BeginErrorReadLine();
        dotnet.BeginOutputReadLine();
        dotnet.WaitForExit();
        dotnet.CancelErrorRead();
        dotnet.CancelOutputRead();

        string error = sbErr.ToString().Trim();
        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrEmpty(error), error);
            Assert.That(demoProjectPath, Is.Not.Null);
        });

        dotnet = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --seed {seed} --requests-count {requestCounts} --referenced-count {referencedCount} --with-log-style 1 --no-build {(string.IsNullOrEmpty(buildConfigurationName) ? string.Empty : $"-c {buildConfigurationName}")}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = demoProjectPath,
            }
        };

        sbErr.Clear();
        dotnet.ErrorDataReceived += OnErrorData;
        dotnet.Start();
        dotnet.BeginErrorReadLine();
        while (!dotnet.StandardOutput.EndOfStream)
        {
            if(dotnet.StandardOutput.ReadLine() is string line)
            {
                OnLogEntry(line);
            }
        }
        dotnet.WaitForExit();
        dotnet.CancelErrorRead();
        error = sbErr.ToString().Trim();
        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrEmpty(error), error);
            Assert.That(lifetimeEntries.Values.Where(v => !v.Created || !v.Passed || !v.Traced), Is.Empty);
            Assert.That(lifetimeEntries.Values.Where(v => v.Untraced != v.Finalized), Is.Empty);
        });
        Assert.That(lifetimeEntries, Has.Count.LessThanOrEqualTo(AuxMethods.s_ModelRequestsCount));

        Console.WriteLine(lifetimeEntries.Count);

        void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is { })
            {
                sbErr.AppendLine(e.Data.Trim());
            }
            else
            {
                sbErr.AppendLine();
            }
        }

        void OnLogEntry(string logEntry)
        {
            if (logEntry.StartsWith("log:"))
            {
                //Console.WriteLine(logEntry);
                string[] parts = logEntry["log:".Length..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                LifetimeEntry? lifetimeEntry = null;
                switch (parts[0])
                {
                    case "referencedCount":
                        Assert.That(parts.Length == 2 && int.TryParse(parts[1], out int val) && val == referencedCount);
                        break;
                    case "requestsCount":
                        Assert.That(parts.Length == 2 && int.TryParse(parts[1], out int val1) && val1 == requestCounts);
                        break;
                    case "seed":
                        Assert.That(
                            (
                                parts.Length >= 2
                                && int.TryParse(parts[1], out int val2)
                            )
                            && (
                                (parts.Length == 2 && val2 == seed)
                                || (parts.Length == 3 && seed == -1 && parts[2] == "generated")
                            )
                        );
                        if (seed == -1)
                        {
                            Console.WriteLine($"Generated seed: {int.Parse(parts[1])}");
                        }
                        break;
                    case "Created":
                        Assert.That(parts, Has.Length.EqualTo(3));
                        if (int.TryParse(parts[2], out int id))
                        {
                            if (lifetimeEntries.ContainsKey(id))
                            {
                                Assert.Fail($"Invalid lifetime order: {logEntry}.");
                            }
                            lifetimeEntry = new LifetimeEntry
                            {
                                Id = id,
                                TypeName = parts[1],
                                Created = true,
                            };
                            lifetimeEntries.Add(id, lifetimeEntry);
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Passed":
                        Assert.That(parts, Has.Length.EqualTo(3));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(entry.Passed, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry.Traced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry.Untraced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry.Finalized, Is.False, $"Invalid lifetime order: {logEntry}.");
                            });
                            lifetimeEntry = entry;
                            lifetimeEntry.Passed = true;
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Traced":
                        Assert.That(parts, Has.Length.EqualTo(4));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry1)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(entry1.Traced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry1.Untraced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry1.Finalized, Is.False, $"Invalid lifetime order: {logEntry}.");
                            });
                            lifetimeEntry = entry1;
                            lifetimeEntry.Traced = true;
                            lifetimeEntry.Info = parts[3];
                            lifetimeEntry.NewInfo = parts[3];

                            ++tracedCount;
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Untraced":
                        Assert.That(parts, Has.Length.EqualTo(4));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry2)
                        {
                            Assert.That(entry2.Untraced, Is.False, $"Invalid lifetime order: {logEntry}.");
                            lifetimeEntry = entry2;
                            lifetimeEntry.Untraced = true;
                            Assert.That(lifetimeEntry.NewInfo, Is.EqualTo(parts[3]));
                            --tracedCount;
                            Assert.That(!isRunning || tracedCount >= referencedCount);
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Finalized":
                        Assert.That(parts, Has.Length.EqualTo(3));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry3)
                        {
                            Assert.That(entry3.Finalized, Is.False, $"Invalid lifetime order: {logEntry}.");
                            lifetimeEntry = entry3;
                            lifetimeEntry.Finalized = true;
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Variant":
                        Assert.That(parts, Has.Length.EqualTo(4));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry4 && int.TryParse(parts[3], out int variant))
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(entry4.Passed, Is.True, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry4.Traced, Is.True, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry4.Untraced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry4.Finalized, Is.False, $"Invalid lifetime order: {logEntry}.");
                            });
                            lifetimeEntry = entry4;
                            lifetimeEntry.Variant = variant;
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                    case "Finished":
                        isRunning = false;
                        break;
                    case "Info":
                        Assert.That(parts, Has.Length.AtLeast(4));
                        if (GetLifetimeEntry(parts[1], parts[2]) is LifetimeEntry entry5)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(entry5.Passed, Is.True, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry5.Traced, Is.True, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry5.Untraced, Is.False, $"Invalid lifetime order: {logEntry}.");
                                Assert.That(entry5.Finalized, Is.False, $"Invalid lifetime order: {logEntry}.");
                            });
                            lifetimeEntry = entry5;
                            lifetimeEntry.NewInfo = parts[3];
                        }
                        else
                        {
                            Assert.Fail($"Invalid log entry: {logEntry}.");
                        }
                        break;
                }
                if (lifetimeEntry is { })
                {
                    if (
                        lifetimeEntry.Created
                        && lifetimeEntry.Passed
                        && lifetimeEntry.Traced
                        && lifetimeEntry.Untraced
                        && lifetimeEntry.Finalized
                        && lifetimeEntry.Variant >= 0
                    )
                    {
                        lifetimeEntries.Remove(lifetimeEntry.Id);
                    }
                }
            }

        }
        LifetimeEntry? GetLifetimeEntry(string typeName, string idString)
        {
            if (int.TryParse(idString, out int id) && lifetimeEntries.TryGetValue(id, out LifetimeEntry? entry) && entry.TypeName == typeName)
            {
                return entry;
            }
            return null;
        }
    }
    [Test]
    [TestCase(-1, 1000000)]
    public void TestSequence(int seed, int count)
    {
        double newScopeRatio = 0.1;
        double scopeRatio = 0.5;

        List<int> expexted = [];
        Dictionary<int, LifetimeEntry> passed = [];
        int prevSeed = seed;
        List<ScriptEntry> script = AuxMethods.GetScript(ref seed, newScopeRatio, scopeRatio).Take(count).ToList();
        if (prevSeed != seed)
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
            if (i == 1)
            {
                host.Services.GetRequiredService<LifetimeObserver>().ProxyPassthroughOccured += Tests_ProxyPassthroughOccured;
            }
            FillExpectedOrAssertThatAllOk(host, script, expexted, i == 0);
            if (i == 1)
            {
                Assert.That(passed.Where(e => e.Value.Created != e.Value.Passed), Is.Empty);
            }
        }
        void Tests_ProxyPassthroughOccured(object? sender, ProxyPassthroughEventArgs args)
        {
            lock (passed)
            {
                if (passed.TryGetValue(args.Hash, out LifetimeEntry? cape))
                {
                    cape.Passed = true;
                }
                else
                {
                    cape = new LifetimeEntry { TypeName = args.Type.FullName!, Passed = true };
                    passed.Add(args.Hash, cape);
                }
            }
        }
        void Model_LifetimeEventOccured(object? sender, LifetimeEventArgs args)
        {
            lock (passed)
            {
                if (!passed.TryGetValue(args.Hash, out LifetimeEntry? cape))
                {
                    cape = new LifetimeEntry { TypeName = args.Type.FullName!, Created = true };
                    passed.Add(args.Hash, cape);
                }
                else
                {
                    cape.Created = true;
                }
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