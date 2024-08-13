using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IKinaUnaTextService
    {
        /// <summary>
        /// Gets a KinaUnaText by Title, Page and language.
        /// </summary>
        /// <param name="title">The Title of the KinaUnaText.</param>
        /// <param name="page">The Page the text belongs to.</param>
        /// <param name="languageId">The LanguageId of the language.</param>
        /// <returns>KinaUnaText.</returns>
        Task<KinaUnaText> GetTextByTitle(string title, string page, int languageId);

        /// <summary>
        /// Gets a KinaUnaText by Id.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to get.</param>
        /// <returns>KinaUnaText with the given Id.</returns>
        Task<KinaUnaText> GetTextById(int id);

        /// <summary>
        /// Gets a KinaUnaText by TextId and language.
        /// Each text is translated into all languages, with the same TextId.
        /// </summary>
        /// <param name="textId">The TextId of the text to get.</param>
        /// <param name="languageId">The language of the translation to get.</param>
        /// <returns>The KinaUnaText with the TextId and in the language given.</returns>
        Task<KinaUnaText> GetTextByTextId(int textId, int languageId);

        /// <summary>
        /// Gets a list of all KinaUnaTexts for a page, in the specified language.
        /// </summary>
        /// <param name="page">The Page to get KinaUnaTexts for.</param>
        /// <param name="languageId">The LanguageId of the language to get KinaUnaTexts for.</param>
        /// <returns>List of KinaUnaTexts.</returns>
        Task<List<KinaUnaText>> GetPageTextsList(string page, int languageId);

        /// <summary>
        /// Gets a list of all KinaUnaTexts in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language to get KinaUnaTexts for.</param>
        /// <returns>List of KinaUnaTexts.</returns>
        Task<List<KinaUnaText>> GetAllPageTextsList(int languageId);

        /// <summary>
        /// Checks each TextNumber to see if there are texts for all languages.
        /// If not, it adds a text for the missing languages.
        /// </summary>
        /// <returns></returns>
        Task CheckLanguages();

        /// <summary>
        /// Adds a new KinaUnaText to the database.
        /// Adds a KinaUnaTextNumber to the database for the KinaUnaText, so translations can be managed.
        /// Then adds translations for the text in all languages.
        /// </summary>
        /// <param name="text">The KinaUnaText object to add.</param>
        /// <returns>The added KinaUnaText object.</returns>
        Task<KinaUnaText> AddText(KinaUnaText text);

        /// <summary>
        /// Updates a KinaUnaText in the database.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText</param>
        /// <param name="text">The KinaUnaText object with updated properties.</param>
        /// <returns>The updated KinaUnaText updated.</returns>
        Task<KinaUnaText> UpdateText(int id, KinaUnaText text);

        /// <summary>
        /// Deletes a KinaUnaText from the database.
        /// Also deletes all translations of the text.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to delete.</param>
        /// <returns>The deleted KinaUnaText object.</returns>
        Task<KinaUnaText> DeleteText(int id);

        /// <summary>
        /// Deletes a single KinaUnaText from the database.
        /// Doesn't delete translations of the text.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to delete.</param>
        /// <returns>The deleted KinaUnaText object.</returns>
        Task<KinaUnaText> DeleteSingleText(int id);
    }
}
