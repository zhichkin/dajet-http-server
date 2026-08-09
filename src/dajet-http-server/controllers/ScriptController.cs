using DaJet.Http.Model;
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
        public ScriptController() { }

        [HttpPost("{**path}")]
        public async Task<ContentResult> ExecuteScript([FromRoute] string path)
        {
            QueryResponse response = new()
            {
                Success = false,
                Message = "Deprecated: use /api endpoint instead.",
                Result = null,
                IsLongRunning = false
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);

            ContentResult result = Content(json, "application/json", Encoding.UTF8);

            result.StatusCode = (int)HttpStatusCode.Gone;

            return result;
        }
    }
}