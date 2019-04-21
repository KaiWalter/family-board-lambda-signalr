using FamilyBoardInteractive.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBoardInteractive.Services
{
    public interface ICalendarService
    {
        Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate);
        Task<List<CalendarEntry>> GetEventsSample();
    }
}