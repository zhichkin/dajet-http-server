using DaJet.Http.Model;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

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

        [HttpGet("")]
        public ActionResult GetDataSources()
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
    }
}