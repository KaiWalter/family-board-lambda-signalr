using FamilyBoardInteractive.Models;
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
        private const string SIGNALRMESSAGEUPDATECALENDER = "updateCalendar";
        private const string SIGNALRMESSAGEUPDATEIMAGE = "updateImage";
        private const string SIGNALRMESSAGE = "newMessage";

        [FunctionName("negotiate")]
        public static async Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
            [SignalRConnectionInfo(HubName = HUBNAME)]SignalRConnectionInfo connectionInfo,
            [OrchestrationClient] DurableOrchestrationClient starter)
        {
            await starter.StartNewAsync(nameof(Flows.FullBoardUpdate), "new client connection " + DateTime.UtcNow.ToString("u"));
            return connectionInfo;
        }

        [FunctionName(nameof(SendMessageActivity))]
        public static Task SendMessageActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            [SignalR(HubName = HUBNAME)] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGE,
                    Arguments = new[] { context.GetInput<Message>() }
                });
        }


        [FunctionName(nameof(UpdateCalendarActivity))]
        public static Task UpdateCalendarActivity(
                [ActivityTrigger] DurableActivityContextBase context,
                [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
                [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.GOOGLETOKEN_ROWKEY)] TokenEntity googleToken,
                [SignalR(HubName = HUBNAME)] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var events = CalendarServer.GetCalendars(msaToken, googleToken).GetAwaiter().GetResult();

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATECALENDER,
                    Arguments = new[] { events }
                });
        }

        [FunctionName(nameof(UpdateImageActivity))]
        public static Task UpdateImageActivity(
                [ActivityTrigger] DurableActivityContextBase context,
                [SignalR(HubName = HUBNAME)] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = SIGNALRMESSAGEUPDATEIMAGE,
                    Arguments = new[] { context.GetInput<string>() }
                });
        }
    }
}
