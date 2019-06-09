using FamilyBoardInteractive.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FamilyBoardInteractive
{
    public class Storage
    {
        [FunctionName(nameof(QueuedConfiguartionCheck))]
        [Singleton(Mode = SingletonMode.Listener)]
        public static async Task QueuedConfiguartionCheck(
                [QueueTrigger(Constants.QUEUEMESSAGECONFIGURUATION)]string queueMessage,
                [Blob(Constants.BLOBPATHCONTAINER, FileAccess.ReadWrite)] CloudBlobContainer storageContainer,
                ILogger log)
        {
            await CheckStorageConfiguration(storageContainer, log);
        }

        /// <summary>
        /// Check the storage configuration
        /// </summary>
        /// <param name="storageContainer">reference to Blob Storage Container</param>
        /// <returns></returns>
        private static async Task CheckStorageConfiguration(CloudBlobContainer storageContainer, ILogger log)
        {
            if (await storageContainer.CreateIfNotExistsAsync())
            {
                log.LogInformation($"creating container {Constants.BLOBPATHCONTAINER}");
                await storageContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off });
            }

            string blobName = Constants.BLOBPATHIMAGESPLAYED.Replace(Constants.BLOBPATHCONTAINER + "/", "");
            var blob = storageContainer.GetBlockBlobReference(blobName);

            if (!await blob.ExistsAsync())
            {
                log.LogInformation($"creating blob {Constants.BLOBPATHIMAGESPLAYED}");

                blob.Properties.ContentType = Constants.BLOBCONTENTTYPEIMAGESPLAYED;

                var imagesPlayedStorage = new ImagesPlayedStorage { ImagesPlayed = new List<ImagePlayed>() };

                string blobContent = JsonConvert.SerializeObject(imagesPlayedStorage);

                await blob.UploadTextAsync(blobContent);
            }
        }


    }
}
