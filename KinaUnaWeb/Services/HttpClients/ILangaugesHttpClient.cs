﻿using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ILanguagesHttpClient
    {
        Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false);

        Task<KinaUnaLanguage> GetLanguage(int languageId, bool updateCache = false);
        Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> DeleteLanguage(KinaUnaLanguage language);
    }
}
