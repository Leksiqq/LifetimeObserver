using Net.Leksi.Util;
namespace Net.Leksi.LifetimeObserverDemo;
public class Model: IModel
{
    public static event LifetimeEventHandler? LifetimeEventOccured;
    internal static int _idGen = 0;
    private readonly Type _type;
    private readonly int _hash;
    public int Id { get; private init; }
    public int Variant { get; set; } = -1;
    public Model()
    {
        Id = Interlocked.Increment(ref _idGen);
        _type = GetType();
        _hash = GetHashCode();
        LifetimeEventOccured?.Invoke(null, new LifetimeEventArgs { Type = _type, Hash = _hash, Kind = LifetimeEventKind.Created });
    }
    ~Model()
    {
        LifetimeEventOccured?.Invoke(null, new LifetimeEventArgs { Type = _type, Hash = _hash, Kind = LifetimeEventKind.Finalized });
    }
    public static void ResetIdGen()
    {
        _idGen = 0;
    }
    public override int GetHashCode()
    {
        return Id;
    }
}
