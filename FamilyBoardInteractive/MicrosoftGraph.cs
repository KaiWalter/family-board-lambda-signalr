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
        [FunctionName(nameof(MicrosoftGraphToken))]
        public static IActionResult MicrosoftGraphToken(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Token(Resource = "https://graph.microsoft.com")]string token,
            ILogger log)
        {
            return (ActionResult)new OkObjectResult(token);
        }
    }
}
