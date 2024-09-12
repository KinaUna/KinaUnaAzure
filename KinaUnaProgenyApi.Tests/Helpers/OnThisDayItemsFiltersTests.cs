using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Helpers;

namespace KinaUnaProgenyApi.Tests.Helpers
{
    public class OnThisDayItemsFiltersTests
    {
        [Fact]
        public void FilterOnThisDayItemsByTimeLineType_Returns_All_Items_When_TimeLineTypes_Is_Empty()
        {
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Calendar },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Contact },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Note },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Sleep },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Video }
            ];

            List<KinaUnaTypes.TimeLineType> timeLineTypes = new List<KinaUnaTypes.TimeLineType>();
            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByTimeLineType(timeLineItems, timeLineTypes);

            Assert.Equal(timeLineItems.Count, result.Count);
        }

        [Fact]
        public void FilterOnThisDayItemsByTimeLineType_Returns_Empty_List_When_No_TimeLineItems_Have_The_TimeLineTypes()
        {
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Calendar },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Contact },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Note },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Sleep },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Video }
            ];

            List<KinaUnaTypes.TimeLineType> timeLineTypes =
            [
                KinaUnaTypes.TimeLineType.Friend
            ];

            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByTimeLineType(timeLineItems, timeLineTypes);

            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.Empty(result);
        }

        [Fact]
        public void FilterOnThisDayItemsByTimeLineType_Returns_List_With_Same_Types_As_The_TimeLineTypes_List_Only()
        {
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Video },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Video },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Photo },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Video },
                new TimeLineItem { ItemType = (int)KinaUnaTypes.TimeLineType.Sleep }
            ];

            List<KinaUnaTypes.TimeLineType> timeLineTypes =
            [
                KinaUnaTypes.TimeLineType.Photo,
                KinaUnaTypes.TimeLineType.Video
            ];
            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByTimeLineType(timeLineItems, timeLineTypes);

            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.NotEmpty(result);
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void FilterOnThisDayItemsByPeriod_Returns_Only_Items_With_Same_Day_When_Period_Is_Week()
        {
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-1) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-2) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-3) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-4) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-5) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-6) },
                new TimeLineItem { ProgenyTime = DateTime.UtcNow.AddDays(-7) }
            ];

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ThisDayDateTime = DateTime.UtcNow,
                OnThisDayPeriod = OnThisDayPeriod.Week
            };

            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByPeriod(timeLineItems, onThisDayRequest);

            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.NotEmpty(result);
            Assert.Single(result);
        }

        [Fact]
        public void FilterOnThisDayItemsByPeriod_Returns_Only_Items_With_Same_DayOfMonth_When_Period_Is_Month()
        {
            DateTime sampleDateTime = new DateTime(2000, 1, 1);
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-1) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-2) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-14) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-30) },
            ];

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ThisDayDateTime = sampleDateTime,
                OnThisDayPeriod = OnThisDayPeriod.Month
            };

            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByPeriod(timeLineItems, onThisDayRequest);

            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.NotEmpty(result);
            Assert.Single(result);
        }

        [Fact]
        public void FilterOnThisDayItemsByPeriod_Returns_Only_Items_With_Same_DayOfMonth_When_Period_Is_Quarter()
        {
            DateTime sampleDateTime = new DateTime(2000, 1, 1);
            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-12) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-3) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-6) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-9) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-2) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-14) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-30) },
            ];

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ThisDayDateTime = sampleDateTime,
                OnThisDayPeriod = OnThisDayPeriod.Quarter
            };

            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByPeriod(timeLineItems, onThisDayRequest);

            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.NotEmpty(result);
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void FilterOnThisDayItemsByPeriod_Returns_Only_Items_With_Same_DayOfYear_When_Period_Is_Year()
        {
            DateTime sampleDateTime = new DateTime(2000, 1, 1);

            List<TimeLineItem> timeLineItems =
            [
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-12) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-24) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-6) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddMonths(-9) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-2) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-14) },
                new TimeLineItem { ProgenyTime = sampleDateTime.AddDays(-30) },
            ];

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ThisDayDateTime = sampleDateTime,
                OnThisDayPeriod = OnThisDayPeriod.Year
            };

            List<TimeLineItem> result = OnThisDayItemsFilters.FilterOnThisDayItemsByPeriod(timeLineItems, onThisDayRequest);
            Assert.NotEqual(timeLineItems.Count, result.Count);
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
        }
    }
}
