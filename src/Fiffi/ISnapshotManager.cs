using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public interface ISnapshotManager
    {
        Task<T> Get<T>(string key)
                where T : class, new();
        Task Apply<T>(string key, Func<T, T> f)
                where T : class, new();
    }
}