using DaJet.Data;
using DaJet.Http.Model;
using DaJet.Metadata;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using MetadataCache = DaJet.Metadata.MetadataProvider;

namespace DaJet.Http.Server
{
    [ApiController]
    [Route("md")]
    public class MetadataController : ControllerBase
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private readonly DataSourceRepository _repository;
        public MetadataController(RepositoryFactory factory)
        {
            _repository = factory.Get<DataSourceRepository>();

            if (_repository is null)
            {
                throw new InvalidOperationException("Required DataSourceRepository is not found.");
            }
        }

        #region "Управление списком баз данных и кэшем метаданных"

        [HttpGet("")]
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

        [HttpPut("")]
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

        [HttpPost("update")]
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

        #endregion

        [HttpGet("{database}")]
        public ActionResult GetConfigurations([FromRoute] string database)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            List<Configuration> configurations = provider.GetConfigurations();

            List<InfoBaseConfig> list = new(configurations.Count);

            foreach (Configuration config in configurations)
            {
                list.Add(new InfoBaseConfig()
                {
                    Uuid = config.Uuid.ToString(),
                    Name = config.Name,
                    NamePrefix = string.IsNullOrEmpty(config.NamePrefix) ? string.Empty: config.NamePrefix,
                    YearOffset = config.YearOffset,
                    AppVersion = config.AppConfigVersion,
                    PlatformVersion = config.CompatibilityVersion
                });
            }

            string json = JsonSerializer.Serialize(list, JsonOptions);

            return Content(json);
        }

        [HttpGet("{database}/{configuration}")]
        public ActionResult GetConfiguration([FromRoute] string database, [FromRoute] string configuration)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            Configuration config = (configuration == "main")
                ? provider.GetConfiguration()
                : provider.GetConfiguration(in configuration);

            if (config is null)
            {
                return NotFound();
            }

            InfoBaseConfig infoBase = new()
            {
                Uuid = config.Uuid.ToString(),
                Name = config.Name,
                NamePrefix = string.IsNullOrEmpty(config.NamePrefix) ? string.Empty : config.NamePrefix,
                YearOffset = config.YearOffset,
                AppVersion = config.AppConfigVersion,
                PlatformVersion = config.CompatibilityVersion
            };

            string json = JsonSerializer.Serialize(infoBase, JsonOptions);

            return Content(json);
        }
        
        [HttpGet("{database}/{configuration}/{type}")]
        public ActionResult GetMetadataNames([FromRoute] string database, [FromRoute] string configuration, [FromRoute] string type)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            Configuration config = (configuration == "main")
                ? provider.GetConfiguration()
                : provider.GetConfiguration(in configuration);

            List<string> names = provider.GetMetadataNames(config.Name, in type);

            string json = JsonSerializer.Serialize(names, JsonOptions);

            return Content(json);
        }

        [HttpGet("entity/{database}/{code:int}")]
        public ActionResult GetMetadataObjectByCode([FromRoute] string database, [FromRoute] int code)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            EntityDefinition entity = provider.GetMetadataObject(code);

            string json = JsonSerializer.Serialize(entity, JsonOptions);

            return Content(json);
        }

        [HttpGet("entity/{database}/{type}/{name}")]
        public ActionResult GetMetadataObjectByName([FromRoute] string database, [FromRoute] string configuration, [FromRoute] string type, [FromRoute] string name)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            EntityDefinition entity = provider.GetMetadataObject($"{type}.{name}");

            string json = JsonSerializer.Serialize(entity, JsonOptions);

            return Content(json);
        }

        [HttpPost("references/{database}")]
        public ActionResult GetPropertyDataType([FromRoute] string database, [FromBody] List<string> references)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return BadRequest($"База данных '{database}' не найдена на сервере DaJet.");
            }

            List<Guid> list = new(references.Count);

            foreach (string reference in references)
            {
                list.Add(new Guid(reference));
            }

            List<string> names = provider.ResolveReferences(list);

            string json = JsonSerializer.Serialize(names, JsonOptions);

            return Content(json);
        }
    }
}