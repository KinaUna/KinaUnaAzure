using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ITextTranslationService
    {
        /// <summary>
        /// Gets a list of all TextTranslations.
        /// </summary>
        /// <param name="languageId">The LanguageId to get translations in.</param>
        /// <returns>List of TextTranslation objects.</returns>
        Task<List<TextTranslation>> GetAllTranslations(int languageId);

        /// <summary>
        /// Gets a TextTranslation with the specified Id.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to get.</param>
        /// <returns>TextTranslation with the given Id. Null if the TextTranslation doesn't exist.</returns>
        Task<TextTranslation> GetTranslationById(int id);

        /// <summary>
        /// Gets a TextTranslation by word, page and language.
        /// </summary>
        /// <param name="word">The word to get a translation for.</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The language the text is viewed in.</param>
        /// <returns>TextTranslation object. Null if the TextTranslation doesn't exist.</returns>
        Task<TextTranslation> GetTranslationByWord(string word, string page, int languageId);

        /// <summary>
        /// Gets a list of all TextTranslations for a specific page.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language the text is viewed in.</param>
        /// <param name="pageName">The page the texts appear on.</param>
        /// <returns>List of TextTranslation objects.</returns>
        Task<List<TextTranslation>> GetPageTranslations(int languageId, string pageName);

        /// <summary>
        /// Adds a new TextTranslation to the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object to add.</param>
        /// <returns>The added TextTranslation object.</returns>
        Task<TextTranslation> AddTranslation(TextTranslation translation);

        /// <summary>
        /// Updates a TextTranslation in the database.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to update.</param>
        /// <param name="translation">The TextTranslation object with the updated properties.</param>
        /// <returns>The updated TextTranslation object.</returns>
        Task<TextTranslation> UpdateTranslation(int id, TextTranslation translation);

        /// <summary>
        /// Deletes a TextTranslation from the database.
        /// Also deletes translations in all other languages for the same word and page.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        Task<TextTranslation> DeleteTranslation(int id);

        /// <summary>
        /// Deletes a single translation from the database.
        /// Doesn't delete other language translations for the same word and page.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        Task<TextTranslation> DeleteSingleTranslation(int id);
    }
}
