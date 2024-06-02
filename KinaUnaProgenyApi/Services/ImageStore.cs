using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaProgenyApi.Services
{
    public class ImageStore(IConfiguration configuration) : IImageStore
    {
        private readonly BlobServiceClient _blobServiceClient = new(configuration.GetValue<string>("BlobStorageConnectionString"));
        private readonly string _storageKey = configuration.GetValue<string>("BlobStorageKey");
        private readonly string _baseUri = configuration.GetValue<string>("CloudBlobBase");
        private readonly string _cloudBlobUserName = configuration.GetValue<string>("CloudBlobUserName");

        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures", string fileFormat = ".jpg")
        {
            string imageId = Guid.NewGuid() + fileFormat;
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);

            await blob.UploadAsync(imageStream);
            return imageId;
        }

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

        public async Task<MemoryStream> GetStream(string imageId, string containerName = "pictures")
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);
            MemoryStream memoryStream = new();
            await blob.DownloadToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream;
        }

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
    }
}
