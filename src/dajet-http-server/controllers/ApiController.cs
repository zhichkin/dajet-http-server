using DaJet.Host;
using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Scripting.Model;
using DaJet.TypeSystem;
using DaJet.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Channels;

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
        private readonly Channel<QueryResponse> _events;
        public ApiController(DaJetHost host)
        {
            _host = host;
            _events = Channel.CreateBounded<QueryResponse>(new BoundedChannelOptions(64)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            JsonOptions.Converters.Add(new EntityJsonConverter());
            JsonOptions.Converters.Add(new DataTypeJsonConverter());
            JsonOptions.Converters.Add(new DataObjectJsonConverter());
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

                _ = task.ContinueWith(SendLongTaskResponse, path);

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

        private void SendLongTaskResponse(Task<object> task, object state)
        {
            if (state is not string scriptPath)
            {
                return;
            }

            QueryResponse response = new()
            {
                Success = false,
                Message = "message",
                Result = null
            };

            if (task.IsCompletedSuccessfully)
            {
                object value = task.Result;

                if (value is not null)
                {
                    string json = JsonSerializer.Serialize(value, value.GetType(), JsonOptions);

                    Console.WriteLine($"Task [{task.Id}] return value:");
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine($"Task [{task.Id}] returned null value.");
                }
            }
            else if (task.IsCanceled)
            {
                Console.WriteLine($"Task [{task.Id}] is canceled.");
            }
            else
            {
                Console.WriteLine($"Task [{task.Id}] is faulted: {task.Exception?.InnerException?.Message}");
            }

            _ = _events.Writer.TryWrite(response);
        }

        [HttpGet]
        [Route("sse")]
        public async Task ServerSentEvents()
        {
            //1. Set content type
            Response.StatusCode = 200;
            Response.ContentType = "text/event-stream";

            StreamWriter streamWriter = new(Response.Body);

            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                //2. Await something that generates messages
                await Task.Delay(5000, HttpContext.RequestAborted);

                //3. Write to the Response.Body stream
                await streamWriter.WriteLineAsync($"{DateTime.Now} Looping");
                await streamWriter.FlushAsync();

            }
        }

        //private async Task ObserveScriptCatalog()
        //{
        //    while (!_cancellationToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            await ProcessScriptCatalogEvents();
        //        }
        //        catch (Exception error)
        //        {
        //            FileLogger.Default.Write(ExceptionHelper.GetErrorMessageAndStackTrace(error));
        //        }

        //        try
        //        {
        //            FileLogger.Default.Write($"Script catalog watcher delay 30 seconds ...");

        //            await Task.Delay(TimeSpan.FromSeconds(30), _cancellationToken);
        //        }
        //        catch // (OperationCanceledException)
        //        {
        //            // do nothing - host shutdown requested
        //        }
        //    }
        //}

        //private async ValueTask ProcessScriptCatalogEvents()
        //{
        //    FileLogger.Default.Write("Waiting for file system events ...");

        //    while (await _events.Reader.WaitToReadAsync(_cancellationToken))
        //    {
        //        FileLogger.Default.Write("Processing file system events ...");

        //        while (_events.Reader.TryRead(out FileSystemEventArgs _event))
        //        {
        //            if (_cancellationToken.IsCancellationRequested)
        //            {
        //                return; // Сервис остановлен принудительно
        //            }

        //            if (_event.ChangeType == WatcherChangeTypes.Created)
        //            {
        //                FileLogger.Default.Write($"Created: {_event.FullPath}");
        //            }
        //            else if (_event.ChangeType == WatcherChangeTypes.Changed)
        //            {
        //                FileLogger.Default.Write($"Changed: {_event.FullPath}");
        //            }
        //            else if (_event.ChangeType == WatcherChangeTypes.Deleted)
        //            {
        //                FileLogger.Default.Write($"Deleted: {_event.FullPath}");
        //            }
        //            else if (_event.ChangeType == WatcherChangeTypes.Renamed)
        //            {
        //                FileLogger.Default.Write($"Renamed: {_event.FullPath}");
        //            }

        //            FileLogger.Default.Write($"Processed {_event.FullPath} successfully");
        //        }
        //    }
        //}
    }
}