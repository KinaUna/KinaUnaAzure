using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Translations API.
    /// Contains the methods for adding, retrieving and updating data relevant to translation functions.
    /// </summary>
    public interface ITranslationsHttpClient
    {
        /// <summary>
        /// Gets a translation for a given word (or phrase) and page translated into a given language.
        /// If the TextTranslation doesn't exist, it is added to the database.
        /// </summary>
        /// <param name="word">The Word property of the translation to get (usually the English version).</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The language it should be translated to.</param>
        /// <param name="updateCache">Force update the cache. False: Attempt to get the translation from the cache. True: Get the translation from the API and update the cache.</param>
        /// <returns>String with the translated word or phrase.</returns>
        Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false);

        /// <summary>
        /// Adds a new TextTranslation to the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object to add.</param>
        /// <returns>The added TextTranslation object.If an error happens a new TextTranslation with Id=0 is returned.</returns>
        Task<TextTranslation> AddTranslation(TextTranslation translation);

        /// <summary>
        /// Updates a TextTranslation in the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object with the updated properties.</param>
        /// <returns>The updated TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        Task<TextTranslation> UpdateTranslation(TextTranslation translation);

        /// <summary>
        /// Gets the list of all TextTranslations in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the TextTranslations to get.</param>
        /// <param name="updateCache">Force update the cache. If False, tries to get the list from the cache first, if True gets the list from the API first and updates the cache.</param>
        /// <returns>List of TextTranslation objects.</returns>
        Task<List<TextTranslation>> GetAllTranslations(int languageId = 0, bool updateCache = false);

        /// <summary>
        /// Gets a TextTranslation by Id.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to get.</param>
        /// <param name="updateCache">Force update the cache. If False, tries to get the TextTranslation from the cache first, if True gets it from the API first and updates the cache.</param>
        /// <returns>TextTranslation object with the given Id. New TextTranslation object with Id=0 if not found or an error occurs.</returns>
        Task<TextTranslation> GetTranslationById(int id, bool updateCache = false);

        /// <summary>
        /// Deletes a TextTranslation from the database.
        /// Also deletes the translations in all other languages for the same word and page.
        /// </summary>
        /// <param name="translation">The TextTranslation object to delete.</param>
        /// <returns>The deleted TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        Task<TextTranslation> DeleteTranslation(TextTranslation translation);

        /// <summary>
        /// Deletes a single translation from the database.
        /// Doesn't delete translations in other languages for the same word and page.
        /// </summary>
        /// <param name="translation">The TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        Task<TextTranslation> DeleteSingleItemTranslation(TextTranslation translation);
    }
}
