using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FamilyBoardInteractive.Models;
using Newtonsoft.Json.Linq;

namespace FamilyBoardInteractive.Services
{
    public class PublicHolidaysService : ICalendarService
    {
        const string PUBLICHOLIDAYSURL = "https://feiertage-api.de/api/?nur_land=BW&jahr={0}";

        public PublicHolidaysService()
        {

        }


        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate)
        {
            if ((endDate.Year - startDate.Year) > 1)
            {
                throw new ArgumentException($"maximum span of years between {nameof(startDate)} and {nameof(endDate)} is 2");
            }

            var result = new List<CalendarEntry>();

            result.AddRange(await GetHolidaysForYear(startDate, endDate, startDate.Year));
            if (endDate.Year != startDate.Year)
            {
                result.AddRange(await GetHolidaysForYear(startDate, endDate, endDate.Year));
            }

            return result;
        }

        public async Task<List<CalendarEntry>> GetEventsSample()
        {
            var result = new List<CalendarEntry>();

            result.AddRange(await GetHolidaysForYear(DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.Year));

            return result;
        }

        private static async Task<List<CalendarEntry>> GetHolidaysForYear(DateTime startDate, DateTime endDate, int year)
        {
            var startDateISO = startDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Substring(0, 10);
            var endDateISO = endDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture).Substring(0, 10);

            var yearResult = new List<CalendarEntry>();

            using (var client = new HttpClient())
            {
                var holidayRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(PUBLICHOLIDAYSURL, year));
                var holidayResponse = await client.SendAsync(holidayRequest);
                if (holidayResponse.IsSuccessStatusCode)
                {
                    var holidaysPayload = await holidayResponse.Content.ReadAsStringAsync();
                    var holidays = JObject.Parse(holidaysPayload);
                    foreach (var holidayToken in holidays)
                    {
                        var holiday = (JObject)holidayToken.Value;
                        var day = holiday["datum"].Value<string>();
                        if (day.CompareTo(startDateISO) >= 0 && day.CompareTo(endDateISO) <= 0)
                        {
                            yearResult.Add(new CalendarEntry()
                            {
                                AllDayEvent = true,
                                PublicHoliday = true,
                                Description = holidayToken.Key,
                                Date = day
                            });
                        }
                    }
                }
            }

            return yearResult;
        }

    }
}
