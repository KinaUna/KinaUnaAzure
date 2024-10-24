using System.IO;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

public interface IImageStore
{
    /// <summary>
    /// Saves an image file Stream to a blob storage.
    /// The file name is a new GUID.
    /// </summary>
    /// <param name="imageStream">The Stream containing the file data.</param>
    /// <param name="containerName">The Name of the storage container to save the file to.</param>
    /// <param name="fileFormat">The file extension of the file.</param>
    /// <returns>The filename of the saved file.</returns>
    Task<string> SaveImage(Stream imageStream, string containerName = "pictures", string fileFormat = ".jpg");

    /// <summary>
    /// Gets a SAS URI for a file/blob in a storage container.
    /// </summary>
    /// <param name="imageId">The filename of the file.</param>
    /// <param name="containerName">The storage container's name.</param>
    /// <returns>String with the URI for the file.</returns>
    string UriFor(string imageId, string containerName = "pictures");

    /// <summary>
    /// Gets a Stream for a file/blob in a storage container.
    /// </summary>
    /// <param name="imageId">The filename of the file to get a stream for.</param>
    /// <param name="containerName">The name of the storage container for the file.</param>
    /// <returns>MemoryStream for the file.</returns>
    Task<MemoryStream> GetStream(string imageId, string containerName = "pictures");

    /// <summary>
    /// Deletes a file/blob from a storage container.
    /// </summary>
    /// <param name="imageId">The filename of the file to delete.</param>
    /// <param name="containerName">The name of the container for the file.</param>
    /// <returns>String with the filename.</returns>
    Task<string> DeleteImage(string imageId, string containerName = "pictures");

    /// <summary>
    /// Refreshes blob storage links in a text.
    /// SAS links are valid for a limited time, so they need to be updated.
    /// </summary>
    /// <param name="originalText">string: The text to update links for.</param>
    /// <param name="noteId">int: The Id of the note the text is from.</param>
    /// <returns>string: The text with updated links.</returns>
    string UpdateBlobLinks(string originalText, int noteId);

    /// <summary>
    /// Checks if an image exists in a storage container.
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="containerName"></param>
    /// <returns></returns>
    Task<bool> ImageExists(string imageId, string containerName = BlobContainers.Pictures);
}