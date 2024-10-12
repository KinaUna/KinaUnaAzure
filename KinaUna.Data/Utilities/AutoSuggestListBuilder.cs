using System.Collections.Generic;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Utilities
{
    public class AutoSuggestListBuilder
    {
        private readonly List<string> _categoryAutoSuggestList = [];
        private readonly List<string> _contextAutoSuggestList = [];

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

        public void AddItemsToContextsList(IEnumerable<IContexted> itemsList)
        {
            foreach (IContexted contextedItem in itemsList)
            {
                if (string.IsNullOrEmpty(contextedItem.Context)) continue;

                List<string> contextsList = [.. contextedItem.Context.Split(',')];
                foreach (string contextString in contextsList)
                {
                    if (!string.IsNullOrEmpty(contextString) && !_contextAutoSuggestList.Contains(contextString.Trim()))
                    {
                        _contextAutoSuggestList.Add(contextString.Trim());
                    }
                }
            }
        }

        public List<string> GetContextsList()
        {
            return _contextAutoSuggestList;
        }
    }
}
