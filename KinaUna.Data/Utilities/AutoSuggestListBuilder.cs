using System.Collections.Generic;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Utilities
{
    public class AutoSuggestListBuilder
    {
        private readonly List<string> _categoriesAutoSuggestList = [];
        private readonly List<string> _contextsAutoSuggestList = [];
        private readonly List<string> _locationsAutoSuggestList = [];
        private readonly List<string> _tagsAutoSuggestList = [];

        public void AddItemsToCategoriesList(IEnumerable<ICategorical> itemsList)
        {
            foreach (ICategorical categoricalItem  in itemsList)
            {
                if (string.IsNullOrEmpty(categoricalItem.Category)) continue;

                List<string> categoriesList = [.. categoricalItem.Category.Split(',')];
                foreach (string categoryString in categoriesList)
                {
                    if (!string.IsNullOrEmpty(categoryString) && !_categoriesAutoSuggestList.Contains(categoryString.Trim()))
                    {
                        _categoriesAutoSuggestList.Add(categoryString.Trim());
                    }
                }
            }
        }

        public List<string> GetCategoriesList()
        {
            return _categoriesAutoSuggestList;
        }

        public void AddItemsToContextsList(IEnumerable<IContexted> itemsList)
        {
            foreach (IContexted contextedItem in itemsList)
            {
                if (string.IsNullOrEmpty(contextedItem.Context)) continue;

                List<string> contextsList = [.. contextedItem.Context.Split(',')];
                foreach (string contextString in contextsList)
                {
                    if (!string.IsNullOrEmpty(contextString) && !_contextsAutoSuggestList.Contains(contextString.Trim()))
                    {
                        _contextsAutoSuggestList.Add(contextString.Trim());
                    }
                }
            }
        }

        public List<string> GetContextsList()
        {
            return _contextsAutoSuggestList;
        }

        public void AddItemsToLocationsList(IEnumerable<ILocatable> itemsList)
        {
            foreach (ILocatable locatableItem in itemsList)
            {
                if (string.IsNullOrEmpty(locatableItem.GetLocationString())) continue;

                List<string> locationsList = [.. locatableItem.GetLocationString().Split(',')];
                foreach (string locationString in locationsList)
                {
                    if (!string.IsNullOrEmpty(locationString) && !_locationsAutoSuggestList.Contains(locationString.Trim()))
                    {
                        _locationsAutoSuggestList.Add(locationString.Trim());
                    }
                }
            }
        }

        public List<string> GetLocationsList()
        {
            return _locationsAutoSuggestList;
        }

        public void AddItemsToTagsList(IEnumerable<ITaggable> itemsList)
        {
            foreach (ITaggable taggableItem in itemsList)
            {
                if (string.IsNullOrEmpty(taggableItem.Tags)) continue;

                List<string> tagsList = [.. taggableItem.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!string.IsNullOrEmpty(tagString) && !_tagsAutoSuggestList.Contains(tagString.Trim()))
                    {
                        _tagsAutoSuggestList.Add(tagString.Trim());
                    }
                }
            }
        }

        public List<string> GetTagsList()
        {
            return _tagsAutoSuggestList;
        }
    }
}
