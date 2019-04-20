using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
            [Blob("images/boardimage.jpg",access: FileAccess.Write)] CloudBlockBlob outputBlob,
            ILogger log)
        {
            if (DateTime.UtcNow > msaToken.Expires) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(PushNextImage)}");
                return new StatusCodeResult(503);
            }

            var result = await GetNextImage(msaToken, updateImageMessage);
            outputBlob.Properties.ContentType = "image/jpeg";
            await outputBlob.UploadFromStreamAsync(await GetNextBlobImage(msaToken));

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

                    JObject imageObject = FindRandomImage(imageList);

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

        private static JObject FindRandomImage(JArray imageList)
        {
            string imageMimeType = string.Empty;
            JObject imageObject = null;

            while (imageMimeType.CompareTo("image/jpeg") != 0)
            {
                var randomImageIndex = new Random().Next(imageList.Count);
                imageObject = (JObject)(imageList[randomImageIndex]);
                var imageFile = (JObject)imageObject["file"];
                imageMimeType = imageFile["mimeType"].Value<string>();
            }

            return imageObject;
        }

        private static async Task<Stream> GetNextBlobImage(MSAToken msaToken)
        {
            Stream imageResult = null;

            using (var client = new HttpClient())
            {
                var imageListRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(ONEDRIVEPATH, "Dakboard"));
                imageListRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(msaToken.TokenType, msaToken.AccessToken);

                var imageListResponse = await client.SendAsync(imageListRequest);
                if (imageListResponse.IsSuccessStatusCode)
                {
                    var imageListString = await imageListResponse.Content.ReadAsStringAsync();
                    var imageList = (JArray)JObject.Parse(imageListString)["value"];

                    JObject imageObject = FindRandomImage(imageList);

                    var imagePath = imageObject["@microsoft.graph.downloadUrl"].Value<string>();

                    var imageData = new JObject();
                    imageData["path"] = imageObject["@microsoft.graph.downloadUrl"];
                    imageData["specs"] = imageObject["image"];
                    imageData["photoSpecs"] = imageObject["photo"];

                    using (var webClient = new WebClient())
                    {
                        byte[] imageBytes = webClient.DownloadData(imagePath);
                        imageBytes = TransformImage(imageBytes);
                        imageResult = new MemoryStream(imageBytes);
                    }
                }
            }

            return imageResult;
        }

        private static byte[] TransformImage(byte[] imageInBytes)
        {
            using (var image = Image.Load(imageInBytes))
            {
                ExifValue exifOrientation = image.MetaData?.ExifProfile?.GetValue(ExifTag.Orientation);

                if (exifOrientation == null) return imageInBytes;

                RotateMode rotateMode;
                FlipMode flipMode;
                SetRotateFlipMode(exifOrientation, out rotateMode, out flipMode);

                image.Mutate(x => x.RotateFlip(rotateMode, flipMode));
                image.MetaData.ExifProfile.SetValue(ExifTag.Orientation, (ushort)1);

                var imageFormat = Image.DetectFormat(imageInBytes);

                return ImageToByteArray(image, imageFormat);
            }
        }

        private static byte[] ImageToByteArray(Image<Rgba32> image, IImageFormat imageFormat)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, imageFormat);
                return ms.ToArray();
            }
        }

        private static void SetRotateFlipMode(ExifValue exifOrientation, out RotateMode rotateMode, out FlipMode flipMode)
        {
            var orientation = exifOrientation.Value.ToString();

            switch (orientation)
            {
                case "2":
                    rotateMode = RotateMode.None;
                    flipMode = FlipMode.Horizontal;
                    break;
                case "3":
                    rotateMode = RotateMode.Rotate180;
                    flipMode = FlipMode.None;
                    break;
                case "4":
                    rotateMode = RotateMode.Rotate180;
                    flipMode = FlipMode.Horizontal;
                    break;
                case "5":
                    rotateMode = RotateMode.Rotate90;
                    flipMode = FlipMode.Horizontal;
                    break;
                case "6":
                    rotateMode = RotateMode.Rotate90;
                    flipMode = FlipMode.None;
                    break;
                case "7":
                    rotateMode = RotateMode.Rotate90;
                    flipMode = FlipMode.Vertical;
                    break;
                case "8":
                    rotateMode = RotateMode.Rotate270;
                    flipMode = FlipMode.None;
                    break;
                default:
                    rotateMode = RotateMode.None;
                    flipMode = FlipMode.None;
                    break;
            }
        }
    }
}
