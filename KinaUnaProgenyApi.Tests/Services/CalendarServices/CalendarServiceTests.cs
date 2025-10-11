using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
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
        private readonly IDistributedCache _memoryCache;

        public CalendarServiceTests()
        {
            _mockCalendarRecurrencesService = new Mock<ICalendarRecurrencesService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            _memoryCache = new MemoryDistributedCache(memoryCacheOptions);
        }

        private static ProgenyDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ProgenyDbContext>()
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

        #region GetCalendarItem Tests

        [Fact]
        public async Task GetCalendarItem_Should_Return_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarItem_Valid");
            var userInfo = GetTestUserInfo();
            var calendarItem = new CalendarItem
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Event",
                StartTime = DateTime.UtcNow,
                AccessLevel = 0,
                UId = Guid.NewGuid().ToString()
            };
            context.CalendarDb.Add(calendarItem);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EventId);
            Assert.Equal("Test Event", result.Title);
        }

        [Fact]
        public async Task GetCalendarItem_Should_Return_Empty_CalendarItem_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarItem_NoPermission");
            var userInfo = GetTestUserInfo();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.EventId);
        }

        [Fact]
        public async Task GetCalendarItem_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarItem_Cache");
            var userInfo = GetTestUserInfo();
            var calendarItem = new CalendarItem
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
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetCalendarItem(1, userInfo);
            var result2 = await service.GetCalendarItem(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Title, result2.Title);
        }

        #endregion

        #region AddCalendarItem Tests

        [Fact]
        public async Task AddCalendarItem_Should_Add_CalendarItem_For_Progeny_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("AddCalendarItem_Progeny");
            var userInfo = GetTestUserInfo();
            var newItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.EventId);
            Assert.Equal("New Event", result.Title);
            Assert.NotNull(result.UId);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Add_CalendarItem_For_Family_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("AddCalendarItem_Family");
            var userInfo = GetTestUserInfo();
            var newItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.EventId);
            Assert.Equal("Family Event", result.Title);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Return_Null_When_Both_ProgenyId_And_FamilyId_Are_Set()
        {
            // Arrange
            await using var context = GetInMemoryContext("AddCalendarItem_BothIds");
            var userInfo = GetTestUserInfo();
            var newItem = new CalendarItem
            {
                ProgenyId = 1,
                FamilyId = 1,
                Title = "Invalid Event"
            };

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("AddCalendarItem_NoPermission");
            var userInfo = GetTestUserInfo();
            var newItem = new CalendarItem
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "No Permission Event"
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Add_RecurrenceRule_When_Provided()
        {
            // Arrange
            await using var context = GetInMemoryContext("AddCalendarItem_Recurrence");
            var userInfo = GetTestUserInfo();
            var newItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddCalendarItem(newItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.RecurrenceRuleId);
        }

        #endregion

        #region UpdateCalendarItem Tests

        [Fact]
        public async Task UpdateCalendarItem_Should_Update_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("UpdateCalendarItem_Valid");
            var userInfo = GetTestUserInfo();
            var existingItem = new CalendarItem
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

            var updatedItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Return_Empty_CalendarItem_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("UpdateCalendarItem_NoPermission");
            var userInfo = GetTestUserInfo();
            var updatedItem = new CalendarItem
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title"
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.EventId);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Add_RecurrenceRule_When_None_Exists()
        {
            // Arrange
            await using var context = GetInMemoryContext("UpdateCalendarItem_AddRecurrence");
            var userInfo = GetTestUserInfo();
            var existingItem = new CalendarItem
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

            var updatedItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.RecurrenceRuleId);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Remove_RecurrenceRule_When_Frequency_Is_Zero()
        {
            // Arrange
            await using var context = GetInMemoryContext("UpdateCalendarItem_RemoveRecurrence");
            var userInfo = GetTestUserInfo();
            var recurrenceRule = new RecurrenceRule
            {
                RecurrenceRuleId = 1,
                Frequency = 2,
                ProgenyId = 1
            };
            var existingItem = new CalendarItem
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

            var updatedItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.RecurrenceRuleId);
            Assert.Empty(context.RecurrenceRulesDb);
        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Update_Reminders_When_StartTime_Changes()
        {
            // Arrange
            await using var context = GetInMemoryContext("UpdateCalendarItem_UpdateReminders");
            var userInfo = GetTestUserInfo();
            var originalStartTime = DateTime.UtcNow;
            var existingItem = new CalendarItem
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event",
                StartTime = originalStartTime,
                UId = Guid.NewGuid().ToString()
            };
            var reminder = new CalendarReminder
            {
                CalendarReminderId = 1,
                EventId = 1,
                NotifyTime = originalStartTime.AddMinutes(-30)
            };
            context.CalendarDb.Add(existingItem);
            context.CalendarRemindersDb.Add(reminder);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var newStartTime = originalStartTime.AddHours(1);
            var updatedItem = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateCalendarItem(updatedItem, userInfo);

            // Assert
            Assert.NotNull(result);
            var updatedReminder = await context.CalendarRemindersDb.FirstAsync();
            Assert.Equal(newStartTime.AddMinutes(-30), updatedReminder.NotifyTime);
        }

        #endregion

        #region DeleteCalendarItem Tests

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_CalendarItem_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("DeleteCalendarItem_Valid");
            var userInfo = GetTestUserInfo();
            var itemToDelete = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EventId);
            Assert.Empty(context.CalendarDb);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("DeleteCalendarItem_NoPermission");
            var userInfo = GetTestUserInfo();
            var itemToDelete = new CalendarItem
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event"
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_RecurrenceRule_When_Present()
        {
            // Arrange
            await using var context = GetInMemoryContext("DeleteCalendarItem_WithRecurrence");
            var userInfo = GetTestUserInfo();
            var recurrenceRule = new RecurrenceRule
            {
                RecurrenceRuleId = 1,
                Frequency = 2,
                ProgenyId = 1
            };
            var itemToDelete = new CalendarItem
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

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteCalendarItem(itemToDelete, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(context.RecurrenceRulesDb);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Delete_Reminders_When_Present()
        {
            // Arrange
            await using var context = GetInMemoryContext("DeleteCalendarItem_WithReminders");
            var userInfo = GetTestUserInfo();
            var itemToDelete = new CalendarItem
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Event with Reminders",
                UId = Guid.NewGuid().ToString()
            };
            var reminder1 = new CalendarReminder { CalendarReminderId = 1, EventId = 1 };
            var reminder2 = new CalendarReminder { CalendarReminderId = 2, EventId = 1 };
            context.CalendarDb.Add(itemToDelete);
            context.CalendarRemindersDb.Add(reminder1);
            context.CalendarRemindersDb.Add(reminder2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteCalendarItem(itemToDelete, userInfo);

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
            await using var context = GetInMemoryContext("GetCalendarList_All");
            var userInfo = GetTestUserInfo();
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = Guid.NewGuid().ToString() };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = Guid.NewGuid().ToString() };
            var item3 = new CalendarItem { EventId = 3, ProgenyId = 2, FamilyId = 0, Title = "Event 3", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarList(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetCalendarList_Should_Return_Empty_List_When_ProgenyId_And_FamilyId_Are_Zero()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarList_NoIds");
            var userInfo = GetTestUserInfo();
            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarList(0, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCalendarList_Should_Filter_By_Date_Range_When_Provided()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarList_DateRange");
            var userInfo = GetTestUserInfo();
            var now = DateTime.UtcNow;
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(-2), Title = "Past Event", UId = Guid.NewGuid().ToString() };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, StartTime = now, Title = "Current Event", UId = Guid.NewGuid().ToString() };
            var item3 = new CalendarItem { EventId = 3, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(2), Title = "Future Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(new List<CalendarItem>());

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarList(1, 0, userInfo, now.AddDays(-1), now.AddDays(1));

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Current Event", result[0].Title);
        }

        [Fact]
        public async Task GetCalendarList_Should_Include_Recurring_Events_When_Date_Range_Provided()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarList_WithRecurring");
            var userInfo = GetTestUserInfo();
            var now = DateTime.UtcNow;
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, StartTime = now, Title = "Regular Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.Add(item1);
            await context.SaveChangesAsync();

            var recurringItem = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, StartTime = now.AddDays(1), Title = "Recurring Event", UId = Guid.NewGuid().ToString() };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(new List<CalendarItem> { recurringItem });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarList(1, 0, userInfo, now.AddDays(-1), now.AddDays(2));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetCalendarList_Should_Filter_Out_Items_Without_Permission()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarList_FilterPermissions");
            var userInfo = GetTestUserInfo();
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Accessible Event", UId = Guid.NewGuid().ToString() };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Restricted Event", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarList(1, 0, userInfo);

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
            await using var context = GetInMemoryContext("GetRecurringCalendarItemsOnThisDay_Valid");
            var userInfo = GetTestUserInfo();
            var recurrenceRule = new RecurrenceRule
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            await context.SaveChangesAsync();

            var recurringEvents = new List<CalendarItem>
            {
                new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Birthday 2020", StartTime = new DateTime(2020, 10, 11), UId = Guid.NewGuid().ToString() },
                new() { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Birthday 2021", StartTime = new DateTime(2021, 10, 11), UId = Guid.NewGuid().ToString() }
            };

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(recurringEvents);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetRecurringCalendarItemsOnThisDay(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetRecurringCalendarItemsOnThisDay_Should_Return_Empty_List_When_No_Recurrence_Rules()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetRecurringCalendarItemsOnThisDay_NoRules");
            var userInfo = GetTestUserInfo();
            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetRecurringCalendarItemsOnThisDay(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetRecurringCalendarItemsLatestPosts Tests

        [Fact]
        public async Task GetRecurringCalendarItemsLatestPosts_Should_Return_Events_From_1900_To_Now()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetRecurringCalendarItemsLatestPosts_Valid");
            var userInfo = GetTestUserInfo();
            var recurrenceRule = new RecurrenceRule
            {
                RecurrenceRuleId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Frequency = 2
            };
            context.RecurrenceRulesDb.Add(recurrenceRule);
            await context.SaveChangesAsync();

            var recurringEvents = new List<CalendarItem>
            {
                new() { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = Guid.NewGuid().ToString() }
            };

            _mockCalendarRecurrencesService.Setup(x => x.GetRecurringEventsForProgenyOrFamily(
                1, 0, new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), It.IsAny<DateTime>(), false, userInfo))
                .ReturnsAsync(recurringEvents);

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetRecurringCalendarItemsLatestPosts(1, 0, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetRecurringCalendarItemsLatestPosts_Should_Return_Empty_List_When_No_Recurrence_Rules()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetRecurringCalendarItemsLatestPosts_NoRules");
            var userInfo = GetTestUserInfo();
            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetRecurringCalendarItemsLatestPosts(1, 0, userInfo);

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
            await using var context = GetInMemoryContext("GetCalendarItemsWithContext_Match");
            var userInfo = GetTestUserInfo();
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Context = "Birthday", Title = "Event 1", UId = Guid.NewGuid().ToString() };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Context = "Holiday", Title = "Event 2", UId = Guid.NewGuid().ToString() };
            var item3 = new CalendarItem { EventId = 3, ProgenyId = 1, FamilyId = 0, Context = "Birthday Party", Title = "Event 3", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarItemsWithContext(1, 0, "birthday", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Contains("birthday", item.Context, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetCalendarItemsWithContext_Should_Return_All_Items_When_Context_Is_Null()
        {
            // Arrange
            await using var context = GetInMemoryContext("GetCalendarItemsWithContext_Null");
            var userInfo = GetTestUserInfo();
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Context = "Birthday", Title = "Event 1", UId = Guid.NewGuid().ToString() };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Context = "Holiday", Title = "Event 2", UId = Guid.NewGuid().ToString() };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetCalendarItemsWithContext(1, 0, null, userInfo);

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
            await using var context = GetInMemoryContext("CheckCalendarItemsForUId_Valid");
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = null };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = "" };
            var item3 = new CalendarItem { EventId = 3, ProgenyId = 1, FamilyId = 0, Title = "Event 3", UId = "existing-uid" };
            context.CalendarDb.AddRange(item1, item2, item3);
            await context.SaveChangesAsync();

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            var items = await context.CalendarDb.ToListAsync();
            Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.UId)));
        }

        [Fact]
        public async Task CheckCalendarItemsForUId_Should_Not_Change_Existing_UIds()
        {
            // Arrange
            await using var context = GetInMemoryContext("CheckCalendarItemsForUId_Existing");
            var existingUId = "existing-uid";
            var item = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event", UId = existingUId };
            context.CalendarDb.Add(item);
            await context.SaveChangesAsync();

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            var updatedItem = await context.CalendarDb.FirstAsync();
            Assert.Equal(existingUId, updatedItem.UId);
        }

        [Fact]
        public async Task CheckCalendarItemsForUId_Should_Do_Nothing_When_All_Items_Have_UIds()
        {
            // Arrange
            await using var context = GetInMemoryContext("CheckCalendarItemsForUId_AllHaveUIds");
            var item1 = new CalendarItem { EventId = 1, ProgenyId = 1, FamilyId = 0, Title = "Event 1", UId = "uid-1" };
            var item2 = new CalendarItem { EventId = 2, ProgenyId = 1, FamilyId = 0, Title = "Event 2", UId = "uid-2" };
            context.CalendarDb.AddRange(item1, item2);
            await context.SaveChangesAsync();

            var service = new CalendarService(context, _memoryCache, _mockCalendarRecurrencesService.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckCalendarItemsForUId();

            // Assert
            var items = await context.CalendarDb.ToListAsync();
            Assert.Equal("uid-1", items[0].UId);
            Assert.Equal("uid-2", items[1].UId);
        }

        #endregion
    }
}