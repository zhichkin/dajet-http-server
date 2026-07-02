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
            JsonOptions.Converters.Add(new EntityJsonConverter());
            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new DataObjectJsonConverter());
        }

        [HttpPost("")]
        public async Task<ActionResult> ExecuteQuery()
        {
            DataObject parameters;

            try
            {
                parameters = await HttpContext.Request.GetParametersFromBody();
            }
            catch
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, "Failed to get parameters from request body");
            }

            if (!(parameters.TryGetValue("database", out object value1) && value1 is string database))
            {
                return CreateErrorResult(HttpStatusCode.NotFound, $"Database name is not provided");
            }

            MetadataProvider provider;

            try
            {
                provider = MetadataCache.Get(in database);
            }
            catch (Exception exception)
            {
                return CreateErrorResult(HttpStatusCode.NotFound, exception.Message);
            }

            if (provider is null)
            {
                return CreateErrorResult(HttpStatusCode.NotFound, $"Database '{database}' is not found");
            }

            if (!(parameters.TryGetValue("script", out object value2) && value2 is string script))
            {
                return CreateErrorResult(HttpStatusCode.NotFound, $"Script is not provided");
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, "Script is empty");
            }

            Parser parser = new();

            if (!parser.TryParse(in script, out Script query, out string error))
            {
                return CreateErrorResult(HttpStatusCode.BadRequest, error);
            }

            if (!(parameters.TryGetValue("parameters", out object value3) && value3 is DataObject input))
            {
                return CreateErrorResult(HttpStatusCode.NotFound, $"Script parameters is not provided");
            }

            ContentResult result;

            try
            {
                Script model = AssembleQueryScript(in database, in query, in input);

                model = new ScriptBuilder().FromScript(in model).Build();

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
                Message = string.Empty,
                Result = value
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)HttpStatusCode.OK;

            return result;
        }
        private static Script AssembleQueryScript(in string database, in Script query, in DataObject input)
        {
            Script script = new();

            foreach (var parameter in input)
            {
                DeclareStatement declare = new()
                {
                    Identifier = string.Format("@{0}", parameter.Key)
                };

                object value = parameter.Value;

                declare.Type = DataType.FromType(value.GetType());

                script.Statements.Add(declare);
            }

            string outputTable = "@query_output_table";

            script.Statements.Add(new DeclareStatement()
            {
                Type = DataType.Array(),
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

                    use.Statements.Add(select);

                    break; // use only the first SELECT statement - ignore the rest
                }
            }

            script.Statements.Add(use);

            script.Statements.Add(new ReturnStatement()
            {
                Expression = new VariableReference()
                {
                    Identifier = outputTable
                }
            });

            return script;
        }
    }
}