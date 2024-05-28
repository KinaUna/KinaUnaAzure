using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace KinaUnaWebBlazor.Services
{
    public class ImageStore(IConfiguration configuration)
    {
        private readonly BlobServiceClient _blobServiceClient = new(configuration["kinaunastorageconnectionstring"]);

        /// <summary>
        /// Saves an image file to the storage account specified in Constants.CloudBlobBase.
        /// </summary>
        /// <param name="imageStream">Stream: The stream for the image file.</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The file name (Picture.PictureLink)</returns>
        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures")
        {
            string imageId = Guid.NewGuid().ToString();
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(imageId);
            await blobClient.UploadAsync(imageStream, true);
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
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = containerName,
                Resource = "b",
                BlobName = imageId,
                StartsOn = DateTime.UtcNow.AddMinutes(-15),
                ExpiresOn = DateTime.UtcNow.AddMinutes(60)
            };

            sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(imageId);
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            return sasUri.AbsoluteUri;
        }

        /// <summary>
        /// Removes an image from the Azure Storage Blob.
        /// </summary>
        /// <param name="imageId">string: The image Id (Picture.PictureLink).</param>
        /// <param name="containerName">string: The name of the Azure Storage Blob Container. Default: pictures</param>
        /// <returns>string: The image Id (Picture.PictureLink).</returns>
        public async Task<string> DeleteImage(string imageId, string containerName = "pictures")
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(imageId);
            await blobClient.DeleteAsync();
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
                linkFound = originalText.IndexOf("https://kinaunastorage.blob.core.windows.net", lastIndex, StringComparison.Ordinal);
                if (linkFound <= 0) continue;

                int linkEnd = originalText.IndexOf("sp=r", linkFound, StringComparison.Ordinal) + 4;
                blobStrings.Add(originalText[linkFound..linkEnd]);
                lastIndex = linkEnd;

            }

            if (blobStrings.Count == 0) return updatedText;

            foreach (string blobString in blobStrings)
            {
                int firstSlash = blobString.IndexOf('/', 15);
                int secondSlash = blobString.IndexOf('/', 15);
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
