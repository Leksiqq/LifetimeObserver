namespace Net.Leksi.Util;
internal class Tracer
{
    private readonly LifetimeObserver _lifetimeObserver;
    private readonly Type _type;
    private readonly string _info;
    private readonly int _hash;
    internal Tracer(LifetimeObserver lifetimeObserver, object obj)
    {
        _lifetimeObserver = lifetimeObserver;
        _type = obj.GetType();
        _info = obj.ToString() ?? string.Empty;
        _hash = obj.GetHashCode();
        _lifetimeObserver.ReportLifetimeEvent(LifetimeEventKind.Created, _type, _hash, _info);
    }

    ~Tracer()
    {
        _lifetimeObserver.ReportLifetimeEvent(LifetimeEventKind.Finalized, _type, _hash, _info);
    }
}