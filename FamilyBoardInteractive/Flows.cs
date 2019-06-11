using FamilyBoardInteractive.Models;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public class Flows
    {
        [FunctionName(nameof(FullBoardUpdate))]
        public static async Task FullBoardUpdate(
            [OrchestrationTrigger] DurableOrchestrationContextBase context
            )
        {
            await context.CallActivityAsync(nameof(Storage.CheckConfigurationActivity), string.Empty);
            await context.CallActivityAsync(nameof(GoogleTokenServer.RefreshGoogleTokenActivity), string.Empty);
            await context.CallActivityAsync(nameof(MSATokenServer.RefreshMSATokenActivity), string.Empty);
            await context.CallActivityAsync(nameof(SignalRHandler.UpdateCalendarActivity), string.Empty);
            string imageUpdateMessage = await context.CallActivityAsync<string>(nameof(ImageHandler.RollImageActivity), string.Empty);
            await context.CallActivityAsync(nameof(SignalRHandler.UpdateImageActivity), imageUpdateMessage);
        }

        [FunctionName(nameof(CalendarUpdate))]
        public static async Task CalendarUpdate(
            [OrchestrationTrigger] DurableOrchestrationContextBase context
            )
        {
            await context.CallActivityAsync(nameof(GoogleTokenServer.RefreshGoogleTokenActivity), string.Empty);
            await context.CallActivityAsync(nameof(MSATokenServer.RefreshMSATokenActivity), string.Empty);
            await context.CallActivityAsync(nameof(SignalRHandler.UpdateCalendarActivity), string.Empty);
        }

        [FunctionName(nameof(ImageUpdate))]
        public static async Task ImageUpdate(
            [OrchestrationTrigger] DurableOrchestrationContextBase context
            )
        {
            await context.CallActivityAsync(nameof(Storage.CheckConfigurationActivity), string.Empty);
            await context.CallActivityAsync(nameof(MSATokenServer.RefreshMSATokenActivity), string.Empty);
            string imageUpdateMessage = await context.CallActivityAsync<string>(nameof(ImageHandler.RollImageActivity), string.Empty);
            await context.CallActivityAsync(nameof(SignalRHandler.UpdateImageActivity), imageUpdateMessage);
        }

        [FunctionName(nameof(MessageSend))]
        public static async Task MessageSend(
            [OrchestrationTrigger] DurableOrchestrationContextBase context
            )
        {
            await context.CallActivityAsync(nameof(SignalRHandler.SendMessageActivity), context.GetInput<Message>());
        }
    }
}
