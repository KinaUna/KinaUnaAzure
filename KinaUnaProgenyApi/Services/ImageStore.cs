﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaProgenyApi.Services
{
    public class ImageStore : IImageStore
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _storageKey;
        private readonly string _baseUri;
        private readonly string _cloudBlobUserName;

        public ImageStore(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration.GetValue<string>("BlobStorageConnectionString"));
            _storageKey = configuration.GetValue<string>("BlobStorageKey");
            _baseUri = configuration.GetValue<string>("CloudBlobBase");
            _cloudBlobUserName = configuration.GetValue<string>("CloudBlobUserName");
        }

        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures")
        {
            string imageId = Guid.NewGuid().ToString();
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);

            await blob.UploadAsync(imageStream);
            return imageId;
        }

        public string UriFor(string imageId, string containerName = "pictures")
        {
            if (imageId.ToLower().StartsWith("http"))
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
            if (string.IsNullOrEmpty(imageId) || imageId.ToLower().StartsWith("http"))
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
            List<string> blobStrings = new();
            int linkFound = 1000000000;

            while (linkFound > 0)
            {
                linkFound = originalText.IndexOf(_baseUri, lastIndex, StringComparison.Ordinal);
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
