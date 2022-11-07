namespace Fiffi;

public interface ISnapshotStore
{
    Task<T?> Get<T>(string key)
            where T : class;
    Task Apply<T>(string key, T defaultValue, Func<T, T> f)
            where T : class;
}
