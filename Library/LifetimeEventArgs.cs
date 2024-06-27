namespace Net.Leksi.Util;

public class LifetimeEventArgs: EventArgs
{
    public LifetimeEventKind Kind { get; init; }
    public Type Type { get; init; } = null!;
    public object? Info { get; init; }
    public int Hash { get; init; }
}
