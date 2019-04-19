using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.AuthTokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FamilyBoardInteractive
{
    public static class MicrosoftGraph
    {
        [FunctionName(nameof(token1))]
        public static IActionResult token1(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Token(Resource = "https://graph.microsoft.com", Identity = TokenIdentityMode.UserFromId, UserId = "sid:4d3451ed0d1da1d0c082d33aef95f627")]string token,
            ILogger log)
        {
            log.LogInformation(token);
            return (ActionResult)new OkObjectResult(token);
        }

        [FunctionName(nameof(token2))]
        public static IActionResult token2(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Token(Resource = "https://graph.microsoft.com", Identity = TokenIdentityMode.UserFromId, UserId = "%WEBSITE_AUTH_MSA_CLIENT_ID%")]string token,
            ILogger log)
        {
            log.LogInformation(token);
            return (ActionResult)new OkObjectResult(token);
        }

        [FunctionName(nameof(token3))]
        public static IActionResult token3(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Token(Resource = "https://graph.microsoft.com", Identity = TokenIdentityMode.UserFromToken, UserToken = "{headers.X-MS-TOKEN-MICROSOFTACCOUNT-ACCESS-TOKEN}")]string token,
            ILogger log)
        {
            log.LogInformation(token);
            return (ActionResult)new OkObjectResult(token);
        }

    }
}
