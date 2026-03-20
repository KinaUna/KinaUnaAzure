using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class TimelineServiceTests
    {
        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private UserInfo CreateTestUser(string id = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = id,
                UserEmail = id,
                Timezone = TimeZoneInfo.Utc.Id
            };
        }

        [Fact]
        public async Task GetTimeLineItem_Returns_Item_When_Permission_Granted()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetTimeLineItem_Returns_Item_When_Permission_Granted));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem tlItem = new()
            {
                ItemId = "1",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(tlItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 1, It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(true);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);

            UserInfo user = CreateTestUser();
            TimeLineItem? result = await service.GetTimeLineItem(tlItem.TimeLineId, user);

            Assert.NotNull(result);
            Assert.Equal("1", result.ItemId);
        }

        [Fact]
        public async Task GetTimeLineItem_Returns_Null_When_No_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetTimeLineItem_Returns_Null_When_No_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem tlItem = new()
            {
                ItemId = "2",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(tlItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 2, It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(false);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);

            UserInfo user = CreateTestUser();
            TimeLineItem? result = await service.GetTimeLineItem(tlItem.TimeLineId, user);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddTimeLineItem_Adds_And_Caches_When_Permissions_OK()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(AddTimeLineItem_Adds_And_Caches_When_Permissions_OK));
            IDistributedCache cache = GetMemoryCache();

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasProgenyPermission(1, It.IsAny<UserInfo>(), PermissionLevel.Add)).ReturnsAsync(true);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);

            UserInfo user = CreateTestUser();
            TimeLineItem newItem = new()
            {
                ItemId = "10",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };

            TimeLineItem? added = await service.AddTimeLineItem(newItem, user);

            Assert.NotNull(added);
            Assert.True(added.TimeLineId > 0);
            // verify cached copy exists via direct cache read
            string? cached = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + added.TimeLineId, token: TestContext.Current.CancellationToken);
            Assert.False(string.IsNullOrEmpty(cached));
            TimeLineItem? deserialized = JsonConvert.DeserializeObject<TimeLineItem>(cached);
            Assert.Equal("10", deserialized!.ItemId);
        }

        [Fact]
        public async Task AddTimeLineItem_Returns_Null_When_Already_Exists()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(AddTimeLineItem_Returns_Null_When_Already_Exists));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem existing = new()
            {
                ItemId = "20",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(existing);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimeLineItem attempt = new()
            {
                ItemId = "20",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u2",
                CreatedTime = DateTime.UtcNow
            };

            TimeLineItem? result = await service.AddTimeLineItem(attempt, user);
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTimeLineItem_Updates_When_Permission_Edit()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(UpdateTimeLineItem_Updates_When_Permission_Edit));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem original = new()
            {
                ItemId = "30",
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };
            context.TimeLineDb.Add(original);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 30, It.IsAny<UserInfo>(), PermissionLevel.Edit))
                      .ReturnsAsync(true);
            // note: HasItemPermission uses parsed ItemId; item.ItemId will be "30" -> int 30
            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();
            DateTime newProgenyTime = DateTime.UtcNow;
            TimeLineItem updated = new()
            {
                TimeLineId = original.TimeLineId,
                ItemId = "30",
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = newProgenyTime,
            };

            TimeLineItem? result = await service.UpdateTimeLineItem(updated, user);

            Assert.NotNull(result);
            Assert.Equal(newProgenyTime, result.ProgenyTime);
            // confirm DB was updated
            TimeLineItem? dbItem = await context.TimeLineDb.FindAsync(new object?[] { original.TimeLineId }, TestContext.Current.CancellationToken);
            Assert.Equal(newProgenyTime, dbItem!.ProgenyTime);
        }

        [Fact]
        public async Task UpdateTimeLineItem_Returns_Null_When_No_Edit_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(UpdateTimeLineItem_Returns_Null_When_No_Edit_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem original = new()
            {
                ItemId = "40",
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(original);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 40, It.IsAny<UserInfo>(), PermissionLevel.Edit))
                      .ReturnsAsync(false);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimeLineItem updated = new()
            {
                TimeLineId = original.TimeLineId,
                ItemId = "40",
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ProgenyId = 1,
                FamilyId = 0,
                ProgenyTime = original.ProgenyTime,
            };

            TimeLineItem? result = await service.UpdateTimeLineItem(updated, user);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteTimeLineItem_Removes_When_Admin_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(DeleteTimeLineItem_Removes_When_Admin_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem toDelete = new()
            {
                ItemId = "50",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(toDelete);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 50, It.IsAny<UserInfo>(), PermissionLevel.Admin))
                      .ReturnsAsync(true);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimeLineItem input = new()
            {
                TimeLineId = toDelete.TimeLineId,
                ItemId = "50",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                FamilyId = 0
            };

            TimeLineItem? result = await service.DeleteTimeLineItem(input, user);
            Assert.NotNull(result);

            TimeLineItem? dbItem = await context.TimeLineDb.FindAsync(new object?[] { toDelete.TimeLineId }, TestContext.Current.CancellationToken);
            Assert.Null(dbItem);
        }

        [Fact]
        public async Task DeleteTimeLineItem_Returns_Null_When_No_Admin_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(DeleteTimeLineItem_Returns_Null_When_No_Admin_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem toDelete = new()
            {
                ItemId = "60",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(toDelete);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 60, It.IsAny<UserInfo>(), PermissionLevel.Admin))
                      .ReturnsAsync(false);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimeLineItem input = new()
            {
                TimeLineId = toDelete.TimeLineId,
                ItemId = "60",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                FamilyId = 0
            };

            TimeLineItem? result = await service.DeleteTimeLineItem(input, user);
            Assert.Null(result);
            TimeLineItem? dbItem = await context.TimeLineDb.FindAsync(new object?[] { toDelete.TimeLineId }, TestContext.Current.CancellationToken);
            Assert.NotNull(dbItem);
        }

        [Fact]
        public async Task GetTimeLineItemByItemId_Returns_Item_When_Valid_And_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetTimeLineItemByItemId_Returns_Item_When_Valid_And_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem item = new()
            {
                ItemId = "70",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 1,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.Add(item);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 70, It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(true);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimeLineItem? result = await service.GetTimeLineItemByItemId("70", (int)KinaUnaTypes.TimeLineType.Photo, user);
            Assert.NotNull(result);
            Assert.Equal("70", result.ItemId);
        }

        [Fact]
        public async Task GetTimeLineList_Filters_By_Permission()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetTimeLineList_Filters_By_Permission));
            IDistributedCache cache = GetMemoryCache();

            TimeLineItem allowed = new()
            {
                ItemId = "80",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 2,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            TimeLineItem denied = new()
            {
                ItemId = "81",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 2,
                FamilyId = 0,
                ProgenyTime = DateTime.UtcNow,
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow
            };
            context.TimeLineDb.AddRange(allowed, denied);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 80, It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(true);
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), 81, It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(false);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            Mock<ICalendarService> calendarMock = new();

            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            List<TimeLineItem>? list = await service.GetTimeLineList(2, 0, user);
            Assert.Single(list);
            Assert.Equal("80", list.First().ItemId);
        }

        [Fact]
        public async Task GetOnThisDayData_Returns_Filtered_Response_With_Paging()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetOnThisDayData_Returns_Filtered_Response_With_Paging));
            IDistributedCache cache = GetMemoryCache();

            // Create 3 items for progeny 3
            for (int i = 1; i <= 3; i++)
            {
                context.TimeLineDb.Add(new TimeLineItem
                {
                    ItemId = (300 + i).ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                    ProgenyId = 3,
                    ProgenyTime = DateTime.UtcNow.AddYears(-i),
                    CreatedBy = "u",
                    CreatedTime = DateTime.UtcNow.AddYears(-i)
                });
            }
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(true);
            
            Mock<ITimelineFilteringService> timelineFilteringMock = new();
            // No tag/category/context filters are provided in this test, so mocks are not used.

            Mock<ICalendarService> calendarMock = new();
            calendarMock.Setup(x => x.GetRecurringCalendarItemsLatestPosts(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserInfo>()))
                .ReturnsAsync([]);
            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            OnThisDayRequest req = new()
            {
                Progenies = new List<int> { 3 },
                Families = new List<int>(),
                NumberOfItems = 2,
                Skip = 0,
                SortOrder = 1 // descending
            };

            OnThisDayResponse? resp = await service.GetOnThisDayData(req, user);

            Assert.NotNull(resp);
            Assert.Equal(2, resp.TimeLineItems.Count);
            Assert.True(resp.RemainingItemsCount >= 0);
            Assert.Equal((DateTime.UtcNow.AddYears(-3).Year), resp.Request.FirstItemYear);
        }

        [Fact]
        public async Task GetTimelineData_Includes_Recurring_Calendar_Items()
        {
            await using ProgenyDbContext context = GetInMemoryDbContext(nameof(GetTimelineData_Includes_Recurring_Calendar_Items));
            IDistributedCache cache = GetMemoryCache();

            // Create one timeline item for progeny 4
            context.TimeLineDb.Add(new TimeLineItem
            {
                ItemId = "400",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = 4,
                ProgenyTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "u",
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Mock<IAccessManagementService> accessMock = new();
            accessMock.Setup(x => x.HasItemPermission(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<UserInfo>(), PermissionLevel.View))
                      .ReturnsAsync(true);

            Mock<ITimelineFilteringService> timelineFilteringMock = new();

            Mock<ICalendarService> calendarMock = new();
            // Return a calendar item with StartTime so it will be converted to a TimeLineItem
            calendarMock.Setup(x => x.GetRecurringCalendarItemsLatestPosts(4, 0, It.IsAny<UserInfo>()))
                        .ReturnsAsync(new List<CalendarItem>
                        {
                            new()
                            {
                                EventId = 999,
                                ProgenyId = 4,
                                StartTime = DateTime.UtcNow.AddMonths(-1),
                                EndTime = DateTime.UtcNow.AddMonths(-1).AddHours(1),
                                Context = "Recurring",
                                Location = "Home"
                            }
                        });
            calendarMock.Setup(x => x.GetCalendarItem(It.IsAny<int>(), It.IsAny<UserInfo>())).ReturnsAsync(new CalendarItem());
            TimelineService service = new(context, timelineFilteringMock.Object, cache, calendarMock.Object, accessMock.Object);
            UserInfo user = CreateTestUser();

            TimelineRequest req = new()
            {
                Progenies = new List<int> { 4 },
                Families = new List<int>(),
                NumberOfItems = 10,
                Skip = 0,
                SortOrder = 1
            };

            TimelineResponse? resp = await service.GetTimelineData(req, user);

            Assert.NotNull(resp);
            // original timeline item + calendar-based recurring item -> at least 2
            Assert.True(resp.TimeLineItems.Count >= 2);
            Assert.Equal((DateTime.UtcNow.AddDays(-1).Year), resp.Request.FirstItemYear);
        }
    }
}