﻿using System;
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
        const string OUTLOOKURL = "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime={0}&endDateTime={1}";

        MSAToken MSAToken;

        public OutlookCalendarService()
        {

        }

        public OutlookCalendarService(MSAToken msaToken)
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
