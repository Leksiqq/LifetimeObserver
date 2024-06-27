namespace Net.Leksi.Util;
public class LifetimeObserverException: Exception
{
    public enum Kind { NoServiceRegisteredForType, CalledOutsideOfConfiguring, CalledInsideOfConfiguring,
        NotAClass,
    }
    public Type? Type { get; internal init; }
    public Kind KindOfError { get; internal init; }
    public LifetimeObserverException(string message): base(message) { }
}
