using DaJet.Http.Model;
using DaJet.Json;
using DaJet.TypeSystem;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace DaJet.Http.Client
{
    public sealed class DaJetHttpClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        static DaJetHttpClient()
        {
            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new DataObjectJsonConverter());
            JsonOptions.Converters.Add(new DictionaryJsonConverter());
            JsonOptions.Converters.Add(new JsonStringEnumConverter<ColumnPurpose>());
            JsonOptions.Converters.Add(new JsonStringEnumConverter<PropertyPurpose>());
        }

        private readonly HttpClient _client;
        public DaJetHttpClient(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<DaJetServerStatus> GetServerStatus()
        {
            string url = "/status";

            HttpResponseMessage response = await _client.GetAsync(url);

            return await response.Content.ReadFromJsonAsync<DaJetServerStatus>();
        }
        public async Task<List<DataSourceStatus>> GetDataSources()
        {
            string url = "/";

            HttpResponseMessage response = await _client.GetAsync(url);

            return await response.Content.ReadFromJsonAsync<List<DataSourceStatus>>();
        }
        public async Task<RequestResult> CreateDataSource(DataSourceRecord record)
        {
            string url = "/";

            string json = JsonSerializer.Serialize(record, JsonOptions);

            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PutAsync(url, content);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return new RequestResult(true, "База данных зарегистрирована успешно");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                return new RequestResult(true, "База данных уже зарегистрирована ранее");
            }

            string message = await response.Content.ReadAsStringAsync();

            return new RequestResult(false, in message);
        }
        public async Task<RequestResult> UpdateDataSource(DataSourceRecord record)
        {
            string url = "/";

            string json = JsonSerializer.Serialize(record, JsonOptions);

            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PatchAsync(url, content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new RequestResult(true);
            }

            string message = await response.Content.ReadAsStringAsync();

            return new RequestResult(false, in message);
        }
        public async Task<RequestResult> ResetDataSource(string name)
        {
            string url = $"/reset/{name}";

            StringContent content = new(string.Empty, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return new RequestResult(true);
            }

            string message = await response.Content.ReadAsStringAsync();

            return new RequestResult(false, in message);
        }
        public async Task<RequestResult> DeleteDataSource(string name)
        {
            string url = $"/{name}";

            HttpResponseMessage response = await _client.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return new RequestResult(true);
            }

            string message = await response.Content.ReadAsStringAsync();

            return new RequestResult(false, in message);
        }

        public async Task<string> CompareMetadataAndDatabaseSchema(string infobase)
        {
            string url = $"/md/compare/{infobase}";

            HttpResponseMessage response = await _client.GetAsync(url);
            
            return await response.Content.ReadAsStringAsync();
        }
    }
}