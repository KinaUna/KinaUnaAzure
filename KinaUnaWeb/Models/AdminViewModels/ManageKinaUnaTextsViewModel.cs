﻿using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.AdminViewModels
{
    public class ManageKinaUnaTextsViewModel : BaseViewModel
    {

        public List<KinaUnaText> Texts { get; set; }
        public List<string> PagesList { get; set; }
        public List<string> TitlesList { get; set; }
        public List<KinaUnaLanguage> LanguagesList { get; set; }
        public int Language { get; set; }
        public KinaUnaText KinaUnaText { get; set; }
        public int MessageId { get; set; }

        public ManageKinaUnaTextsViewModel()
        {
            Texts = new List<KinaUnaText>();
            PagesList = new List<string>();
            TitlesList = new List<string>();
            LanguagesList = new List<KinaUnaLanguage>();
        }
    }
}
