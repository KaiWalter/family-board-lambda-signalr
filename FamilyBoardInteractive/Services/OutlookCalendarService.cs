using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FamilyBoardInteractive.Models;
using Newtonsoft.Json.Linq;

namespace FamilyBoardInteractive.Services
{
    public class OutlookCalendarService : ICalendarService
    {
        const string OUTLOOKURL = "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime={0}&endDateTime={1}&$select=subject,isAllDay,start,end";

        TokenEntity MSAToken;

        string TimeZone;

        public OutlookCalendarService(TokenEntity msaToken, string timeZone = null)
        {
            MSAToken = msaToken;
            TimeZone = timeZone ?? Constants.DEFAULT_TIMEZONE;
        }

        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate, bool isPrimary = false, bool isSecondary = false)
        {
            return await GetCalendar(startDate, endDate, isPrimary, isSecondary);
        }

        public async Task<List<CalendarEntry>> GetEventsSample()
        {
            return await GetCalendar(startDate: DateTime.UtcNow.Date, endDate: DateTime.UtcNow.Date.AddDays(7));
        }

        private async Task<List<CalendarEntry>> GetCalendar(DateTime startDate, DateTime endDate, bool isPrimary = false, bool isSecondary = false)
        {
            List<CalendarEntry> eventResults = new List<CalendarEntry>();

            using (var client = new HttpClient())
            {
                var eventRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(OUTLOOKURL, startDate.ToString("u").Substring(0, 10), endDate.ToString("u").Substring(0, 10)));
                eventRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(MSAToken.TokenType, MSAToken.AccessToken);
                eventRequest.Headers.Add("Prefer", $"outlook.timezone=\"{this.TimeZone}\"");

                var eventResponse = await client.SendAsync(eventRequest);
                if (eventResponse.IsSuccessStatusCode)
                {
                    var eventPayload = await eventResponse.Content.ReadAsStringAsync();
                    var eventList = (JArray)JObject.Parse(eventPayload)["value"];

                    foreach (var eventToken in eventList)
                    {
                        var eventItem = (JObject)eventToken;
                        var subject = eventItem["subject"].Value<string>();
                        var isAllDay = eventItem["isAllDay"].Value<bool>();
                        var start = (JObject)eventItem["start"];
                        var startTime = start["dateTime"].Value<DateTime>();
                        var end = (JObject)eventItem["end"];
                        var endTime = end["dateTime"].Value<DateTime>();

                        if (isAllDay)
                        {
                            var currentDT = startTime;
                            while (currentDT < endTime)
                            {
                                var eventResult = new CalendarEntry()
                                {
                                    Date = currentDT.ToString("u").Substring(0, 10),
                                    Description = subject,
                                    AllDayEvent = isAllDay,
                                    IsPrimary = isPrimary,
                                    IsSecondary = isSecondary
                                };
                                eventResults.Add(eventResult);
                                currentDT = currentDT.AddDays(1);
                            }
                        }
                        else
                        {
                            var eventResult = new CalendarEntry()
                            {
                                Date = startTime.ToString("u").Substring(0, 10),
                                Time = startTime.Hour.ToString().PadLeft(2, '0') + ":" + startTime.Minute.ToString().PadLeft(2, '0'),
                                Description = subject,
                                IsPrimary = isPrimary,
                                IsSecondary = isSecondary
                            };
                            eventResults.Add(eventResult);
                        }
                    }
                }
            }

            return eventResults;
        }
    }
}
