using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public static class MSATokenServer
    {
        private const string MSA_TOKEN_URI = "https://login.live.com/oauth20_token.srf";

        [FunctionName(nameof(StoreMSAToken))]
        [return: Table("Tokens")]
        public static async Task<MSAToken> StoreMSAToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table("Tokens", partitionKey: "Token", rowKey: "MSA")] MSAToken inputToken,
            ILogger log)
        {
            string code = req.Query["code"];

            var tokenCreated = DateTime.UtcNow;

            var token = await AccessTokenFromCode(code);

            var outputToken = JsonConvert.DeserializeObject<MSAToken>(token);
            if (inputToken == null)
            {
                outputToken.PartitionKey = "Token";
                outputToken.RowKey = "MSA";
            }
            else
            {
                outputToken.ETag = inputToken.ETag;
                outputToken.PartitionKey = inputToken.PartitionKey;
                outputToken.RowKey = inputToken.RowKey;
            }

            outputToken.Created = tokenCreated;
            outputToken.Expires = tokenCreated.AddSeconds(outputToken.ExpiresIn);

            return outputToken;
        }

        [FunctionName(nameof(RefreshMSAToken))]
        [Singleton(Mode = SingletonMode.Listener)]
        [return: Table("Tokens")]
        public static async Task<MSAToken> RefreshMSAToken(
            [QueueTrigger(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] string queueMessage,
            [Table("Tokens", partitionKey: "Token", rowKey: "MSA")] MSAToken inputToken,
            ILogger log)
        {
            if (inputToken?.RefreshToken == null)
            {
                throw new ArgumentNullException($"{nameof(RefreshMSAToken)} {nameof(inputToken.RefreshToken)}");
            }

            if (DateTime.UtcNow < inputToken.Expires.AddMinutes(5)) // token still valid
            {
                return null;
            }

            var tokenCreated = DateTime.UtcNow;

            var token = await AccessTokenFromRefreshToken(inputToken.RefreshToken);

            var outputToken = JsonConvert.DeserializeObject<MSAToken>(token);
            outputToken.ETag = inputToken.ETag;
            outputToken.PartitionKey = inputToken.PartitionKey;
            outputToken.RowKey = inputToken.RowKey;
            outputToken.Created = tokenCreated;
            outputToken.Expires = tokenCreated.AddSeconds(outputToken.ExpiresIn);

            return outputToken;
        }

        private static async Task<string> AccessTokenFromCode(string code)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                Dictionary<string, string> formValues = new Dictionary<string, string>();
                formValues.Add("client_id", Util.GetEnvironmentVariable("MSA_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("MSA_CLIENT_SECRET"));
                formValues.Add("code", code);
                formValues.Add("grant_type", "authorization_code");
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("MSA_REDIRECT_URI"));

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, MSA_TOKEN_URI)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };
                var tokenResponse = await client.SendAsync(tokenRequest);
                if(tokenResponse.IsSuccessStatusCode)
                {
                    result = await tokenResponse.Content.ReadAsStringAsync();
                }
            }

            return result;
        }

        internal static async Task<string> AccessTokenFromRefreshToken(string refreshToken)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                Dictionary<string, string> formValues = new Dictionary<string, string>();
                formValues.Add("client_id", Util.GetEnvironmentVariable("MSA_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("MSA_CLIENT_SECRET"));
                formValues.Add("refresh_token", refreshToken);
                formValues.Add("grant_type", "refresh_token");
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("MSA_REDIRECT_URI"));

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, MSA_TOKEN_URI)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };
                var tokenResponse = await client.SendAsync(tokenRequest);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    result = await tokenResponse.Content.ReadAsStringAsync();
                }
            }

            return result;
        }

    }
}
