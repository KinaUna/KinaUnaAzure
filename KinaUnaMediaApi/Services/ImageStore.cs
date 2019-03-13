using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaMediaApi.Services
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
    }
}
