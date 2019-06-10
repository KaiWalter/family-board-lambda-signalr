﻿using FamilyBoardInteractive.Models;
using FamilyBoardInteractive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] Message message,
            [SignalR(HubName = HUBNAME)] IAsyncCollector<SignalRMessage> signalRMessages)
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
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = CalendarServer.GetCalendars(msaToken).GetAwaiter().GetResult();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATECALENDER,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(QueuedBoardUpdate))]
        [Singleton(Mode = SingletonMode.Listener)]
        public static void QueuedBoardUpdate(
            [QueueTrigger(QUEUEMESSAGEBOARDUPDATE)]string queueMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATECALENDER)]out string updateCalendarMessage,
            [Queue(Constants.QUEUEMESSAGEPUSHIMAGE)]out string pushImageMessage,
            [Queue(Constants.QUEUEMESSAGECONFIGURUATION)]out string configuration
            )
        {
            updateCalendarMessage = configuration = pushImageMessage = queueMessage;

        }

        [FunctionName(nameof(QueuedCalendarUpdate))]
        [Singleton(Mode = SingletonMode.Listener)]
        public static Task QueuedCalendarUpdate(
            [QueueTrigger(Constants.QUEUEMESSAGEUPDATECALENDER)]string queueMessage,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
            [SignalR(HubName = HUBNAME)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = CalendarServer.GetCalendars(msaToken).GetAwaiter().GetResult();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATECALENDER,
                    Arguments = new[] { events }
                });
        }


        [FunctionName(nameof(QueuedImageUpdate))]
        [Singleton(Mode = SingletonMode.Listener)]
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
    }
}
