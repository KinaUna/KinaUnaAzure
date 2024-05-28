using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyItemViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; } = [];
        public VocabularyItem VocabularyItem { get; set; } = new();
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public VocabularyItemViewModel()
        {
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public VocabularyItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetProgenyList()
        {
            VocabularyItem.ProgenyId = CurrentProgenyId;
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[VocabularyItem.AccessLevel].Selected = true;
            AccessLevelListDa[VocabularyItem.AccessLevel].Selected = true;
            AccessLevelListDe[VocabularyItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetPropertiesFromVocabularyItem(VocabularyItem vocabularyItem)
        {
            VocabularyItem.AccessLevel = vocabularyItem.AccessLevel;
            VocabularyItem.Author = vocabularyItem.Author;
            VocabularyItem.Date = vocabularyItem.Date ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

            VocabularyItem.DateAdded = vocabularyItem.DateAdded;
            VocabularyItem.Description = vocabularyItem.Description;
            VocabularyItem.Language = vocabularyItem.Language;
            VocabularyItem.SoundsLike = vocabularyItem.SoundsLike;
            VocabularyItem.Word = vocabularyItem.Word;
            VocabularyItem.WordId = vocabularyItem.WordId;
        }
    }


    public class WordDateCount
    {
        public DateTime WordDate { get; set; }
        public int WordCount { get; set; }
    }
}
