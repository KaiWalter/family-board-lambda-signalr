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
    public class HttpHandler
    {
        [FunctionName(nameof(UpdateCalendar))]
        public static async Task UpdateCalendar(
            [HttpTrigger(AuthorizationLevel.Function, "post")]object message,
            [OrchestrationClient] DurableOrchestrationClient starter)
        {
            await starter.StartNewAsync(nameof(Flows.CalendarUpdate), $"initiated by {nameof(UpdateCalendar)}");
        }

        [FunctionName(nameof(UpdateImage))]
        public static async Task UpdateImage(
            [HttpTrigger(AuthorizationLevel.Function, "post")]object message,
            [OrchestrationClient] DurableOrchestrationClient starter)
        {
            await starter.StartNewAsync(nameof(Flows.ImageUpdate), $"initiated by {nameof(UpdateImage)}");
        }

        [FunctionName(nameof(SendMessage))]
        public static async Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] Message message,
            [OrchestrationClient] DurableOrchestrationClient starter)
        {
            await starter.StartNewAsync(nameof(Flows.MessageSend), message);
        }

        [FunctionName("Health")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req,
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

            // check Google Token
            if (googleToken == null)
            {
                statusCode = StatusCodes.Status424FailedDependency;
            }

            // assemble service info
            return statusCode == StatusCodes.Status200OK
                 ? (ActionResult)new OkObjectResult(new
                 {
                     home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process),
                     webSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process),
                     appRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase),
                     staticFilesRoot = Util.GetApplicationRoot(),
                     googleCalendarResult,
                     outlookCalendarResult,
                     msaToken,
                     googleToken
                 })
                 : (ActionResult)new StatusCodeResult(statusCode);
        }
    }
}
