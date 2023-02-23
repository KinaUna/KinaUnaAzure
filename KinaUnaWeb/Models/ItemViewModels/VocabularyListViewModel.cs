using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseItemsViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; }
        public List<WordDateCount> ChartData { get; set; } = new List<WordDateCount>();

        public VocabularyListViewModel()
        {
            VocabularyList = new List<VocabularyItemViewModel>();
        }

        public VocabularyListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetVocabularyList(List<VocabularyItem> vocabularyList)
        {
            VocabularyList = new List<VocabularyItemViewModel>();

            vocabularyList = vocabularyList.OrderBy(w => w.Date).ToList();
            if (vocabularyList.Count != 0)
            {
                foreach (VocabularyItem vocabularyItem in vocabularyList)
                {
                    if (vocabularyItem.AccessLevel >= CurrentAccessLevel)
                    {
                        VocabularyItemViewModel vocabularyItemViewModel = new VocabularyItemViewModel();
                        vocabularyItemViewModel.VocabularyItem.ProgenyId = vocabularyItem.ProgenyId;
                        vocabularyItemViewModel.VocabularyItem.Date = vocabularyItem.Date;
                        vocabularyItemViewModel.VocabularyItem.DateAdded = vocabularyItem.DateAdded;
                        vocabularyItemViewModel.VocabularyItem.Description = vocabularyItem.Description;
                        vocabularyItemViewModel.VocabularyItem.Language = vocabularyItem.Language;
                        vocabularyItemViewModel.VocabularyItem.SoundsLike = vocabularyItem.SoundsLike;
                        vocabularyItemViewModel.VocabularyItem.Word = vocabularyItem.Word;
                        vocabularyItemViewModel.IsCurrentUserProgenyAdmin = IsCurrentUserProgenyAdmin;
                        vocabularyItemViewModel.VocabularyItem.WordId = vocabularyItem.WordId;

                        VocabularyList.Add(vocabularyItemViewModel);
                    }
                }
            }
        }

        public void SetChartData()
        {
            List<WordDateCount> dateTimesList = new List<WordDateCount>();
            int wordCount = 0;
            foreach (VocabularyItemViewModel wordItem in VocabularyList)
            {
                wordCount++;
                if (wordItem.VocabularyItem.Date != null)
                {
                    if (dateTimesList.SingleOrDefault(d => d.WordDate.Date == wordItem.VocabularyItem.Date.Value.Date) == null)
                    {
                        WordDateCount newDate = new WordDateCount();
                        newDate.WordDate = wordItem.VocabularyItem.Date.Value.Date;
                        newDate.WordCount = wordCount;
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
            }

            ChartData = dateTimesList;
        }
    }
}
