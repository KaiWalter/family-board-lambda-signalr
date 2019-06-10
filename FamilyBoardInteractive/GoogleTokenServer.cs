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
    public static class GoogleTokenServer
    {
        private const string GOOGLE_TOKEN_URI = "https://www.googleapis.com/oauth2/v4/token";

        [FunctionName(nameof(StoreGoogleToken))]
        [return: Table("Tokens")]
        public static async Task<TokenEntity> StoreGoogleToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.GOOGLETOKEN_ROWKEY)] TokenEntity inputToken,
            ILogger log)
        {
            string code = req.Query["code"];

            var tokenCreated = DateTime.UtcNow;

            var token = await AccessTokenFromCode(code, log);

            var outputToken = JsonConvert.DeserializeObject<TokenEntity>(token);
            if (inputToken == null)
            {
                outputToken.PartitionKey = Constants.TOKEN_PARTITIONKEY;
                outputToken.RowKey = Constants.GOOGLETOKEN_ROWKEY;
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

        [FunctionName(nameof(RefreshGoogleToken))]
        [Singleton(Mode = SingletonMode.Listener)]
        [return: Table("Tokens")]
        public static async Task<TokenEntity> RefreshGoogleToken(
            [QueueTrigger(Constants.QUEUEMESSAGEREFRESHGOOGLETOKEN)] string queueMessage,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.GOOGLETOKEN_ROWKEY)] TokenEntity inputToken,
            ILogger log)
        {
            if (inputToken?.RefreshToken == null)
            {
                throw new ArgumentNullException($"{nameof(RefreshGoogleToken)} {nameof(inputToken.RefreshToken)}");
            }

            if (!inputToken.NeedsRefresh)
            {
                return null;
            }

            var tokenCreated = DateTime.UtcNow;

            var token = await AccessTokenFromRefreshToken(inputToken.RefreshToken, log);

            var outputToken = JsonConvert.DeserializeObject<TokenEntity>(token);
            outputToken.ETag = inputToken.ETag;
            outputToken.PartitionKey = inputToken.PartitionKey;
            outputToken.RowKey = inputToken.RowKey;
            outputToken.Created = tokenCreated;
            outputToken.Expires = tokenCreated.AddSeconds(outputToken.ExpiresIn);

            return outputToken;
        }

        private static async Task<string> AccessTokenFromCode(string code, ILogger log)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                Dictionary<string, string> formValues = new Dictionary<string, string>();
                formValues.Add("code", code);
                formValues.Add("client_id", Util.GetEnvironmentVariable("GOOGLE_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"));
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("GOOGLE_REDIRECT_URI"));
                formValues.Add("grant_type", "authorization_code");

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, GOOGLE_TOKEN_URI)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };
                var tokenResponse = await client.SendAsync(tokenRequest);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    result = await tokenResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorMessage = await tokenResponse.Content.ReadAsStringAsync();
                    log.LogError(errorMessage);
                }
            }

            return result;
        }

        internal static async Task<string> AccessTokenFromRefreshToken(string refreshToken, ILogger log)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                Dictionary<string, string> formValues = new Dictionary<string, string>();
                formValues.Add("client_id", Util.GetEnvironmentVariable("GOOGLE_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"));
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("GOOGLE_REDIRECT_URI"));
                formValues.Add("refresh_token", refreshToken);
                formValues.Add("grant_type", "refresh_token");

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, GOOGLE_TOKEN_URI)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };
                var tokenResponse = await client.SendAsync(tokenRequest);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    result = await tokenResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorMessage = await tokenResponse.Content.ReadAsStringAsync();
                    log.LogError(errorMessage);
                }
            }

            return result;
        }
    }
}
