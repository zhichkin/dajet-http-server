using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Scripting;
using DaJet.Scripting.Model;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DaJet.Http.Server
{
    [ApiController]
    [Route("script")]
    public class ScriptController : ControllerBase
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        public ScriptController()
        {
            JsonOptions.Converters.Add(new EntityJsonConverter());
            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new DataObjectJsonConverter());
        }

        [HttpPost("{**path}")]
        public async Task<ContentResult> ExecuteScript([FromRoute] string path)
        {
            string rootPath = Path.Combine(AppContext.BaseDirectory, "scripts");
            
            string fullPath = Path.GetFullPath(rootPath);

            string filePath = Path.GetFullPath(Path.Combine(rootPath, path));

            if (!filePath.StartsWith(fullPath))
            {
                return CreateErrorResult(HttpStatusCode.Forbidden, "Access denied");
            }

            DataObject input;

            try
            {
                input = await HttpContext.Request.GetParametersFromBody();
            }
            catch
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, "Failed to get parameters from request body");
            }

            ContentResult result;

            try
            {
                Script script = new ScriptBuilder().FromFile(in filePath).Build();

                Interpreter executor = new(in script);

                object value = executor.Execute(in input);

                result = CreateSuccessResult(in value);
            }
            catch (Exception exception)
            {
                result = CreateErrorResult(HttpStatusCode.BadRequest, exception.Message);
            }

            return result;
        }
        private ContentResult CreateSuccessResult(in object value)
        {
            QueryResponse response = new()
            {
                Success = true,
                Message = string.Empty,
                Result = value
            };

            if (value is Entity entity)
            {
                response.Result = entity.ToString();
            }
            else if (value is DateTime datetime)
            {
                response.Result = datetime.ToString("yyyy-MM-ddTHH:mm:ss");
            }

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)HttpStatusCode.OK;

            return result;
        }
        private ContentResult CreateErrorResult(HttpStatusCode code, string message)
        {
            QueryResponse response = new()
            {
                Success = false,
                Message = message,
                Result = null
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)code;

            return result;
        }
    }
}