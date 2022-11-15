﻿using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class LanguageService: ILanguageService
    {
        private readonly ProgenyDbContext _context;

        public LanguageService(ProgenyDbContext context)
        {
            _context = context;
        }

        public async Task<List<KinaUnaLanguage>> GetAllLanguages()
        {
            List<KinaUnaLanguage> languagesList = await _context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languagesList)
            {
                // Downloaded from here : https://www.countryflags.com/en/icons-overview/
                lang.IconLink = "https://www.kinauna.com/images/flags/64/" + lang.Icon + ".png";
            }

            return languagesList;
        }

        public async Task<KinaUnaLanguage> GetLanguage(int languageId)
        {
            KinaUnaLanguage language = await _context.Languages.AsNoTracking().SingleOrDefaultAsync(l => l.Id == languageId);
            return language;
        }

        public async Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language)
        {
            _context.Languages.Add(language);
            await _context.SaveChangesAsync();
            return language;
        }

        public async Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language)
        {
            _context.Languages.Update(language);
            await _context.SaveChangesAsync();
            return language;
        }

        public async Task<KinaUnaLanguage> DeleteLanguage(int languageId)
        {
            KinaUnaLanguage language = await _context.Languages.SingleOrDefaultAsync(l => l.Id == languageId);
            if (language == null)
            {
                return new KinaUnaLanguage() { Id = -1 };
            }

            _context.Languages.Remove(language);
            await _context.SaveChangesAsync();

            return language;
        }
    }
}