namespace Net.Leksi.Util;

public class LifetimeEventArgs: EventArgs
{
    public LifetimeEventKind Kind { get; init; }
    public Type Type { get; init; } = null!;
    public string Info { get; init; } = null!;
    public int Hash { get; init; }
}
