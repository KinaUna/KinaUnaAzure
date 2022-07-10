using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ITextTranslationService
    {
        Task<List<TextTranslation>> GetAllTranslations(int languageId);
        Task<TextTranslation> GetTranslationById(int id);
        Task<TextTranslation> GetTranslationByWord(string word, string page, int languageId);
        Task<List<TextTranslation>> GetPageTranslations(int languageId, string pageName);
        Task<TextTranslation> AddTranslation(TextTranslation translation);
        Task<TextTranslation> UpdateTranslation(int id, TextTranslation translation);
        Task<TextTranslation> DeleteTranslation(int id);
        Task<TextTranslation> DeleteSingleTranslation(int id);
        Task<KinaUnaText> TextByTitle(string title, string page, int languageId);
        Task<KinaUnaText> TextById(int id);
        Task<KinaUnaText> TextByTextId(int textId, int languageId);
        Task<List<KinaUnaText>> PageTexts(string page, int languageId);
        Task<List<KinaUnaText>> AllPageTexts(int languageId);
        Task CheckLanguages();
        Task<KinaUnaText> AddText(KinaUnaText text);
        Task<KinaUnaText> UpdateText(int id, KinaUnaText text);
        Task<KinaUnaText> DeleteText(int id);
        Task<KinaUnaText> DeleteSingleText(int id);
    }
}
