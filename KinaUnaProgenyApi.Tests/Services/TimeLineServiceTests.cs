using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class TimeLineServiceTests
    {
        [Fact]
        public async Task GetTimeLineItem_Should_Return_TimeLineItem_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTimeLineItem_Should_Return_TimeLineItem_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            TimeLineItem resultTimeLineItem1 = await timelineService.GetTimeLineItem(1);
            TimeLineItem resultTimeLineItem2 = await timelineService.GetTimeLineItem(1); // Uses cache

            Assert.NotNull(resultTimeLineItem1);
            Assert.IsType<TimeLineItem>(resultTimeLineItem1);
            Assert.Equal(timeLineItem1.CreatedBy, resultTimeLineItem1.CreatedBy);
            Assert.Equal(timeLineItem1.CreatedTime, resultTimeLineItem1.CreatedTime);
            Assert.Equal(timeLineItem1.AccessLevel, resultTimeLineItem1.AccessLevel);
            Assert.Equal(timeLineItem1.ProgenyId, resultTimeLineItem1.ProgenyId);

            Assert.NotNull(resultTimeLineItem2);
            Assert.IsType<TimeLineItem>(resultTimeLineItem2);
            Assert.Equal(timeLineItem1.CreatedBy, resultTimeLineItem2.CreatedBy);
            Assert.Equal(timeLineItem1.CreatedTime, resultTimeLineItem2.CreatedTime);
            Assert.Equal(timeLineItem1.AccessLevel, resultTimeLineItem2.AccessLevel);
            Assert.Equal(timeLineItem1.ProgenyId, resultTimeLineItem2.ProgenyId);
        }

        [Fact]
        public async Task GetTimeLineItem_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTimeLineItem_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            TimeLineItem resultTimeLineItem1 = await timelineService.GetTimeLineItem(2);
            TimeLineItem resultTimeLineItem2 = await timelineService.GetTimeLineItem(2); // Using cache

            Assert.Null(resultTimeLineItem1);
            Assert.Null(resultTimeLineItem2);
        }

        [Fact]
        public async Task AddTimeLineItem_Should_Save_TimeLineItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddTimeLineItem_Should_Save_TimeLineItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            TimeLineItem timeLineItemToAdd = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem addedTimeLineItem = await timelineService.AddTimeLineItem(timeLineItemToAdd);
            TimeLineItem? dbTimeLineItem = await context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.TimeLineId == addedTimeLineItem.TimeLineId);
            TimeLineItem savedTimeLineItem = await timelineService.GetTimeLineItem(addedTimeLineItem.TimeLineId);

            Assert.NotNull(addedTimeLineItem);
            Assert.IsType<TimeLineItem>(addedTimeLineItem);
            Assert.Equal(timeLineItemToAdd.CreatedBy, addedTimeLineItem.CreatedBy);
            Assert.Equal(timeLineItemToAdd.CreatedTime, addedTimeLineItem.CreatedTime);
            Assert.Equal(timeLineItemToAdd.AccessLevel, addedTimeLineItem.AccessLevel);
            Assert.Equal(timeLineItemToAdd.ProgenyId, addedTimeLineItem.ProgenyId);

            if (dbTimeLineItem != null)
            {
                Assert.IsType<TimeLineItem>(dbTimeLineItem);
                Assert.Equal(timeLineItemToAdd.CreatedBy, dbTimeLineItem.CreatedBy);
                Assert.Equal(timeLineItemToAdd.CreatedTime, dbTimeLineItem.CreatedTime);
                Assert.Equal(timeLineItemToAdd.AccessLevel, dbTimeLineItem.AccessLevel);
                Assert.Equal(timeLineItemToAdd.ProgenyId, dbTimeLineItem.ProgenyId);
            }
            Assert.NotNull(savedTimeLineItem);
            Assert.IsType<TimeLineItem>(savedTimeLineItem);
            Assert.Equal(timeLineItemToAdd.CreatedBy, savedTimeLineItem.CreatedBy);
            Assert.Equal(timeLineItemToAdd.CreatedTime, savedTimeLineItem.CreatedTime);
            Assert.Equal(timeLineItemToAdd.AccessLevel, savedTimeLineItem.AccessLevel);
            Assert.Equal(timeLineItemToAdd.ProgenyId, savedTimeLineItem.ProgenyId);
        }

        [Fact]
        public async Task UpdateTimeLineItem_Should_Save_TimeLineItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateTimeLineItem_Should_Save_TimeLineItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            TimeLineItem timeLineItemToUpdate = await timelineService.GetTimeLineItem(1);
            timeLineItemToUpdate.AccessLevel = 5;
            TimeLineItem updatedTimeLineItem = await timelineService.UpdateTimeLineItem(timeLineItemToUpdate);
            TimeLineItem? dbTimeLineItem = await context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.TimeLineId == 1);
            TimeLineItem savedTimeLineItem = await timelineService.GetTimeLineItem(1);

            Assert.NotNull(updatedTimeLineItem);
            Assert.IsType<TimeLineItem>(updatedTimeLineItem);
            Assert.NotEqual(0, updatedTimeLineItem.TimeLineId);
            Assert.Equal("User1", updatedTimeLineItem.CreatedBy);
            Assert.Equal(5, updatedTimeLineItem.AccessLevel);
            Assert.Equal(1, updatedTimeLineItem.ProgenyId);

            if (dbTimeLineItem != null)
            {
                Assert.IsType<TimeLineItem>(dbTimeLineItem);
                Assert.NotEqual(0, dbTimeLineItem.TimeLineId);
                Assert.Equal("User1", dbTimeLineItem.CreatedBy);
                Assert.Equal(5, dbTimeLineItem.AccessLevel);
                Assert.Equal(1, dbTimeLineItem.ProgenyId);
            }

            Assert.NotNull(savedTimeLineItem);
            Assert.IsType<TimeLineItem>(savedTimeLineItem);
            Assert.NotEqual(0, savedTimeLineItem.TimeLineId);
            Assert.Equal("User1", savedTimeLineItem.CreatedBy);
            Assert.Equal(5, savedTimeLineItem.AccessLevel);
            Assert.Equal(1, savedTimeLineItem.ProgenyId);
        }

        [Fact]
        public async Task DeleteTimeLineItem_Should_Remove_TimeLineItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteTimeLineItem_Should_Remove_TimeLineItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            int timeLineItemItemsCountBeforeDelete = context.TimeLineDb.Count();
            TimeLineItem timeLineItemToDelete = await timelineService.GetTimeLineItem(1);

            await timelineService.DeleteTimeLineItem(timeLineItemToDelete);
            TimeLineItem? deletedTimeLineItem = await context.TimeLineDb.SingleOrDefaultAsync(ti => ti.TimeLineId == 1);
            int timeLineItemItemsCountAfterDelete = context.TimeLineDb.Count();

            Assert.Null(deletedTimeLineItem);
            Assert.Equal(2, timeLineItemItemsCountBeforeDelete);
            Assert.Equal(1, timeLineItemItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetTimeLineItemsList_Should_Return_List_Of_TimeLineItem_When_Progeny_Has_Saved_TimeLineItems()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTimeLineItemsList_Should_Return_List_Of_TimeLineItem_When_Progeny_Has_Saved_TimeLineItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            List<TimeLineItem> timeLineItemsList = await timelineService.GetTimeLineList(1);
            List<TimeLineItem> timeLineItemsList2 = await timelineService.GetTimeLineList(1); // Test cached result.
            TimeLineItem firstTimeLineItem = timeLineItemsList.First();

            Assert.NotNull(timeLineItemsList);
            Assert.IsType<List<TimeLineItem>>(timeLineItemsList);
            Assert.Equal(2, timeLineItemsList.Count);
            Assert.NotNull(timeLineItemsList2);
            Assert.IsType<List<TimeLineItem>>(timeLineItemsList2);
            Assert.Equal(2, timeLineItemsList2.Count);
            Assert.NotNull(firstTimeLineItem);
            Assert.IsType<TimeLineItem>(firstTimeLineItem);
        }

        [Fact]
        public async Task GetTimeLineItemsList_Should_Return_Empty_List_Of_TimeLineItem_When_Progeny_Has_No_Saved_TimeLineItems()
        {

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTimeLineItemsList_Should_Return_Empty_List_Of_TimeLineItem_When_Progeny_Has_No_Saved_TimeLineItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            List<TimeLineItem> timeLineItemsList = await timelineService.GetTimeLineList(2);
            List<TimeLineItem> timeLineItemsList2 = await timelineService.GetTimeLineList(2); // Test cached result.

            Assert.NotNull(timeLineItemsList);
            Assert.IsType<List<TimeLineItem>>(timeLineItemsList);
            Assert.Empty(timeLineItemsList);
            Assert.NotNull(timeLineItemsList2);
            Assert.IsType<List<TimeLineItem>>(timeLineItemsList2);
            Assert.Empty(timeLineItemsList2);
        }

        [Fact]
        public async Task GetOnThisDayData_Should_Return_OnThisDayResponse_With_Empty_List_Of_TimeLineItem_When_Progeny_Has_No_Saved_TimeLineItems()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTimeLineItemsList_Should_Return_Empty_List_Of_TimeLineItem_When_Progeny_Has_No_Saved_TimeLineItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow,
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow,
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ProgenyId = 2,
                ThisDayDateTime = DateTime.UtcNow,
                AccessLevel = 0,
                Skip = 0,
                NumberOfItems = 10,
                TagFilter = string.Empty,
                OnThisDayPeriod = OnThisDayPeriod.Year,
                TimeLineTypeFilter = new List<KinaUnaTypes.TimeLineType>()
            };

            OnThisDayResponse onThisDayResponse = await timelineService.GetOnThisDayData(onThisDayRequest);

            Assert.NotNull(onThisDayResponse);
            Assert.IsType<OnThisDayResponse>(onThisDayResponse);
            Assert.Empty(onThisDayResponse.TimeLineItems);
            Assert.NotNull(onThisDayResponse.Request);
        }

        [Fact]
        public async Task GetOnThisDayData_Should_Return_OnThisDayResponse_With_List_Of_TimeLineItem_When_Progeny_Has_Saved_TimeLineItems()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTimeLineItemsList_Should_Return_Empty_List_Of_TimeLineItem_When_Progeny_Has_No_Saved_TimeLineItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            TimeLineItem timeLineItem1 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow - TimeSpan.FromDays(14),
                ItemId = "1",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow - TimeSpan.FromDays(14),
            };

            TimeLineItem timeLineItem2 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow - TimeSpan.FromDays(7),
                ItemId = "2",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow - TimeSpan.FromDays(7),
            };

            TimeLineItem timeLineItem3 = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                CreatedBy = "User1",
                CreatedTime = DateTime.UtcNow - TimeSpan.FromDays(1),
                ItemId = "3",
                ItemType = 1,
                ProgenyTime = DateTime.UtcNow - TimeSpan.FromDays(1),
            };

            context.Add(timeLineItem1);
            context.Add(timeLineItem2);
            context.Add(timeLineItem3);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            TimelineService timelineService = new(context, memoryCache);

            OnThisDayRequest onThisDayRequest = new OnThisDayRequest
            {
                ProgenyId = 1,
                ThisDayDateTime = DateTime.UtcNow,
                AccessLevel = 0,
                Skip = 0,
                NumberOfItems = 10,
                TagFilter = string.Empty,
                OnThisDayPeriod = OnThisDayPeriod.Week,
                TimeLineTypeFilter = new List<KinaUnaTypes.TimeLineType>()
            };

            OnThisDayResponse onThisDayResponse = await timelineService.GetOnThisDayData(onThisDayRequest);

            Assert.NotNull(onThisDayResponse);
            Assert.IsType<OnThisDayResponse>(onThisDayResponse);
            Assert.NotEmpty(onThisDayResponse.TimeLineItems);
            Assert.Equal(2, onThisDayResponse.TimeLineItems.Count);
            Assert.Equal(0, onThisDayResponse.RemainingItems);
        }
    }
}
