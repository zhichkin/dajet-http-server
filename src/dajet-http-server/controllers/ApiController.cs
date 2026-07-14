using DaJet.Host;
using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Scripting.Model;
using DaJet.TypeSystem;
using DaJet.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DaJet.Http.Server
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        private readonly DaJetHost _host;
        private readonly LongTaskResultStorage _storage;
        public ApiController(DaJetHost host, LongTaskResultStorage storage)
        {
            ArgumentNullException.ThrowIfNull(host, nameof(host));
            ArgumentNullException.ThrowIfNull(storage, nameof(storage));

            _host = host;
            _storage = storage;

            JsonOptions.Converters.Add(new EntityJsonConverter());
            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new DataObjectJsonConverter());
        }

        [HttpGet("monitor")]
        public ContentResult GetRunningTasks()
        {
            List<RunningTaskStatus> tasks = _host.GetRunningTasks();

            QueryResponse result = new()
            {
                Success = true,
                Message = string.Empty,
                Result = tasks,
                IsLongRunning = false
            };

            string json = JsonSerializer.Serialize(result, JsonOptions);

            ContentResult response = Content(json, "application/json", Encoding.UTF8);

            response.StatusCode = (int)HttpStatusCode.OK;

            return response;
        }

        [HttpPost("{**path}")]
        public async Task<ContentResult> Execute([FromRoute] string path)
        {
            if (!_host.TryGetOrCreate(in path, out Script script, out string error))
            {
                FileLogger.Default.Write($"[API][ERROR][{path}] {error}");
                return CreateErrorResult(HttpStatusCode.BadRequest, "Script is not found or invalid: see server log to find out more.");
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

            if (script.IsLongRunning)
            {
                Task<object> task = _host.RunAsync(in script, in input);

                _ = task.ContinueWith(StoreLongTaskResult, path);

                return CreateLongTaskResult(task.Id);
            }
            
            ContentResult result;

            try
            {
                object value = await _host.RunAsync(in script, in input);

                result = CreateSuccessResult(in value);
            }
            catch (OperationCanceledException)
            {
                result = CreateErrorResult(HttpStatusCode.BadRequest, $"Execution is canceled.");
            }
            catch (AggregateException aggregate)
            {
                Exception inner = aggregate.Flatten().InnerException;
                result = CreateErrorResult(HttpStatusCode.BadRequest, inner.Message);
            }
            catch (Exception exception)
            {
                result = CreateErrorResult(HttpStatusCode.BadRequest, exception.Message);
            }

            return result;
        }
        private ContentResult CreateLongTaskResult(int taskId)
        {
            QueryResponse response = new()
            {
                Success = true,
                Message = string.Empty,
                Result = taskId,
                IsLongRunning = true
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)HttpStatusCode.OK;

            return result;
        }
        private ContentResult CreateSuccessResult(in object value)
        {
            QueryResponse response = new()
            {
                Success = true,
                Message = string.Empty,
                Result = value,
                IsLongRunning = false
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
                Result = null,
                IsLongRunning = false
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)code;

            return result;
        }
        private void StoreLongTaskResult(Task<object> task, object state)
        {
            if (state is not string scriptPath)
            {
                return;
            }

            QueryResponse result = new()
            {
                IsLongRunning = true
            };

            if (task.IsCompletedSuccessfully)
            {
                result.Success = true;
                result.Message = string.Empty;
                result.Result = task.Result;
                
                if (result.Result is Entity entity)
                {
                    result.Result = entity.ToString();
                }
                else if (result.Result is DateTime datetime)
                {
                    result.Result = datetime.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }
            else if (task.IsCanceled)
            {
                result.Success = false;
                result.Message = $"Task [{task.Id}] is canceled.";
                result.Result = null;
            }
            else
            {
                result.Success = false;
                result.Message = $"Task [{task.Id}] is faulted: {task.Exception?.InnerException?.Message}";
                result.Result = null;
            }
            
            _storage.Store(task.Id, result);
        }

        [HttpPost("cancel/{taskId:int}")]
        public ActionResult Cancel(int taskId)
        {
            _host.Cancel(taskId);

            _storage.Delete(taskId);

            return Ok();
        }

        [HttpGet("result/pull/{taskId:int}")]
        public ContentResult PullLongTaskResult(int taskId)
        {
            QueryResponse result;

            RunningTaskStatus status = _host.GetRunningTask(taskId);

            if (status.Id != 0) // running
            {
                result = new QueryResponse()
                {
                    Success = false,
                    Message = status.Status,
                    Result = null,
                    IsLongRunning = true
                };
            }
            else if (!_storage.TryGetValue(taskId, out result))
            {
                result = new QueryResponse()
                {
                    Success = false,
                    Message = $"Data is not available",
                    Result = null,
                    IsLongRunning = true
                };
            }
            else
            {
                _storage.Delete(taskId);
            }

            string json = JsonSerializer.Serialize(result, JsonOptions);

            ContentResult response = Content(json, "application/json", Encoding.UTF8);

            response.StatusCode = (int)HttpStatusCode.OK;

            return response;
        }

        [HttpGet("result/push/{taskId:int}")]
        public ServerSentEventsResult<QueryResponse> PushLongTaskResult(int taskId)
        {
            return TypedResults.ServerSentEvents(GetLongTaskResult(taskId));
        }
        private async IAsyncEnumerable<SseItem<QueryResponse>> GetLongTaskResult(int taskId)
        {
            while (_host.GetRunningTask(taskId).Id > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (!_storage.TryGetValue(taskId, out QueryResponse result))
            {
                result = new QueryResponse()
                {
                    Success = true,
                    Message = string.Empty,
                    Result = null,
                    IsLongRunning = true
                };
            }
            else
            {
                _storage.Delete(taskId);
            }

            //string data = JsonSerializer.Serialize(result, JsonOptions);

            yield return new SseItem<QueryResponse>(result, "long-task-result")
            {
                EventId = taskId.ToString()
            };
        }
    }
}

//Response.StatusCode = 200;
//Response.ContentType = "text/event-stream";
//Response.Headers.Append("Connection", "keep-alive");
//Response.Headers.Append("Cache-Control", "no-cache");

//if (!HttpContext.RequestAborted.IsCancellationRequested)
//{
//    if (!_store.TryGetValue(taskId, out QueryResponse result))
//    {
//        result = new QueryResponse()
//        {
//            Success = true,
//            Message = $"Data is not available",
//            Result = null,
//            IsLongRunning = true
//        };
//    }

//    string data = JsonSerializer.Serialize(result, JsonOptions);

//    try
//    {
//        await Response.WriteAsync($"event: long-task-result\n");
//        await Response.WriteAsync($"id: {taskId}\n");
//        await Response.WriteAsync($"data: {data}\n\n");

//        await Response.Body.FlushAsync();

//        _store.Delete(taskId);
//    }
//    catch (Exception error)
//    {
//        string message = ExceptionHelper.GetErrorMessage(error);

//        FileLogger.Default.Write($"[SSE] Task [{taskId}] {message}");
//    }
//}