using Newtonsoft.Json;
using System;

namespace FamilyBoardInteractive.Models
{
    public class ImagePlayed
    {
        public string ImageName { get; set; }
        [JsonIgnore]
        public string ImageUrl { get; set; }
        public int Count { get; set; }

        public DateTime LastPlayed { get; set; }
    }
}
