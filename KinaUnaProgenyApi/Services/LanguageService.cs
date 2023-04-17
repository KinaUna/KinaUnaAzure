using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class LanguageService : ILanguageService
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
                lang.IconLink = "https://web.kinauna.com/images/flags/64/" + lang.Icon + ".png";
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
            _ = _context.Languages.Add(language);
            _ = await _context.SaveChangesAsync();
            return language;
        }

        public async Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage languageToUpdate = await _context.Languages.SingleOrDefaultAsync(l => l.Id == language.Id);
            if (languageToUpdate != null)
            {
                languageToUpdate.Name = language.Name;
                languageToUpdate.Code = language.Code;
                languageToUpdate.Icon = language.Icon;
                languageToUpdate.IconLink = language.IconLink;

                _ = _context.Languages.Update(languageToUpdate);
                _ = await _context.SaveChangesAsync();
            }

            return language;
        }

        public async Task<KinaUnaLanguage> DeleteLanguage(int languageId)
        {
            KinaUnaLanguage language = await _context.Languages.SingleOrDefaultAsync(l => l.Id == languageId);
            if (language == null)
            {
                return new KinaUnaLanguage() { Id = -1 };
            }

            _ = _context.Languages.Remove(language);
            _ = await _context.SaveChangesAsync();

            return language;
        }
    }
}
