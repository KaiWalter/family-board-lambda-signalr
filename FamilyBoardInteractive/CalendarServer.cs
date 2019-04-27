using FamilyBoardInteractive.Models;
using FamilyBoardInteractive.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public static class CalendarServer
    {
        public static async Task<System.Collections.Generic.List<Models.CalendarEntry>> GetCalendars(MSAToken msaToken)
        {
            var start = DateTime.Now.Date.AddDays(-7);
            var end = DateTime.Now.Date.AddDays(Constants.CalendarWeeks * 7);
            var events = new System.Collections.Generic.List<CalendarEntry>();

            try
            {
                var holidays = new System.Collections.Generic.List<CalendarEntry>();

                // combine public and school holidays
                var publicHolidaysService = new PublicHolidaysService();
                var schoolHolidaysService = new SchoolHolidaysService();

                holidays.AddRange(await publicHolidaysService.GetEvents(start, end));
                holidays.AddRange(await schoolHolidaysService.GetEvents(start, end));
                var deduplicatedHolidays = holidays.GroupBy(x => x.Date).Select(y => y.First()).ToList<CalendarEntry>();
                events.AddRange(deduplicatedHolidays);

                var googleCalendarService = new GoogleCalendarService(
                    serviceAccount: Util.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT"),
                    certificateThumbprint: Util.GetEnvironmentVariable("GOOGLE_CERTIFICATE_THUMBPRINT"),
                    calendarId: Util.GetEnvironmentVariable("GOOGLE_CALENDAR_ID"),
                    timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var googleEvents = await googleCalendarService.GetEvents(start, end, isPrimary: true);
                events.AddRange(googleEvents);

                var outlookCalendarService = new OutlookCalendarService(msaToken,
                   timeZone: Util.GetEnvironmentVariable("CALENDAR_TIMEZONE"));
                var outlookEvents = await outlookCalendarService.GetEvents(start, end, isSecondary: true);
                events.AddRange(outlookEvents);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return events;
        }
    }
}
