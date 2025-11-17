using System.Collections.Generic;
using System.Linq;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseItemsViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; }
        public List<WordDateCount> ChartData { get; set; } = [];
        public int VocabularyId { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public VocabularyListViewModel()
        {
            VocabularyList = [];
        }

        public VocabularyListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetVocabularyList(List<VocabularyItem> vocabularyList, BaseItemsViewModel baseItemsViewModel)
        {
            VocabularyList = [];

            vocabularyList = [.. vocabularyList.OrderBy(w => w.Date)];
            if (vocabularyList.Count == 0) return;

            foreach (VocabularyItem vocabularyItem in vocabularyList)
            {
                VocabularyItemViewModel vocabularyItemViewModel = new(baseItemsViewModel)
                {
                    VocabularyItem =
                    {
                        ProgenyId = vocabularyItem.ProgenyId,
                        Date = vocabularyItem.Date,
                        DateAdded = vocabularyItem.DateAdded,
                        Description = vocabularyItem.Description,
                        Language = vocabularyItem.Language,
                        SoundsLike = vocabularyItem.SoundsLike,
                        Word = vocabularyItem.Word,
                        WordId = vocabularyItem.WordId,
                        ItemPerMission = vocabularyItem.ItemPerMission
                    }
                };

                VocabularyList.Add(vocabularyItemViewModel);
            }
        }

        public void SetChartData()
        {
            List<WordDateCount> dateTimesList = [];
            int wordCount = 0;
            foreach (VocabularyItemViewModel wordItem in VocabularyList)
            {
                wordCount++;
                if (wordItem.VocabularyItem.Date == null) continue;

                if (dateTimesList.SingleOrDefault(d => d.WordDate.Date == wordItem.VocabularyItem.Date.Value.Date) == null)
                {
                    WordDateCount newDate = new()
                    {
                        WordDate = wordItem.VocabularyItem.Date.Value.Date,
                        WordCount = wordCount
                    };
                    dateTimesList.Add(newDate);
                }
                else
                {
                    WordDateCount wrdDateCount = dateTimesList.SingleOrDefault(d => d.WordDate.Date == wordItem.VocabularyItem.Date.Value.Date);
                    if (wrdDateCount != null)
                    {
                        wrdDateCount.WordCount = wordCount;
                    }
                }
            }

            ChartData = dateTimesList;
        }
    }
}
