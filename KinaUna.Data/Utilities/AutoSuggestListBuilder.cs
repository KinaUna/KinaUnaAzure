using System.Collections.Generic;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Utilities
{
    public class AutoSuggestListBuilder
    {
        private readonly List<string> _categoryAutoSuggestList = [];

        public void AddItemsToCategoriesList(IEnumerable<ICategorical> itemsList)
        {
            foreach (ICategorical categoricalItem  in itemsList)
            {
                if (string.IsNullOrEmpty(categoricalItem.Category)) continue;

                List<string> categoriesList = [.. categoricalItem.Category.Split(',')];
                foreach (string categoryString in categoriesList)
                {
                    if (!string.IsNullOrEmpty(categoryString) && !_categoryAutoSuggestList.Contains(categoryString.Trim()))
                    {
                        _categoryAutoSuggestList.Add(categoryString.Trim());
                    }
                }
            }
        }

        public List<string> GetCategoriesList()
        {
            return _categoryAutoSuggestList;
        }
    }
}
