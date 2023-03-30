using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;

namespace KinaUnaWeb.Services
{
    public class ImageStore
    {
        readonly CloudBlobClient blobClient;
        private readonly string _baseUri;

        public ImageStore(IConfiguration configuration)
        {
            _baseUri = configuration.GetValue<string>("CloudBlobBase");
            string cloudBlobUserName = configuration.GetValue<string>("CloudBlobUserName");
            StorageCredentials credentials = new(cloudBlobUserName, configuration.GetValue<string>("BlobStorageKey"));
            blobClient = new CloudBlobClient(new Uri(_baseUri), credentials);
        }

        /// <summary>
        /// Saves an image file to the storage account specified in Secrets: CloudBlobBase.
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
            if (string.IsNullOrEmpty(imageId) || imageId.ToLower().StartsWith("http"))
            {
                if (string.IsNullOrEmpty(imageId))
                {
                    imageId = Constants.ProfilePictureUrl;
                }

                return imageId;
            }

            SharedAccessBlobPolicy sasPolicy = new()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(60)
            };

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
            string sas = blob.GetSharedAccessSignature(sasPolicy);
            return $"{_baseUri}{containerName}/{imageId}{sas}";
        }

        /// <summary>
        /// Removes an image from the Azure Storage Blob.
        /// </summary>
        /// <param name="imageId">string: The image Id (Picture.PictureLink).</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The image Id (Picture.PictureLink).</returns>
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

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
            await blob.DeleteIfExistsAsync();
            return imageId;
        }
    }
}
