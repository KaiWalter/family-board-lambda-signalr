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

            if (string.IsNullOrEmpty(token))
            {
                log.LogError("Google token is invalid");
                return null;
            }

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

        [FunctionName(nameof(RefreshGoogleTokenActivity))]
        [Singleton(Mode = SingletonMode.Listener)]
        [return: Table("Tokens")]
        public static async Task<TokenEntity> RefreshGoogleTokenActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.GOOGLETOKEN_ROWKEY)] TokenEntity inputToken,
            ILogger log)
        {
            if (inputToken?.RefreshToken == null)
            {
                throw new ArgumentNullException($"{nameof(RefreshGoogleTokenActivity)} {nameof(inputToken.RefreshToken)}");
            }

            TokenEntity outputToken = null;

            if (inputToken.NeedsRefresh)
            {
                var tokenCreated = DateTime.UtcNow;

                var token = await AccessTokenFromRefreshToken(inputToken.RefreshToken, log);

                outputToken = JsonConvert.DeserializeObject<TokenEntity>(token);
                outputToken.ETag = inputToken.ETag;
                outputToken.PartitionKey = inputToken.PartitionKey;
                outputToken.RowKey = inputToken.RowKey;
                outputToken.RefreshToken = inputToken.RefreshToken; // Google RefreshToken can be re-used
                outputToken.Created = tokenCreated;
                outputToken.Expires = tokenCreated.AddSeconds(outputToken.ExpiresIn);
            }

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

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.GOOGLE_TOKEN_URI)
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

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.GOOGLE_TOKEN_URI)
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
