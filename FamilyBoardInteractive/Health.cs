using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FamilyBoardInteractive.Services;

namespace FamilyBoardInteractive
{
    public static class Health
    {
        [FunctionName("Health")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string googleCalendarResult;

            // check Google calendar
            try
            {
                var calenderService = new GoogleCalendarService();
                var result = calenderService.GetEventsSample();
                googleCalendarResult = result?.Count.ToString() ?? "empty resultset";
                log.LogInformation(googleCalendarResult);
            }
            catch (Exception ex)
            {
                googleCalendarResult = ex.Message;
                log.LogError(ex, "GoogleCalendarService");
            }

            // assemble service info
            var serviceInfo = new
            {
                home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process),
                webSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process),
                appRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase),
                staticFilesRoot = Util.GetApplicationRoot(),
                googleCalendarResult,
                MSA_AccessToken = req.Headers["X-MS-TOKEN-MICROSOFTACCOUNT-ACCESS-TOKEN"],
                MSA_ExpiresOn = req.Headers["X-MS-TOKEN-MICROSOFTACCOUNT-EXPIRES-ON"],
                MSA_AuthenticationToken = req.Headers["X-MS-TOKEN-MICROSOFTACCOUNT-AUTHENTICATION-TOKEN"],
                MSA_RefreshToken = req.Headers["X-MS-TOKEN-MICROSOFTACCOUNT-REFRESH-TOKEN"]
            };

            return (ActionResult)new OkObjectResult(serviceInfo);
        }
    }
}
