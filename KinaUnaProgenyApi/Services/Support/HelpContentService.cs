using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models.Support;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
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
        /// Retrieves the help content entry with the specified identifier.
        /// </summary>
        /// <param name="helpContentId">The unique identifier of the help content to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HelpContent"/>
        /// with the specified identifier, or <see langword="null"/> if no matching entry is found.</returns>
        public async Task<HelpContent> GetHelpContentById(int helpContentId)
        {
            HelpContent helpContent = await progenyDbContext.HelpContentsDb.AsNoTracking().SingleOrDefaultAsync(hc => hc.Id == helpContentId);
            return helpContent;
        }

        /// <summary>
        /// Asynchronously retrieves the help content associated with the specified identifier.
        /// </summary>
        /// <param name="helpContentTextId">The unique identifier of the help content to retrieve. Must be a positive integer.</param>
        /// <param name="languageId">The unique identifier of the language to retrieve the help content for. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HelpContent"/>
        /// object if found; otherwise, <c>null</c>.</returns>
        public async Task<HelpContent> GetHelpContentByTextId(int helpContentTextId, int languageId)
        {
            HelpContent helpContent = await progenyDbContext.HelpContentsDb.AsNoTracking()
                .SingleOrDefaultAsync(hc => hc.TextId == helpContentTextId && hc.LanguageId == languageId);
            return helpContent;
        }

        /// <summary>
        /// Adds a new help content entry to the data store asynchronously.
        /// </summary>
        /// <param name="helpContent">The help content to add. The <see cref="HelpContent.Page"/> and <see cref="HelpContent.Content"/> properties
        /// must not be null or empty.</param>
        /// <param name="currentUserInfo">The current user's information for authorization purposes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see
        /// cref="HelpContent"/> if the operation succeeds; otherwise, <see langword="null"/> if required properties are
        /// missing.</returns>
        public async Task<HelpContent> AddHelpContent(HelpContent helpContent, UserInfo currentUserInfo)
        {
            if (!currentUserInfo.IsKinaUnaAdmin)
            {
                return null;
            }

            if (helpContent.TextId == 0)
            {
                HelpTextNumber helpTextNumber = new();
                progenyDbContext.HelpTextNumbersDb.Add(helpTextNumber);
                await progenyDbContext.SaveChangesAsync();
                helpContent.TextId = helpTextNumber.TextId;
            }

            helpContent.CreatedTime = System.DateTime.UtcNow;
            helpContent.UpdatedTime = System.DateTime.UtcNow;
            if(string.IsNullOrEmpty(helpContent.Page) || string.IsNullOrEmpty(helpContent.Content))
            {
                return null;
            }

            progenyDbContext.HelpContentsDb.Add(helpContent);
            await progenyDbContext.SaveChangesAsync();

            await VerifyTranslationsExist();

            return helpContent;
        }

        /// <summary>
        /// Updates the specified help content entry in the data store and returns the updated entity.
        /// </summary>
        /// <param name="helpContent">The help content entity to update. Must not be null and should represent an existing entry in the data
        /// store.</param>
        /// <param name="currentUserInfo">The current user's information for authorization purposes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated help content entity.</returns>
        public async Task<HelpContent> UpdateHelpContent(HelpContent helpContent, UserInfo currentUserInfo)
        {
            if (!currentUserInfo.IsKinaUnaAdmin)
            {
                return null;
            }

            HelpContent originalHelpContent = await progenyDbContext.HelpContentsDb.SingleOrDefaultAsync(hc => hc.Id == helpContent.Id);
            if (originalHelpContent == null)
            {
                return null;
            }

            originalHelpContent.UpdatedTime = System.DateTime.UtcNow;
            originalHelpContent.Content = helpContent.Content;
            originalHelpContent.Title = helpContent.Title;

            progenyDbContext.HelpContentsDb.Update(originalHelpContent);
            await progenyDbContext.SaveChangesAsync();
            return originalHelpContent;
        }

        /// <summary>
        /// Deletes the help content entry with the specified identifier from the data store.
        /// </summary>
        /// <remarks>If no help content with the specified identifier exists, the method returns <see
        /// langword="null"/> and no action is taken.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content to delete.</param>
        /// <param name="currentUserInfo">The current user's information for authorization purposes.</param>
        /// <returns>The deleted help content entry if found and removed; otherwise, <see langword="null"/>.</returns>
        public async Task<HelpContent> DeleteHelpContent(int helpContentId, UserInfo currentUserInfo)
        {
            if (!currentUserInfo.IsKinaUnaAdmin)
            {
                return null;
            }

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
        /// <param name="page">The name of the page for which to retrieve help content. Empty string for all pages.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A List of <see cref="HelpContent"/> entries for the specified page and language.</returns>
        public async Task<List<HelpContent>> GetHelpContentForPage(string page, int languageId)
        {
            if (string.IsNullOrEmpty(page))
            {
                return await progenyDbContext.HelpContentsDb.AsNoTracking().Where(hc => hc.LanguageId == languageId).ToListAsync();
            }

            List<HelpContent> helpContents = await progenyDbContext.HelpContentsDb.AsNoTracking()
                .Where(hc => hc.Page == page && hc.LanguageId == languageId)
                .ToListAsync();
            return helpContents;
        }

        /// <summary>
        /// Asynchronously retrieves a list of unique help content page names.
        /// </summary>
        /// <returns>A list of strings containing the names of all distinct help content pages. The list will be empty if no help
        /// content pages are found.</returns>
        public async Task<List<string>> GetHelpContentPages()
        {
            // Verify that translations exist for all languages for all help content entries. Ensures adding future languages does not result in missing help content.
            await VerifyTranslationsExist();

            List<string> pages = await progenyDbContext.HelpContentsDb.AsNoTracking()
                .Select(hc => hc.Page)
                .Distinct()
                .ToListAsync();
            return pages;
        }

        /// <summary>
        /// Ensures that a help content translation exists for each supported language and help text number, creating
        /// missing translations by duplicating the default language content if necessary.
        /// </summary>
        /// <remarks>This method checks all help text numbers and verifies that a corresponding help
        /// content entry exists for every supported language. If a translation is missing, it creates a new entry by
        /// copying the content from the default language. This operation may result in database writes if missing
        /// translations are found.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task VerifyTranslationsExist()
        {
            List<HelpTextNumber> helpTextNumbers = await progenyDbContext.HelpTextNumbersDb.AsNoTracking().ToListAsync();
            foreach (HelpTextNumber number in helpTextNumbers)
            {
                List<HelpContent> helpContentsWithNumber = await progenyDbContext.HelpContentsDb
                    .AsNoTracking()
                    .Where(hc => hc.TextId == number.TextId)
                    .ToListAsync();
                if(helpContentsWithNumber.Count < 1)
                {
                    continue;
                }

                List<KinaUnaLanguage> languages = await progenyDbContext.Languages.AsNoTracking().ToListAsync();
                foreach (KinaUnaLanguage language in languages)
                {
                    bool hasTranslation = helpContentsWithNumber.Any(hc => hc.LanguageId == language.Id);
                    if (!hasTranslation)
                    {
                        HelpContent defaultHelpContent = helpContentsWithNumber.FirstOrDefault(hc => hc.LanguageId == 1);
                        if (defaultHelpContent != null)
                        {
                            HelpContent newHelpContent = new()
                            {
                                TextId = number.TextId,
                                Page = defaultHelpContent.Page,
                                Element = defaultHelpContent.Element,
                                LanguageId = language.Id,
                                Title = defaultHelpContent.Title,
                                Content = defaultHelpContent.Content,
                                CreatedTime = System.DateTime.UtcNow,
                                UpdatedTime = System.DateTime.UtcNow
                            };
                            progenyDbContext.HelpContentsDb.Add(newHelpContent);
                            await progenyDbContext.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}
