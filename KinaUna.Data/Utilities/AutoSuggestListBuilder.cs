using System.Collections.Generic;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Utilities
{
    /// <summary>
    /// Provides functionality to build and manage auto-suggest lists for categories, contexts, locations, and tags.
    /// </summary>
    /// <remarks>This class allows adding items to specific auto-suggest lists based on their properties
    /// (e.g., categories, contexts, locations, or tags) and retrieving the resulting lists. Duplicate entries are
    /// automatically filtered, and items are trimmed of whitespace.</remarks>
    public class AutoSuggestListBuilder
    {
        private readonly List<string> _categoriesAutoSuggestList = [];
        private readonly List<string> _contextsAutoSuggestList = [];
        private readonly List<string> _locationsAutoSuggestList = [];
        private readonly List<string> _tagsAutoSuggestList = [];

        /// <summary>
        /// Adds the categories from the provided list of items to the internal category suggestion list.
        /// </summary>
        /// <remarks>This method processes each item in the provided collection, extracts the categories
        /// from the <c>Category</c> property, and adds them to the internal category suggestion list if they are not
        /// already present. Empty or null category values are ignored.</remarks>
        /// <param name="itemsList">A collection of items implementing <see cref="ICategorical"/>. Each item's <c>Category</c> property is
        /// expected to contain a comma-separated list of category names.</param>
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

        /// <summary>
        /// Retrieves the list of categories available for auto-suggestion.
        /// </summary>
        /// <returns>A list of strings representing the available categories. The list may be empty if no categories are defined.</returns>
        public List<string> GetCategoriesList()
        {
            return _categoriesAutoSuggestList;
        }

        /// <summary>
        /// Adds the contexts from the specified list of items to the internal context suggestion list.
        /// </summary>
        /// <remarks>This method processes each item in the provided collection, extracts the context
        /// strings from the <c>Context</c> property, and adds them to the internal suggestion list if they are not
        /// already present. Empty or null context strings are ignored. Duplicate entries are not added to the
        /// suggestion list.</remarks>
        /// <param name="itemsList">A collection of items implementing <see cref="IContexted"/>. Each item's <c>Context</c> property is expected
        /// to contain a comma-separated list of context strings.</param>
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

        /// <summary>
        /// Retrieves the list of context names available for auto-suggestion.
        /// </summary>
        /// <returns>A list of strings containing the context names. The list may be empty if no contexts are available.</returns>
        public List<string> GetContextsList()
        {
            return _contextsAutoSuggestList;
        }

        /// <summary>
        /// Adds location strings from a collection of locatable items to the auto-suggest list.
        /// </summary>
        /// <remarks>This method processes each item in the provided collection, extracts its location
        /// string, splits it into individual locations (if it contains multiple, separated by commas), and adds each
        /// unique, non-empty location to the auto-suggest list. Duplicate or empty locations are ignored.</remarks>
        /// <param name="itemsList">A collection of objects implementing <see cref="ILocatable"/>. Each object's location string will be parsed
        /// and added to the auto-suggest list if it is not null, empty, or already present.</param>
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

        /// <summary>
        /// Retrieves the list of location suggestions.
        /// </summary>
        /// <returns>A list of strings containing location suggestions. The list may be empty if no suggestions are available.</returns>
        public List<string> GetLocationsList()
        {
            return _locationsAutoSuggestList;
        }

        /// <summary>
        /// Adds the tags from the specified list of taggable items to the auto-suggest list.
        /// </summary>
        /// <remarks>This method processes each item in the provided collection, extracts the tags from
        /// the <c>Tags</c> property, and adds them to the internal auto-suggest list if they are not already present.
        /// Tags are trimmed of whitespace before being added. Empty or null tags are ignored.</remarks>
        /// <param name="itemsList">A collection of objects implementing the <see cref="ITaggable"/> interface. Each object's <c>Tags</c>
        /// property is expected to contain a comma-separated list of tags.</param>
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

        /// <summary>
        /// Retrieves the list of tags available for auto-suggestion.
        /// </summary>
        /// <returns>A list of strings representing the tags available for auto-suggestion. The list may be empty if no tags are
        /// available.</returns>
        public List<string> GetTagsList()
        {
            return _tagsAutoSuggestList;
        }
    }
}
