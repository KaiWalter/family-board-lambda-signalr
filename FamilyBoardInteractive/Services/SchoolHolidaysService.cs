using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FamilyBoardInteractive.Models;

namespace FamilyBoardInteractive.Services
{
    public class SchoolHolidaysService : ICalendarService
    {
        public SchoolHolidaysService()
        {

        }

        public async Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }

        public async Task<List<CalendarEntry>> GetEventsSample()
        {
            throw new NotImplementedException();
        }
    }
}
