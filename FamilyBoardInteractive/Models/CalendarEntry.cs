using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyBoardInteractive.Models
{
    public class CalendarEntry
    {
        [JsonProperty("date")]
        public string Date { get; set; }
        [JsonProperty("time")]
        public string Time { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("allDayEvent")]
        public bool AllDayEvent { get; set; }
        [JsonProperty("publicHoliday")]
        public bool PublicHoliday { get; set; }
        [JsonProperty("schoolHoliday")]
        public bool SchoolHoliday { get; set; }
        [JsonProperty("isPrimary")]
        public bool IsPrimary { get; set; }
        [JsonProperty("isSecondary")]
        public bool IsSecondary { get; set; }
    }
}
