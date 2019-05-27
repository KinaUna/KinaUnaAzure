using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaWeb.Services
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

        /// <summary>
        /// Saves an image file to the storage account specified in Constants.CloudBlobBase.
        /// </summary>
        /// <param name="imageStream">Stream: The stream for the image file.</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The file name (Picture.PictureLink)</returns>
        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures")
        {
            var imageId = Guid.NewGuid().ToString();
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
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

        /// <summary>
        /// Removes an image from the Azure Storage Blob.
        /// </summary>
        /// <param name="imageId">string: The image Id (Picture.PictureLink).</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The image Id (Picture.PictureLink).</returns>
        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(imageId);
            await blob.DeleteIfExistsAsync();
            return imageId;
        }
    }
}
