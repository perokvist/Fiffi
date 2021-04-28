using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public interface ISnapshotStore
    {
        Task<Maybe<T>> Get<T>(string key);
        Task Apply<T>(string key, Func<T, T> f);
    }
}