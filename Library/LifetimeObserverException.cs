namespace Net.Leksi.Util;
public class LifetimeObserverException: Exception
{
    public enum Kind { NoServiceRegisteredForType, CalledOutsideOfConfiguring, CalledBeforeHostIsBuilt, }
    public Type? Type { get; internal init; }
    public Kind KindOfError { get; internal init; }
    public LifetimeObserverException(string message): base(message) { }
}
