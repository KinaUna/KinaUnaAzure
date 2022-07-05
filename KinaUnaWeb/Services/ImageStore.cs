using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;

namespace KinaUnaWeb.Services
{
    public class ImageStore
    {
        CloudBlobClient blobClient;
        string baseUri = Constants.CloudBlobBase;
        
        public ImageStore(IConfiguration configuration)
        {
            StorageCredentials credentials = new StorageCredentials(Constants.CloudBlobUsername, configuration["BlobStorageKey"]);
            blobClient = new CloudBlobClient(new Uri(baseUri), credentials);
        }

        /// <summary>
        /// Saves an image file to the storage account specified in Constants.CloudBlobBase.
        /// </summary>
        /// <param name="imageStream">Stream: The stream for the image file.</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The file name (Picture.PictureLink)</returns>
        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures")
        {
            string imageId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
            await blob.UploadFromStreamAsync(imageStream);
            return imageId;
        }

        /// <summary>
        /// Gets the URI, including access signature, for an image stored in an Azure Storage Blob container.
        /// </summary>
        /// <param name="imageId">string: The image Id (Picture.PictureLink).</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The URI for the image.</returns>
        public string UriFor(string imageId, string containerName = "pictures")
        {
            SharedAccessBlobPolicy sasPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(60)
            };

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
            string sas = blob.GetSharedAccessSignature(sasPolicy);
            return $"{baseUri}{containerName}/{imageId}{sas}";
        }

        /// <summary>
        /// Removes an image from the Azure Storage Blob.
        /// </summary>
        /// <param name="imageId">string: The image Id (Picture.PictureLink).</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The image Id (Picture.PictureLink).</returns>
        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
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
                    int secondSlash = blobString.IndexOf("/", firstSlash + 1, StringComparison.Ordinal);
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
