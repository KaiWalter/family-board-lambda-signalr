using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FamilyBoardInteractive.Models;
using Newtonsoft.Json.Linq;

namespace FamilyBoardInteractive.Services
{
    public class SchoolHolidaysService : ICalendarService
    {
        const string SCHOOLHOLIDAYSURL = "https://www.mehr-schulferien.de/api/v1.0/periods";

        public SchoolHolidaysService()
        {

        }

        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate, bool isPrimary = false, bool isSecondary = false)
        {
            var result = new List<CalendarEntry>();

            result.AddRange(await GetHolidaysForYear(startDate, endDate, startDate.Year));

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
                var holidayRequest = new HttpRequestMessage(HttpMethod.Get, SCHOOLHOLIDAYSURL);
                var holidayResponse = await client.SendAsync(holidayRequest);
                if (holidayResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var holidaysPayload = await holidayResponse.Content.ReadAsStringAsync();
                        var holidaysData = JObject.Parse(holidaysPayload);
                        var holidays = (JArray)holidaysData["data"];

                        foreach (var holiday in holidays)
                        {
                            var stateId = holiday["federal_state_id"];
                            if (stateId.Type != JTokenType.Null)
                            {
                                if (holiday["federal_state_id"].Value<int>() == 1)
                                {
                                    var startsOn = holiday["starts_on"].Value<DateTime>();
                                    var endsOn = holiday["ends_on"].Value<DateTime>();
                                    var name = holiday["name"].Value<string>();

                                    var duration = endsOn - startsOn;

                                    if (startsOn.CompareTo(endDate) <= 0 && endsOn.CompareTo(startDate) >= 0 &&
                                        duration.CompareTo(new TimeSpan(0)) > 0)
                                    {
                                        var date = startsOn;
                                        while(date <= endsOn)
                                        {
                                            yearResult.Add(new CalendarEntry()
                                            {
                                                AllDayEvent = true,
                                                SchoolHoliday = true,
                                                Date = date.ToString("u").Substring(0, 10),
                                                Description = holiday["name"].Value<string>().Replace("Himmelfahrt","Pfingsten")
                                            });
                                            date = date.AddDays(1);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }

            return yearResult.GroupBy(x => x.Date).Select(y => y.First()).ToList<CalendarEntry>();
        }
    }
}
