using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class TextTranslationService(ProgenyDbContext context) : ITextTranslationService
    {
        /// <summary>
        /// Gets a list of all TextTranslations.
        /// </summary>
        /// <param name="languageId">The LanguageId to get translations in.</param>
        /// <returns>List of TextTranslation objects.</returns>
        public async Task<List<TextTranslation>> GetAllTranslations(int languageId)
        {
            List<TextTranslation> translations;
            if (languageId == 0)
            {
                translations = await context.TextTranslations.AsNoTracking().ToListAsync();
            }
            else
            {
                translations = await context.TextTranslations.AsNoTracking().Where(t => t.LanguageId == languageId).ToListAsync();
            }

            return translations;
        }

        /// <summary>
        /// Gets a TextTranslation with the specified Id.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to get.</param>
        /// <returns>TextTranslation with the given Id. Null if the TextTranslation doesn't exist.</returns>
        public async Task<TextTranslation> GetTranslationById(int id)
        {
            TextTranslation translation = await context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return translation;
        }

        /// <summary>
        /// Gets a list of all TextTranslations for a specific page.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language the text is viewed in.</param>
        /// <param name="pageName">The page the texts appear on.</param>
        /// <returns>List of TextTranslation objects.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        public async Task<List<TextTranslation>> GetPageTranslations(int languageId, string pageName)
        {
            List<TextTranslation> translations = await context.TextTranslations.AsNoTracking().Where(t => t.LanguageId == languageId && t.Page.ToUpper() == pageName.ToUpper()).ToListAsync();

            return translations;
        }

        /// <summary>
        /// Gets a TextTranslation by word, page and language.
        /// </summary>
        /// <param name="word">The word to get a translation for.</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The language the text is viewed in.</param>
        /// <returns>TextTranslation object. Null if the TextTranslation doesn't exist.</returns>
        public async Task<TextTranslation> GetTranslationByWord(string word, string page, int languageId)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }

            TextTranslation textTranslation = await context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Word == word && t.Page == page && t.LanguageId == languageId);
            return textTranslation;
        }

        /// <summary>
        /// Adds a new TextTranslation to the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object to add.</param>
        /// <returns>The added TextTranslation object.</returns>
        public async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            _ = context.TextTranslations.Add(translation);
            _ = await context.SaveChangesAsync();

            await AddMissingLanguagesForTranslation(translation);

            return translation;
        }

        /// <summary>
        /// Checks if a TextTranslation has translated version in each language, and adds any missing entries.
        /// </summary>
        /// <param name="translation">The TextTranslation to check for.</param>
        /// <returns></returns>
        private async Task AddMissingLanguagesForTranslation(TextTranslation translation)
        {
            List<KinaUnaLanguage> languages = await context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languages)
            {
                if (lang.Id == translation.LanguageId) continue;

                TextTranslation translationItem = await context.TextTranslations.SingleOrDefaultAsync(t => t.Word == translation.Word && t.Page == translation.Page && t.LanguageId == lang.Id);
                if (translationItem != null) continue;

                translationItem = new TextTranslation
                {
                    LanguageId = lang.Id,
                    Page = translation.Page,
                    Word = translation.Word,
                    Translation = translation.Translation
                };
                _ = context.TextTranslations.Add(translationItem);
                _ = await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Updates a TextTranslation in the database.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to update.</param>
        /// <param name="translation">The TextTranslation object with the updated properties.</param>
        /// <returns>The updated TextTranslation object.</returns>
        public async Task<TextTranslation> UpdateTranslation(int id, TextTranslation translation)
        {
            TextTranslation translationItem = await context.TextTranslations.SingleOrDefaultAsync(t => t.Id == id);
            if (translationItem == null) return null;

            translationItem.Translation = translation.Translation;
            _ = context.TextTranslations.Update(translationItem);
            _ = await context.SaveChangesAsync();

            return translationItem;
        }

        /// <summary>
        /// Deletes a TextTranslation from the database.
        /// Also deletes translations in all other languages for the same word and page.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        public async Task<TextTranslation> DeleteTranslation(int id)
        {
            TextTranslation translation = await context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (translation == null) return null;
            {
                List<TextTranslation> translationsList = await context.TextTranslations.Where(t => t.Word == translation.Word && t.Page == translation.Page).ToListAsync();
                if (translationsList.Count != 0)
                {
                    foreach (TextTranslation textTranslation in translationsList)
                    {
                        _ = context.TextTranslations.Remove(textTranslation);
                    }
                }

                _ = await context.SaveChangesAsync();
            }

            return translation;
        }

        /// <summary>
        /// Deletes a single translation from the database.
        /// Doesn't delete other language translations for the same word and page.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        public async Task<TextTranslation> DeleteSingleTranslation(int id)
        {
            TextTranslation translation = await context.TextTranslations.SingleOrDefaultAsync(t => t.Id == id);
            if (translation == null) return null;

            _ = context.TextTranslations.Remove(translation);
            _ = await context.SaveChangesAsync();

            return translation;
        }
    }
}
