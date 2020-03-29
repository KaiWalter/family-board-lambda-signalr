using FamilyBoardInteractive.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBoardInteractive.Services
{
    public interface ICalendarService
    {
        Task<List<CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate, ILogger logger, bool isPrimary=false, bool isSecondary=false);
        Task<List<CalendarEntry>> GetEventsSample();
    }
}