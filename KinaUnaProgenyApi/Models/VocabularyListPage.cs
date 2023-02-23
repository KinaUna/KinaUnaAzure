using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class VocabularyListPage
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<VocabularyItem> VocabularyList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public VocabularyListPage()
        {
            VocabularyList = new List<VocabularyItem>();
        }

        public void ProcessVocabularyList(List<VocabularyItem> vocabularyList, int sortBy, int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            vocabularyList = vocabularyList.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                vocabularyList.Reverse();
            }

            int vocabularyCounter = 1;
            int vocabularyCount = vocabularyList.Count;
            foreach (VocabularyItem word in vocabularyList)
            {
                if (sortBy == 1)
                {
                    word.VocabularyItemNumber = vocabularyCount - vocabularyCounter + 1;
                }
                else
                {
                    word.VocabularyItemNumber = vocabularyCounter;
                }

                vocabularyCounter++;
            }

            List<VocabularyItem> itemsOnPage = vocabularyList
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();
            
            VocabularyList = itemsOnPage;
            TotalPages = (int)Math.Ceiling(vocabularyList.Count / (double)pageSize);
            PageNumber = pageIndex;
            SortBy = sortBy;
        }
    }
}
