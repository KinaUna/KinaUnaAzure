using System;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class OnThisDayViewModel : BaseItemsViewModel
    {
        public OnThisDayRequest OnThisDayRequest { get; set; }

        public OnThisDayViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetRequestParameters(int skip, int numberOfItems, OnThisDayPeriod onThisDayPeriod, int year, int month, int day, string tagFilter, int sortOrder)
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

            OnThisDayRequest = new OnThisDayRequest
            {
                ProgenyId = CurrentProgeny.Id,
                Skip = skip,
                NumberOfItems = numberOfItems,
                OnThisDayPeriod = onThisDayPeriod,
                ThisDayDateTime = thisDayDateTime,
                TagFilter = tagFilter,
                AccessLevel = CurrentAccessLevel,
                TimeLineTypeFilter = [],
                SortOrder = sortOrder
            };
        }
    }
}