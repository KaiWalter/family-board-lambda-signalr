using FamilyBoardInteractive.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBoardInteractive.Services
{
    public interface ICalendarService
    {
        Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate, bool isPrimary=false, bool isSecondary=false);
        Task<List<CalendarEntry>> GetEventsSample();
    }
}