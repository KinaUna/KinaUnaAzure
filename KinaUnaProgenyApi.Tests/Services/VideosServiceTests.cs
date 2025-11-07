using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class VideosServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();

        private static MediaDbContext GetInMemoryDbContext(string dbName)
        {
            DbContextOptions<MediaDbContext> options = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new MediaDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        #region GetVideo Tests

        [Fact]
        public async Task GetVideo_Should_Return_Video_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideo_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                ThumbLink = "https://example.com/thumb.jpg",
                VideoTime = DateTime.UtcNow,
                Tags = "Tag1, Tag2",
                Location = "Test Location",
                Author = "testuser@test.com",
                AccessLevel = 0,
                VideoType = 1,
                Duration = TimeSpan.FromSeconds(60)
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideo(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VideoId);
            Assert.Equal("https://example.com/video.mp4", result.VideoLink);
            Assert.Equal("Tag1, Tag2", result.Tags);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetVideo_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideo_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideo(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVideo_Should_Return_Null_When_Video_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideo_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideo(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVideo_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideo_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                Tags = "Tag1, Tag2"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result1 = await service.GetVideo(1, userInfo);
            Video? result2 = await service.GetVideo(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.VideoLink, result2.VideoLink);
            Assert.Equal(result1.Tags, result2.Tags);
        }

        [Fact]
        public async Task GetVideo_Should_Trim_Trailing_Comma_From_Tags()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideo_TrimTags");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                Tags = "Tag1, Tag2,",
                CreatedBy = "testuser",
                CreatedTime = DateTime.UtcNow,
                ModifiedBy = "testuser",
                ModifiedTime = DateTime.UtcNow
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideo(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tag1, Tag2", result.Tags);
        }

        #endregion

        #region GetVideoByLink Tests

        [Fact]
        public async Task GetVideoByLink_Should_Return_Video_When_Link_And_ProgenyId_Match()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideoByLink_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                ThumbLink = "https://example.com/thumb.jpg"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideoByLink("https://example.com/video.mp4", 1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VideoId);
            Assert.Equal("https://example.com/video.mp4", result.VideoLink);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetVideoByLink_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideoByLink_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideoByLink("https://example.com/video.mp4", 1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVideoByLink_Should_Return_Null_When_Video_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideoByLink_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideoByLink("https://example.com/nonexistent.mp4", 1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVideoByLink_Should_Return_Null_When_ProgenyId_Does_Not_Match()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideoByLink_WrongProgeny");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.GetVideoByLink("https://example.com/video.mp4", 2, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetVideoInCache Tests

        [Fact]
        public async Task SetVideoInCache_Should_Return_Video_And_Cache_It()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideoInCache_Valid");
            IDistributedCache cache = GetMemoryCache();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                Tags = "Tag1, Tag2"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.SetVideoInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VideoId);
            Assert.Equal("https://example.com/video.mp4", result.VideoLink);

            // Verify it's cached
            string? cachedValue = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "video1");
            Assert.NotNull(cachedValue);
        }

        [Fact]
        public async Task SetVideoInCache_Should_Return_Null_When_Video_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideoInCache_NotFound");
            IDistributedCache cache = GetMemoryCache();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.SetVideoInCache(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetVideoInCache_Should_Trim_Trailing_Comma_From_Tags()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideoInCache_TrimTags");
            IDistributedCache cache = GetMemoryCache();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                Tags = "Tag1, Tag2, ",
                CreatedBy = "testuser",
                CreatedTime = DateTime.UtcNow,
                ModifiedBy = "testuser",
                ModifiedTime = DateTime.UtcNow
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.SetVideoInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tag1, Tag2", result.Tags);

            // Verify the database was updated
            Video? dbVideo = await context.VideoDb.FindAsync(1);
            Assert.Equal("Tag1, Tag2", dbVideo!.Tags);
        }

        #endregion

        #region AddVideo Tests

        [Fact]
        public async Task AddVideo_Should_Add_Video_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("AddVideo_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                ProgenyId = 1,
                VideoLink = "https://example.com/new-video.mp4",
                ThumbLink = "https://example.com/new-thumb.jpg",
                VideoTime = DateTime.UtcNow,
                Tags = "NewTag1, NewTag2",
                Location = "New Location",
                Author = "testuser@test.com",
                AccessLevel = 0,
                VideoType = 1,
                Duration = TimeSpan.FromSeconds(120),
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.AddVideo(video, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.VideoId);
            Assert.Equal("https://example.com/new-video.mp4", result.VideoLink);
            Assert.Equal("NewTag1, NewTag2", result.Tags);

            Video? dbVideo = await context.VideoDb.FindAsync(result.VideoId);
            Assert.NotNull(dbVideo);
            Assert.Equal("https://example.com/new-video.mp4", dbVideo.VideoLink);
        }

        [Fact]
        public async Task AddVideo_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("AddVideo_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                ProgenyId = 1,
                VideoLink = "https://example.com/new-video.mp4"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.AddVideo(video, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.VideoDb);
        }

        [Fact]
        public async Task AddVideo_Should_Remove_Null_Strings()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("AddVideo_RemoveNulls");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4",
                Tags = null,
                Location = null,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.AddVideo(video, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Tags);
            Assert.Equal(string.Empty, result.Location);
        }

        #endregion

        #region UpdateVideo Tests

        [Fact]
        public async Task UpdateVideo_Should_Update_Video_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdateVideo_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/original.mp4",
                Tags = "OriginalTag",
                Location = "Original Location"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();
            context.Entry(video).State = EntityState.Detached;

            Video updatedVideo = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/updated.mp4",
                Tags = "UpdatedTag",
                Location = "Updated Location",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.UpdateVideo(updatedVideo, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://example.com/updated.mp4", result.VideoLink);
            Assert.Equal("UpdatedTag", result.Tags);
            Assert.Equal("Updated Location", result.Location);

            Video? dbVideo = await context.VideoDb.FindAsync(1);
            Assert.NotNull(dbVideo);
            Assert.Equal("https://example.com/updated.mp4", dbVideo.VideoLink);
        }

        [Fact]
        public async Task UpdateVideo_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdateVideo_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/original.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            Video updatedVideo = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/updated.mp4"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.UpdateVideo(updatedVideo, userInfo);

            // Assert
            Assert.Null(result);

            // Verify original video unchanged
            Video? dbVideo = await context.VideoDb.FindAsync(1);
            Assert.Equal("https://example.com/original.mp4", dbVideo!.VideoLink);
        }

        [Fact]
        public async Task UpdateVideo_Should_Return_Null_When_Video_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdateVideo_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video updatedVideo = new()
            {
                VideoId = 999,
                ProgenyId = 1,
                VideoLink = "https://example.com/updated.mp4"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.UpdateVideo(updatedVideo, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteVideo Tests

        [Fact]
        public async Task DeleteVideo_Should_Delete_Video_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeleteVideo_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.DeleteVideo(video, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VideoId);

            Video? dbVideo = await context.VideoDb.FindAsync(1);
            Assert.Null(dbVideo);
        }

        [Fact]
        public async Task DeleteVideo_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeleteVideo_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.DeleteVideo(video, userInfo);

            // Assert
            Assert.Null(result);

            // Verify video still exists
            Video? dbVideo = await context.VideoDb.FindAsync(1);
            Assert.NotNull(dbVideo);
        }

        [Fact]
        public async Task DeleteVideo_Should_Return_Null_When_Video_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeleteVideo_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new()
            {
                VideoId = 999,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            Video? result = await service.DeleteVideo(video, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteVideo_Should_Remove_From_Cache()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeleteVideo_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video1 = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video1.mp4"
            };

            Video video2 = new()
            {
                VideoId = 2,
                ProgenyId = 1,
                VideoLink = "https://example.com/video2.mp4"
            };

            context.VideoDb.AddRange(video1, video2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Prime the cache
            await service.GetVideosList(1, userInfo);

            // Act
            await service.DeleteVideo(video1, userInfo);

            // Get list again - should be updated
            List<Video>? result = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].VideoId);
        }

        #endregion

        #region RemoveVideoFromCache Tests

        [Fact]
        public async Task RemoveVideoFromCache_Should_Remove_Video_And_Update_List()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("RemoveVideoFromCache_Valid");
            IDistributedCache cache = GetMemoryCache();

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                VideoLink = "https://example.com/video.mp4"
            };

            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Add to cache first
            await service.SetVideoInCache(1);

            // Act
            await service.RemoveVideoFromCache(1, 1);

            // Assert
            string? cachedVideo = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "video1");
            Assert.Null(cachedVideo);
        }

        #endregion

        #region GetVideosList Tests

        [Fact]
        public async Task GetVideosList_Should_Return_List_Of_Accessible_Videos()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosList_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetVideosList_Should_Return_Only_Accessible_Videos()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosList_PartialAccess");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, v => v.VideoId == 1);
            Assert.Contains(result, v => v.VideoId == 3);
            Assert.DoesNotContain(result, v => v.VideoId == 2);
        }

        [Fact]
        public async Task GetVideosList_Should_Return_Empty_List_When_No_Videos_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosList_Empty");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetVideosList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosList_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new() { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video.mp4" };
            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result1 = await service.GetVideosList(1, userInfo);
            List<Video>? result2 = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);
        }

        [Fact]
        public async Task GetVideosList_Should_Not_Include_Videos_From_Other_Progenies()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosList_FilterByProgeny");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4" },
                new Video { VideoId = 2, ProgenyId = 2, VideoLink = "https://example.com/video2.mp4" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(1, v.ProgenyId));
        }

        #endregion

        #region GetVideosWithTag Tests

        [Fact]
        public async Task GetVideosWithTag_Should_Return_Videos_With_Matching_Tag()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Family, Vacation" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = "Birthday" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4", Tags = "Family, Birthday" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "Family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, v => v.VideoId == 1);
            Assert.Contains(result, v => v.VideoId == 3);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Return_All_Videos_When_Tag_Is_Null()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_NoFilter");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Family" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = "Birthday" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Return_All_Videos_When_Tag_Is_Empty()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_EmptyFilter");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Family" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = "Birthday" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_CaseInsensitive");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new() { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video.mp4", Tags = "Family Vacation" };
            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Match_Substring()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_Substring");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Video video = new() { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video.mp4", Tags = "Summer Family Vacation" };
            context.VideoDb.Add(video);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "Family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Handle_Null_Tags_In_Video()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_NullTags");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Family" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = null },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4", Tags = "Birthday" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "Family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].VideoId);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Return_Empty_List_When_No_Matching_Tags()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_NoMatch");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Birthday" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = "Party" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "Family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetVideosWithTag_Should_Only_Return_Accessible_Videos()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetVideosWithTag_AccessFiltered");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4", Tags = "Family" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4", Tags = "Family" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4", Tags = "Family" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.GetVideosWithTag(1, "Family", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, v => v.VideoId == 1);
            Assert.Contains(result, v => v.VideoId == 3);
        }

        #endregion

        #region SetVideosListInCache Tests

        [Fact]
        public async Task SetVideosListInCache_Should_Return_List_And_Cache_It()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideosListInCache_Valid");
            IDistributedCache cache = GetMemoryCache();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4" },
                new Video { VideoId = 2, ProgenyId = 1, VideoLink = "https://example.com/video2.mp4" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.SetVideosListInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            // Verify it's cached
            string? cachedValue = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist1");
            Assert.NotNull(cachedValue);
        }

        [Fact]
        public async Task SetVideosListInCache_Should_Return_Empty_List_When_No_Videos_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideosListInCache_Empty");
            IDistributedCache cache = GetMemoryCache();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.SetVideosListInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SetVideosListInCache_Should_Only_Include_Videos_From_Specified_Progeny()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetVideosListInCache_FilterByProgeny");
            IDistributedCache cache = GetMemoryCache();

            List<Video> videos = new()
            {
                new Video { VideoId = 1, ProgenyId = 1, VideoLink = "https://example.com/video1.mp4" },
                new Video { VideoId = 2, ProgenyId = 2, VideoLink = "https://example.com/video2.mp4" },
                new Video { VideoId = 3, ProgenyId = 1, VideoLink = "https://example.com/video3.mp4" }
            };

            context.VideoDb.AddRange(videos);
            await context.SaveChangesAsync();

            VideosService service = new(context, cache, _mockAccessManagementService.Object);

            // Act
            List<Video>? result = await service.SetVideosListInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(1, v.ProgenyId));
        }

        #endregion
    }
}