namespace TestProject1;

public class Model
{
    private static int _idGen = 0;
    public int Id { get; private init; }
    public Model()
    {
        Id = Interlocked.Increment(ref _idGen);
    }
}
