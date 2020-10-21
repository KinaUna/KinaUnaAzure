using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

namespace KinaUnaProgenyApi.Services
{
    public class ImageStore
    {
        CloudBlobClient blobClient;
        string baseUri = Constants.CloudBlobBase;
        
        public ImageStore(IConfiguration configuration)
        {
            var credentials = new StorageCredentials(Constants.CloudBlobUsername, configuration["BlobStorageKey"]);
            blobClient = new CloudBlobClient(new Uri(baseUri), credentials);
        }

        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures")
        {
            var imageId = Guid.NewGuid().ToString();
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
            await blob.UploadFromStreamAsync(imageStream);
            return imageId;
        }

        public string UriFor(string imageId, string containerName = "pictures")
        {
            var sasPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(60)
            };

            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
            var sas = blob.GetSharedAccessSignature(sasPolicy);
            return $"{baseUri}{containerName}/{imageId}{sas}";
        }

        public async Task<MemoryStream> GetStream(string imageId, string containerName = "pictures")
        {
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
            var memoryStream = new MemoryStream();
            await blob.DownloadToStreamAsync(memoryStream).ConfigureAwait(false);
            return memoryStream;
        }

        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
            await blob.DeleteIfExistsAsync();
            return imageId;
        }

        /// <summary>
        /// Refreshes blob storage links in a text.
        /// </summary>
        /// <param name="originalText">string: The text to update links for.</param>
        /// <returns>string: The text with updated links.</returns>
        public string UpdateBlobLinks(string originalText)
        {
            if (string.IsNullOrEmpty(originalText))
            {
                originalText = "";
            }
            string updatedText = originalText;
            int lastIndex = 0;
            List<string> blobStrings = new List<string>();
            int linkFound = 1000000000;
            while (linkFound > 0)
            {
                linkFound = originalText.IndexOf("https://kinaunastorage.blob.core.windows.net", lastIndex, StringComparison.Ordinal);
                if (linkFound > 0)
                {
                    int linkEnd = originalText.IndexOf("sp=r", linkFound, StringComparison.Ordinal) + 4;
                    blobStrings.Add(originalText.Substring(linkFound, linkEnd - linkFound));
                    lastIndex = linkEnd;
                }
                
            }

            if (blobStrings.Any())
            {
                foreach (string blobString in blobStrings)
                {
                    int firstSlash = blobString.IndexOf("/", 15, StringComparison.Ordinal);
                    int secondSlash = blobString.IndexOf("/", firstSlash +1, StringComparison.Ordinal);
                    int firstQuestionmark = blobString.IndexOf("?", StringComparison.Ordinal);
                    string container = blobString.Substring(firstSlash, secondSlash - firstSlash).Replace("/", "");
                    string blobId = blobString.Substring(secondSlash, firstQuestionmark - secondSlash).Replace("/", "").Replace("?", "");
                    string updatedBlobUri = UriFor(blobId, container);
                    updatedText = updatedText.Replace(blobString, updatedBlobUri);
                }
            }

            return updatedText;
        }
    }
}
