namespace DaJet.Http.Server
{
    public sealed class RepositoryFactory
    {
        private readonly Dictionary<Type, RepositoryBase> _repositories = new(1);
        public RepositoryFactory(in string connectionString)
        {
            _repositories.Add(typeof(DataSourceRepository), new DataSourceRepository(in connectionString));
        }
        public TRepository Get<TRepository>() where TRepository : RepositoryBase
        {
            if (!_repositories.TryGetValue(typeof(TRepository), out RepositoryBase repository))
            {
                return default;
            }

            return repository as TRepository;
        }
    }
}