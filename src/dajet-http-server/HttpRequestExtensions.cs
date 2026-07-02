using DaJet.Json;
using DaJet.TypeSystem;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DaJet.Http.Server
{
    internal static class HttpRequestExtensions
    {
        internal static async Task<DataObject> GetParametersFromBody(this HttpRequest request)
        {
            if (request.ContentLength == 0)
            {
                return new DataObject();
            }

            JsonSerializerOptions options = new()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                Converters = { new DataObjectJsonConverter() }
            };

            return await JsonSerializer.DeserializeAsync<DataObject>(request.Body, options);
        }
    }
}