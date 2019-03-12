using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyItemViewModel
    {
        public int WordId { get; set; }
        public string Word { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string SoundsLike { get; set; }
        public DateTime? Date { get; set; }
        public DateTime DateAdded { get; set; }
        public string Author { get; set; }
        public int ProgenyId { get; set; }
        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public int AccessLevel { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public VocabularyItemViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }

    public class WordDateCount
    {
        public DateTime WordDate { get; set; }
        public int WordCount { get; set; }
    }
}
