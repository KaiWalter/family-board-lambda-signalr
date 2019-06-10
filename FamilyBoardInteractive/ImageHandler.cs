using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;

namespace FamilyBoardInteractive
{
    public static class ImageHandler
    {
        const string ONEDRIVEPATH = "https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/children";
        private static readonly RNGCryptoServiceProvider Randomizer = new RNGCryptoServiceProvider();

        [FunctionName(nameof(PushNextImage))]
        public static async Task<IActionResult> PushNextImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
            [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
            [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
            [Blob(Constants.BLOBPATHIMAGESPLAYED, FileAccess.ReadWrite)] CloudBlockBlob imagesPlayedStorageBlob,
            ILogger log)
        {
            if (msaToken.NeedsRefresh) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(PushNextImage)}");
                return new StatusCodeResult(503);
            }

            var imagesPlayedStorage = await imagesPlayedStorageBlob.DownloadTextAsync();

            imagesPlayedStorage = await ProcessNextImage(msaToken, updateImageMessage, imagesPlayedStorage);

            await imagesPlayedStorageBlob.UploadTextAsync(imagesPlayedStorage);

            return new OkResult();
        }

        [FunctionName(nameof(QueuedPushNextImage))]
        [Singleton(Mode = SingletonMode.Listener)]
        public static async Task QueuedPushNextImage(
                    [QueueTrigger(Constants.QUEUEMESSAGEPUSHIMAGE)]string queueMessage,
                    [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
                    [Queue(Constants.QUEUEMESSAGEREFRESHMSATOKEN)] IAsyncCollector<string> refreshTokenMessage,
                    [Queue(Constants.QUEUEMESSAGEUPDATEIMAGE)] IAsyncCollector<string> updateImageMessage,
                    [Blob(Constants.BLOBPATHIMAGESPLAYED, FileAccess.ReadWrite)] CloudBlockBlob imagesPlayedStorageBlob,
                    ILogger log)
        {
            if (msaToken.NeedsRefresh) // token invalid
            {
                await refreshTokenMessage.AddAsync($"initiated by {nameof(QueuedPushNextImage)}");
                return;
            }

            var imagesPlayedStorage = await imagesPlayedStorageBlob.DownloadTextAsync();

            imagesPlayedStorage = await ProcessNextImage(msaToken, updateImageMessage, imagesPlayedStorage);

            await imagesPlayedStorageBlob.UploadTextAsync(imagesPlayedStorage);
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
                string tempFile = Path.Combine(Util.GetImagePath(), "image.png");

                HttpResponseMessage response = StaticFileServer.ServeFile(tempFile, logger);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static async Task<string> ProcessNextImage(TokenEntity msaToken, IAsyncCollector<string> updateImageMessage, string imagesPlayedStorageIn)
        {
            var (imageStream, imagesPlayedStorage) = await GetNextBlobImage(msaToken, JsonConvert.DeserializeObject<ImagesPlayedStorage>(imagesPlayedStorageIn));

            string tempFile = Path.Combine(Util.GetImagePath(), "image.png");

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

            return JsonConvert.SerializeObject(imagesPlayedStorage);
        }

        private static async Task<(Stream, ImagesPlayedStorage)> GetNextBlobImage(TokenEntity msaToken, ImagesPlayedStorage imagesPlayedStorage)
        {
            Stream imageResult = null;
            ImagesPlayedStorage imagesPlayedStorageNew = null;

            using (var client = new HttpClient())
            {
                var imageListRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(ONEDRIVEPATH, "Dakboard"));
                imageListRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(msaToken.TokenType, msaToken.AccessToken);

                var imageListResponse = await client.SendAsync(imageListRequest);
                if (imageListResponse.IsSuccessStatusCode)
                {
                    var imageListPayload = await imageListResponse.Content.ReadAsStringAsync();
                    var imageList = (JArray)JObject.Parse(imageListPayload)["value"];

                    // merge list returned with images already played
                    imagesPlayedStorageNew = MergeImagesPlayed(imageList, imagesPlayedStorage);

                    // take image only from top 1/3rd
                    imagesPlayedStorageNew.ImagesPlayed = imagesPlayedStorageNew.ImagesPlayed.OrderBy(i => i.Count).ThenBy(i => i.LastPlayed).ToList();
                    int upperBound = imagesPlayedStorageNew.ImagesPlayed.Count / 3;
                    if (upperBound > imagesPlayedStorageNew.ImagesPlayed.Count)
                    {
                        upperBound = imagesPlayedStorageNew.ImagesPlayed.Count;
                    }

                    var randomImageIndex = RandomBetween(0, upperBound - 1);

                    var imagePath = imagesPlayedStorageNew.ImagesPlayed[randomImageIndex].ImageUrl;

                    using (var webClient = new WebClient())
                    {
                        byte[] imageBytes = webClient.DownloadData(imagePath);
                        imageBytes = TransformImage(imageBytes);
                        imageResult = new MemoryStream(imageBytes);
                    }

                    imagesPlayedStorageNew.ImagesPlayed[randomImageIndex].Count++;
                    imagesPlayedStorageNew.ImagesPlayed[randomImageIndex].LastPlayed = DateTime.UtcNow;
                }
            }

            return (imageResult, imagesPlayedStorageNew);
        }

        /// <summary>
        /// merge list returned with images already played
        /// </summary>
        /// <param name="imageList">list of images returned from service</param>
        /// <param name="imagesPlayedStorage">storage of images already played</param>
        /// <returns>merged list</returns>
        private static ImagesPlayedStorage MergeImagesPlayed(JArray imageList, ImagesPlayedStorage imagesPlayedStorage)
        {
            var imagesPlayedStorageResult = new ImagesPlayedStorage { ImagesPlayed = new List<ImagePlayed>() };

            foreach (var imageToken in imageList)
            {
                JObject imageObject = (JObject)imageToken;

                if (imageObject["name"] == null || imageObject["@microsoft.graph.downloadUrl"] == null || imageObject["file"] == null)
                {

                }
                else
                {
                    var imageName = imageObject["name"].Value<string>();
                    var imagePath = imageObject["@microsoft.graph.downloadUrl"].Value<string>();
                    var imageFile = (JObject)imageObject["file"];
                    var imageMimeType = imageFile["mimeType"].Value<string>();

                    if (imageMimeType.CompareTo("image/jpeg") == 0)
                    {
                        var imagePlayed = imagesPlayedStorage.ImagesPlayed.FirstOrDefault(i => i.ImageName == imageName);

                        if (imagePlayed == null)
                        {
                            imagePlayed = new ImagePlayed()
                            {
                                ImageName = imageName,
                                ImageUrl = imagePath,
                                Count = 0
                            };
                        }
                        else
                        {
                            imagePlayed.ImageUrl = imagePath;
                        }

                        imagesPlayedStorageResult.ImagesPlayed.Add(imagePlayed);
                    }
                }
            }

            return imagesPlayedStorageResult;
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

        /// <summary>
        /// https://scottlilly.com/create-better-random-numbers-in-c/
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        private static int RandomBetween(int minimumValue, int maximumValue)
        {
            byte[] randomNumber = new byte[1];

            Randomizer.GetBytes(randomNumber);

            double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            // We are using Math.Max, and substracting 0.00000000001, 
            // to ensure "multiplier" will always be between 0.0 and .99999999999
            // Otherwise, it's possible for it to be "1", which causes problems in our rounding.
            double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

            // We need to add one to the range, to allow for the rounding done with Math.Floor
            int range = maximumValue - minimumValue + 1;

            double randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }
    }
}
