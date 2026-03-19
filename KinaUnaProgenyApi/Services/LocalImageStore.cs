using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Local filesystem implementation of IImageStore.
    /// Files are stored under a configurable base path, organized by container subdirectories.
    /// Drop-in replacement for the Azure Blob Storage ImageStore.
    /// </summary>
    public class LocalImageStore : IImageStore
    {
        private readonly string _basePath;
        private readonly string _baseUri;
        public LocalImageStore(IConfiguration configuration)
        {
            _basePath = configuration.GetValue<string>("LocalStorageBasePath")
                        ?? Path.Combine(AppContext.BaseDirectory, "storage");
            _baseUri = configuration.GetValue<string>("CloudBlobBase");
        }

        private string GetContainerPath(string containerName)
        {
            string containerPath = Path.Combine(_basePath, containerName);
            Directory.CreateDirectory(containerPath);
            return containerPath;
        }

        /// <summary>
        /// Saves an image file Stream to the local filesystem.
        /// The file name is a new GUID.
        /// </summary>
        /// <param name="imageStream">The Stream containing the file data.</param>
        /// <param name="containerName">The Name of the storage container (subdirectory) to save the file to.</param>
        /// <param name="fileFormat">The file extension of the file.</param>
        /// <returns>The filename of the saved file.</returns>
        public async Task<string> SaveImage(Stream imageStream, string containerName = "pictures", string fileFormat = ".jpg")
        {
            string imageId = Guid.NewGuid() + fileFormat;
            string filePath = Path.Combine(GetContainerPath(containerName), imageId);

            imageStream.Position = 0;
            await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            await imageStream.CopyToAsync(fileStream);

            return imageId;
        }

        /// <summary>
        /// Gets a URI for a file. For local storage, this returns the imageId unchanged
        /// since files are served through API endpoints using GetStream().
        /// This method is obsolete — file-serving endpoints use GetStream() directly.
        /// </summary>
        /// <param name="imageId">The filename of the file.</param>
        /// <param name="containerName">The storage container's name.</param>
        /// <returns>The imageId unchanged, or Constants.ProfilePictureUrl if empty.</returns>
        public string UriFor(string imageId, string containerName = "pictures")
        {
            if (string.IsNullOrEmpty(imageId))
            {
                return Constants.ProfilePictureUrl;
            }

            if (imageId.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                return imageId;
            }

            // For local storage, return the imageId as-is.
            // File-serving endpoints use GetStream() to serve the actual file data.
            return imageId;
        }

        /// <summary>
        /// Gets a Stream for a file on the local filesystem.
        /// </summary>
        /// <param name="imageId">The filename of the file to get a stream for.</param>
        /// <param name="containerName">The name of the storage container (subdirectory) for the file.</param>
        /// <returns>MemoryStream for the file.</returns>
        public async Task<MemoryStream> GetStream(string imageId, string containerName = "pictures")
        {
            string filePath = Path.Combine(GetContainerPath(containerName), imageId);
            // Check if file exists before trying to open it
            if (!File.Exists(filePath))
            {
                // Return a default image if the requested file doesn't exist
                filePath = Path.Combine(GetContainerPath(BlobContainers.Pictures), "ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png");
            }

            MemoryStream memoryStream = new();
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }

        /// <summary>
        /// Deletes a file from the local filesystem.
        /// </summary>
        /// <param name="imageId">The filename of the file to delete.</param>
        /// <param name="containerName">The name of the container (subdirectory) for the file.</param>
        /// <returns>String with the filename.</returns>
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

            string filePath = Path.Combine(GetContainerPath(containerName), imageId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return await Task.FromResult(imageId);
        }

        /// <summary>
        /// Refreshes blob storage links in a text.
        /// </summary>
        public string UpdateBlobLinks(string originalText, int noteId)
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
                int firstQuestionMark = blobString.IndexOf('?');
                string blobId = blobString[secondSlash..firstQuestionMark].Replace("/", "").Replace("?", "");
                string updatedBlobUri = "/Notes/Image?noteId=" + noteId + "&imageId=" + blobId;
                updatedText = updatedText.Replace(blobString, updatedBlobUri);
            }

            return updatedText;
        }

        public Task<bool> ImageExists(string imageId, string containerName = BlobContainers.Pictures)
        {
            string filePath = Path.Combine(GetContainerPath(containerName), imageId);
            return Task.FromResult(File.Exists(filePath));
        }
    }
}
