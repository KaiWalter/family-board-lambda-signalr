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
        [FunctionName(nameof(StoreMSAToken))]
        [return: Table("Tokens")]
        public static async Task<TokenEntity> StoreMSAToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity inputToken,
            ILogger log)
        {
            string code = req.Query["code"];

            var tokenCreated = DateTime.UtcNow;

            var token = await AccessTokenFromCode(code, log);

            var outputToken = JsonConvert.DeserializeObject<TokenEntity>(token);
            if (inputToken == null)
            {
                outputToken.PartitionKey = Constants.TOKEN_PARTITIONKEY;
                outputToken.RowKey = Constants.MSATOKEN_ROWKEY;
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

        [FunctionName(nameof(RefreshMSATokenActivity))]
        [Singleton(Mode = SingletonMode.Listener)]
        [return: Table("Tokens")]
        public static async Task<TokenEntity> RefreshMSATokenActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity inputToken,
            ILogger log)
        {
            if (inputToken?.RefreshToken == null)
            {
                throw new ArgumentNullException($"{nameof(RefreshMSATokenActivity)} {nameof(inputToken.RefreshToken)}");
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
                formValues.Add("client_id", Util.GetEnvironmentVariable("MSA_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("MSA_CLIENT_SECRET"));
                formValues.Add("code", code);
                formValues.Add("grant_type", "authorization_code");
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("MSA_REDIRECT_URI"));

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.MSA_TOKEN_URI)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };
                var tokenResponse = await client.SendAsync(tokenRequest);
                if(tokenResponse.IsSuccessStatusCode)
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
                formValues.Add("client_id", Util.GetEnvironmentVariable("MSA_CLIENT_ID"));
                formValues.Add("client_secret", Util.GetEnvironmentVariable("MSA_CLIENT_SECRET"));
                formValues.Add("refresh_token", refreshToken);
                formValues.Add("grant_type", "refresh_token");
                formValues.Add("redirect_uri", Util.GetEnvironmentVariable("MSA_REDIRECT_URI"));

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.MSA_TOKEN_URI)
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
