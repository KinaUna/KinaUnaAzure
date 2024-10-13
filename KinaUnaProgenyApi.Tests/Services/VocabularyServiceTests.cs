using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class VocabularyServiceTests
    {
        [Fact]
        public async Task GetVocabularyItem_Should_Return_VocabularyItem_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVocabularyItem_Should_Return_VocabularyItem_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, Date = DateTime.UtcNow,
                DateAdded = DateTime.Now, VocabularyItemNumber = 1, Description = "Description1",
                Language = "Language1", SoundsLike = "SoundsLike1", Word = "Word1"
            };
            
            VocabularyItem vocabularyItem2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };

            context.Add(vocabularyItem1);
            context.Add(vocabularyItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            VocabularyItem resultVocabularyItem1 = await vocabularyService.GetVocabularyItem(1);
            VocabularyItem resultVocabularyItem2 = await vocabularyService.GetVocabularyItem(1); // Uses cache

            Assert.NotNull(resultVocabularyItem1);
            Assert.IsType<VocabularyItem>(resultVocabularyItem1);
            Assert.Equal(vocabularyItem1.Author, resultVocabularyItem1.Author);
            Assert.Equal(vocabularyItem1.Word, resultVocabularyItem1.Word);
            Assert.Equal(vocabularyItem1.AccessLevel, resultVocabularyItem1.AccessLevel);
            Assert.Equal(vocabularyItem1.ProgenyId, resultVocabularyItem1.ProgenyId);

            Assert.NotNull(resultVocabularyItem2);
            Assert.IsType<VocabularyItem>(resultVocabularyItem2);
            Assert.Equal(vocabularyItem1.Author, resultVocabularyItem2.Author);
            Assert.Equal(vocabularyItem1.Word, resultVocabularyItem2.Word);
            Assert.Equal(vocabularyItem1.AccessLevel, resultVocabularyItem2.AccessLevel);
            Assert.Equal(vocabularyItem1.ProgenyId, resultVocabularyItem2.ProgenyId);
        }

        [Fact]
        public async Task GetVocabularyItem_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVocabularyItem_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };
            
            context.Add(vocabularyItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            VocabularyItem resultVocabularyItem1 = await vocabularyService.GetVocabularyItem(2);
            VocabularyItem resultVocabularyItem2 = await vocabularyService.GetVocabularyItem(2); // Using cache
            
            Assert.Null(resultVocabularyItem1);
            Assert.Null(resultVocabularyItem2);
        }

        [Fact]
        public async Task AddVocabularyItem_Should_Save_VocabularyItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddVocabularyItem_Should_Save_VocabularyItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };
            
            context.Add(vocabularyItem1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);
            
            VocabularyItem vocabularyItemToAdd = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };

            VocabularyItem addedVocabularyItem = await vocabularyService.AddVocabularyItem(vocabularyItemToAdd);
            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == addedVocabularyItem.WordId);
            VocabularyItem savedVocabularyItem = await vocabularyService.GetVocabularyItem(addedVocabularyItem.WordId);

            Assert.NotNull(addedVocabularyItem);
            Assert.IsType<VocabularyItem>(addedVocabularyItem);
            Assert.Equal(vocabularyItemToAdd.Author, addedVocabularyItem.Author);
            Assert.Equal(vocabularyItemToAdd.Word, addedVocabularyItem.Word);
            Assert.Equal(vocabularyItemToAdd.AccessLevel, addedVocabularyItem.AccessLevel);
            Assert.Equal(vocabularyItemToAdd.ProgenyId, addedVocabularyItem.ProgenyId);

            if (dbVocabularyItem != null)
            {
                Assert.IsType<VocabularyItem>(dbVocabularyItem);
                Assert.Equal(vocabularyItemToAdd.Author, dbVocabularyItem.Author);
                Assert.Equal(vocabularyItemToAdd.Word, dbVocabularyItem.Word);
                Assert.Equal(vocabularyItemToAdd.AccessLevel, dbVocabularyItem.AccessLevel);
                Assert.Equal(vocabularyItemToAdd.ProgenyId, dbVocabularyItem.ProgenyId);
            }
            Assert.NotNull(savedVocabularyItem);
            Assert.IsType<VocabularyItem>(savedVocabularyItem);
            Assert.Equal(vocabularyItemToAdd.Author, savedVocabularyItem.Author);
            Assert.Equal(vocabularyItemToAdd.Word , savedVocabularyItem.Word);
            Assert.Equal(vocabularyItemToAdd.AccessLevel, savedVocabularyItem.AccessLevel);
            Assert.Equal(vocabularyItemToAdd.ProgenyId, savedVocabularyItem.ProgenyId);

        }

        [Fact]
        public async Task UpdateVocabularyItem_Should_Save_VocabularyItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateVocabularyItem_Should_Save_VocabularyItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };

            VocabularyItem vocabularyItem2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };
            
            context.Add(vocabularyItem1);
            context.Add(vocabularyItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            VocabularyItem vocabularyItemToUpdate = await vocabularyService.GetVocabularyItem(1);
            vocabularyItemToUpdate.AccessLevel = 5;
            VocabularyItem updatedVocabularyItem = await vocabularyService.UpdateVocabularyItem(vocabularyItemToUpdate);
            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == 1);
            VocabularyItem savedVocabularyItem = await vocabularyService.GetVocabularyItem(1);

            Assert.NotNull(updatedVocabularyItem);
            Assert.IsType<VocabularyItem>(updatedVocabularyItem);
            Assert.NotEqual(0, updatedVocabularyItem.WordId);
            Assert.Equal("User1", updatedVocabularyItem.Author);
            Assert.Equal(5, updatedVocabularyItem.AccessLevel);
            Assert.Equal(1, updatedVocabularyItem.ProgenyId);

            if (dbVocabularyItem != null)
            {
                Assert.IsType<VocabularyItem>(dbVocabularyItem);
                Assert.NotEqual(0, dbVocabularyItem.WordId);
                Assert.Equal("User1", dbVocabularyItem.Author);
                Assert.Equal(5, dbVocabularyItem.AccessLevel);
                Assert.Equal(1, dbVocabularyItem.ProgenyId);
            }

            Assert.NotNull(savedVocabularyItem);
            Assert.IsType<VocabularyItem>(savedVocabularyItem);
            Assert.NotEqual(0, savedVocabularyItem.WordId);
            Assert.Equal("User1", savedVocabularyItem.Author);
            Assert.Equal(5, savedVocabularyItem.AccessLevel);
            Assert.Equal(1, savedVocabularyItem.ProgenyId);
        }

        [Fact]
        public async Task DeleteVocabularyItem_Should_Remove_VocabularyItem()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteVocabularyItem_Should_Remove_VocabularyItem").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };

            VocabularyItem vocabularyItem2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };

            context.Add(vocabularyItem1);
            context.Add(vocabularyItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            int vocabularyItemItemsCountBeforeDelete = context.VocabularyDb.Count();
            VocabularyItem vocabularyItemToDelete = await vocabularyService.GetVocabularyItem(1);

            await vocabularyService.DeleteVocabularyItem(vocabularyItemToDelete);
            VocabularyItem? deletedVocabularyItem = await context.VocabularyDb.SingleOrDefaultAsync(v => v.WordId == 1);
            int vocabularyItemItemsCountAfterDelete = context.VocabularyDb.Count();

            Assert.Null(deletedVocabularyItem);
            Assert.Equal(2, vocabularyItemItemsCountBeforeDelete);
            Assert.Equal(1, vocabularyItemItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetVocabularyItemsList_Should_Return_List_Of_VocabularyItem_When_Progeny_Has_Saved_VocabularyItems()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVocabularyItemsList_Should_Return_List_Of_VocabularyItem_When_Progeny_Has_Saved_VocabularyItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };

            VocabularyItem vocabularyItem2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };

            context.Add(vocabularyItem1);
            context.Add(vocabularyItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            List<VocabularyItem> vocabularyItemsList = await vocabularyService.GetVocabularyList(1, 0);
            List<VocabularyItem> vocabularyItemsList2 = await vocabularyService.GetVocabularyList(1,0); // Test cached result.
            VocabularyItem firstVocabularyItem = vocabularyItemsList.First();

            Assert.NotNull(vocabularyItemsList);
            Assert.IsType<List<VocabularyItem>>(vocabularyItemsList);
            Assert.Equal(2, vocabularyItemsList.Count);
            Assert.NotNull(vocabularyItemsList2);
            Assert.IsType<List<VocabularyItem>>(vocabularyItemsList2);
            Assert.Equal(2, vocabularyItemsList2.Count);
            Assert.NotNull(firstVocabularyItem);
            Assert.IsType<VocabularyItem>(firstVocabularyItem);
        }

        [Fact]
        public async Task GetVocabularyItemsList_Should_Return_Empty_List_Of_VocabularyItem_When_Progeny_Has_No_Saved_VocabularyItems()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVocabularyItemsList_Should_Return_Empty_List_Of_VocabularyItem_When_Progeny_Has_No_Saved_VocabularyItems").Options;
            await using ProgenyDbContext context = new(dbOptions);

            VocabularyItem vocabularyItem1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 1,
                Description = "Description1",
                Language = "Language1",
                SoundsLike = "SoundsLike1",
                Word = "Word1"
            };
            
            VocabularyItem vocabularyItem2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Date = DateTime.UtcNow,
                DateAdded = DateTime.Now,
                VocabularyItemNumber = 2,
                Description = "Description2",
                Language = "Language2",
                SoundsLike = "SoundsLike2",
                Word = "Word2"
            };

            context.Add(vocabularyItem1);
            context.Add(vocabularyItem2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VocabularyService vocabularyService = new(context, memoryCache);

            List<VocabularyItem> vocabularyItemsList = await vocabularyService.GetVocabularyList(2, 0);
            List<VocabularyItem> vocabularyItemsList2 = await vocabularyService.GetVocabularyList(2, 0); // Test cached result.

            Assert.NotNull(vocabularyItemsList);
            Assert.IsType<List<VocabularyItem>>(vocabularyItemsList);
            Assert.Empty(vocabularyItemsList);
            Assert.NotNull(vocabularyItemsList2);
            Assert.IsType<List<VocabularyItem>>(vocabularyItemsList2);
            Assert.Empty(vocabularyItemsList2);
        }
    }
}
