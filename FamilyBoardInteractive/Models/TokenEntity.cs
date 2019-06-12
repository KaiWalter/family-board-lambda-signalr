using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace FamilyBoardInteractive.Models
{
    public class TokenEntity : TableEntity
    {
        public DateTime Created { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        public DateTime Expires { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonIgnore]
        public bool NeedsRefresh
        {
            get
            {
                return DateTime.UtcNow > Expires.AddMinutes(-5);
            }
        }
    }
}
