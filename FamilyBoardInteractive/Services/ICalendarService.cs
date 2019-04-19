using FamilyBoardInteractive.Models;
using System;
using System.Collections.Generic;

namespace FamilyBoardInteractive.Services
{
    public interface ICalendarService
    {
        List<CalendarEntry> GetEvents(DateTime startDate, DateTime endDate);
        List<CalendarEntry> GetEventsSample();
    }
}