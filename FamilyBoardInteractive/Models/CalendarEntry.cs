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
    }
}
