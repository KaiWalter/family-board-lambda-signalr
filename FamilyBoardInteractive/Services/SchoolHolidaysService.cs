using System;
using System.Collections.Generic;
using System.Text;
using FamilyBoardInteractive.Models;

namespace FamilyBoardInteractive.Services
{
    public class SchoolHolidaysService : ICalendarService
    {
        public SchoolHolidaysService()
        {

        }

        public List<CalendarEntry> GetEvents(DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }

        public List<CalendarEntry> GetEventsSample()
        {
            throw new NotImplementedException();
        }
    }
}
