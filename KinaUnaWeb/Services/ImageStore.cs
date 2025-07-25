using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using KinaUna.Data;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Provides methods for saving, retrieving and deleting images from Azure Blob Storage.
    /// </summary>
    /// <param name="configuration"></param>
    public class ImageStore(IConfiguration configuration)
    {
        private readonly BlobServiceClient _blobServiceClient = new(configuration.GetValue<string>("BlobStorageConnectionString"));
        private readonly string _storageKey = configuration.GetValue<string>("BlobStorageKey");
        private readonly string _baseUri = configuration.GetValue<string>("CloudBlobBase");
        private readonly string _cloudBlobUserName = configuration.GetValue<string>("CloudBlobUserName");

        /// <summary>
        /// Saves a stream of image data to Azure Blob Storage.
        /// </summary>
        /// <param name="imageStream">The image data stream to save.</param>
        /// <param name="containerName">The name of the container to store the image in.</param>
        /// <param name="fileFormat">The file extension to save the file with.</param>
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
        /// Gets a URI for an image in Azure Blob Storage.
        /// SAS tokens expire after a limited time, so the URI is only valid for a short time.
        /// </summary>
        /// <param name="imageId">The file name of the image to get a URL for.</param>
        /// <param name="containerName">The name of the container the image file is stored in.</param>
        /// <returns>String with the URL for the image.</returns>
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
        /// Gets a stream of image data from Azure Blob Storage.
        /// </summary>
        /// <param name="imageId">The file name of the image.</param>
        /// <param name="containerName">The name of the container the image file is stored in.</param>
        /// <returns>MemoryStream with the image data. If the image file isn't found a default dummy file is used.</returns>
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

        /// <summary>
        /// Deletes an image from Azure Blob Storage.
        /// </summary>
        /// <param name="imageId">The file name of the image.</param>
        /// <param name="containerName">The container the image file is stored in.</param>
        /// <returns>The filename of the deleted file. If the imageId isn't a file name nothing is deleted and the default ProfilePictureUrl is returned instead.</returns>
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
