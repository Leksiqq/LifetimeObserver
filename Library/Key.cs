namespace Net.Leksi.Util;
public class Key
{
    internal object? SourceKey { get; private init; }
    internal Key(object? sourceKey = null)
    {
        SourceKey = sourceKey;
    }
    public override string ToString()
    {
        return $"{GetType().Name}({SourceKey})";
    }
}
