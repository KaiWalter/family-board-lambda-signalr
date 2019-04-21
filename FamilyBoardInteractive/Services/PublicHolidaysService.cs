using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using FamilyBoardInteractive.Models;

namespace FamilyBoardInteractive.Services
{
    public class PublicHolidaysService : ICalendarService
    {
        const string PUBLICHOLIDAYSURL = "https://feiertage-api.de/api/?nur_land=BW&jahr={0}";

        public PublicHolidaysService()
        {

        }


        public List<CalendarEntry> GetEvents(DateTime startDate, DateTime endDate)
        {
            if ((endDate.Year - startDate.Year) > 1)
            {
                throw new ArgumentException($"maximum span of years between {nameof(startDate)} and {nameof(endDate)} is 2");
            }

            int year = startDate.Year;

            using (var client = new HttpClient())
            {
                var holidayRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(PUBLICHOLIDAYSURL, year));
            }
        }

        public List<CalendarEntry> GetEventsSample()
        {
            throw new NotImplementedException();
        }
    }
}
