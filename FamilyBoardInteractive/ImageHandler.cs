using FamilyBoardInteractive.Models;
using Microsoft.AspNetCore.Http;
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
        private static readonly RNGCryptoServiceProvider Randomizer = new RNGCryptoServiceProvider();

        [FunctionName(nameof(RollImageActivity))]
        public static async Task<string> RollImageActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            [Table(Constants.TOKEN_TABLE, partitionKey: Constants.TOKEN_PARTITIONKEY, rowKey: Constants.MSATOKEN_ROWKEY)] TokenEntity msaToken,
            [Blob(Constants.BLOBPATHIMAGESPLAYED, FileAccess.ReadWrite)] CloudBlockBlob imagesPlayedStorageBlob,
            ILogger log)
        {
            var imagesPlayedStorage = await imagesPlayedStorageBlob.DownloadTextAsync();

            string updateImageMessage;

            (imagesPlayedStorage, updateImageMessage) = await ProcessNextImage(msaToken, imagesPlayedStorage);

            await imagesPlayedStorageBlob.UploadTextAsync(imagesPlayedStorage);

            return updateImageMessage;
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
                string tempFile = GetTempFile();

                HttpResponseMessage response = StaticFileServer.ServeFile(tempFile, logger);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static string GetTempFile()
        {
            return Path.Combine(Util.GetImagePath(), "image.png");
        }

        private static async Task<(string, string)> ProcessNextImage(TokenEntity msaToken, string imagesPlayedStorageIn)
        {
            var (imageStream, imagesPlayedStorage) = await GetNextBlobImage(msaToken, JsonConvert.DeserializeObject<ImagesPlayedStorage>(imagesPlayedStorageIn));

            string tempFile = GetTempFile();

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

            return (JsonConvert.SerializeObject(imagesPlayedStorage), imageObject.ToString());
        }

        /// <summary>
        /// Determine the next image to be played
        /// - download set of available images from OneDrive
        /// - merge with the storage of images already played
        /// - balance images played
        /// - get next random image from top x % of available images
        /// </summary>
        /// <param name="msaToken">OneDrive access token</param>
        /// <param name="imagesPlayedStorage">storage of images already played</param>
        /// <returns>stream with image content and updated storage of images already played</returns>
        private static async Task<(Stream, ImagesPlayedStorage)> GetNextBlobImage(TokenEntity msaToken, ImagesPlayedStorage imagesPlayedStorage)
        {
            Stream imageResult = null;
            ImagesPlayedStorage imagesPlayedStorageReturned = null;

            using (var client = new HttpClient())
            {
                var imageListRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(Constants.ONEDRIVEPATH, Util.GetEnvironmentVariable("ONEDRIVE_FOLDER")));
                imageListRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(msaToken.TokenType, msaToken.AccessToken);

                var imageListResponse = await client.SendAsync(imageListRequest);
                if (imageListResponse.IsSuccessStatusCode)
                {
                    var imageListPayload = await imageListResponse.Content.ReadAsStringAsync();
                    var imageList = (JArray)JObject.Parse(imageListPayload)["value"];

                    // merge list returned with images already played
                    var imagesPlayedStorageMerged = MergeImagesPlayed(imageList, imagesPlayedStorage);

                    // sort ascending by count of played
                    imagesPlayedStorageMerged.ImagesPlayed = imagesPlayedStorageMerged.ImagesPlayed.OrderBy(i => i.Count).ThenBy(i => i.LastPlayed).ToList();

                    // balance image played counters
                    var imagesPlayedStorageBalanced = BalanceImages(imagesPlayedStorageMerged);

                    // get random image
                    var (imagePath, imagesPlayedStorageUpdated) = GetRandomImageUrl(imagesPlayedStorageBalanced);

                    imageResult = GetImageContent(imagePath);

                    imagesPlayedStorageReturned = imagesPlayedStorageUpdated;
                }
            }

            return (imageResult, imagesPlayedStorageReturned);
        }

        /// <summary>
        /// Download and transform the image
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns>image content as stream</returns>
        private static Stream GetImageContent(string imagePath)
        {
            Stream imageResult;
            using (var webClient = new WebClient())
            {
                byte[] imageBytes = webClient.DownloadData(imagePath);
                imageBytes = TransformImage(imageBytes);
                imageResult = new MemoryStream(imageBytes);
            }

            return imageResult;
        }

        /// <summary>
        /// Determine URL of next random image to by played
        /// </summary>
        /// <param name="imagesPlayedStorage">set of available images</param>
        /// <returns>URL of image to be played and updated image storage</returns>
        private static (string,ImagesPlayedStorage) GetRandomImageUrl(ImagesPlayedStorage imagesPlayedStorage)
        {
            // take image only from top x %
            int upperBound = ( imagesPlayedStorage.ImagesPlayed.Count * Constants.IMAGES_SELECTION_POOL_TOP_X_PERCENT) / 100;
            if (upperBound > imagesPlayedStorage.ImagesPlayed.Count)
            {
                upperBound = imagesPlayedStorage.ImagesPlayed.Count;
            }

            var randomImageIndex = RandomBetween(0, upperBound - 1);
            imagesPlayedStorage.ImagesPlayed[randomImageIndex].Count++;
            imagesPlayedStorage.ImagesPlayed[randomImageIndex].LastPlayed = DateTime.UtcNow;

            return (imagesPlayedStorage.ImagesPlayed[randomImageIndex].ImageUrl, imagesPlayedStorage);
        }

        /// <summary>
        /// shave down image played counter when all images have been played x times,
        /// (before new images are added)
        /// so that new images do not need to be played so often 
        /// to be in balance with the previous set of images
        /// </summary>
        /// <param name="imagesPlayedStorage">set of available images</param>
        /// <returns>balanced list</returns>
        private static ImagesPlayedStorage BalanceImages(ImagesPlayedStorage imagesPlayedStorage)
        {
            if (imagesPlayedStorage.ImagesPlayed.Count > 0)
            {
                if (imagesPlayedStorage.ImagesPlayed[0].Count > Constants.IMAGES_PLAYED_CUTOFF)
                {
                    foreach (var ip in imagesPlayedStorage.ImagesPlayed)
                    {
                        ip.Count -= Constants.IMAGES_PLAYED_CUTOFF;
                    }
                }
            }

            return imagesPlayedStorage;
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

        /// <summary>
        /// Rotate image
        /// </summary>
        /// <param name="imageInBytes">image content</param>
        /// <returns>rotated image content</returns>
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
