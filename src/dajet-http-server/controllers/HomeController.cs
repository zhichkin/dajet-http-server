using DaJet.Data;
using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MetadataCache = DaJet.Metadata.MetadataProvider;

namespace DaJet.Http.Server
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private readonly DataSourceRepository _repository;
        public HomeController(RepositoryFactory factory)
        {
            _repository = factory.Get<DataSourceRepository>();

            if (_repository is null)
            {
                throw new InvalidOperationException("Required DataSourceRepository is not found.");
            }

            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [HttpGet("status")]
        public ActionResult GetServerStatus()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            AssemblyName name = assembly.GetName();

            Version version = name.Version;

            DaJetServerStatus status = new()
            {
                Name = name.Name,
                Version = version is null
                ? string.Empty
                : $"{version.Major}.{version.Minor}.{version.Build}",
                ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonSerializer.Serialize(status, JsonOptions);

            return Content(json);
        }

        [HttpGet()]
        public ActionResult GetDataSources()
        {
            List<MetadataProviderStatus> providers = MetadataCache.ToList();

            List<DataSourceStatus> list = new(providers.Count);

            foreach (MetadataProviderStatus provider in providers)
            {
                list.Add(new DataSourceStatus()
                {
                    Name = provider.Name,
                    DataSource = provider.DataSource.ToString(),
                    ConnectionString = provider.ConnectionString,
                    LastUpdated = provider.LastUpdated,
                    IsInitialized = provider.IsInitialized
                });
            }

            string json = JsonSerializer.Serialize(list, JsonOptions);

            return Content(json);
        }

        [HttpPut()]
        public ActionResult CreateDataSource([FromBody] DataSourceRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.Name) ||
                string.IsNullOrWhiteSpace(record.Type) ||
                string.IsNullOrWhiteSpace(record.Path))
            {
                return BadRequest("Неверно указаны параметры."); // 400
            }

            if (!Enum.TryParse(record.Type, out DataSourceType dataSource))
            {
                return BadRequest($"Неверно указано значение '{record.Type}' свойства \"Type\".");
            }

            try
            {
                if (!MetadataCache.TryAdd(record.Name, dataSource, record.Path))
                {
                    return Ok(); // База данных уже существует на сервере
                }
            }
            catch (Exception exception)
            {
                return BadRequest($"Регистрация '{record.Name}' невозможна по причине: {exception.Message}.");
            }

            if (!_repository.TrySave(in record, out string error))
            {
                return BadRequest(error);
            }

            return Created();
        }

        [HttpPost("reset/{name}")]
        public ActionResult ResetDataSource([FromRoute] string name)
        {
            try
            {
                MetadataCache.Reset(in name);
            }
            catch (Exception error)
            {
                return BadRequest($"Обновление кэша '{name}' невозможно по причине: {error.Message}.");
            }

            return Ok();
        }

        [HttpPatch()]
        public ActionResult UpdateDataSource([FromBody] DataSourceRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.Name) ||
                string.IsNullOrWhiteSpace(record.Type) ||
                string.IsNullOrWhiteSpace(record.Path))
            {
                return BadRequest("Неверно указаны параметры.");
            }

            if (!Enum.TryParse(record.Type, out DataSourceType dataSource))
            {
                return BadRequest($"Неверно указано значение '{record.Type}' свойства \"Type\".");
            }

            try
            {
                if (!MetadataCache.TryUpdate(record.Name, dataSource, record.Path))
                {
                    return NotFound();
                }
            }
            catch (Exception exception)
            {
                return BadRequest($"Регистрация '{record.Name}' невозможна по причине: {exception.Message}.");
            }

            if (!_repository.TrySave(in record, out string error))
            {
                return BadRequest(error);
            }

            return Ok();
        }

        [HttpDelete("{name}")]
        public ActionResult DeleteDataSource([FromRoute] string name)
        {
            MetadataCache.Remove(in name);

            if (!_repository.TryDelete(in name, out string error))
            {
                return BadRequest(error); // 400
            }

            return Ok();
        }
    }
}