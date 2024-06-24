using Net.Leksi.Util;

namespace TestProject1;

public class Model
{
    public static event LifetimeEventHandler? LifetimeEventOccured;
    internal static int _idGen = 0;
    private readonly Type _type;
    private readonly int _hash;
    public int Id { get; private init; }
    public Model()
    {
        _type = GetType();
        _hash = GetHashCode();
        Id = Interlocked.Increment(ref _idGen);
        LifetimeEventOccured?.Invoke(null, new LifetimeEventArgs { Type = _type, Hash = _hash, Kind = LifetimeEventKind.Created });
    }
    ~Model()
    {
        LifetimeEventOccured?.Invoke(null, new LifetimeEventArgs { Type = _type, Hash = _hash, Kind = LifetimeEventKind.Finalized });
    }
}
