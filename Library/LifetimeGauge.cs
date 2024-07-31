namespace Net.Leksi.Util;
public class LifetimeGauge
{
    private Dictionary<Type, CountHolder> _counts = [];
    private readonly LifetimeObserver _lifetimeObserver;
    public LifetimeGauge(LifetimeObserver lifetimeObserver)
    {
        _lifetimeObserver = lifetimeObserver;
        lock (_counts)
        {
            foreach (Type type in _lifetimeObserver!.GetTracedTypes())
            {
                _counts.Add(type, new CountHolder());
            }
        }
        _lifetimeObserver.LifetimeEventOccured += LifetimeObserver_LifetimeEventOccured;
    }
    public IEnumerable<Type> GetTracedTypes()
    {
        return _counts.Keys;
    }
    public (int created, int released, int waterMark) GetCounts(Type type)
    {
        lock (type)
        {
            if (_counts.TryGetValue(type, out CountHolder? ch)) 
            {
                return (created: ch._incCount, released: ch._decCount, waterMark: ch._maxCount);
            }
        }
        return (0, 0, 0);
    }
    public void Stop()
    {
        if(_lifetimeObserver != null)
        {
            _lifetimeObserver.LifetimeEventOccured -= LifetimeObserver_LifetimeEventOccured;
        }
    }
    private void LifetimeObserver_LifetimeEventOccured(object? sender, LifetimeEventArgs e)
    {
        lock (e.Type)
        {
            CountHolder ch = _counts[e.Type];
            switch (e.Kind)
            {
                case LifetimeEventKind.Created:
                    ++ch._incCount;
                    if (ch._incCount - ch._decCount > ch._maxCount)
                    {
                        ch._maxCount = ch._incCount - ch._decCount;
                    }
                    break;
                case LifetimeEventKind.Finalized:
                    ++ch._decCount;
                    break;
            };
        }
    }
}
