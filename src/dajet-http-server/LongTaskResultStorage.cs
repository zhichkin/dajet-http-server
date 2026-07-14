using DaJet.Http.Model;
using System.Collections.Concurrent;

namespace DaJet.Http.Server
{
    public sealed class LongTaskResultStorage
    {
        private readonly ConcurrentDictionary<int, QueryResponse> _store = new(-1, 64);
        public void Store(int taskId, QueryResponse result)
        {
            _ = _store.TryAdd(taskId, result);
        }
        public bool TryGetValue(int taskId, out QueryResponse result)
        {
            return _store.TryGetValue(taskId, out result);
        }
        public void Delete(int taskId)
        {
            _ = _store.TryRemove(taskId, out _);
        }
    }
}