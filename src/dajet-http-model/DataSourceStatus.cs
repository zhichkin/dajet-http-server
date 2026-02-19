namespace DaJet.Http.Model
{
    public sealed class DataSourceStatus
    {
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string ConnectionString { get; set; }
        public int LastUpdated { get; set; }
        public bool IsInitialized { get; set; }
    }
}