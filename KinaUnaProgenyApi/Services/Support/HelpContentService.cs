using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models.Support;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.Support
{
    /// <summary>
    /// Service for retrieving help content.
    /// </summary>
    public class HelpContentService(ProgenyDbContext progenyDbContext): IHelpContentService
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
            HelpContent sampleHelpContent = new HelpContent
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

            HelpContent helpContent = await progenyDbContext.HelpContentsDb.AsNoTracking().SingleOrDefaultAsync(hc => hc.Page == page && hc.Element == element && hc.LanguageId == languageId);
            if (helpContent == null)
            {
                helpContent = sampleHelpContent;
            }
            
            return helpContent;
        }

        /// <summary>
        /// Adds a new help content entry to the data store asynchronously.
        /// </summary>
        /// <param name="helpContent">The help content to add. The <see cref="HelpContent.Page"/> and <see cref="HelpContent.Content"/> properties
        /// must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see
        /// cref="HelpContent"/> if the operation succeeds; otherwise, <see langword="null"/> if required properties are
        /// missing.</returns>
        public async Task<HelpContent> AddHelpContent(HelpContent helpContent)
        {
            helpContent.CreatedTime = System.DateTime.UtcNow;
            helpContent.UpdatedTime = System.DateTime.UtcNow;
            if(string.IsNullOrEmpty(helpContent.Page) || string.IsNullOrEmpty(helpContent.Content))
            {
                return null;
            }

            progenyDbContext.HelpContentsDb.Add(helpContent);
            await progenyDbContext.SaveChangesAsync();

            return helpContent;
        }

        /// <summary>
        /// Updates the specified help content entry in the data store and returns the updated entity.
        /// </summary>
        /// <param name="helpContent">The help content entity to update. Must not be null and should represent an existing entry in the data
        /// store.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated help content entity.</returns>
        public async Task<HelpContent> UpdateHelpContent(HelpContent helpContent)
        {
            helpContent.UpdatedTime = System.DateTime.UtcNow;
            progenyDbContext.HelpContentsDb.Update(helpContent);
            await progenyDbContext.SaveChangesAsync();
            return helpContent;
        }

        /// <summary>
        /// Deletes the help content entry with the specified identifier from the data store.
        /// </summary>
        /// <remarks>If no help content with the specified identifier exists, the method returns <see
        /// langword="null"/> and no action is taken.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content to delete.</param>
        /// <returns>The deleted help content entry if found and removed; otherwise, <see langword="null"/>.</returns>
        public async Task<HelpContent> DeleteHelpContent(int helpContentId)
        {
            HelpContent helpContent = await progenyDbContext.HelpContentsDb.SingleOrDefaultAsync(hc => hc.Id == helpContentId);
            if (helpContent != null)
            {
                progenyDbContext.HelpContentsDb.Remove(helpContent);
                await progenyDbContext.SaveChangesAsync();
            }
            return helpContent;
        }

        /// <summary>
        /// Retrieves all help content entries for a specified page and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A List of <see cref="HelpContent"/> entries for the specified page and language.</returns>
        public async Task<List<HelpContent>> GetHelpContentForPage(string page, int languageId)
        {
            List<HelpContent> helpContents = await progenyDbContext.HelpContentsDb.AsNoTracking()
                .Where(hc => hc.Page == page && hc.LanguageId == languageId)
                .ToListAsync();
            return helpContents;
        }
    }
}
