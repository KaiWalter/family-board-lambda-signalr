using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyBoardInteractive.Models
{
    public class CalendarEntry
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string Description { get; set; }
        public bool AllDayEvent { get; set; }
    }
}
