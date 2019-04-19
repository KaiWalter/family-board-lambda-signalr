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
        private const string QUEUENAME = "boardUpdates";
        private const string SCHEDULEUPDATECALENDAR = "0 */1 * * * *";
        private const string QUEUEMESSAGEUPDATECALENDER = "updateCalendar";
        private const string SIGNALRMESSAGEUPDATECALENDER = "updateCalendar";
        private const string SIGNALRMESSAGE = "newMessage";

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
            [SignalRConnectionInfo(HubName = HUBNAME)]SignalRConnectionInfo connectionInfo,
            [Queue(QUEUENAME)]out string queueMessage)
        {
            queueMessage = QUEUEMESSAGEUPDATECALENDER;
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
                    Target = SIGNALRMESSAGE,
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
                    Target = SIGNALRMESSAGEUPDATECALENDER,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(QueuedBoardUpdate))]
        public static Task QueuedBoardUpdate(
            [QueueTrigger(QUEUENAME)]string queueMessage,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            switch (queueMessage)
            {
                case QUEUEMESSAGEUPDATECALENDER:
                    {
                        var events = GetCalendars();

                        return signalRMessages.AddAsync(
                            new SignalRMessage
                            {
                                Target = SIGNALRMESSAGEUPDATECALENDER,
                                Arguments = new[] { events }
                            });
                    }

            }

            return null;
        }

        [FunctionName(nameof(ScheduledCalendarUpdate))]
        public static Task ScheduledCalendarUpdate(
        [TimerTrigger(SCHEDULEUPDATECALENDAR, RunOnStartup = true)]TimerInfo timer,
        [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = GetCalendars();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATECALENDER,
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
