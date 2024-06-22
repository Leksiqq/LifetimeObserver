namespace Net.Leksi.System;

public class CountHolder
{
    private ulong _count;
    public Type Type { get; private init; }
    public ulong Count => _count;
    internal void Increment()
    {
        Interlocked.Increment(ref _count);
    }
    internal void Decrement()
    {
        Interlocked.Decrement(ref _count);
    }
}