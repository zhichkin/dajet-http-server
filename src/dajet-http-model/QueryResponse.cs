using System.Text.Json.Serialization;

namespace DaJet.Http.Model
{
    public sealed class QueryResponse
    {
        [JsonPropertyName("async")]
        public bool IsLongRunning { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public object Result { get; set; }
    }
}