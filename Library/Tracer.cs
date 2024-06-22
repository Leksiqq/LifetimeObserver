namespace Net.Leksi.System;
internal class Tracer
{
    private readonly LifetimeObserver _lifetimeObserver;
    internal Type Type { get; private init; }
    internal Tracer(LifetimeObserver lifetimeObserver, Type type)
    {
        _lifetimeObserver = lifetimeObserver;
        Type = type;
    }

    ~Tracer() 
    {
        _lifetimeObserver.ReportFinalized(Type);
    }
}