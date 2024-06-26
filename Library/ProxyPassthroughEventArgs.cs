namespace Net.Leksi.Util;
public class ProxyPassthroughEventArgs: EventArgs
{
    public Type ServiceType { get; init; } = null!;
    public Key ServiceKey { get; init; } = null!;
    public Type Type { get; init; } = null!;
    public int Hash { get; init; }
    public int Variant { get; init; } = -1;
}
