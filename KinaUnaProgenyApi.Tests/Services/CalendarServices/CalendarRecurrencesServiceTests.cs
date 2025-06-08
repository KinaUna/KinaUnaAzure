using System.Diagnostics;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.CalendarServices
{
    public class CalendarRecurrencesServiceTests
    {
        [Fact]
        public async Task GetRecurringEventsForProgeny_Should_Return_Empty_List_When_No_RecurrenceRules()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetRecurringEventsForProgeny_NoRules")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetRecurringEventsForProgeny(1, start, end, false);
            
            // Assert
            Assert.Empty(results);
        }
        
        [Fact]
        public async Task GetRecurringEventsForProgeny_Should_Return_Items_When_Rules_Exist()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetRecurringEventsForProgeny_WithRules")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (daily)
            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = dailyRule.RecurrenceRuleId,
                Title = "Daily Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(dailyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetRecurringEventsForProgeny(progenyId, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            // Expect 4 items (Jan 2-5, original Jan 1 excluded as per includeOriginal=false)
            Assert.Equal(4, results.Count);
            
            // Verify titles are preserved
            Assert.All(results, item => Assert.Equal("Daily Meeting", item.Title));
            
            // Verify dates are sequential
            Assert.Collection(results,
                item => Assert.Equal(new DateTime(2023, 1, 2, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 3, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 4, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 5, 10, 0, 0, DateTimeKind.Utc), item.StartTime)
            );
        }
        
        [Fact]
        public async Task GetRecurringEventsForProgeny_Should_Include_Original_When_Specified()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetRecurringEventsForProgeny_IncludeOriginal")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (daily)
            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = dailyRule.RecurrenceRuleId,
                Title = "Daily Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(dailyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 5, 23, 59, 59, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetRecurringEventsForProgeny(progenyId, start, end, true);
            
            // Assert
            Assert.NotEmpty(results);
            // Expect 5 items (Jan 1-5, including original)
            Assert.Equal(5, results.Count);
            
            // Verify first item is for Jan 1
            Assert.Equal(new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc), results.First().StartTime);
        }
        
        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_Rule_Is_Null()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetCalendarItemsForRecurrenceRule_NullRule")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(null, start, end, false);
            
            // Assert
            Assert.Empty(results);
        }
        
        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_StartDate_Is_After_EndDate()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetCalendarItemsForRecurrenceRule_StartAfterEnd")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc), // Future date
                EndOption = 0,
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false);
            
            // Assert
            Assert.Empty(results);
        }
        
        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_UntilDate_Is_Before_StartDate()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetCalendarItemsForRecurrenceRule_UntilBeforeStart")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 1, // End on date
                Until = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc), // Before search start
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false);
            
            // Assert
            Assert.Empty(results);
        }
        
        [Fact]
        public async Task GetDailyRecurrences_Should_Generate_Daily_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetDailyRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (daily)
            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = dailyRule.RecurrenceRuleId,
                Title = "Daily Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(dailyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 23, 59, 59, DateTimeKind.Utc);
            
            // Act - this will internally call GetDailyRecurrences
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(dailyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            // Expect 9 items (Jan 2-10, original Jan 1 excluded per includeOriginal=false)
            Assert.Equal(9, results.Count);
            
            // Verify time is preserved
            Assert.All(results, item =>
            {
                if (item.StartTime != null) Assert.Equal(10, item.StartTime.Value.Hour);
            });
            Assert.All(results, item =>
            {
                if (item.EndTime != null) Assert.Equal(11, item.EndTime.Value.Hour);
            });
            
            // Verify properties are copied correctly
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            Assert.All(results, item => 
            {
                Assert.Equal(calendarItem.Title, item.Title);
                Assert.Equal(calendarItem.ProgenyId, item.ProgenyId);
                Assert.NotEqual(0, item.EventId); // New items should have default EventId
            });
        }
        
        [Fact]
        public async Task GetDailyRecurrences_Should_Handle_Interval()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetDailyRecurrences_HandleInterval")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (every 2 days)
            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 1, // Daily
                Interval = 2, // Every 2 days
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = dailyRule.RecurrenceRuleId,
                Title = "Every Other Day Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(dailyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(dailyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            // Expect 5 items (Jan 3, 5, 7, 9 - skipping original Jan 1 per includeOriginal=false)
            Assert.Equal(4, results.Count);
            
            // Verify dates are correct (every other day)
            Assert.Collection(results,
                item => Assert.Equal(new DateTime(2023, 1, 3, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 5, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 7, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 9, 10, 0, 0, DateTimeKind.Utc), item.StartTime)
            );
        }
        
        [Fact]
        public async Task GetWeeklyRecurrences_Should_Generate_Weekly_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetWeeklyRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (weekly on Monday and Wednesday)
            RecurrenceRule weeklyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 2, // Weekly
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Sunday
                EndOption = 0, // Never
                ByDay = "MO,WE", // Monday and Wednesday
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = weeklyRule.RecurrenceRuleId,
                Title = "Weekly Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc), // Sunday
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(weeklyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(weeklyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            
            // Verify we get the expected dates (Mondays: Jan 2, 9) and (Wednesdays: Jan 4, 11)
            List<DateTime> dates = [.. results.Select(r =>
            {
                Debug.Assert(r.StartTime != null, "r.StartTime != null");
                return r.StartTime.Value.Date;
            }).OrderBy(d => d)];
            
            Assert.Contains(new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc), dates); // Monday
            Assert.Contains(new DateTime(2023, 1, 4, 0, 0, 0, DateTimeKind.Utc), dates); // Wednesday
            Assert.Contains(new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc), dates); // Monday
            Assert.Contains(new DateTime(2023, 1, 11, 0, 0, 0, DateTimeKind.Utc), dates); // Wednesday
        }
        
        [Fact]
        public async Task GetMonthlyByDayRecurrences_Should_Generate_Monthly_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetMonthlyByDayRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (monthly on the second Monday)
            RecurrenceRule monthlyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 3, // Monthly by day
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "2MO", // Second Monday of the month
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = monthlyRule.RecurrenceRuleId,
                Title = "Monthly Meeting",
                StartTime = new DateTime(2023, 1, 9, 10, 0, 0, DateTimeKind.Utc), // Second Monday of January
                EndTime = new DateTime(2023, 1, 9, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(monthlyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 6, 30, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(monthlyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            
            // Should include the second Monday of each month (Feb 13, Mar 13, Apr 10, May 8, Jun 12)
            List<DateTime> dates = [.. results.Select(r =>
            {
                Debug.Assert(r.StartTime != null, "r.StartTime != null");
                return r.StartTime.Value.Date;
            }).OrderBy(d => d)];
            
            Assert.Contains(new DateTime(2023, 2, 13, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 3, 13, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 4, 10, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 5, 8, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 6, 12, 0, 0, 0, DateTimeKind.Utc), dates);
        }
        
        [Fact]
        public async Task GetMonthlyByDateRecurrences_Should_Generate_Monthly_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetMonthlyByDateRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (monthly on the 15th)
            RecurrenceRule monthlyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 4, // Monthly by date
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "15", // 15th of each month
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = monthlyRule.RecurrenceRuleId,
                Title = "Monthly Payment",
                StartTime = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 15, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(monthlyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 5, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(monthlyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            
            // Should include the 15th of each month (Feb 15, Mar 15, Apr 15, May 15) - skipping Jan 15 as it's the original
            List<DateTime> dates = [.. results.Select(r =>
            {
                Debug.Assert(r.StartTime != null, "r.StartTime != null");
                return r.StartTime.Value.Date;
            }).OrderBy(d => d)];
            
            Assert.Contains(new DateTime(2023, 2, 15, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 3, 15, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 4, 15, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 5, 15, 0, 0, 0, DateTimeKind.Utc), dates);
        }
        
        [Fact]
        public async Task GetYearlyByDayRecurrences_Should_Generate_Yearly_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetYearlyByDayRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (yearly on the 3rd Thursday of November - like Thanksgiving)
            RecurrenceRule yearlyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 5, // Yearly by day
                Interval = 1,
                Start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "3TH", // Third Thursday
                ByMonthDay = "",
                ByMonth = "11" // November
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = yearlyRule.RecurrenceRuleId,
                Title = "Thanksgiving",
                StartTime = new DateTime(2020, 11, 19, 18, 0, 0, DateTimeKind.Utc), // 3rd Thursday in Nov 2020
                EndTime = new DateTime(2020, 11, 19, 22, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(yearlyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(yearlyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            
            // Verify we have the correct dates for Thanksgiving (3rd Thursday in November)
            // 2021: Nov 18, 2022: Nov 17, 2023: Nov 16
            List<DateTime> dates = [.. results.Select(r =>
            {
                Debug.Assert(r.StartTime != null, "r.StartTime != null");
                return r.StartTime.Value.Date;
            }).OrderBy(d => d)];
            
            Assert.Contains(new DateTime(2021, 11, 18, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2022, 11, 17, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 11, 16, 0, 0, 0, DateTimeKind.Utc), dates);
        }
        
        [Fact]
        public async Task GetYearlyByDateRecurrences_Should_Generate_Yearly_Events()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetYearlyByDateRecurrences_GenerateEvents")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (yearly on July 4 - Independence Day)
            RecurrenceRule yearlyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 6, // Yearly by date
                Interval = 1,
                Start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0, // Never
                ByDay = "",
                ByMonthDay = "4", // 4th day of the month
                ByMonth = "7" // July
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = yearlyRule.RecurrenceRuleId,
                Title = "Independence Day",
                StartTime = new DateTime(2020, 7, 4, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2020, 7, 4, 22, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(yearlyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetCalendarItemsForRecurrenceRule(yearlyRule, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            
            // Verify we have the correct dates for July 4th each year
            List<DateTime> dates = [.. results.Select(r =>
            {
                Debug.Assert(r.StartTime != null, "r.StartTime != null");
                return r.StartTime.Value.Date;
            }).OrderBy(d => d)];
            
            Assert.Contains(new DateTime(2021, 7, 4, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2022, 7, 4, 0, 0, 0, DateTimeKind.Utc), dates);
            Assert.Contains(new DateTime(2023, 7, 4, 0, 0, 0, DateTimeKind.Utc), dates);
        }
        
        [Fact]
        public async Task GetRecurringEventsForProgeny_Should_Handle_Count_EndOption()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetRecurringEventsForProgeny_HandleCount")
                .Options;
            
            await using ProgenyDbContext context = new(dbOptions);
            
            int progenyId = 1;
            
            // Create a recurrence rule (daily with Count = 5)
            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = progenyId,
                Frequency = 1, // Daily
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 2, // After count
                Count = 5, // Only 5 occurrences total
                ByDay = "",
                ByMonthDay = "",
                ByMonth = ""
            };
            
            // Create a calendar item for the rule
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = progenyId,
                RecurrenceRuleId = dailyRule.RecurrenceRuleId,
                Title = "Limited Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            
            context.Add(dailyRule);
            context.Add(calendarItem);
            await context.SaveChangesAsync();
            
            CalendarRecurrencesService service = new(context);
            
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc);
            
            // Act
            List<CalendarItem> results = await service.GetRecurringEventsForProgeny(progenyId, start, end, false);
            
            // Assert
            Assert.NotEmpty(results);
            // Expect 4 items (Jan 2-5, original Jan 1 excluded as per includeOriginal=false)
            // The Count of 5 includes the original event
            Assert.Equal(4, results.Count);
            
            // Verify dates are sequential and only up to the count limit
            Assert.Collection(results,
                item => Assert.Equal(new DateTime(2023, 1, 2, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 3, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 4, 10, 0, 0, DateTimeKind.Utc), item.StartTime),
                item => Assert.Equal(new DateTime(2023, 1, 5, 10, 0, 0, DateTimeKind.Utc), item.StartTime)
            );
        }
    }
}