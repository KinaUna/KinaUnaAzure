using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public class ImageStore(IConfiguration configuration) : IImageStore
    {
        private readonly BlobServiceClient _blobServiceClient = new(configuration.GetValue<string>("BlobStorageConnectionString"));
        private readonly string _storageKey = configuration.GetValue<string>("BlobStorageKey");
        private readonly string _baseUri = configuration.GetValue<string>("CloudBlobBase");
        private readonly string _cloudBlobUserName = configuration.GetValue<string>("CloudBlobUserName");

        /// <summary>
        /// Saves an image file Stream to a blob storage.
        /// The file name is a new GUID.
        /// </summary>
        /// <param name="imageStream">The Stream containing the file data.</param>
        /// <param name="containerName">The Name of the storage container to save the file to.</param>
        /// <param name="fileFormat">The file extension of the file.</param>
        /// <returns>The filename of the saved file.</returns>
        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures", string fileFormat = ".jpg")
        {
            string imageId = Guid.NewGuid() + fileFormat;
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);

            await blob.UploadAsync(imageStream);
            return imageId;
        }

        /// <summary>
        /// Gets a SAS URI for a file/blob in a storage container.
        /// </summary>
        /// <param name="imageId">The filename of the file.</param>
        /// <param name="containerName">The storage container's name.</param>
        /// <returns>String with the URI for the file.</returns>
        public string UriFor(string imageId, string containerName = "pictures")
        {
            if (imageId.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(imageId))
                {
                    imageId = Constants.ProfilePictureUrl;
                }

                return imageId;
            }

            StorageSharedKeyCredential credential = new(_cloudBlobUserName, _storageKey);
            BlobSasBuilder sas = new()
            {
                BlobName = imageId,
                BlobContainerName = containerName,
                StartsOn = DateTime.UtcNow.AddMinutes(-15),
                ExpiresOn = DateTime.UtcNow.AddMinutes(60)
            };

            sas.SetPermissions(BlobAccountSasPermissions.Read);
            UriBuilder sasUri = new($"{_baseUri}{containerName}/{imageId}")
            {
                Query = sas.ToSasQueryParameters(credential).ToString()
            };

            return sasUri.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Gets a Stream for a file/blob in a storage container.
        /// </summary>
        /// <param name="imageId">The filename of the file to get a stream for.</param>
        /// <param name="containerName">The name of the storage container for the file.</param>
        /// <returns>MemoryStream for the file.</returns>
        public async Task<MemoryStream> GetStream(string imageId, string containerName = "pictures")
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);
            MemoryStream memoryStream = new();
            await blob.DownloadToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream;
        }

        /// <summary>
        /// Deletes a file/blob from a storage container.
        /// </summary>
        /// <param name="imageId">The filename of the file to delete.</param>
        /// <param name="containerName">The name of the container for the file.</param>
        /// <returns>String with the filename.</returns>
        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            if (string.IsNullOrEmpty(imageId) || imageId.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(imageId))
                {
                    imageId = Constants.ProfilePictureUrl;
                }

                return imageId;
            }

            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);
            await blob.DeleteIfExistsAsync();

            return imageId;
        }

        /// <summary>
        /// Refreshes blob storage links in a text.
        /// SAS links are valid for a limited time, so they need to be updated.
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
            List<string> blobStrings = [];
            int linkFound = 1000000000;

            while (linkFound > 0)
            {
                linkFound = originalText.IndexOf(_baseUri, lastIndex, StringComparison.Ordinal);
                if (linkFound <= 0) continue;

                int linkEnd = originalText.IndexOf("sp=r", linkFound, StringComparison.Ordinal) + 4;
                blobStrings.Add(originalText[linkFound..linkEnd]);
                lastIndex = linkEnd;
            }

            if (blobStrings.Count == 0) return updatedText;

            foreach (string blobString in blobStrings)
            {
                int firstSlash = blobString.IndexOf('/', 15);
                int secondSlash = blobString.IndexOf('/', firstSlash + 1);
                int firstQuestionmark = blobString.IndexOf('?');
                string container = blobString[firstSlash..secondSlash].Replace("/", "");
                string blobId = blobString[secondSlash..firstQuestionmark].Replace("/", "").Replace("?", "");
                string updatedBlobUri = UriFor(blobId, container);
                updatedText = updatedText.Replace(blobString, updatedBlobUri);
            }

            return updatedText;
        }

        public async Task<bool> ImageExists(string imageId, string containerName = BlobContainers.Pictures)
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(imageId);
            return await blob.ExistsAsync();
        }
    }
}
