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
    public class VideosServiceTests
    {
        [Fact]
        public async Task GetVideo_Should_Return_Video_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetVideo_Should_Return_Video_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, VideoLink = Constants.ProfilePictureUrl, Tags = "Tag1, Tag2", Altitude = "0", Latitude = "0",
                CommentThreadNumber = 1, Location = "Location1", Longtitude = "0", Owners = "User1", VideoNumber = 1, VideoTime = DateTime.UtcNow, TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30), ThumbLink = Constants.ProfilePictureUrl, VideoType = 1
            };


            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            Video resultVideo1 = await videoService.GetVideo(1);
            Video resultVideo2 = await videoService.GetVideo(1); // Uses cache

            Assert.NotNull(resultVideo1);
            Assert.IsType<Video>(resultVideo1);
            Assert.Equal(video1.Author, resultVideo1.Author);
            Assert.Equal(video1.Location, resultVideo1.Location);
            Assert.Equal(video1.AccessLevel, resultVideo1.AccessLevel);
            Assert.Equal(video1.ProgenyId, resultVideo1.ProgenyId);

            Assert.NotNull(resultVideo2);
            Assert.IsType<Video>(resultVideo2);
            Assert.Equal(video1.Author, resultVideo2.Author);
            Assert.Equal(video1.Location, resultVideo2.Location);
            Assert.Equal(video1.AccessLevel, resultVideo2.AccessLevel);
            Assert.Equal(video1.ProgenyId, resultVideo2.ProgenyId);
        }

        [Fact]
        public async Task GetVideo_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetVideo_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };
            
            context.Add(video1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            Video resultVideo1 = await videoService.GetVideo(2);
            Video resultVideo2 = await videoService.GetVideo(2); // Using cache
            
            Assert.Null(resultVideo1);
            Assert.Null(resultVideo2);
        }

        [Fact]
        public async Task AddVideo_Should_Save_Video()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("AddVideo_Should_Save_Video").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };
            
            context.Add(video1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            Video videoToAdd = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            Video addedVideo = await videoService.AddVideo(videoToAdd);
            Video? dbVideo = await context.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == addedVideo.VideoId);
            Video savedVideo = await videoService.GetVideo(addedVideo.VideoId);

            Assert.NotNull(addedVideo);
            Assert.IsType<Video>(addedVideo);
            Assert.Equal(videoToAdd.Author, addedVideo.Author);
            Assert.Equal(videoToAdd.Location, addedVideo.Location);
            Assert.Equal(videoToAdd.AccessLevel, addedVideo.AccessLevel);
            Assert.Equal(videoToAdd.ProgenyId, addedVideo.ProgenyId);

            if (dbVideo != null)
            {
                Assert.IsType<Video>(dbVideo);
                Assert.Equal(videoToAdd.Author, dbVideo.Author);
                Assert.Equal(videoToAdd.Location, dbVideo.Location);
                Assert.Equal(videoToAdd.AccessLevel, dbVideo.AccessLevel);
                Assert.Equal(videoToAdd.ProgenyId, dbVideo.ProgenyId);
            }
            Assert.NotNull(savedVideo);
            Assert.IsType<Video>(savedVideo);
            Assert.Equal(videoToAdd.Author, savedVideo.Author);
            Assert.Equal(videoToAdd.Location, savedVideo.Location);
            Assert.Equal(videoToAdd.AccessLevel, savedVideo.AccessLevel);
            Assert.Equal(videoToAdd.ProgenyId, savedVideo.ProgenyId);

        }

        [Fact]
        public async Task UpdateVideo_Should_Save_Video()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("UpdateVideo_Should_Save_Video").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };


            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };
            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            Video videoToUpdate = await videoService.GetVideo(1);
            videoToUpdate.AccessLevel = 5;
            Video updatedVideo = await videoService.UpdateVideo(videoToUpdate);
            Video? dbVideo = await context.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == 1);
            Video savedVideo = await videoService.GetVideo(1);

            Assert.NotNull(updatedVideo);
            Assert.IsType<Video>(updatedVideo);
            Assert.NotEqual(0, updatedVideo.VideoId);
            Assert.Equal("User1", updatedVideo.Author);
            Assert.Equal(5, updatedVideo.AccessLevel);
            Assert.Equal(1, updatedVideo.ProgenyId);

            if (dbVideo != null)
            {
                Assert.IsType<Video>(dbVideo);
                Assert.NotEqual(0, dbVideo.VideoId);
                Assert.Equal("User1", dbVideo.Author);
                Assert.Equal(5, dbVideo.AccessLevel);
                Assert.Equal(1, dbVideo.ProgenyId);
            }

            Assert.NotNull(savedVideo);
            Assert.IsType<Video>(savedVideo);
            Assert.NotEqual(0, savedVideo.VideoId);
            Assert.Equal("User1", savedVideo.Author);
            Assert.Equal(5, savedVideo.AccessLevel);
            Assert.Equal(1, savedVideo.ProgenyId);
        }

        [Fact]
        public async Task DeleteVideo_Should_Remove_Video()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("DeleteVideo_Should_Remove_Video").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };


            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };
            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            int videoItemsCountBeforeDelete = context.VideoDb.Count();
            Video videoToDelete = await videoService.GetVideo(1);

            await videoService.DeleteVideo(videoToDelete);
            Video? deletedVideo = await context.VideoDb.SingleOrDefaultAsync(f => f.VideoId == 1);
            int videoItemsCountAfterDelete = context.VideoDb.Count();

            Assert.Null(deletedVideo);
            Assert.Equal(2, videoItemsCountBeforeDelete);
            Assert.Equal(1, videoItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetVideoByLink_Returns_Video_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetVideoByLink_Returns_Video_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            Video resultVideo1 = await videoService.GetVideoByLink(Constants.ProfilePictureUrl, 1);
            
            Assert.NotNull(resultVideo1);
            Assert.IsType<Video>(resultVideo1);
            Assert.Equal(video1.Author, resultVideo1.Author);
            Assert.Equal(video1.Location, resultVideo1.Location);
            Assert.Equal(video1.AccessLevel, resultVideo1.AccessLevel);
            Assert.Equal(video1.ProgenyId, resultVideo1.ProgenyId);
        }

        [Fact]
        public async Task GetVideosList_Should_Return_List_Of_Video_When_Progeny_Has_Saved_Videos()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetVideosList_Should_Return_List_Of_Video_When_Progeny_Has_Saved_Videos").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };


            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            List<Video> videosList = await videoService.GetVideosList(1);
            List<Video> videosList2 = await videoService.GetVideosList(1); // Test cached result.
            Video firstVideo = videosList.First();

            Assert.NotNull(videosList);
            Assert.IsType<List<Video>>(videosList);
            Assert.Equal(2, videosList.Count);
            Assert.NotNull(videosList2);
            Assert.IsType<List<Video>>(videosList2);
            Assert.Equal(2, videosList2.Count);
            Assert.NotNull(firstVideo);
            Assert.IsType<Video>(firstVideo);
        }

        [Fact]
        public async Task GetVideosList_Should_Return_Empty_List_Of_Video_When_Progeny_Has_No_Saved_Videos()
        {
            
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetVideosList_Should_Return_Empty_List_Of_Video_When_Progeny_Has_No_Saved_Videos").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Video video1 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 1,
                Location = "Location1",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 1,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };


            Video video2 = new Video
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                VideoLink = Constants.ProfilePictureUrl,
                Tags = "Tag2, Tag3",
                Altitude = "0",
                Latitude = "0",
                CommentThreadNumber = 2,
                Location = "Location2",
                Longtitude = "0",
                Owners = "User1",
                VideoNumber = 2,
                VideoTime = DateTime.UtcNow,
                TimeZone = Constants.DefaultTimezone,
                Duration = TimeSpan.FromSeconds(30),
                ThumbLink = Constants.ProfilePictureUrl,
                VideoType = 1
            };

            context.Add(video1);
            context.Add(video2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VideosService videoService = new VideosService(context, memoryCache);

            List<Video> videosList = await videoService.GetVideosList(2);
            List<Video> videosList2 = await videoService.GetVideosList(2); // Test cached result.

            Assert.NotNull(videosList);
            Assert.IsType<List<Video>>(videosList);
            Assert.Empty(videosList);
            Assert.NotNull(videosList2);
            Assert.IsType<List<Video>>(videosList2);
            Assert.Empty(videosList2);
        }
    }
}
