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
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.GOOGLETOKEN_ROWKEY)] TokenEntity googleToken,
            ILogger log)
        {
            string googleCalendarResult;
            string outlookCalendarResult;
            int statusCode = StatusCodes.Status200OK;

            // check Google calendar
            try
            {
                var calendarService = new GoogleCalendarService(
                    googleToken,
                    calendarId: Util.GetEnvironmentVariable("GOOGLE_CALENDAR_ID"),
                    timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var result = await calendarService.GetEventsSample();
                googleCalendarResult = result?.Count.ToString() ?? "empty resultset";
                log.LogInformation(googleCalendarResult);
            }
            catch (Exception ex)
            {
                googleCalendarResult = ex.Message;
                log.LogError(ex, nameof(GoogleCalendarService));
                statusCode = StatusCodes.Status424FailedDependency;
            }

            // check Outlook calender
            try
            {
                var calendarService = new OutlookCalendarService(msaToken,
                   timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var result = await calendarService.GetEventsSample();
                outlookCalendarResult = result?.Count.ToString() ?? "empty resultset";
                log.LogInformation(googleCalendarResult);
            }
            catch (Exception ex)
            {
                outlookCalendarResult = ex.Message;
                log.LogError(ex, nameof(OutlookCalendarService));
                statusCode = StatusCodes.Status424FailedDependency;
            }

            // check MSA Token
            if (msaToken == null)
            {
                statusCode = StatusCodes.Status424FailedDependency;
            }

            // assemble service info
            return statusCode == StatusCodes.Status200OK
                 ? (ActionResult)new OkObjectResult(new
                 {
                     build = Environment.GetEnvironmentVariable("FB_BUILD", EnvironmentVariableTarget.Process) ?? "",
                     home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process),
                     webSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process),
                     appRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase),
                     staticFilesRoot = Util.GetApplicationRoot(),
                     googleCalendarResult,
                     outlookCalendarResult,
                     msaToken
                 })
                 : (ActionResult)new StatusCodeResult(statusCode);
        }
    }
}
