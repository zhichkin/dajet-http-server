using System.Text.Json.Serialization;

namespace DaJet.Http.Model
{
    public class DataSourceRecord
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    }
}