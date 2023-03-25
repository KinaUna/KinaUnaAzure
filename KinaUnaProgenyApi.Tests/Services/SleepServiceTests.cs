using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class SleepServiceTests
    {
        [Fact]
        public async Task GetSleep_Should_Return_Sleep_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSleep_Should_Return_Sleep_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, CreatedDate = DateTime.UtcNow, SleepStart = DateTime.Now, SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1), SleepNotes = "Note1", SleepNumber = 1, SleepRating = 0
            };
            
            Sleep sleep2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };

            context.Add(sleep1);
            context.Add(sleep2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            Sleep resultSleep1 = await sleepService.GetSleep(1);
            Sleep resultSleep2 = await sleepService.GetSleep(1); // Uses cache

            Assert.NotNull(resultSleep1);
            Assert.IsType<Sleep>(resultSleep1);
            Assert.Equal(sleep1.Author, resultSleep1.Author);
            Assert.Equal(sleep1.SleepStart, resultSleep1.SleepStart);
            Assert.Equal(sleep1.AccessLevel, resultSleep1.AccessLevel);
            Assert.Equal(sleep1.ProgenyId, resultSleep1.ProgenyId);

            Assert.NotNull(resultSleep2);
            Assert.IsType<Sleep>(resultSleep2);
            Assert.Equal(sleep1.Author, resultSleep2.Author);
            Assert.Equal(sleep1.SleepStart, resultSleep2.SleepStart);
            Assert.Equal(sleep1.AccessLevel, resultSleep2.AccessLevel);
            Assert.Equal(sleep1.ProgenyId, resultSleep2.ProgenyId);
        }

        [Fact]
        public async Task GetSleep_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSleep_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };
            
            context.Add(sleep1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            Sleep resultSleep1 = await sleepService.GetSleep(2);
            Sleep resultSleep2 = await sleepService.GetSleep(2); // Using cache
            
            Assert.Null(resultSleep1);
            Assert.Null(resultSleep2);
        }

        [Fact]
        public async Task AddSleep_Should_Save_Sleep()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddSleep_Should_Save_Sleep").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };
            
            context.Add(sleep1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);
            
            Sleep sleepToAdd = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };

            Sleep addedSleep = await sleepService.AddSleep(sleepToAdd);
            Sleep? dbSleep = await context.SleepDb.AsNoTracking().SingleOrDefaultAsync(f => f.SleepId == addedSleep.SleepId);
            Sleep savedSleep = await sleepService.GetSleep(addedSleep.SleepId);

            Assert.NotNull(addedSleep);
            Assert.IsType<Sleep>(addedSleep);
            Assert.Equal(sleepToAdd.Author, addedSleep.Author);
            Assert.Equal(sleepToAdd.SleepStart, addedSleep.SleepStart);
            Assert.Equal(sleepToAdd.AccessLevel, addedSleep.AccessLevel);
            Assert.Equal(sleepToAdd.ProgenyId, addedSleep.ProgenyId);

            if (dbSleep != null)
            {
                Assert.IsType<Sleep>(dbSleep);
                Assert.Equal(sleepToAdd.Author, dbSleep.Author);
                Assert.Equal(sleepToAdd.SleepStart, dbSleep.SleepStart);
                Assert.Equal(sleepToAdd.AccessLevel, dbSleep.AccessLevel);
                Assert.Equal(sleepToAdd.ProgenyId, dbSleep.ProgenyId);
            }
            Assert.NotNull(savedSleep);
            Assert.IsType<Sleep>(savedSleep);
            Assert.Equal(sleepToAdd.Author, savedSleep.Author);
            Assert.Equal(sleepToAdd.SleepStart , savedSleep.SleepStart);
            Assert.Equal(sleepToAdd.AccessLevel, savedSleep.AccessLevel);
            Assert.Equal(sleepToAdd.ProgenyId, savedSleep.ProgenyId);

        }

        [Fact]
        public async Task UpdateSleep_Should_Save_Sleep()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateSleep_Should_Save_Sleep").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };

            Sleep sleep2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };
            
            context.Add(sleep1);
            context.Add(sleep2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            Sleep sleepToUpdate = await sleepService.GetSleep(1);
            sleepToUpdate.AccessLevel = 5;
            Sleep updatedSleep = await sleepService.UpdateSleep(sleepToUpdate);
            Sleep? dbSleep = await context.SleepDb.AsNoTracking().SingleOrDefaultAsync(f => f.SleepId == 1);
            Sleep savedSleep = await sleepService.GetSleep(1);

            Assert.NotNull(updatedSleep);
            Assert.IsType<Sleep>(updatedSleep);
            Assert.NotEqual(0, updatedSleep.SleepId);
            Assert.Equal("User1", updatedSleep.Author);
            Assert.Equal(5, updatedSleep.AccessLevel);
            Assert.Equal(1, updatedSleep.ProgenyId);

            if (dbSleep != null)
            {
                Assert.IsType<Sleep>(dbSleep);
                Assert.NotEqual(0, dbSleep.SleepId);
                Assert.Equal("User1", dbSleep.Author);
                Assert.Equal(5, dbSleep.AccessLevel);
                Assert.Equal(1, dbSleep.ProgenyId);
            }

            Assert.NotNull(savedSleep);
            Assert.IsType<Sleep>(savedSleep);
            Assert.NotEqual(0, savedSleep.SleepId);
            Assert.Equal("User1", savedSleep.Author);
            Assert.Equal(5, savedSleep.AccessLevel);
            Assert.Equal(1, savedSleep.ProgenyId);
        }

        [Fact]
        public async Task DeleteSleep_Should_Remove_Sleep()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteSleep_Should_Remove_Sleep").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };

            Sleep sleep2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };

            context.Add(sleep1);
            context.Add(sleep2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            int sleepItemsCountBeforeDelete = context.SleepDb.Count();
            Sleep sleepToDelete = await sleepService.GetSleep(1);

            await sleepService.DeleteSleep(sleepToDelete);
            Sleep? deletedSleep = await context.SleepDb.SingleOrDefaultAsync(f => f.SleepId == 1);
            int sleepItemsCountAfterDelete = context.SleepDb.Count();

            Assert.Null(deletedSleep);
            Assert.Equal(2, sleepItemsCountBeforeDelete);
            Assert.Equal(1, sleepItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetSleepsList_Should_Return_List_Of_Sleep_When_Progeny_Has_Saved_Sleeps()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSleepsList_Should_Return_List_Of_Sleep_When_Progeny_Has_Saved_Sleeps").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };

            Sleep sleep2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };

            context.Add(sleep1);
            context.Add(sleep2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            List<Sleep> sleepsList = await sleepService.GetSleepList(1);
            List<Sleep> sleepsList2 = await sleepService.GetSleepList(1); // Test cached result.
            Sleep firstSleep = sleepsList.First();

            Assert.NotNull(sleepsList);
            Assert.IsType<List<Sleep>>(sleepsList);
            Assert.Equal(2, sleepsList.Count);
            Assert.NotNull(sleepsList2);
            Assert.IsType<List<Sleep>>(sleepsList2);
            Assert.Equal(2, sleepsList2.Count);
            Assert.NotNull(firstSleep);
            Assert.IsType<Sleep>(firstSleep);
        }

        [Fact]
        public async Task GetSleepsList_Should_Return_Empty_List_Of_Sleep_When_Progeny_Has_No_Saved_Sleeps()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSleepsList_Should_Return_Empty_List_Of_Sleep_When_Progeny_Has_No_Saved_Sleeps").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Sleep sleep1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now,
                SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(1),
                SleepNotes = "Note1",
                SleepNumber = 1,
                SleepRating = 0
            };
            
            Sleep sleep2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                CreatedDate = DateTime.UtcNow,
                SleepStart = DateTime.Now - TimeSpan.FromDays(1),
                SleepEnd = DateTime.UtcNow - TimeSpan.FromDays(1) + TimeSpan.FromHours(1),
                SleepNotes = "Note2",
                SleepNumber = 1,
                SleepRating = 0
            };

            context.Add(sleep1);
            context.Add(sleep2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SleepService sleepService = new(context, memoryCache);

            List<Sleep> sleepsList = await sleepService.GetSleepList(2);
            List<Sleep> sleepsList2 = await sleepService.GetSleepList(2); // Test cached result.

            Assert.NotNull(sleepsList);
            Assert.IsType<List<Sleep>>(sleepsList);
            Assert.Empty(sleepsList);
            Assert.NotNull(sleepsList2);
            Assert.IsType<List<Sleep>>(sleepsList2);
            Assert.Empty(sleepsList2);
        }
    }
}
