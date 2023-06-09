﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class TextTranslationService : ITextTranslationService
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
                        translationItem = new TextTranslation
                        {
                            LanguageId = lang.Id,
                            Page = translation.Page,
                            Word = translation.Word,
                            Translation = translation.Translation
                        };
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
    }
}
