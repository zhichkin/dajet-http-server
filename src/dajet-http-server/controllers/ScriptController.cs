using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Scripting;
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

        public ScriptController(RepositoryFactory factory)
        {
            JsonOptions.Converters.Add(new DictionaryJsonConverter());
        }

        [HttpPost("{**path}")]
        public ActionResult ExecuteScript([FromRoute] string path, [FromBody] Dictionary<string, JsonElement> parameters)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "scripts", path);

            if (!System.IO.File.Exists(filePath))
            {
                return CreateErrorResult(HttpStatusCode.NotFound, "Script is not found");
            }

            string source;

            using (StreamReader reader = new(filePath, Encoding.UTF8))
            {
                source = reader.ReadToEnd();
            }

            ContentResult result;

            try
            {
                Dictionary<string, object> input = GetInputFromParameters(parameters);

                Interpreter executor = new(in source);

                object value = executor.Execute(in input);

                result = CreateSuccessResult(in value);
            }
            catch (Exception exception)
            {
                //string message = ExceptionHelper.GetErrorMessageAndStackTrace(exception);
                result = CreateErrorResult(HttpStatusCode.BadRequest, exception.Message);
            }

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
        private static Dictionary<string, object> GetInputFromParameters(in Dictionary<string, JsonElement> parameters)
        {
            Dictionary<string, object> input = new();

            if (parameters is null || parameters.Count == 0)
            {
                return input;
            }

            foreach (var parameter in parameters)
            {
                string name = parameter.Key;

                JsonElement value = parameter.Value;

                if (value.ValueKind == JsonValueKind.True)
                {
                    input.Add(name, true);
                }
                else if (value.ValueKind == JsonValueKind.False)
                {
                    input.Add(name, false);
                }
                else if (value.ValueKind == JsonValueKind.Number)
                {
                    input.Add(name, value.GetDecimal());
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    string text = value.GetString();

                    if (Guid.TryParse(text, out Guid uuid))
                    {
                        input.Add(name, uuid);
                    }
                    else if (DateTime.TryParse(text, out DateTime datetime))
                    {
                        input.Add(name, datetime);
                    }
                    else if (text.StartsWith('{'))
                    {
                        if (!Entity.TryParse(text, out Entity entity))
                        {
                            throw new JsonException($"Input parameter '{name}' parse error. Incorrect value is {text}.");
                        }

                        input.Add(name, entity);
                    }
                    else
                    {
                        input.Add(name, text);
                    }
                }
            }

            return input;
        }
    }
}