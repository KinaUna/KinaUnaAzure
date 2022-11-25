using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class PicturesServiceTests
    {
        [Fact]
        public async Task GetPicture_Should_Return_Picture_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetPicture_Should_Return_Picture_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, PictureLink = Constants.ProfilePictureUrl, Tags = "Tag1, Tag2", Altitude = "0", Latitude = "0",
                CommentThreadNumber = 1, Location = "Location1", Longtitude = "0", Owners = "User1", PictureHeight = 100, PictureWidth = 100, PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl, PictureNumber = 1, PictureRotation = 0, PictureTime = DateTime.UtcNow, TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            Picture resultPicture1 = await pictureService.GetPicture(1);
            Picture resultPicture2 = await pictureService.GetPicture(1); // Uses cache

            Assert.NotNull(resultPicture1);
            Assert.IsType<Picture>(resultPicture1);
            Assert.Equal(picture1.Author, resultPicture1.Author);
            Assert.Equal(picture1.Location, resultPicture1.Location);
            Assert.Equal(picture1.AccessLevel, resultPicture1.AccessLevel);
            Assert.Equal(picture1.ProgenyId, resultPicture1.ProgenyId);

            Assert.NotNull(resultPicture2);
            Assert.IsType<Picture>(resultPicture2);
            Assert.Equal(picture1.Author, resultPicture2.Author);
            Assert.Equal(picture1.Location, resultPicture2.Location);
            Assert.Equal(picture1.AccessLevel, resultPicture2.AccessLevel);
            Assert.Equal(picture1.ProgenyId, resultPicture2.ProgenyId);
        }

        [Fact]
        public async Task GetPicture_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetPicture_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };
            
            context.Add(picture1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            Picture resultPicture1 = await pictureService.GetPicture(2);
            Picture resultPicture2 = await pictureService.GetPicture(2); // Using cache
            
            Assert.Null(resultPicture1);
            Assert.Null(resultPicture2);
        }

        [Fact]
        public async Task AddPicture_Should_Save_Picture()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("AddPicture_Should_Save_Picture").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };
            
            context.Add(picture1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            Picture pictureToAdd = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };

            Picture addedPicture = await pictureService.AddPicture(pictureToAdd);
            Picture? dbPicture = await context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(f => f.PictureId == addedPicture.PictureId);
            Picture savedPicture = await pictureService.GetPicture(addedPicture.PictureId);

            Assert.NotNull(addedPicture);
            Assert.IsType<Picture>(addedPicture);
            Assert.Equal(pictureToAdd.Author, addedPicture.Author);
            Assert.Equal(pictureToAdd.Location, addedPicture.Location);
            Assert.Equal(pictureToAdd.AccessLevel, addedPicture.AccessLevel);
            Assert.Equal(pictureToAdd.ProgenyId, addedPicture.ProgenyId);

            if (dbPicture != null)
            {
                Assert.IsType<Picture>(dbPicture);
                Assert.Equal(pictureToAdd.Author, dbPicture.Author);
                Assert.Equal(pictureToAdd.Location, dbPicture.Location);
                Assert.Equal(pictureToAdd.AccessLevel, dbPicture.AccessLevel);
                Assert.Equal(pictureToAdd.ProgenyId, dbPicture.ProgenyId);
            }
            Assert.NotNull(savedPicture);
            Assert.IsType<Picture>(savedPicture);
            Assert.Equal(pictureToAdd.Author, savedPicture.Author);
            Assert.Equal(pictureToAdd.Location, savedPicture.Location);
            Assert.Equal(pictureToAdd.AccessLevel, savedPicture.AccessLevel);
            Assert.Equal(pictureToAdd.ProgenyId, savedPicture.ProgenyId);

        }

        [Fact]
        public async Task UpdatePicture_Should_Save_Picture()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("UpdatePicture_Should_Save_Picture").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };
            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            Picture pictureToUpdate = await pictureService.GetPicture(1);
            pictureToUpdate.AccessLevel = 5;
            Picture updatedPicture = await pictureService.UpdatePicture(pictureToUpdate);
            Picture? dbPicture = await context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(f => f.PictureId == 1);
            Picture savedPicture = await pictureService.GetPicture(1);

            Assert.NotNull(updatedPicture);
            Assert.IsType<Picture>(updatedPicture);
            Assert.NotEqual(0, updatedPicture.PictureId);
            Assert.Equal("User1", updatedPicture.Author);
            Assert.Equal(5, updatedPicture.AccessLevel);
            Assert.Equal(1, updatedPicture.ProgenyId);

            if (dbPicture != null)
            {
                Assert.IsType<Picture>(dbPicture);
                Assert.NotEqual(0, dbPicture.PictureId);
                Assert.Equal("User1", dbPicture.Author);
                Assert.Equal(5, dbPicture.AccessLevel);
                Assert.Equal(1, dbPicture.ProgenyId);
            }

            Assert.NotNull(savedPicture);
            Assert.IsType<Picture>(savedPicture);
            Assert.NotEqual(0, savedPicture.PictureId);
            Assert.Equal("User1", savedPicture.Author);
            Assert.Equal(5, savedPicture.AccessLevel);
            Assert.Equal(1, savedPicture.ProgenyId);
        }

        [Fact]
        public async Task DeletePicture_Should_Remove_Picture()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("DeletePicture_Should_Remove_Picture").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };
            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            int pictureItemsCountBeforeDelete = context.PicturesDb.Count();
            Picture pictureToDelete = await pictureService.GetPicture(1);

            await pictureService.DeletePicture(pictureToDelete);
            Picture? deletedPicture = await context.PicturesDb.SingleOrDefaultAsync(f => f.PictureId == 1);
            int pictureItemsCountAfterDelete = context.PicturesDb.Count();

            Assert.Null(deletedPicture);
            Assert.Equal(2, pictureItemsCountBeforeDelete);
            Assert.Equal(1, pictureItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetPictureByLink_Returns_Picture_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetPictureByLink_Returns_Picture_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = "TestLink1",
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = "TestLink2",
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            Picture resultPicture1 = await pictureService.GetPictureByLink("TestLink1");
            
            Assert.NotNull(resultPicture1);
            Assert.IsType<Picture>(resultPicture1);
            Assert.Equal(picture1.Author, resultPicture1.Author);
            Assert.Equal(picture1.Location, resultPicture1.Location);
            Assert.Equal(picture1.AccessLevel, resultPicture1.AccessLevel);
            Assert.Equal(picture1.ProgenyId, resultPicture1.ProgenyId);
        }

        [Fact]
        public async Task GetPicturesList_Should_Return_List_Of_Picture_When_Progeny_Has_Saved_Pictures()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetPicturesList_Should_Return_List_Of_Picture_When_Progeny_Has_Saved_Pictures").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            List<Picture> picturesList = await pictureService.GetPicturesList(1);
            List<Picture> picturesList2 = await pictureService.GetPicturesList(1); // Test cached result.
            Picture firstPicture = picturesList.First();

            Assert.NotNull(picturesList);
            Assert.IsType<List<Picture>>(picturesList);
            Assert.Equal(2, picturesList.Count);
            Assert.NotNull(picturesList2);
            Assert.IsType<List<Picture>>(picturesList2);
            Assert.Equal(2, picturesList2.Count);
            Assert.NotNull(firstPicture);
            Assert.IsType<Picture>(firstPicture);
        }

        [Fact]
        public async Task GetPicturesList_Should_Return_Empty_List_Of_Picture_When_Progeny_Has_No_Saved_Pictures()
        {
            
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetPicturesList_Should_Return_Empty_List_Of_Picture_When_Progeny_Has_No_Saved_Pictures").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Picture picture1 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };


            Picture picture2 = new Picture
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                PictureHeight = 100,
                PictureWidth = 100,
                PictureLink600 = Constants.ProfilePictureUrl,
                PictureLink1200 = Constants.ProfilePictureUrl,
                PictureNumber = 1,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(picture1);
            context.Add(picture2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            PicturesService pictureService = new PicturesService(context, memoryCache);

            List<Picture> picturesList = await pictureService.GetPicturesList(2);
            List<Picture> picturesList2 = await pictureService.GetPicturesList(2); // Test cached result.

            Assert.NotNull(picturesList);
            Assert.IsType<List<Picture>>(picturesList);
            Assert.Empty(picturesList);
            Assert.NotNull(picturesList2);
            Assert.IsType<List<Picture>>(picturesList2);
            Assert.Empty(picturesList2);
        }
    }
}
