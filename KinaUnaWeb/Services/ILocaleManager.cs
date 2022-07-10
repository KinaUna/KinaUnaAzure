﻿using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.HomeViewModels;

namespace KinaUnaWeb.Services
{
    public interface ILocaleManager
    {
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);
        
        Task<string> GetTranslation(string word, string page, int languageId);
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId);
    }
}
