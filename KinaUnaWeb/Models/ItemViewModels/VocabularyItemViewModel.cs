using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyItemViewModel: BaseItemsViewModel
    {
        public VocabularyItem VocabularyItem { get; set; } = new();
        
        
        public VocabularyItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        /// <summary>
        /// Set the ProgenyList with the current ProgenyId selected.
        /// </summary>
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
        
        /// <summary>
        /// Set the VocabularyItem properties from a VocabularyItem object.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem to copy properties from.</param>
        public void SetPropertiesFromVocabularyItem(VocabularyItem vocabularyItem)
        {
            VocabularyItem.AccessLevel = vocabularyItem.AccessLevel;
            VocabularyItem.Author = vocabularyItem.Author;
            VocabularyItem.Date = vocabularyItem.Date ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            VocabularyItem.ProgenyId = vocabularyItem.ProgenyId;
            VocabularyItem.DateAdded = vocabularyItem.DateAdded;
            VocabularyItem.Description = vocabularyItem.Description;
            VocabularyItem.Language = vocabularyItem.Language;
            VocabularyItem.SoundsLike = vocabularyItem.SoundsLike;
            VocabularyItem.Word = vocabularyItem.Word;
            VocabularyItem.WordId = vocabularyItem.WordId;
            VocabularyItem.ItemPerMission = vocabularyItem.ItemPerMission;
        }
    }


    public class WordDateCount
    {
        public DateTime WordDate { get; set; }
        public int WordCount { get; set; }
    }
}
