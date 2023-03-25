using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class CalendarServiceTests
    {
        [Fact]
        public async Task GetCalendarItem_Should_Return_CalendarItem_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetCalendarItem_Should_Return_CalendarItem_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };

            CalendarItem calendarItem2 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 2,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            context.Add(calendarItem1);
            context.Add(calendarItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            CalendarItem resultCalendarItem1 = await calendarService.GetCalendarItem(1);
            CalendarItem resultCalendarItem2 = await calendarService.GetCalendarItem(1); // Uses cache

            Assert.NotNull(resultCalendarItem1);
            Assert.IsType<CalendarItem>(resultCalendarItem1);
            Assert.Equal(calendarItem1.Author, resultCalendarItem1.Author);
            Assert.Equal(calendarItem1.Title, resultCalendarItem1.Title);
            Assert.Equal(calendarItem1.AccessLevel, resultCalendarItem1.AccessLevel);
            Assert.Equal(calendarItem1.ProgenyId, resultCalendarItem1.ProgenyId);

            Assert.NotNull(resultCalendarItem2);
            Assert.IsType<CalendarItem>(resultCalendarItem2);
            Assert.Equal(calendarItem1.Author, resultCalendarItem2.Author);
            Assert.Equal(calendarItem1.Title, resultCalendarItem2.Title);
            Assert.Equal(calendarItem1.AccessLevel, resultCalendarItem2.AccessLevel);
            Assert.Equal(calendarItem1.ProgenyId, resultCalendarItem2.ProgenyId);
        }

        [Fact]
        public async Task GetCalendarItem_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetCalendarItem_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };
            context.Add(calendarItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            CalendarItem calendarItem = await calendarService.GetCalendarItem(2);
            CalendarItem calendarItem2 = await calendarService.GetCalendarItem(2);
            
            Assert.Null(calendarItem);

            Assert.Null(calendarItem2);
        }

        [Fact]
        public async Task AddCalendarItem_Should_Save_CalendarItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddCalendarItem_Should_Save_CalendarItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };
            context.Add(calendarItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            CalendarItem calendarItemToAdd = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 2,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            CalendarItem addedCalendarItem = await calendarService.AddCalendarItem(calendarItemToAdd);
            CalendarItem? dbCalendarItem = await context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(ci => ci.EventId == addedCalendarItem.EventId);
            CalendarItem savedCalendarItem = await calendarService.GetCalendarItem(addedCalendarItem.EventId);

            Assert.NotNull(addedCalendarItem);
            Assert.IsType<CalendarItem>(addedCalendarItem);
            Assert.NotEqual(0, addedCalendarItem.EventId);
            Assert.Equal("User2", addedCalendarItem.Author);
            Assert.Equal(0, addedCalendarItem.AccessLevel);
            Assert.Equal(2, addedCalendarItem.ProgenyId);

            if (dbCalendarItem != null)
            {
                Assert.IsType<CalendarItem>(dbCalendarItem);
                Assert.NotEqual(0, dbCalendarItem.EventId);
                Assert.Equal("User2", dbCalendarItem.Author);
                Assert.Equal(0, dbCalendarItem.AccessLevel);
                Assert.Equal(2, dbCalendarItem.ProgenyId);
            }
            Assert.NotNull(savedCalendarItem);
            Assert.IsType<CalendarItem>(savedCalendarItem);
            Assert.NotEqual(0, savedCalendarItem.EventId);
            Assert.Equal("User2", savedCalendarItem.Author);
            Assert.Equal(0, savedCalendarItem.AccessLevel);
            Assert.Equal(2, savedCalendarItem.ProgenyId);

        }

        [Fact]
        public async Task UpdateCalendarItem_Should_Save_CalendarItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateCalendarItem_Should_Save_CalendarItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };

            CalendarItem calendarItem2 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 2,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            context.Add(calendarItem1);
            context.Add(calendarItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            CalendarItem calendarItemToUpdate = await calendarService.GetCalendarItem(1);
            calendarItemToUpdate.AccessLevel = 5;
            CalendarItem updatedCalendarItem = await calendarService.UpdateCalendarItem(calendarItemToUpdate);
            CalendarItem? dbCalendarItem = await context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(ci => ci.EventId == 1);
            CalendarItem savedCalendarItem = await calendarService.GetCalendarItem(1);

            Assert.NotNull(updatedCalendarItem);
            Assert.IsType<CalendarItem>(updatedCalendarItem);
            Assert.NotEqual(0, updatedCalendarItem.EventId);
            Assert.Equal("User1", updatedCalendarItem.Author);
            Assert.Equal(5, updatedCalendarItem.AccessLevel);
            Assert.Equal(1, updatedCalendarItem.ProgenyId);

            if (dbCalendarItem != null)
            {
                Assert.IsType<CalendarItem>(dbCalendarItem);
                Assert.NotEqual(0, dbCalendarItem.EventId);
                Assert.Equal("User1", dbCalendarItem.Author);
                Assert.Equal(5, dbCalendarItem.AccessLevel);
                Assert.Equal(1, dbCalendarItem.ProgenyId);
            }

            Assert.NotNull(savedCalendarItem);
            Assert.IsType<CalendarItem>(savedCalendarItem);
            Assert.NotEqual(0, savedCalendarItem.EventId);
            Assert.Equal("User1", savedCalendarItem.Author);
            Assert.Equal(5, savedCalendarItem.AccessLevel);
            Assert.Equal(1, savedCalendarItem.ProgenyId);
        }

        [Fact]
        public async Task DeleteCalendarItem_Should_Remove_CalendarItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteCalendarItem_Should_Remove_CalendarItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };

            CalendarItem calendarItem2 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 2,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            context.Add(calendarItem1);
            context.Add(calendarItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            int calendarItemsCountBeforeDelete = context.CalendarDb.Count();
            CalendarItem calendarItemToDelete = await calendarService.GetCalendarItem(1);

            await calendarService.DeleteCalendarItem(calendarItemToDelete);
            CalendarItem? deletedCalendarItem = await context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == 1);
            int calendarItemsCountAfterDelete = context.CalendarDb.Count();

            Assert.Null(deletedCalendarItem);
            Assert.Equal(2, calendarItemsCountBeforeDelete);
            Assert.Equal(1, calendarItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetCalendarList_Should_Return_List_Of_CalendarItem_When_Progeny_Has_Stored_Events()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetCalendarList_Should_Return_List_Of_CalendarItem_When_Progeny_Has_Stored_Events").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };

            CalendarItem calendarItem2 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            context.Add(calendarItem1);
            context.Add(calendarItem2);

            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            List<CalendarItem> calendarList = await calendarService.GetCalendarList(1);
            List<CalendarItem> calendarList2 = await calendarService.GetCalendarList(1); // Test cached result.
            CalendarItem firstCalendarItem = calendarList.First();

            Assert.NotNull(calendarList);
            Assert.Equal(2, calendarList.Count);
            Assert.NotNull(calendarList2);
            Assert.Equal(2, calendarList2.Count);
            Assert.NotNull(firstCalendarItem);
            Assert.IsType<CalendarItem>(firstCalendarItem);

        }

        [Fact]
        public async Task GetCalendarList_Should_Return_Empty_List_Of_CalendarItems_When_Progeny_Has_No_Stored_Events()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetCalendarList_Should_Return_Empty_List_Of_CalendarItems_When_Progeny_Has_No_Stored_Events").Options;
            await using ProgenyDbContext context = new(dbOptions);

            CalendarItem calendarItem1 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User1",
                Context = "Context1",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test1",
                Location = "Location1"
            };

            CalendarItem calendarItem2 = new()
            {
                StartTime = DateTime.UtcNow,
                ProgenyId = 1,
                AccessLevel = 0,
                AllDay = false,
                Author = "User2",
                Context = "Context2",
                EndTime = DateTime.UtcNow + TimeSpan.FromHours(1),
                Title = "Test2",
                Location = "Location2"
            };

            context.Add(calendarItem1);
            context.Add(calendarItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CalendarService calendarService = new(context, memoryCache);

            List<CalendarItem> calendarList = await calendarService.GetCalendarList(2);
            List<CalendarItem> calendarList2 = await calendarService.GetCalendarList(2); // Test cached result.

            Assert.NotNull(calendarList);
            Assert.IsType<List<CalendarItem>>(calendarList);
            Assert.Empty(calendarList);
            Assert.NotNull(calendarList2);
            Assert.IsType<List<CalendarItem>>(calendarList2);
            Assert.Empty(calendarList2);
        }
    }
}
