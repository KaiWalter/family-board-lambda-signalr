﻿using FamilyBoardInteractive.Services;
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
        private const string QUEUEMESSAGEBOARDUPDATE = "boardUpdates";
        private const string SIGNALRMESSAGEUPDATECALENDER = "updateCalendar";
        private const string SIGNALRMESSAGEUPDATEIMAGE = "updateImage";
        private const string SIGNALRMESSAGE = "newMessage";

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
            [SignalRConnectionInfo(HubName = HUBNAME)]SignalRConnectionInfo connectionInfo,
            [Queue(QUEUEMESSAGEBOARDUPDATE)]out string queueMessage)
        {
            queueMessage = "new client connection " + DateTime.UtcNow.ToString("u");
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
        public static async Task<IAsyncCollector<SignalRMessage>> UpdateCalendar(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = await GetCalendars();

            return (IAsyncCollector<SignalRMessage>)signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATECALENDER,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(QueuedBoardUpdate))]
        public static void QueuedBoardUpdate(
            [QueueTrigger(QUEUEMESSAGEBOARDUPDATE)]string queueMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATECALENDER)]out string updateCalendarMessage,
            [Queue(Constants.QUEUEMESSAGEPUSHIMAGE)]out string pushImageMessage)
        {
            updateCalendarMessage = pushImageMessage = queueMessage;
        }

        [FunctionName(nameof(QueuedCalendarUpdate))]
        public static Task QueuedCalendarUpdate(
            [QueueTrigger(Constants.QUEUEMESSAGEUPDATECALENDER)]string queueMessage,
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


        [FunctionName(nameof(QueuedImageUpdate))]
        public static Task QueuedImageUpdate(
            [QueueTrigger(Constants.QUEUEMESSAGEUPDATEIMAGE)]string queueMessage,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATEIMAGE,
                    Arguments = new[] { queueMessage }
                });
        }

        [FunctionName(nameof(ScheduledCalendarUpdate))]
        public static void ScheduledCalendarUpdate(
            [TimerTrigger(Constants.SCHEDULEUPDATECALENDAR, RunOnStartup = false)]TimerInfo timer,
            [Queue(Constants.QUEUEMESSAGEUPDATECALENDER)]out string updateCalendarMessage)
        {
            updateCalendarMessage = $"scheduled {DateTime.UtcNow.ToString("u")}";
        }

        [FunctionName(nameof(ScheduledImageUpdate))]
        public static void ScheduledImageUpdate(
            [TimerTrigger(Constants.SCHEDULEUPDATEIMAGE, RunOnStartup = false)]TimerInfo timer,
            [Queue(Constants.QUEUEMESSAGEPUSHIMAGE)]out string pushImageMessage)
        {
            pushImageMessage = $"scheduled {DateTime.UtcNow.ToString("u")}";
        }

        private static async Task<System.Collections.Generic.List<Models.CalendarEntry>> GetCalendars()
        {
            var start = DateTime.Now.Date;
            var end = DateTime.Now.Date.AddDays(Constants.CalendarWeeks * 7);

            var calendarService = new GoogleCalendarService();
            var events = await calendarService.GetEvents(start, end);

            return events;
        }
    }
}
