using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public class ImageStore(IConfiguration configuration)
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
            try
            {
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

                BlobClient blob = container.GetBlobClient(imageId);
                MemoryStream memoryStream = new();
                await blob.DownloadToAsync(memoryStream).ConfigureAwait(false);

                return memoryStream;
            }
            catch (RequestFailedException)
            {
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(BlobContainers.Pictures);

                BlobClient blob = container.GetBlobClient("ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png");
                MemoryStream memoryStream = new();
                await blob.DownloadToAsync(memoryStream).ConfigureAwait(false);

                return memoryStream;
            }
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

    }
}
