namespace Net.Leksi.LifetimeObserverDemo;
public class ScriptEntry
{
    public int Variant { get; private init; }
    public bool CreateNewScope { get; private init; }
    public bool UseScope { get; private init; }
    public ScriptEntry(int variant, bool createNewScope, bool useScope)
    {
        Variant = variant;
        CreateNewScope = createNewScope;
        UseScope = useScope;
    }
}
