using System;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimelineRequestViewModel : BaseItemsViewModel
    {
        public TimelineRequest TimelineRequest { get; set; }

        public TimelineRequestViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetRequestParameters(int skip, int numberOfItems, int year, int month, int day, string tagFilter, string categoryFilter, string contextFilter, int sortOrder)
        {
            DateTime thisDayDateTime = DateTime.UtcNow;
            if (year > 0 && month > 0 && day > 0)
            {
                thisDayDateTime = new DateTime(year, month, day);
            }

            if (string.IsNullOrEmpty(tagFilter))
            {
                tagFilter = string.Empty;
            }

            TimelineRequest = new TimelineRequest
            {
                ProgenyId = CurrentProgeny.Id,
                Skip = skip,
                NumberOfItems = numberOfItems,
                TimelineStartDateTime = thisDayDateTime,
                TagFilter = tagFilter,
                CategoryFilter = categoryFilter,
                ContextFilter = contextFilter,
                AccessLevel = CurrentAccessLevel,
                TimeLineTypeFilter = [],
                SortOrder = sortOrder
            };
        }
    }
}