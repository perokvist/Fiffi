namespace Fiffi;

public interface ISnapshotStore
{
    Task<T> Get<T>(string key)
            where T : class, new();
    Task Apply<T>(string key, Func<T, T> f)
            where T : class, new();
}
