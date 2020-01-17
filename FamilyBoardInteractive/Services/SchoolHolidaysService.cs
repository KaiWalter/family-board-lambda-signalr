using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FamilyBoardInteractive.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FamilyBoardInteractive.Services
{
    public class SchoolHolidaysService : ICalendarService
    {
        const string SCHOOLHOLIDAYSURL = "https://ferien-api.de/api/v1/holidays/BW";

        public SchoolHolidaysService()
        {

        }

        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate, ILogger logger = null, bool isPrimary = false, bool isSecondary = false)
        {
            var result = new List<CalendarEntry>();

            result.AddRange(await GetHolidaysForYear(startDate, endDate, startDate.Year, logger));

            return result;
        }

        public async Task<List<CalendarEntry>> GetEventsSample()
        {
            var result = new List<CalendarEntry>();

            result.AddRange(await GetHolidaysForYear(DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.Year));

            return result;
        }

        private static async Task<List<CalendarEntry>> GetHolidaysForYear(DateTime startDate, DateTime endDate, int year, ILogger logger = null)
        {
            var startDateISO = startDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Substring(0, 10);
            var endDateISO = endDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Substring(0, 10);

            var yearResult = new List<CalendarEntry>();

            try
            {
                using (var client = new HttpClient())
                {
                    var holidayRequest = new HttpRequestMessage(HttpMethod.Get, SCHOOLHOLIDAYSURL);
                    var holidayResponse = await client.SendAsync(holidayRequest);
                    if (holidayResponse.IsSuccessStatusCode)
                    {
                        var holidaysPayload = await holidayResponse.Content.ReadAsStringAsync();
                        var holidays = JArray.Parse(holidaysPayload);

                        foreach (var holiday in holidays)
                        {
                            var startsOn = holiday["start"].Value<DateTime>();
                            var endsOn = holiday["end"].Value<DateTime>();
                            var name = holiday["name"].Value<string>();

                            var duration = endsOn - startsOn;

                            if (startsOn.CompareTo(endDate) <= 0 && endsOn.CompareTo(startDate) >= 0 &&
                                duration.CompareTo(new TimeSpan(0)) > 0 && 
                                name.Length > 1)
                            {
                                var date = startsOn;
                                while (date <= endsOn)
                                {
                                    yearResult.Add(new CalendarEntry()
                                    {
                                        AllDayEvent = true,
                                        SchoolHoliday = true,
                                        Date = date.ToString("u").Substring(0, 10),
                                        Description = name.Substring(0, 1).ToUpper() + name.Substring(1)
                                    }); ;
                                    date = date.AddDays(1);
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in GetHolidaysForYear");
            }

            return yearResult.GroupBy(x => x.Date).Select(y => y.First()).ToList<CalendarEntry>();
        }
    }
}
