using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace KinaUnaProgenyApi.Services
{
    public class TextTranslationService: ITextTranslationService
    {
        private readonly ProgenyDbContext _context;

        public TextTranslationService(ProgenyDbContext context)
        {
            _context = context;
        }

        public async Task<List<TextTranslation>> GetAllTranslations(int languageId)
        {
            List<TextTranslation> translations;
            if (languageId == 0)
            {
                translations = await _context.TextTranslations.AsNoTracking().ToListAsync();
            }
            else
            {
                translations = await _context.TextTranslations.AsNoTracking().Where(t => t.LanguageId == languageId).ToListAsync();
            }

            return translations;
        }

        public async Task<TextTranslation> GetTranslationById(int id)
        {
            TextTranslation translation = await _context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return translation;
        }

        public async Task<List<TextTranslation>> GetPageTranslations(int languageId, string pageName)
        {
            List<TextTranslation> translations = await _context.TextTranslations.AsNoTracking().Where(t => t.LanguageId == languageId && t.Page.ToUpper() == pageName.ToUpper()).ToListAsync();
            
            return translations;
        }

        public async Task<TextTranslation> GetTranslationByWord(string word, string page, int languageId)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }

            TextTranslation textTranslation = await _context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Word == word && t.Page == page && t.LanguageId == languageId);
            return textTranslation;
        }

        public async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            _ = _context.TextTranslations.Add(translation);
            _ = await _context.SaveChangesAsync();

            await AddMissingLanguagesForTranslation(translation);
            
            return translation;
        }

        private async Task AddMissingLanguagesForTranslation(TextTranslation translation)
        {
            List<KinaUnaLanguage> languages = await _context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languages)
            {
                if (lang.Id != translation.LanguageId)
                {
                    TextTranslation translationItem = await _context.TextTranslations.SingleOrDefaultAsync(t => t.Word == translation.Word && t.Page == translation.Page && t.LanguageId == lang.Id);
                    if (translationItem == null)
                    {
                        translationItem = new TextTranslation();
                        translationItem.LanguageId = lang.Id;
                        translationItem.Page = translation.Page;
                        translationItem.Word = translation.Word;
                        translationItem.Translation = translation.Translation;
                        _ = _context.TextTranslations.Add(translationItem);
                        _ = await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task<TextTranslation> UpdateTranslation(int id, TextTranslation translation)
        {
            TextTranslation translationItem = await _context.TextTranslations.SingleOrDefaultAsync(t => t.Id == id);
            if (translationItem != null)
            {
                translationItem.Translation = translation.Translation;
                _ = _context.TextTranslations.Update(translationItem);
                _ = await _context.SaveChangesAsync();
            }

            return translationItem;
        }

        public async Task<TextTranslation> DeleteTranslation(int id)
        {
            TextTranslation translation = await _context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (translation != null)
            {
                List<TextTranslation> translationsList = await _context.TextTranslations.Where(t => t.Word == translation.Word && t.Page == translation.Page).ToListAsync();
                if (translationsList.Any())
                {
                    foreach (TextTranslation textTranslation in translationsList)
                    {
                        _ = _context.TextTranslations.Remove(textTranslation);
                    }
                }

                _ = await _context.SaveChangesAsync();
            }
            
            return translation;
        }

        public async Task<TextTranslation> DeleteSingleTranslation(int id)
        {
            TextTranslation translation = await _context.TextTranslations.SingleOrDefaultAsync(t => t.Id == id);
            if (translation != null)
            {
                _ = _context.TextTranslations.Remove(translation);
                _ = await _context.SaveChangesAsync();
            }

            return translation;
        }

        public async Task<KinaUnaText> GetTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.AsNoTracking().FirstOrDefaultAsync(t => t.Title.ToUpper() == title.Trim().ToUpper() && t.Page.ToUpper() == page.Trim().ToUpper() && t.LanguageId == languageId);
            if (textItem == null)
            {
                textItem = await AddTextLanguageVersion(title, page, languageId);
            }

            return textItem;
        }

        private async Task<KinaUnaText> AddTextLanguageVersion(string title, string page, int languageId)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.AsNoTracking().FirstOrDefaultAsync(t => t.Title.ToUpper() == title.Trim().ToUpper() && t.Page.ToUpper() == page.Trim().ToUpper() && t.LanguageId == 1);
            if (textItem != null)
            {
                KinaUnaText textNewLanguageVersion = new KinaUnaText
                {
                    LanguageId = languageId,
                    Title = textItem.Title,
                    Text = textItem.Text,
                    Page = textItem.Page,
                    TextId = textItem.TextId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };

                _ = await _context.KinaUnaTexts.AddAsync(textNewLanguageVersion);
                _ = await _context.SaveChangesAsync();

                textItem = textNewLanguageVersion;
            }

            return textItem;
        }

        public async Task<KinaUnaText> GetTextById(int id)
        {
            KinaUnaText pivoqText = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return pivoqText;
        }

        public async Task<KinaUnaText> GetTextByTextId(int textId, int languageId)
        {
            KinaUnaText pivoqText = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.TextId == textId && t.LanguageId == languageId);
            return pivoqText;
        }

        public async Task<List<KinaUnaText>> GetPageTextsList(string page, int languageId)
        {
            List<KinaUnaText> texts = await _context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId && t.Page.ToUpper() == page.Trim().ToUpper()).ToListAsync();
            return texts;
        }

        public async Task<List<KinaUnaText>> GetAllPageTextsList(int languageId)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }
            List<KinaUnaText> texts = await _context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId).ToListAsync();
            return texts;
        }

        public async Task CheckLanguages()
        {
            List<KinaUnaTextNumber> textNumbers = await _context.KinaUnaTextNumbers.AsNoTracking().ToListAsync();
            List<KinaUnaLanguage> languages = await _context.Languages.AsNoTracking().ToListAsync();

            foreach (KinaUnaTextNumber tNumber in textNumbers)
            {
                List<KinaUnaText> texts = await _context.KinaUnaTexts.AsNoTracking().Where(t => t.TextId == tNumber.Id).OrderBy(t => t.LanguageId).ToListAsync();
                if (texts.Any() && texts.Count < languages.Count)
                {
                    foreach (KinaUnaLanguage lang in languages)
                    {
                        KinaUnaText textItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == tNumber.Id && t.LanguageId == lang.Id);
                        if (textItem == null)
                        {
                            KinaUnaText oldPivoqText = texts.First();

                            KinaUnaText newPivoqText = new KinaUnaText();
                            newPivoqText.Page = oldPivoqText.Page;
                            newPivoqText.Title = oldPivoqText.Title;
                            newPivoqText.Text = oldPivoqText.Text;
                            newPivoqText.Created = oldPivoqText.Created;
                            newPivoqText.Updated = oldPivoqText.Updated;
                            newPivoqText.LanguageId = lang.Id;
                            newPivoqText.TextId = oldPivoqText.TextId;
                            _ = await _context.KinaUnaTexts.AddAsync(newPivoqText);
                            _ = await _context.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        public async Task<KinaUnaText> AddText(KinaUnaText text)
        {
            text.Title = text.Title.Trim();
            text.Page = text.Page.Trim();

            if (text.Title.StartsWith("__"))
            {
                // Title's starting with double underscore are considered unique system pages, so we make sure no other text has the same title on a page.
                text = await AddSystemPageText(text);
            }
            else
            {
                KinaUnaTextNumber textNumber = new KinaUnaTextNumber();
                textNumber.DefaultLanguage = 1;
                _ = await _context.KinaUnaTextNumbers.AddAsync(textNumber);
                _ = await _context.SaveChangesAsync();
                text.TextId = textNumber.Id;
                text.Created = DateTime.UtcNow;
                text.Updated = text.Created;
                _ = _context.KinaUnaTexts.Add(text);
                _ = await _context.SaveChangesAsync();
            }

            await AddTextForOtherLanguages(text);

            return text;
        }

        private async Task<KinaUnaText> AddSystemPageText(KinaUnaText text)
        {
            KinaUnaText existingTextItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Title == text.Title && t.Page == text.Page && t.LanguageId == text.LanguageId);
            if (existingTextItem == null)
            {
                KinaUnaTextNumber textNumber = new KinaUnaTextNumber();
                textNumber.DefaultLanguage = 1;
                _ = await _context.KinaUnaTextNumbers.AddAsync(textNumber);
                _ = await _context.SaveChangesAsync();
                text.TextId = textNumber.Id;
                text.Created = DateTime.UtcNow;
                text.Updated = text.Created;
                _context.KinaUnaTexts.Add(text);
                _ = await _context.SaveChangesAsync();
                
                return text;

            }
            else
            {
                existingTextItem.Title = text.Title;
                existingTextItem.Text = text.Text;
                existingTextItem.Page = text.Page;
                existingTextItem.Created = DateTime.UtcNow;
                existingTextItem.Updated = existingTextItem.Created;
                _ = _context.KinaUnaTexts.Update(existingTextItem);
                _ = await _context.SaveChangesAsync();

                return existingTextItem;
            }
        }

        private async Task AddTextForOtherLanguages(KinaUnaText text)
        {
            List<KinaUnaLanguage> languages = await _context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languages)
            {
                if (lang.Id != text.LanguageId)
                {
                    KinaUnaText textItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == text.TextId && t.LanguageId == lang.Id);
                    if (textItem == null)
                    {
                        textItem = new KinaUnaText();
                        textItem.LanguageId = lang.Id;
                        textItem.Page = text.Page;
                        textItem.Title = text.Title;
                        textItem.Text = text.Text;
                        textItem.TextId = text.TextId;
                        textItem.Created = text.Created;
                        textItem.Updated = text.Updated;
                        _ = _context.KinaUnaTexts.Add(textItem);
                        _ = await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task<KinaUnaText> UpdateText(int id, KinaUnaText text)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                textItem.LanguageId = text.LanguageId;
                textItem.Page = text.Page.Trim();
                textItem.Title = text.Title.Trim();
                textItem.Text = text.Text;
                textItem.Updated = DateTime.UtcNow;
                _ = _context.KinaUnaTexts.Update(textItem);
                _ = await _context.SaveChangesAsync();
            }

            return textItem;
        }

        public async Task<KinaUnaText> DeleteText(int id)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                List<KinaUnaText> textsList = await _context.KinaUnaTexts.Where(t => t.Title == textItem.Title && t.Page == textItem.Page).ToListAsync();
                if (textsList.Any())
                {
                    foreach (KinaUnaText textEntity in textsList)
                    {
                        _ = _context.KinaUnaTexts.Remove(textEntity);
                    }
                }

                _ = await _context.SaveChangesAsync();
            }
            return textItem;
        }

        public async Task<KinaUnaText> DeleteSingleText(int id)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                _ = _context.KinaUnaTexts.Remove(textItem);
                _ = await _context.SaveChangesAsync();
            }
            
            return textItem;
        }
    }
}
