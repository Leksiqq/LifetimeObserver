namespace TestProject1;
public class LifetimeEntry
{
    public int Id { get; set; }
    public string TypeName { get; set; } = null!;
    public bool Created { get; set; }
    public bool Passed { get; set; }
    public bool Traced { get; set; }
    public bool Untraced { get; set; }
    public bool Finalized { get; set; }
    public int Variant { get; set; } = -1;
    public override string ToString()
    {
        return $"{Id}, {TypeName}, {Created}, {Passed}, {Traced}, {Untraced}, {Finalized}, {Variant}";
    }
}
