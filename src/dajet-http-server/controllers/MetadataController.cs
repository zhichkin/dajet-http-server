using DaJet.Data;
using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Metadata;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [HttpGet()]
        public ActionResult GetDatabases()
        {
            List<MetadataProviderStatus> providers = MetadataCache.ToList();

            List<string> databases = new(providers.Count);

            foreach (MetadataProviderStatus provider in providers)
            {
                if (provider.DataSource == DataSourceType.SqlServer ||
                    provider.DataSource == DataSourceType.PostgreSql)
                {
                    databases.Add(provider.Name);
                }
            }

            string json = JsonSerializer.Serialize(databases, JsonOptions);

            return Content(json);
        }

        [HttpGet("{database}")]
        public ActionResult GetConfigurations([FromRoute] string database)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
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

        [HttpGet("names/{database}/{type}")]
        public ActionResult GetConfiguration([FromRoute] string database, [FromRoute] string type)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
            }

            List<string> names = provider.GetMetadataNames(in type);

            string json = JsonSerializer.Serialize(names, JsonOptions);

            return Content(json);
        }
        
        [HttpGet("names/{database}/{configuration}/{type}")]
        public ActionResult GetMetadataNames([FromRoute] string database, [FromRoute] string configuration, [FromRoute] string type)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
            }

            Configuration config = (configuration == "main")
                ? provider.GetConfiguration()
                : provider.GetConfiguration(in configuration);

            if (config is null)
            {
                return NotFound($"Конфигурация '{configuration}' не найдена в базе данных '{database}'.");
            }

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
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
            }

            EntityDefinition entity = provider.GetMetadataObject(code);

            if (entity is null)
            {
                return NotFound($"Объект метаданных '{code}' не найден в базе данных '{database}'.");
            }

            string json = JsonSerializer.Serialize(entity, JsonOptions);

            return Content(json);
        }

        [HttpGet("entity/{database}/{type}/{name}")]
        public ActionResult GetMetadataObjectByName([FromRoute] string database, [FromRoute] string configuration, [FromRoute] string type, [FromRoute] string name)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
            }

            EntityDefinition entity = provider.GetMetadataObject($"{type}.{name}");

            if (entity is null)
            {
                return NotFound($"Объект метаданных '{type}.{name}' не найден в базе данных '{database}'.");
            }

            string json = JsonSerializer.Serialize(entity, JsonOptions);

            return Content(json);
        }

        [HttpGet("references/{database}")]
        public ActionResult GetPropertyDataType([FromRoute] string database, [FromBody] List<string> references)
        {
            MetadataProvider provider = MetadataCache.Get(in database);

            if (provider is null)
            {
                return NotFound($"База данных '{database}' не найдена на сервере DaJet.");
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