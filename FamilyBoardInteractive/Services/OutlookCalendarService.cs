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

        MSAToken MSAToken;

        public string OutlookTimeZone { get; set; }

        public OutlookCalendarService(string outlookTimeZone = null)
        {
            OutlookTimeZone = outlookTimeZone ?? "W. Europe Standard Time";
        }

        public OutlookCalendarService(MSAToken msaToken, string outlookTimeZone = null)
            : this(outlookTimeZone)
        {
            MSAToken = msaToken;
        }

        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate)
        {
            List<CalendarEntry> eventResults = new List<CalendarEntry>();

            using (var client = new HttpClient())
            {
                var eventRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(OUTLOOKURL, startDate.ToString("u").Substring(0, 10), endDate.ToString("u").Substring(0, 10)));
                eventRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(MSAToken.TokenType, MSAToken.AccessToken);
                eventRequest.Headers.Add("Prefer", $"outlook.timezone=\"{OutlookTimeZone}\"");

                var eventResponse = await client.SendAsync(eventRequest);
                if (eventResponse.IsSuccessStatusCode)
                {
                    var eventPayload = await eventResponse.Content.ReadAsStringAsync();
                    var eventList = (JArray)JObject.Parse(eventPayload)["value"];

                    foreach(var eventToken in eventList)
                    {
                        var eventItem = (JObject)eventToken;
                        var subject = eventItem["subject"].Value<string>();
                        var isAllDay = eventItem["isAllDay"].Value<bool>();
                        var start = (JObject)eventItem["start"];
                        var startTime = start["dateTime"].Value<DateTime>();
                        var end = (JObject)eventItem["end"];
                        var endTime = end["dateTime"].Value<DateTime>();

                        if(isAllDay)
                        {
                            var currentDT = startTime;
                            while (currentDT < endTime)
                            {
                                var eventResult = new CalendarEntry()
                                {
                                    Date = currentDT.ToString("u").Substring(0, 10),
                                    Description = subject,
                                    AllDayEvent = isAllDay
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
                                Description = subject
                            };
                            eventResults.Add(eventResult);
                        }
                    }
                }
            }

            return eventResults;
        }

        public Task<List<CalendarEntry>> GetEventsSample()
        {
            throw new NotImplementedException();
        }
    }
}
