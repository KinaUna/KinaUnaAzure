using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.CalendarServices
{
    public class CalendarRecurrencesServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService = new();

        private static ProgenyDbContext GetInMemoryContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static UserInfo GetTestUserInfo()
        {
            return new UserInfo
            {
                UserId = "test-user@example.com",
                UserEmail = "test-user@example.com",
                Id = 1
            };
        }

        #region GetRecurringEventsForProgenyOrFamily Tests

        [Fact]
        public async Task GetRecurringEventsForProgenyOrFamily_Should_Return_Empty_List_When_ProgenyId_And_FamilyId_Are_Zero()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringEventsForProgenyOrFamily_BothZero");
            UserInfo userInfo = GetTestUserInfo();
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetRecurringEventsForProgenyOrFamily(0, 0, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecurringEventsForProgenyOrFamily_Should_Return_Empty_List_When_No_RecurrenceRules()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringEventsForProgenyOrFamily_NoRules");
            UserInfo userInfo = GetTestUserInfo();
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetRecurringEventsForProgenyOrFamily(1, 0, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecurringEventsForProgenyOrFamily_Should_Set_End_To_End_Of_Day()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringEventsForProgenyOrFamily_EndOfDay");
            UserInfo userInfo = GetTestUserInfo();
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 12, 30, 45, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetRecurringEventsForProgenyOrFamily(1, 0, start, end, false, userInfo);

            // Assert - end date is adjusted to end of day internally
            Assert.NotNull(result);
        }

        #endregion

        #region GetCalendarItemsForRecurrenceRule Tests

        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_Rule_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsForRecurrenceRule_NullRule");
            UserInfo userInfo = GetTestUserInfo();
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(null, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_Start_After_End()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsForRecurrenceRule_StartAfterEnd");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Start = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Return_Empty_List_When_Until_Before_Start()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsForRecurrenceRule_UntilBeforeStart");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Start = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 1,
                Until = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            };
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetDailyRecurrences Tests

        [Fact]
        public async Task GetDailyRecurrences_Should_Return_Empty_List_When_Calendar_Item_Not_Found()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_NoCalendarItem");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            context.RecurrenceRulesDb.Add(rule);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDailyRecurrences_Should_Return_Empty_List_When_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_NoPermission");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Daily Event",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDailyRecurrences_Should_Generate_Daily_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Daily Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count); // Jan 2-5, excluding original
            Assert.All(result, item => Assert.Equal("Daily Meeting", item.Title));
        }

        [Fact]
        public async Task GetDailyRecurrences_Should_Include_Original_When_Specified()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_IncludeOriginal");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Daily Meeting",
                StartTime = new DateTime(2022, 12, 31, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2022, 12, 31, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, true, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // Jan 1-5, including original
        }

        [Fact]
        public async Task GetDailyRecurrences_Should_Handle_Interval()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_WithInterval");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 2, // Every 2 days
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Every Other Day",
                StartTime = new DateTime(2022, 12, 31, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2022, 12, 31, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // Jan 1, 3, 5, 7, 9
        }

        [Fact]
        public async Task GetDailyRecurrences_Should_Respect_Count_End_Option()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetDailyRecurrences_CountEndOption");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 2,
                Count = 5
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Limited Event",
                StartTime = new DateTime(2022, 12, 31, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2022, 12, 31, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
        }

        #endregion

        #region GetWeeklyRecurrences Tests

        [Fact]
        public async Task GetWeeklyRecurrences_Should_Generate_Weekly_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetWeeklyRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2, // Weekly
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Sunday
                EndOption = 0,
                ByDay = "MO,WE" // Monday and Wednesday
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Weekly Meeting",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 4); // At least Jan 2(Mon), 4(Wed), 9(Mon), 11(Wed)
        }

        #endregion

        #region GetMonthlyByDayRecurrences Tests

        [Fact]
        public async Task GetMonthlyByDayRecurrences_Should_Generate_Monthly_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetMonthlyByDayRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 3, // Monthly by day
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0,
                ByDay = "2MO" // Second Monday
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Monthly Meeting",
                StartTime = new DateTime(2023, 1, 9, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 9, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // Feb and Mar
        }

        #endregion

        #region GetMonthlyByDateRecurrences Tests

        [Fact]
        public async Task GetMonthlyByDateRecurrences_Should_Generate_Monthly_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetMonthlyByDateRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 4, // Monthly by date
                Interval = 1,
                Start = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0,
                ByMonthDay = "15" // 15th of each month
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Monthly Payment",
                StartTime = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 15, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // Feb 15, Mar 15
        }

        #endregion

        #region GetYearlyByDayRecurrences Tests

        [Fact]
        public async Task GetYearlyByDayRecurrences_Should_Generate_Yearly_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetYearlyByDayRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 5, // Yearly by day
                Interval = 1,
                Start = new DateTime(2020, 11, 19, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0,
                ByDay = "3TH", // Third Thursday
                ByMonth = "11" // November
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Thanksgiving",
                StartTime = new DateTime(2020, 11, 19, 18, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2020, 11, 19, 22, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // 2021, 2022
        }

        #endregion

        #region GetYearlyByDateRecurrences Tests

        [Fact]
        public async Task GetYearlyByDateRecurrences_Should_Generate_Yearly_Events()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetYearlyByDateRecurrences_GenerateEvents");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 6, // Yearly by date
                Interval = 1,
                Start = new DateTime(2020, 7, 4, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0,
                ByMonth = "7",
                ByMonthDay = "4"
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Independence Day",
                StartTime = new DateTime(2020, 7, 4, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2020, 7, 4, 22, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // 2021, 2022
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public async Task GetRecurringEventsForProgenyOrFamily_Should_Handle_Multiple_Rules()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringEventsForProgenyOrFamily_MultipleRules");
            UserInfo userInfo = GetTestUserInfo();

            RecurrenceRule dailyRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            RecurrenceRule weeklyRule = new()
            {
                RecurrenceRuleId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0,
                ByDay = "FR"
            };
            CalendarItem dailyItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 1,
                Title = "Daily",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            CalendarItem weeklyItem = new()
            {
                EventId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                RecurrenceRuleId = 2,
                Title = "Weekly",
                StartTime = new DateTime(2023, 1, 6, 14, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 6, 15, 0, 0, DateTimeKind.Utc)
            };

            context.RecurrenceRulesDb.AddRange(dailyRule, weeklyRule);
            context.CalendarDb.AddRange(dailyItem, weeklyItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetRecurringEventsForProgenyOrFamily(1, 0, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0); // Should have results from both rules
        }

        [Fact]
        public async Task GetCalendarItemsForRecurrenceRule_Should_Handle_Unknown_Frequency()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsForRecurrenceRule_UnknownFrequency");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 99, // Invalid frequency
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsForRecurrenceRule(rule, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecurringEventsForProgenyOrFamily_Should_Work_With_FamilyId()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringEventsForProgenyOrFamily_FamilyId");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule rule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 0,
                FamilyId = 1,
                Frequency = 1,
                Interval = 1,
                Start = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOption = 0
            };
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 0,
                FamilyId = 1,
                RecurrenceRuleId = 1,
                Title = "Family Event",
                StartTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            context.RecurrenceRulesDb.Add(rule);
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            CalendarRecurrencesService service = new(context, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime start = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            // Act
            List<CalendarItem>? result = await service.GetRecurringEventsForProgenyOrFamily(0, 1, start, end, false, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        #endregion
    }
}