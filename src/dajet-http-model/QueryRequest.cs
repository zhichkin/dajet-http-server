using System.Text.Json.Serialization;

namespace DaJet.Http.Model
{
    public sealed class QueryRequest
    {
        [JsonPropertyName("database")]
        public string Database { get; set; }
        
        [JsonPropertyName("script")]
        public string Script { get; set; }
        
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }
}