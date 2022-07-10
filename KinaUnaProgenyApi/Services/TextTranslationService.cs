using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

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
            TextTranslation textTranslation = await _context.TextTranslations.SingleOrDefaultAsync(t => t.Word == word && t.Page == page && t.LanguageId == languageId);
            return textTranslation;
        }

        public async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            _context.TextTranslations.Add(translation);
            await _context.SaveChangesAsync();

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
                        _context.TextTranslations.Add(translationItem);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return translation;
        }

        public async Task<TextTranslation> UpdateTranslation(int id, TextTranslation translation)
        {
            TextTranslation translationItem = await _context.TextTranslations.SingleOrDefaultAsync(t => t.Id == id);
            if (translationItem != null)
            {
                translationItem.Translation = translation.Translation;
                _context.TextTranslations.Update(translationItem);
                await _context.SaveChangesAsync();
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
                        _context.TextTranslations.Remove(textTranslation);
                    }
                }

                await _context.SaveChangesAsync();
            }
            
            return translation;
        }

        public async Task<TextTranslation> DeleteSingleTranslation(int id)
        {
            TextTranslation translation = await _context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (translation != null)
            {
                _context.TextTranslations.Remove(translation);
                await _context.SaveChangesAsync();
            }

            return translation;
        }

        public async Task<KinaUnaText> TextByTitle(string title, string page, int languageId)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.AsNoTracking().FirstOrDefaultAsync(t => t.Title.ToUpper() == title.Trim().ToUpper() && t.Page.ToUpper() == page.Trim().ToUpper() && t.LanguageId == languageId);
            if (textItem == null)
            {
                textItem = await _context.KinaUnaTexts.FirstOrDefaultAsync(t => t.Title.ToUpper() == title.Trim().ToUpper() && t.Page.ToUpper() == page.Trim().ToUpper() && t.LanguageId == 1);
                if (textItem != null)
                {
                    textItem.LanguageId = languageId;
                    textItem.Id = 0;
                    textItem.Text = "";
                    await _context.KinaUnaTexts.AddAsync(textItem);
                    await _context.SaveChangesAsync();
                }
            }

            return textItem;
        }

        public async Task<KinaUnaText> TextById(int id)
        {
            KinaUnaText pivoqText = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return pivoqText;
        }

        public async Task<KinaUnaText> TextByTextId(int textId, int languageId)
        {
            KinaUnaText pivoqText = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.TextId == textId && t.LanguageId == languageId);
            return pivoqText;
        }

        public async Task<List<KinaUnaText>> PageTexts(string page, int languageId)
        {
            List<KinaUnaText> texts = await _context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId && t.Page.ToUpper() == page.Trim().ToUpper()).ToListAsync();
            return texts;
        }

        public async Task<List<KinaUnaText>> AllPageTexts(int languageId)
        {
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
                            await _context.KinaUnaTexts.AddAsync(newPivoqText);
                            await _context.SaveChangesAsync();

                        }
                    }
                }
            }
        }

        public async Task<KinaUnaText> AddText(KinaUnaText text)
        {
            if (text.Title.StartsWith("__"))
            {
                // Title's starting with double underscore are considered unique system pages.
                KinaUnaText existingTextItem = await _context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Title == text.Title && t.Page == text.Page && t.LanguageId == text.LanguageId);
                if (existingTextItem == null)
                {
                    KinaUnaTextNumber textNumber = new KinaUnaTextNumber();
                    textNumber.DefaultLanguage = 1;
                    await _context.KinaUnaTextNumbers.AddAsync(textNumber);
                    await _context.SaveChangesAsync();
                    text.TextId = textNumber.Id;
                    text.Created = DateTime.UtcNow;
                    text.Updated = text.Created;
                    _context.KinaUnaTexts.Add(text);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    existingTextItem.Title = text.Title;
                    existingTextItem.Text = text.Text;
                    existingTextItem.Page = text.Page;
                    existingTextItem.Created = DateTime.UtcNow;
                    existingTextItem.Updated = existingTextItem.Created;
                    _context.KinaUnaTexts.Update(existingTextItem);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                KinaUnaTextNumber textNumber = new KinaUnaTextNumber();
                textNumber.DefaultLanguage = 1;
                await _context.KinaUnaTextNumbers.AddAsync(textNumber);
                await _context.SaveChangesAsync();
                text.TextId = textNumber.Id;
                text.Created = DateTime.UtcNow;
                text.Updated = text.Created;
                _context.KinaUnaTexts.Add(text);
                await _context.SaveChangesAsync();
            }


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
                        _context.KinaUnaTexts.Add(textItem);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return text;
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
                _context.KinaUnaTexts.Update(textItem);
                await _context.SaveChangesAsync();
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
                        _context.KinaUnaTexts.Remove(textEntity);
                    }
                }

                await _context.SaveChangesAsync();
            }
            return textItem;
        }

        public async Task<KinaUnaText> DeleteSingleText(int id)
        {
            KinaUnaText textItem = await _context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                _context.KinaUnaTexts.Remove(textItem);
                await _context.SaveChangesAsync();
            }
            
            return textItem;
        }
    }
}
