using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Scripting.Model;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using MetadataCache = DaJet.Metadata.MetadataProvider;

namespace DaJet.Http.Server
{
    [ApiController]
    [Route("query")]
    public class QueryController : ControllerBase
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public QueryController()
        {
            JsonOptions.Converters.Add(new DictionaryJsonConverter());
        }

        [HttpPost("")]
        public ActionResult ExecuteQuery([FromBody] QueryRequest request)
        {
            MetadataProvider provider;

            try
            {
                provider = MetadataCache.Get(request.Database);
            }
            catch (Exception exception)
            {
                return CreateErrorResult(HttpStatusCode.NotFound, exception.Message);
            }

            if (provider is null)
            {
                return CreateErrorResult(HttpStatusCode.NotFound, $"Database '{request.Database}' is not found");
            }

            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, "Script is empty");
            }

            Parser parser = new();

            if (!parser.TryParse(request.Script, out Script query, out string error))
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, error);
            }

            ContentResult result;

            try
            {
                Dictionary<string, object> input = GetInputFromParameters(request.Parameters);

                Script model = AssembleQueryScript(request.Database, in query, in input);

                Interpreter executor = new(in model);

                object value = executor.Execute(in input);

                result = CreateSuccessResult(in value);
            }
            catch (Exception exception)
            {
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
                Message = string.Empty
            };

            if (value is null)
            {
                response.Result = "{}";
            }
            else if (value is string text)
            {
                response.Result = text;
            }
            else
            {
                response.Result = JsonSerializer.Serialize(value, value.GetType(), JsonOptions);
            }

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)HttpStatusCode.OK;

            return result;
        }
        private static Dictionary<string, object> GetInputFromParameters(in Dictionary<string, object> parameters)
        {
            Dictionary<string, object> input = new();

            if (parameters is null || parameters.Count == 0)
            {
                return input;
            }

            foreach (var parameter in parameters)
            {
                if (parameter.Value is not JsonElement value)
                {
                    break;
                }

                string name = parameter.Key;

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
        private static Script AssembleQueryScript(in string database, in Script query, in Dictionary<string, object> parameters)
        {
            Script script = new();

            foreach (var parameter in parameters)
            {
                DeclareStatement declare = new()
                {
                    Identifier = string.Format("@{0}", parameter.Key)
                };

                object value = parameter.Value;

                if (value is bool) { declare.Type = DataType.Boolean; }
                else if (value is decimal) { declare.Type = DataType.Decimal(); }
                else if (value is DateTime) { declare.Type = DataType.DateTime; }
                else if (value is string) { declare.Type = DataType.String(); }
                else if (value is Guid) { declare.Type = DataType.Uuid(); }
                else if (value is Entity) { declare.Type = DataType.Entity(); }
                else
                {
                    continue; // Unsupported parameter type
                }

                script.Statements.Add(declare);
            }

            string outputTable = "@query_output_table";

            script.Statements.Add(new DeclareStatement()
            {
                Type = DataType.Array,
                Identifier = outputTable
            });

            UseStatement use = new() { Source = database };

            foreach (SyntaxNode statement in query.Statements)
            {
                if (statement is SelectStatement select)
                {
                    if (select.Expression is SelectExpression expression)
                    {
                        expression.Into = new IntoClause()
                        {
                            Value = new VariableReference()
                            {
                                Identifier = outputTable
                            }
                        };
                    }

                    use.Statements.Statements.Add(select);

                    break; // use only the first SELECT statement - ignore the rest
                }
            }

            script.Statements.Add(use);

            script.Statements.Add(new ReturnStatement()
            {
                Expression = new FunctionExpression()
                {
                    Token = Token.UDF,
                    Name = "JSON",
                    Parameters =
                    [
                        new VariableReference()
                        {
                            Identifier = outputTable
                        }
                    ]
                }
            });

            return script;
        }
    }
}