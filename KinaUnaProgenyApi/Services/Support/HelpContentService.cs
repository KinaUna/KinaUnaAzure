using KinaUna.Data.Models.Support;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.Support
{
    /// <summary>
    /// Service for retrieving help content.
    /// </summary>
    public class HelpContentService: IHelpContentService
    {
        /// <summary>
        /// Asynchronously retrieves help content for a specified page element and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content. Cannot be null or empty.</param>
        /// <param name="element">The identifier of the element on the page for which help content is requested. Empty for page level content.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="HelpContent"/>
        /// object with the requested help information.</returns>
        public async Task<HelpContent> GetHelpContent(string page, string element, int languageId)
        {
            // Placeholder implementation for getting help content.
            // In a real implementation, this would retrieve data from a database or service.
            HelpContent helpContent = new HelpContent
            {
                Id = 1,
                Page = page,
                Element = element,
                LanguageId = languageId,
                Title = "Sample Help Title",
                Content = "This is some sample help content.",
                CreatedTime = System.DateTime.UtcNow,
                UpdatedTime = System.DateTime.UtcNow
            };

            return await Task.FromResult(helpContent);
        }
    }
}
