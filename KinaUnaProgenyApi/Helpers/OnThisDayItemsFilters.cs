﻿using System;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace KinaUnaProgenyApi.Helpers
{
    public static class OnThisDayItemsFilters
    {
        /// <summary>
        /// Filters a list of TimeLineItems by TimeLineType(s).
        /// </summary>
        /// <param name="timeLineItems">The list of TimeLineItems to filter.</param>
        /// <param name="timeLineTypes">A list of TimeLineTypes to include in the result.</param>
        /// <returns>A list of TimeLineItem objects, containing only the TimeLineType(s) specified.</returns>
        public static List<TimeLineItem> FilterOnThisDayItemsByTimeLineType(List<TimeLineItem> timeLineItems, List<KinaUnaTypes.TimeLineType> timeLineTypes)
        {
            if (timeLineTypes.Count <= 0) return timeLineItems;

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (KinaUnaTypes.TimeLineType timeLineType in timeLineTypes)
            {
                foreach (TimeLineItem timeLineItem in timeLineItems)
                {
                    if (timeLineItem.ItemType == (int)timeLineType)
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }
            }

            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters a list of TimeLineItems by the OnThisDayPeriod specified in the OnThisDayRequest.
        /// </summary>
        /// <param name="timeLineItems">The list of TimeLineItems to filter.</param>
        /// <param name="onThisDayRequest">The OnThisDayRequest object containing the OnThisDayPeriod and ThisDayDateTime to filter by.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public static List<TimeLineItem> FilterOnThisDayItemsByPeriod(List<TimeLineItem> timeLineItems, OnThisDayRequest onThisDayRequest)
        {
            switch (onThisDayRequest.OnThisDayPeriod)
            {
                case OnThisDayPeriod.Week:
                    timeLineItems = timeLineItems.Where(t => t.ProgenyTime.DayOfWeek == onThisDayRequest.ThisDayDateTime.DayOfWeek).ToList();
                    break;
                case OnThisDayPeriod.Month:
                    timeLineItems = timeLineItems.Where(t => t.ProgenyTime.Day == onThisDayRequest.ThisDayDateTime.Day).ToList();
                    break;
                case OnThisDayPeriod.Quarter:
                {
                    List<TimeLineItem> quarterTimeLineItems = timeLineItems.Where(t => t.ProgenyTime.Day == onThisDayRequest.ThisDayDateTime.Day).ToList();
                    quarterTimeLineItems = quarterTimeLineItems.Where(t => 
                        t.ProgenyTime.Month == onThisDayRequest.ThisDayDateTime.Month 
                         || t.ProgenyTime.Month == onThisDayRequest.ThisDayDateTime.Month + 3
                         || t.ProgenyTime.Month == onThisDayRequest.ThisDayDateTime.Month + 6 
                         || t.ProgenyTime.Month == onThisDayRequest.ThisDayDateTime.Month + 9)
                        .ToList();
                    timeLineItems = quarterTimeLineItems;
                    break;
                }
                case OnThisDayPeriod.Year:
                    timeLineItems = timeLineItems.Where(t => t.ProgenyTime.Month == onThisDayRequest.ThisDayDateTime.Month && t.ProgenyTime.Day == onThisDayRequest.ThisDayDateTime.Day).ToList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(onThisDayRequest), "onThisDayRequest.OnThisDayPeriod is out of range.");
            }

            return timeLineItems;
        }
    }
}
