namespace DaJet.Http.Model
{
    public sealed class InfoBaseConfig
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string NamePrefix { get; set; }
        public int YearOffset { get; set; }
        public string AppVersion { get; set; }
        public int PlatformVersion { get; set; }
    }
}