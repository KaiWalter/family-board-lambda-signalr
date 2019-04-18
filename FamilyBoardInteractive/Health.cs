using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FamilyBoardInteractive
{
    public static class Health
    {
        [FunctionName("Health")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var serviceInfo = new
            {
                home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process),
                webSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process),
                appRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase),
                staticFilesRoot = Util.GetApplicationRoot()
            };

            return (ActionResult)new OkObjectResult(serviceInfo);
        }
    }
}
