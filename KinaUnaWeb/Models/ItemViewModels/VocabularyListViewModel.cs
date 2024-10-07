using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseItemsViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; }
        public List<WordDateCount> ChartData { get; set; } = [];
        public int VocabularyId { get; set; }

        public VocabularyListViewModel()
        {
            VocabularyList = [];
        }

        public VocabularyListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetVocabularyList(List<VocabularyItem> vocabularyList)
        {
            VocabularyList = [];

            vocabularyList = [.. vocabularyList.OrderBy(w => w.Date)];
            if (vocabularyList.Count == 0) return;

            foreach (VocabularyItem vocabularyItem in vocabularyList)
            {
                if (vocabularyItem.AccessLevel < CurrentAccessLevel) continue;

                VocabularyItemViewModel vocabularyItemViewModel = new()
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
                        WordId = vocabularyItem.WordId
                    },
                    IsCurrentUserProgenyAdmin = IsCurrentUserProgenyAdmin
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
