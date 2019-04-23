using FamilyBoardInteractive.Models;
using FamilyBoardInteractive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public static class Health
    {
        [FunctionName("Health")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] MSAToken msaToken,
            ILogger log)
        {
            string googleCalendarResult;
            string outlookCalendarResult;

            // check Google calendar
            try
            {
                var calenderService = new GoogleCalendarService(
                    serviceAccount: Util.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT"),
                    certificateThumbprint: Util.GetEnvironmentVariable("GOOGLE_CERTIFICATE_THUMBPRINT"),
                    calendarId: Util.GetEnvironmentVariable("GOOGLE_CALENDAR_ID"),
                    timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var result = await calenderService.GetEventsSample();
                googleCalendarResult = result?.Count.ToString() ?? "empty resultset";
                log.LogInformation(googleCalendarResult);
            }
            catch (Exception ex)
            {
                googleCalendarResult = ex.Message;
                log.LogError(ex, nameof(GoogleCalendarService));
            }

            // check Outlook calender
            try
            {
                var calenderService = new OutlookCalendarService(msaToken,
                   timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var result = await calenderService.GetEventsSample();
                outlookCalendarResult = result?.Count.ToString() ?? "empty resultset";
                log.LogInformation(googleCalendarResult);
            }
            catch (Exception ex)
            {
                outlookCalendarResult = ex.Message;
                log.LogError(ex, nameof(OutlookCalendarService));
            }
            // assemble service info
            var serviceInfo = new
            {
                home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process),
                webSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process),
                appRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase),
                staticFilesRoot = Util.GetApplicationRoot(),
                googleCalendarResult,
                msaToken
            };

            return (ActionResult)new OkObjectResult(serviceInfo);
        }
    }
}
