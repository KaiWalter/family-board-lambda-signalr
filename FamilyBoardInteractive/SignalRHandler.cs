using FamilyBoardInteractive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public static class SignalRHandler
    {
        private const string HUBNAME = "fb";
        private const string QUEUENAMEUPDATECALENDAR = "updateCalendar";
        private const string SCHEDULEUPDATECALENDAR = "0 */1 * * * *";

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
            [SignalRConnectionInfo(HubName = HUBNAME)]SignalRConnectionInfo connectionInfo,
            [Queue(QUEUENAMEUPDATECALENDAR)]out string updateCalendarMessage)
        {
            updateCalendarMessage = "new client connected";
            return connectionInfo;
        }

        [FunctionName(nameof(SendMessage))]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { message }
                });
        }

        [FunctionName(nameof(UpdateCalendar))]
        public static Task UpdateCalendar(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = GetCalendars();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = QUEUENAMEUPDATECALENDAR,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(UpdateCalendarQueued))]
        public static Task UpdateCalendarQueued(
            [QueueTrigger(QUEUENAMEUPDATECALENDAR)]string queueMessage,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = GetCalendars();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = QUEUENAMEUPDATECALENDAR,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(UpdateCalendarScheduled))]
        public static Task UpdateCalendarScheduled(
        [TimerTrigger(SCHEDULEUPDATECALENDAR, RunOnStartup = true)]TimerInfo timer,
        [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = GetCalendars();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = QUEUENAMEUPDATECALENDAR,
                    Arguments = new[] { events }
                });
        }

        private static System.Collections.Generic.List<Models.CalendarEntry> GetCalendars()
        {
            var start = DateTime.Now.Date;
            var end = DateTime.Now.Date.AddDays(Constants.CalendarWeeks * 7);

            var calendarService = new GoogleCalendarService();
            var events = calendarService.GetEvents(start, end);

            return events;
        }
    }
}
