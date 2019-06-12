using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public class Scheduler
    {
        [FunctionName(nameof(ScheduledCalendarUpdate))]
        public static async Task ScheduledCalendarUpdate(
            [TimerTrigger(Constants.SCHEDULEUPDATECALENDAR, RunOnStartup = false)]TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient starter) => await starter.StartNewAsync(nameof(Flows.CalendarUpdate), $"scheduled {DateTime.UtcNow.ToString("u")}");

        [FunctionName(nameof(ScheduledImageUpdate))]
        public static async Task ScheduledImageUpdate(
            [TimerTrigger(Constants.SCHEDULEUPDATEIMAGE, RunOnStartup = false)]TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient starter)
        {
            await starter.StartNewAsync(nameof(Flows.ImageUpdate), $"scheduled {DateTime.UtcNow.ToString("u")}");
        }
    }
}
