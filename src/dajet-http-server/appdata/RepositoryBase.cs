namespace DaJet.Http.Server
{
    public abstract class RepositoryBase { }
    public abstract class RepositoryBase<TEntity> : RepositoryBase
    {
        public abstract bool TryGet(out List<TEntity> list, out string error);
        public abstract bool TryGet(in string key, out TEntity entity, out string error);
        public abstract bool TrySave(in TEntity entity, out string error);
        public abstract bool TryDelete(in string key, out string error);
    }
}