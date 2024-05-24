using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaMediaApi.Services
{
    public class ImageStore(IConfiguration configuration)
    {
        private readonly BlobServiceClient _blobServiceClient = new BlobServiceClient(configuration.GetValue<string>("BlobStorageConnectionString"));
        private readonly string _storageKey = configuration.GetValue<string>("BlobStorageKey");
        private readonly string _baseUri = configuration.GetValue<string>("CloudBlobBase");
        private readonly string _blobUserName = configuration.GetValue<string>("CloudBlobUsername");


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
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(_blobUserName, _storageKey);
            BlobSasBuilder sas = new BlobSasBuilder
            {
                BlobName = imageId,
                BlobContainerName = containerName,
                StartsOn = DateTime.UtcNow.AddMinutes(-15),
                ExpiresOn = DateTime.UtcNow.AddMinutes(60)
            };

            sas.SetPermissions(BlobAccountSasPermissions.Read);
            UriBuilder sasUri = new UriBuilder($"{_baseUri}{containerName}/{imageId}");
            
            sasUri.Query = sas.ToSasQueryParameters(credential).ToString();

            return sasUri.Uri.AbsoluteUri;
        }

        public async Task<MemoryStream> GetStream(string imageId, string containerName = "pictures")
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);
            MemoryStream memoryStream = new MemoryStream();
            await blob.DownloadToAsync(memoryStream).ConfigureAwait(false);
            
            return memoryStream;
        }

        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blob = container.GetBlobClient(imageId);
            await blob.DeleteIfExistsAsync();

            return imageId;
        }
    }
}
