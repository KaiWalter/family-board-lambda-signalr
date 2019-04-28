using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace FamilyBoardInteractive
{
    public static class ImageHandler
    {
        const string ONEDRIVEPATH = "https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/children";

        [FunctionName(nameof(PushNextImage))]
        public static async Task<IActionResult> PushNextImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] MSAToken msaToken,
            [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
            ILogger log)
        {
            if (DateTime.UtcNow > msaToken.Expires) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(PushNextImage)}");
                return new StatusCodeResult(503);
            }

            await ProcessNextImage(msaToken, updateImageMessage);

            return new OkResult();
        }

        [FunctionName(nameof(QueuedPushNextImage))]
        [Singleton(Mode = SingletonMode.Listener)]
        public static async Task QueuedPushNextImage(
            [QueueTrigger(Constants.QUEUEMESSAGEPUSHIMAGE)]string queueMessage,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] MSAToken msaToken,
            [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
            ILogger log)
        {
            if (DateTime.UtcNow > msaToken.Expires) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(QueuedPushNextImage)}");
                return;
            }

            await ProcessNextImage(msaToken, updateImageMessage);
        }

        [FunctionName("ImageServer")]
        public static HttpResponseMessage ImageServer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            ILogger logger)
        {
            // check key
            var keyEncrypted = req.Query.FirstOrDefault(q => string.Compare(q.Key, "key", true) == 0).Value[0];
            var keyDecrypted = Services.Encrypt.DecryptString(keyEncrypted, 
                initVector: Util.GetEnvironmentVariable("ENCRYPTION_INITVECTOR"),
                passPhrase: Util.GetEnvironmentVariable("IMAGE_PASSPHRASE"));
            var expiration = DateTime.Parse(keyDecrypted);
            if (DateTime.UtcNow > expiration)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "FamilyBoard");
                string tempFile = Path.Combine(tempDir, "image.png");

                HttpResponseMessage response = StaticFileServer.ServeFile(tempFile, logger);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static async Task ProcessNextImage(MSAToken msaToken, IAsyncCollector<string> updateImageMessage)
        {
            var imageStream = await GetNextBlobImage(msaToken);

            string tempDir = Path.Combine(Path.GetTempPath(), "FamilyBoard");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            string tempFile = Path.Combine(tempDir, "image.png");

            using (FileStream fileOutputStream = new FileStream(tempFile, FileMode.Create))
            {
                imageStream.CopyTo(fileOutputStream);
            }

            string key = Services.Encrypt.EncryptString(DateTime.UtcNow.AddMinutes(1).ToString("u"), 
                initVector: Util.GetEnvironmentVariable("ENCRYPTION_INITVECTOR"),
                passPhrase: Util.GetEnvironmentVariable("IMAGE_PASSPHRASE"));

            var imageObject = new JObject()
            {
                { "path", $"/api/ImageServer?key={HttpUtility.UrlEncode(key)}" }
            };

            await updateImageMessage.AddAsync(imageObject.ToString());
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
                    var imageListPayload = await imageListResponse.Content.ReadAsStringAsync();
                    var imageList = (JArray)JObject.Parse(imageListPayload)["value"];

                    JObject imageObject = FindRandomImage(imageList);

                    var imagePath = imageObject["@microsoft.graph.downloadUrl"].Value<string>();

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
