using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public static class ImageHandler
    {
        const string ONEDRIVEPATH = "https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/children";

        [FunctionName(nameof(QueuedPushNextImage))]
        public static async Task QueuedPushNextImage(
            [QueueTrigger(Constants.QUEUEMESSAGEPUSHIMAGE)]string queueMessage,
            [Table("Tokens", partitionKey: "Token", rowKey: "MSA")] MSAToken msaToken,
            [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
            ILogger log)
        {
            if (DateTime.UtcNow > msaToken.Expires) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(QueuedPushNextImage)}");
                return;
            }

            var result = await GetNextImage(msaToken, updateImageMessage);
            log.LogTrace(result);
        }

        [FunctionName(nameof(PushNextImage))]
        public static async Task<IActionResult> PushNextImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Table("Tokens", partitionKey: "Token", rowKey: "MSA")] MSAToken msaToken,
            [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
            ILogger log)
        {
            if (DateTime.UtcNow > msaToken.Expires) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(PushNextImage)}");
                return new StatusCodeResult(503);
            }

            var result = await GetNextImage(msaToken, updateImageMessage);

            log.LogTrace(result);

            return new OkObjectResult(result);
        }

        private static async Task<string> GetNextImage(MSAToken msaToken, IAsyncCollector<string> updateImageMessage)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                var imageListRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(ONEDRIVEPATH, "Dakboard"));
                imageListRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(msaToken.TokenType, msaToken.AccessToken);

                var imageListResponse = await client.SendAsync(imageListRequest);
                if (imageListResponse.IsSuccessStatusCode)
                {
                    var imageListString = await imageListResponse.Content.ReadAsStringAsync();
                    var imageList = (JArray)JObject.Parse(imageListString)["value"];

                    var randomImageIndex = new Random().Next(imageList.Count);

                    var imageObject = (JObject)(imageList[randomImageIndex]);
                    var imagePath = imageObject["@microsoft.graph.downloadUrl"].Value<string>();

                    var imageData = new JObject();
                    imageData["path"] = imageObject["@microsoft.graph.downloadUrl"];
                    imageData["specs"] = imageObject["image"];
                    imageData["photoSpecs"] = imageObject["photo"];

                    await updateImageMessage.AddAsync(imageData.ToString());

                    result = $"image pushed {imagePath}";
                }
            }

            return result;
        }
    }
}
