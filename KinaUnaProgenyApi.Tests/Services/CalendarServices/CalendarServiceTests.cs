using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.CalendarServices
{
    public class CalendarServiceTests
    {
        private readonly Mock<ICalendarRecurrencesService> _mockCalendarRecurrencesService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;
        private readonly IDistributedCache _memoryCache;

        public CalendarServiceTests()
        {
            _mockCalendarRecurrencesService = new Mock<ICalendarRecurrencesService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockKinaUnaCacheService = new Mock<IKinaUnaCacheService>();
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            _memoryCache = new MemoryDistributedCache(memoryCacheOptions);
        }

        private static ProgenyDbContext GetInMemoryContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static UserInfo GetTestUserInfo(string userId = "test-user@example.com", int userInfoId = 1)
        {
            return new UserInfo
            {
                UserEmail = userId,
                UserId = userId,
                Id = userInfoId
            };
        }

        private void SetupAccessManagementServiceDefaults()
        {
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    It.IsAny<KinaUnaTypes.TimeLineType>(),
                    It.IsAny<int>(),
                    It.IsAny<UserInfo>(),
                    It.IsAny<PermissionLevel>()))
                .ReturnsAsync(false);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(
                    It.IsAny<int>(),
                    It.IsAny<UserInfo>(),
                    It.IsAny<PermissionLevel>()))
                .ReturnsAsync(false);

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(
                    It.IsAny<int>(),
                    It.IsAny<UserInfo>(),
                    It.IsAny<PermissionLevel>()))
                .ReturnsAsync(false);

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<bool>(),
                    It.IsAny<UserInfo>()))
                .ReturnsAsync(new List<CalendarItem>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);
        }

        #region GetCalendarItem Tests

        [Fact]
        public async Task GetCalendarItem_Should_Return_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItem_Valid");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Event",
                StartTime = DateTime.UtcNow,
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EventId);
            Assert.Equal("Test Event", result.Title);
        }

        [Fact]
        public async Task GetCalendarItem_Should_Return_Empty_CalendarItem_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItem_NoPermission");
            UserInfo userInfo = GetTestUserInfo();
            SetupAccessManagementServiceDefaults();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.EventId);
        }

        [Fact]
        public async Task GetCalendarItem_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItem_Cache");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Cached Event",
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result1 = await service.GetCalendarItem(1, userInfo);
            CalendarItem? result2 = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Title, result2.Title);
            _mockAccessManagementService.Verify(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View), Times.Exactly(2));
        }

        #endregion

        #region AddCalendarItem Tests

        [Fact]
        public async Task AddCalendarItem_Should_Add_CalendarItem_For_Progeny_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("AddCalendarItem_Progeny");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem newItem = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "New Event",
                StartTime = DateTime.UtcNow,
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.AddItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.EventId);
            Assert.Equal("New Event", result.Title);
            Assert.NotNull(result.UId);
            _mockAccessManagementService.Verify(x => x.AddItemPermissions(
                KinaUnaTypes.TimeLineType.Calendar, result.EventId, 1, 0, It.IsAny<List<ItemPermissionDto>>(), userInfo), Times.Once);
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar), Times.Once);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Add_CalendarItem_For_Family_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("AddCalendarItem_Family");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem newItem = new()
            {
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Event",
                StartTime = DateTime.UtcNow,
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.AddItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(0, 1, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.EventId);
            Assert.Equal("Family Event", result.Title);
            _mockAccessManagementService.Verify(x => x.AddItemPermissions(
                KinaUnaTypes.TimeLineType.Calendar, result.EventId, 0, 1, It.IsAny<List<ItemPermissionDto>>(), userInfo), Times.Once);
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(0, 1, KinaUnaTypes.TimeLineType.Calendar), Times.Once);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Return_Null_When_Both_ProgenyId_And_FamilyId_Are_Set()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("AddCalendarItem_BothIds");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem newItem = new()
            {
                ProgenyId = 1,
                FamilyId = 1,
                Title = "Invalid Event"
            };

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.Null(result);
            _mockAccessManagementService.Verify(x => x.AddItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("AddCalendarItem_NoPermission");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem newItem = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "No Permission Event"
            };

            SetupAccessManagementServiceDefaults();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.Null(result);
            _mockAccessManagementService.Verify(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add), Times.Once);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Add_RecurrenceRule_When_Provided()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("AddCalendarItem_Recurrence");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem newItem = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Recurring Event",
                StartTime = DateTime.UtcNow,
                RecurrenceRule = new RecurrenceRule
                {
                    Frequency = 2,
                    Interval = 1,
                    ProgenyId = 1
                },
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.AddItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.RecurrenceRuleId);
            RecurrenceRule? savedRule = await context.RecurrenceRulesDb.FindAsync(result.RecurrenceRuleId);
            Assert.NotNull(savedRule);
            Assert.Equal(2, savedRule.Frequency);
        }

        #endregion

        #region UpdateCalendarItem Tests

        [Fact]
        public async Task UpdateCalendarItem_Should_Update_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("UpdateCalendarItem_Valid");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem existingItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Original Title",
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(existingItem);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            CalendarItem updatedItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title",
                RecurrenceRule = new RecurrenceRule { Frequency = 0 },
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.UpdateItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            _mockAccessManagementService.Verify(x => x.UpdateItemPermissions(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), userInfo), Times.Once);
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar), Times.Once);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Return_Empty_CalendarItem_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("UpdateCalendarItem_NoPermission");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem updatedItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title"
            };

            SetupAccessManagementServiceDefaults();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.EventId);
            _mockAccessManagementService.Verify(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit), Times.Once);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Add_RecurrenceRule_When_None_Exists()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("UpdateCalendarItem_AddRecurrence");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem existingItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                StartTime = DateTime.UtcNow,
                RecurrenceRuleId = 0,
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(existingItem);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            CalendarItem updatedItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                StartTime = DateTime.UtcNow,
                RecurrenceRule = new RecurrenceRule
                {
                    Frequency = 2,
                    Interval = 1
                },
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.UpdateItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.RecurrenceRuleId);
            Assert.Single(context.RecurrenceRulesDb);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Remove_RecurrenceRule_When_Frequency_Is_Zero()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("UpdateCalendarItem_RemoveRecurrence");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule recurrenceRule = new()
            {
                RecurrenceRuleId = 1,
                Frequency = 2,
                ProgenyId = 1
            };
            CalendarItem existingItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                RecurrenceRuleId = 1,
                UId = Guid.NewGuid().ToString()
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            context.CalendarDb.Add(existingItem);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            CalendarItem updatedItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                RecurrenceRule = new RecurrenceRule { Frequency = 0 },
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.UpdateItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.RecurrenceRuleId);
            Assert.Empty(context.RecurrenceRulesDb);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Update_Reminders_When_StartTime_Changes()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("UpdateCalendarItem_UpdateReminders");
            UserInfo userInfo = GetTestUserInfo();
            DateTime originalStartTime = DateTime.UtcNow;
            CalendarItem existingItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                StartTime = originalStartTime,
                UId = Guid.NewGuid().ToString()
            };
            CalendarReminder reminder = new()
            {
                CalendarReminderId = 1,
                EventId = 1,
                NotifyTime = originalStartTime.AddMinutes(-30)
            };
            context.CalendarDb.Add(existingItem);
            context.CalendarRemindersDb.Add(reminder);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            DateTime newStartTime = originalStartTime.AddHours(1);
            CalendarItem updatedItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                StartTime = newStartTime,
                RecurrenceRule = new RecurrenceRule { Frequency = 0 },
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.UpdateItemPermissions(
                It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            CalendarReminder updatedReminder = await context.CalendarRemindersDb.FirstAsync();
            Assert.Equal(newStartTime.AddMinutes(-30), updatedReminder.NotifyTime);
        }

        #endregion

        #region DeleteCalendarItem Tests

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("DeleteCalendarItem_Valid");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem itemToDelete = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event to Delete",
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(itemToDelete);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetTimelineItemPermissionsList(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EventId);
            Assert.Empty(context.CalendarDb);
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar), Times.Once);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("DeleteCalendarItem_NoPermission");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem itemToDelete = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event"
            };

            SetupAccessManagementServiceDefaults();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.Null(result);
            _mockAccessManagementService.Verify(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin), Times.Once);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_RecurrenceRule_When_Present()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("DeleteCalendarItem_WithRecurrence");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule recurrenceRule = new()
            {
                RecurrenceRuleId = 1,
                Frequency = 2,
                ProgenyId = 1
            };
            CalendarItem itemToDelete = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Recurring Event",
                RecurrenceRuleId = 1,
                UId = Guid.NewGuid().ToString()
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            context.CalendarDb.Add(itemToDelete);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetTimelineItemPermissionsList(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(context.RecurrenceRulesDb);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_Reminders_When_Present()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("DeleteCalendarItem_WithReminders");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem itemToDelete = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event with Reminders",
                UId = Guid.NewGuid().ToString()
            };
            CalendarReminder reminder1 = new() { CalendarReminderId = 1, EventId = 1 };
            CalendarReminder reminder2 = new() { CalendarReminderId = 2, EventId = 1 };
            context.CalendarDb.Add(itemToDelete);
            context.CalendarRemindersDb.Add(reminder1);
            context.CalendarRemindersDb.Add(reminder2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetTimelineItemPermissionsList(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockKinaUnaCacheService.Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            CalendarItem? result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(context.CalendarRemindersDb);
        }

        #endregion

        #region GetCalendarList Tests

        [Fact]
        public async Task GetCalendarList_Should_Return_All_Items_For_Progeny_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarList_All");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = Guid.NewGuid().ToString() };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = Guid.NewGuid().ToString() };
            CalendarItem item3 = new() { EventId = 3, ProgenyId = 2, FamilyId = 0, Title = "Event 3", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService.Setup(x => x.GetCalendarItemsListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((CalendarListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetCalendarItemsListCache(userInfo.UserId, 1, 0, It.IsAny<CalendarItem[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarList(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetCalendarList_Should_Return_Empty_List_When_ProgenyId_And_FamilyId_Are_Zero()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarList_NoIds");
            UserInfo userInfo = GetTestUserInfo();
            SetupAccessManagementServiceDefaults();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarList(0, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCalendarList_Should_Filter_By_Date_Range_When_Provided()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarList_DateRange");
            UserInfo userInfo = GetTestUserInfo();
            DateTime now = DateTime.UtcNow;
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(-2), Title = "Past Event", UId = Guid.NewGuid().ToString() };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, StartTime = now, Title = "Current Event", UId = Guid.NewGuid().ToString() };
            CalendarItem item3 = new() { EventId = 3, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(2), Title = "Future Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService.Setup(x => x.GetCalendarItemsListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((CalendarListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetCalendarItemsListCache(userInfo.UserId, 1, 0, It.IsAny<CalendarItem[]>()))
                .Returns(Task.CompletedTask);

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(new List<CalendarItem>());

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarList(1, 0, userInfo, now.AddDays(-1), now.AddDays(1));

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Current Event", result[0].Title);
        }

        [Fact]
        public async Task GetCalendarList_Should_Include_Recurring_Events_When_Date_Range_Provided()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarList_WithRecurring");
            UserInfo userInfo = GetTestUserInfo();
            DateTime now = DateTime.UtcNow;
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, StartTime = now, Title = "Regular Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.Add(item1);
            await context.SaveChangesAsync();

            CalendarItem recurringItem = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(1), Title = "Recurring Event", UId = Guid.NewGuid().ToString() };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService.Setup(x => x.GetCalendarItemsListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((CalendarListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetCalendarItemsListCache(userInfo.UserId, 1, 0, It.IsAny<CalendarItem[]>()))
                .Returns(Task.CompletedTask);

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(new List<CalendarItem> { recurringItem });

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarList(1, 0, userInfo, now.AddDays(-1), now.AddDays(2));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockCalendarRecurrencesService.Verify(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo), Times.Once);
        }

        [Fact]
        public async Task GetCalendarList_Should_Filter_Out_Items_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarList_FilterPermissions");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Accessible Event", UId = Guid.NewGuid().ToString() };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Restricted Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService.Setup(x => x.GetCalendarItemsListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((CalendarListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetCalendarItemsListCache(userInfo.UserId, 1, 0, It.IsAny<CalendarItem[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarList(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Accessible Event", result[0].Title);
        }

        #endregion

        #region GetRecurringCalendarItemsOnThisDay Tests

        [Fact]
        public async Task GetRecurringCalendarItemsOnThisDay_Should_Return_Recurring_Events_From_1900()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringCalendarItemsOnThisDay_Valid");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule recurrenceRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            await context.SaveChangesAsync();

            List<CalendarItem> recurringEvents =
            [
                new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Birthday 2020", StartTime = new DateTime(2020, 10, 11), UId = Guid.NewGuid().ToString() },
                new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Birthday 2021", StartTime = new DateTime(2021, 10, 11), UId = Guid.NewGuid().ToString() }
            ];

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(recurringEvents);

            _mockKinaUnaCacheService.Setup(x => x.GetRecurrenceRulesListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((RecurrenceRulesListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetRecurrenceRulesListCache(userInfo.UserId, 1, 0, It.IsAny<RecurrenceRule[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetRecurringCalendarItemsOnThisDay(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _mockCalendarRecurrencesService.Verify(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.Is<DateTime>(d => d.Year == 1900), It.IsAny<DateTime>(), false, userInfo), Times.Once);
        }

        [Fact]
        public async Task GetRecurringCalendarItemsOnThisDay_Should_Return_Empty_List_When_No_Recurrence_Rules()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringCalendarItemsOnThisDay_NoRules");
            UserInfo userInfo = GetTestUserInfo();
            SetupAccessManagementServiceDefaults();

            _mockKinaUnaCacheService.Setup(x => x.GetRecurrenceRulesListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((RecurrenceRulesListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetRecurrenceRulesListCache(userInfo.UserId, 1, 0, It.IsAny<RecurrenceRule[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetRecurringCalendarItemsOnThisDay(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockCalendarRecurrencesService.Verify(x => x.GetRecurringEventsForProgenyOrFamily(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region GetRecurringCalendarItemsLatestPosts Tests

        [Fact]
        public async Task GetRecurringCalendarItemsLatestPosts_Should_Return_Events_From_1900_To_Now()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringCalendarItemsLatestPosts_Valid");
            UserInfo userInfo = GetTestUserInfo();
            RecurrenceRule recurrenceRule = new()
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            await context.SaveChangesAsync();

            List<CalendarItem> recurringEvents =
            [
                new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = Guid.NewGuid().ToString() }
            ];

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(recurringEvents);

            _mockKinaUnaCacheService.Setup(x => x.GetRecurrenceRulesListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((RecurrenceRulesListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetRecurrenceRulesListCache(userInfo.UserId, 1, 0, It.IsAny<RecurrenceRule[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetRecurringCalendarItemsLatestPosts(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _mockCalendarRecurrencesService.Verify(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), It.IsAny<DateTime>(), false, userInfo), Times.Once);
        }

        [Fact]
        public async Task GetRecurringCalendarItemsLatestPosts_Should_Return_Empty_List_When_No_Recurrence_Rules()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetRecurringCalendarItemsLatestPosts_NoRules");
            UserInfo userInfo = GetTestUserInfo();
            SetupAccessManagementServiceDefaults();

            _mockKinaUnaCacheService.Setup(x => x.GetRecurrenceRulesListCache(userInfo.UserId, 1, 0))
                .ReturnsAsync((RecurrenceRulesListCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Calendar))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService.Setup(x => x.SetRecurrenceRulesListCache(userInfo.UserId, 1, 0, It.IsAny<RecurrenceRule[]>()))
                .Returns(Task.CompletedTask);

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetRecurringCalendarItemsLatestPosts(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetCalendarItemsWithContext Tests

        [Fact]
        public async Task GetCalendarItemsWithContext_Should_Return_Items_Matching_Context()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsWithContext_Match");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Context = "Birthday", Title = "Event 1", UId = Guid.NewGuid().ToString() };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Context = "Holiday", Title = "Event 2", UId = Guid.NewGuid().ToString() };
            CalendarItem item3 = new() { EventId = 3, ProgenyId = 1, FamilyId = 0, Context = "Birthday Party", Title = "Event 3", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsWithContext(1, 0, "birthday", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Contains("birthday", item.Context, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetCalendarItemsWithContext_Should_Return_All_Items_When_Context_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("GetCalendarItemsWithContext_Null");
            UserInfo userInfo = GetTestUserInfo();
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Context = "Birthday", Title = "Event 1", UId = Guid.NewGuid().ToString() };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Context = "Holiday", Title = "Event 2", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<CalendarItem>? result = await service.GetCalendarItemsWithContext(1, 0, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region CheckCalendarItemsForUId Tests

        [Fact]
        public async Task CheckCalendarItemsForUId_Should_Assign_UIds_To_Items_Without_One()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("CheckCalendarItemsForUId_Valid");
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = null };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = "" };
            CalendarItem item3 = new() { EventId = 3, ProgenyId = 1, FamilyId = 0, Title = "Event 3", UId = "existing-uid" };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            List<CalendarItem> items = await context.CalendarDb.ToListAsync();
            Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.UId)));
        }

        [Fact]
        public async Task CheckCalendarItemsForUId_Should_Not_Change_Existing_UIds()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("CheckCalendarItemsForUId_Existing");
            string existingUId = "existing-uid";
            CalendarItem item = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event", UId = existingUId };
            context.CalendarDb.Add(item);
            await context.SaveChangesAsync();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            CalendarItem updatedItem = await context.CalendarDb.FirstAsync();
            Assert.Equal(existingUId, updatedItem.UId);
        }

        [Fact]
        public async Task CheckCalendarItemsForUId_Should_Do_Nothing_When_All_Items_Have_UIds()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryContext("CheckCalendarItemsForUId_AllHaveUIds");
            CalendarItem item1 = new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = "uid-1" };
            CalendarItem item2 = new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = "uid-2" };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            CalendarService service = new(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            List<CalendarItem> items = await context.CalendarDb.ToListAsync();
            Assert.Equal("uid-1", items[0].UId);
            Assert.Equal("uid-2", items[1].UId);
        }

        #endregion
    }
}