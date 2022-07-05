using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaMediaApi.Services
{
    public class ImageStore
    {
        private BlobServiceClient _blobServiceClient;
        private string _storageKey;
        string baseUri = Constants.CloudBlobBase;
        
        public ImageStore(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["BlobStorageConnectionString"]);
            _storageKey = configuration["BlobStorageKey"];
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
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(Constants.CloudBlobUsername, _storageKey);
            BlobSasBuilder sas = new BlobSasBuilder
            {
                BlobName = imageId,
                BlobContainerName = containerName,
                StartsOn = DateTime.UtcNow.AddMinutes(-15),
                ExpiresOn = DateTime.UtcNow.AddMinutes(60)
            };

            sas.SetPermissions(BlobAccountSasPermissions.Read);
            UriBuilder sasUri = new UriBuilder($"{baseUri}{containerName}/{imageId}");
            
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
