namespace Net.Leksi.Util;
internal class Tracer
{
    private readonly LifetimeObserver _lifetimeObserver;
    internal Type Type { get; private init; }
    internal string Info { get; private set; }
    internal int Hash { get; private set; }
    internal Tracer(LifetimeObserver lifetimeObserver, object obj)
    {
        _lifetimeObserver = lifetimeObserver;
        Type = obj.GetType();
        Info = obj.ToString() ?? string.Empty;
        Hash = obj.GetHashCode();
        _lifetimeObserver.ReportLifetimeEvent(LifetimeEventKind.Created, Type, Hash, Info);
    }

    ~Tracer()
    {
        _lifetimeObserver.ReportLifetimeEvent(LifetimeEventKind.Finalized, Type, Hash, Info);
    }
}