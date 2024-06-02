using System.IO;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services;

public interface IImageStore
{
    Task<string> SaveImage(Stream imageStream, string containerName = "pictures", string fileFormat = ".jpg");
    string UriFor(string imageId, string containerName = "pictures");
    Task<MemoryStream> GetStream(string imageId, string containerName = "pictures");
    Task<string> DeleteImage(string imageId, string containerName = "pictures");

    /// <summary>
    /// Refreshes blob storage links in a text.
    /// </summary>
    /// <param name="originalText">string: The text to update links for.</param>
    /// <returns>string: The text with updated links.</returns>
    string UpdateBlobLinks(string originalText);
}