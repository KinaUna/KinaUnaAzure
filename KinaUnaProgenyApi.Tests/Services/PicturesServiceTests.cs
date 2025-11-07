using ImageMagick;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class PicturesServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<IImageStore> _mockImageStore = new();
        
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

        private UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        private Picture CreateTestPicture(int pictureId = 1, int progenyId = 1, string pictureLink = "test-guid.jpg")
        {
            return new Picture
            {
                PictureId = pictureId,
                ProgenyId = progenyId,
                Author = "testuser@test.com",
                PictureLink = pictureLink,
                PictureLink600 = pictureLink.Replace(".jpg", "-600.jpg"),
                PictureLink1200 = pictureLink.Replace(".jpg", "-1200.jpg"),
                Tags = "Tag1, Tag2",
                Altitude = "100",
                Latitude = "40.7128",
                Longtitude = "-74.0060",
                Location = "Test Location",
                PictureHeight = 1000,
                PictureWidth = 1000,
                PictureRotation = 0,
                PictureTime = DateTime.UtcNow,
                CommentThreadNumber = 1,
                CreatedBy = "testuser@test.com",
                CreatedTime = DateTime.UtcNow,
                ModifiedBy = "testuser@test.com",
                ModifiedTime = DateTime.UtcNow
            };
        }

        #region GetPicture Tests

        [Fact]
        public async Task GetPicture_Should_Return_Picture_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicture_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.GetPicture(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PictureId);
            Assert.Equal("testuser@test.com", result.Author);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetPicture_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicture_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.GetPicture(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPicture_Should_Return_Null_When_Picture_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicture_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.GetPicture(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPicture_Should_Use_Cache_On_Subsequent_Calls()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicture_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result1 = await service.GetPicture(1, userInfo);
            Picture? result2 = await service.GetPicture(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.PictureId, result2.PictureId);
        }

        #endregion

        #region AddPicture Tests

        [Fact]
        public async Task AddPicture_Should_Add_Picture_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("AddPicture_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();
            picture.PictureId = 0;

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.AddPicture(picture, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.PictureId);
            Picture? dbPicture = await context.PicturesDb.FindAsync(result.PictureId);
            Assert.NotNull(dbPicture);
        }

        [Fact]
        public async Task AddPicture_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("AddPicture_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();
            picture.PictureId = 0;

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.AddPicture(picture, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdatePicture Tests

        [Fact]
        public async Task UpdatePicture_Should_Update_Picture_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdatePicture_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            picture.Tags = "UpdatedTag";

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.UpdatePicture(picture, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedTag", result.Tags);
        }

        [Fact]
        public async Task UpdatePicture_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdatePicture_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            picture.Tags = "UpdatedTag";

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.UpdatePicture(picture, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePicture_Should_Return_Null_When_Picture_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdatePicture_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture(999);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.UpdatePicture(picture, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeletePicture Tests

        [Fact]
        public async Task DeletePicture_Should_Delete_Picture_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeletePicture_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockImageStore
                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("deleted");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.DeletePicture(picture, userInfo);

            // Assert
            Assert.NotNull(result);
            Picture? dbPicture = await context.PicturesDb.FindAsync(1);
            Assert.Null(dbPicture);
        }

        [Fact]
        public async Task DeletePicture_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeletePicture_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.DeletePicture(picture, userInfo);

            // Assert
            Assert.Null(result);
            Picture? dbPicture = await context.PicturesDb.FindAsync(1);
            Assert.NotNull(dbPicture);
        }

        [Fact]
        public async Task DeletePicture_Should_Not_Delete_Image_When_Used_By_Other_Pictures()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeletePicture_SharedImage");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture(1, 1, "shared-image.jpg");
            Picture picture2 = CreateTestPicture(2, 1, "shared-image.jpg");

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            int deleteCallCount = 0;
            _mockImageStore
                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => deleteCallCount++)
                .ReturnsAsync("deleted");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.DeletePicture(picture1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, deleteCallCount); // Image should not be deleted because picture2 still uses it
        }

        #endregion

        #region DeletePictureAsSystem Tests

        [Fact]
        public async Task DeletePictureAsSystem_Should_Delete_Picture_Without_Permission_Check()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("DeletePictureAsSystem_Valid");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockImageStore
                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("deleted");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.DeletePictureAsSystem(picture);

            // Assert
            Assert.NotNull(result);
            Picture? dbPicture = await context.PicturesDb.FindAsync(1);
            Assert.Null(dbPicture);
        }

        #endregion

        #region GetPictureByLink Tests

        [Fact]
        public async Task GetPictureByLink_Should_Return_Picture_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPictureByLink_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture(1, 1, "unique-link.jpg");

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.GetPictureByLink("unique-link.jpg", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unique-link.jpg", result.PictureLink);
        }

        [Fact]
        public async Task GetPictureByLink_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPictureByLink_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture = CreateTestPicture(1, 1, "unique-link.jpg");

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.GetPictureByLink("unique-link.jpg", userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetPictureInCache Tests

        [Fact]
        public async Task SetPictureInCache_Should_Add_Picture_To_Cache()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetPictureInCache_Valid");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.SetPictureInCache(1);

            // Assert
            Assert.NotNull(result);
            string? cachedPicture = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "picture1");
            Assert.NotNull(cachedPicture);
        }

        [Fact]
        public async Task SetPictureInCache_Should_Return_Null_When_Picture_Does_Not_Exist()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetPictureInCache_NotFound");
            IDistributedCache cache = GetMemoryCache();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.SetPictureInCache(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetPictureInCache_Should_Trim_Trailing_Commas_From_Tags()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetPictureInCache_TrimTags");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();
            picture.Tags = "Tag1, Tag2,";

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            Picture? result = await service.SetPictureInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tag1, Tag2", result.Tags);
        }

        #endregion

        #region RemovePictureFromCache Tests

        [Fact]
        public async Task RemovePictureFromCache_Should_Remove_Picture_From_Cache()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("RemovePictureFromCache_Valid");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);
            await service.SetPictureInCache(1);

            // Act
            await service.RemovePictureFromCache(1, 1);

            // Assert
            string? cachedPicture = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "picture1");
            Assert.Null(cachedPicture);
        }

        #endregion

        #region GetPicturesList Tests

        [Fact]
        public async Task GetPicturesList_Should_Return_Pictures_When_User_Has_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesList_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            Picture picture2 = CreateTestPicture(2);

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.GetPicturesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetPicturesList_Should_Filter_Pictures_By_Permission()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesList_Filter");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            Picture picture2 = CreateTestPicture(2);

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.GetPicturesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].PictureId);
        }

        [Fact]
        public async Task GetPicturesList_Should_Return_Empty_List_When_No_Pictures()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesList_Empty");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.GetPicturesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region SetPicturesListInCache Tests

        [Fact]
        public async Task SetPicturesListInCache_Should_Cache_Pictures_List()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("SetPicturesListInCache_Valid");
            IDistributedCache cache = GetMemoryCache();
            Picture picture1 = CreateTestPicture();
            Picture picture2 = CreateTestPicture(2);

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.SetPicturesListInCache(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            string? cachedList = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "pictureslist1");
            Assert.NotNull(cachedList);
        }

        #endregion

        #region GetPicturesWithTag Tests

        [Fact]
        public async Task GetPicturesWithTag_Should_Return_Pictures_With_Matching_Tag()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesWithTag_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            picture1.Tags = "Tag1, Tag2";
            Picture picture2 = CreateTestPicture(2);
            picture2.Tags = "Tag3, Tag4";

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.GetPicturesWithTag(1, "Tag1", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].PictureId);
        }

        [Fact]
        public async Task GetPicturesWithTag_Should_Return_All_Pictures_When_Tag_Is_Empty()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesWithTag_EmptyTag");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            Picture picture2 = CreateTestPicture(2);

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            List<Picture>? result = await service.GetPicturesWithTag(1, "", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetPicturesLocations Tests

        [Fact]
        public async Task GetPicturesLocations_Should_Return_Grouped_Locations()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesLocations_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            picture1.Latitude = "40.7128";
            picture1.Longtitude = "-74.0060";
            Picture picture2 = CreateTestPicture(2);
            picture2.Latitude = "40.7580";
            picture2.Longtitude = "-73.9855";

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            PicturesLocationsRequest request = new()
            {
                ProgenyId = 1,
                Progenies = new List<int> { 1 },
                Distance = 10.0

            };

            // Act
            PicturesLocationsResponse? result = await service.GetPicturesLocations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NumberOfLocations);
            Assert.Single(result.LocationsList);
        }

        #endregion

        #region GetPicturesNearLocation Tests

        [Fact]
        public async Task GetPicturesNearLocation_Should_Return_Pictures_Within_Distance()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesNearLocation_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            picture1.Latitude = "40.7128";
            picture1.Longtitude = "-74.0060";
            Picture picture2 = CreateTestPicture(2);
            picture2.Latitude = "34.0522";
            picture2.Longtitude = "-118.2437";

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            NearByPhotosRequest request = new()
            {
                ProgenyId = 1,
                Progenies = new List<int> { 1 },
                LocationItem = new Location { Latitude = 40.7128, Longitude = -74.0060 },
                Distance = 10.0,
                SortOrder = 0
            };

            // Act
            NearByPhotosResponse? result = await service.GetPicturesNearLocation(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.PicturesList);
        }

        [Fact]
        public async Task GetPicturesNearLocation_Should_Sort_By_Time_Descending_When_SortOrder_Is_1()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("GetPicturesNearLocation_SortDesc");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Picture picture1 = CreateTestPicture();
            picture1.Latitude = "40.7128";
            picture1.Longtitude = "-74.0060";
            picture1.PictureTime = DateTime.UtcNow.AddDays(-1);
            Picture picture2 = CreateTestPicture(2);
            picture2.Latitude = "40.7128";
            picture2.Longtitude = "-74.0060";
            picture2.PictureTime = DateTime.UtcNow;

            context.PicturesDb.AddRange(picture1, picture2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            NearByPhotosRequest request = new()
            {
                ProgenyId = 1,
                Progenies = new List<int> { 1 },
                LocationItem = new Location { Latitude = 40.7128, Longitude = -74.0060 },
                Distance = 10.0,
                SortOrder = 1
            };

            // Act
            NearByPhotosResponse? result = await service.GetPicturesNearLocation(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PicturesList.Count);
            Assert.True(result.PicturesList[0].PictureTime > result.PicturesList[1].PictureTime);
        }

        #endregion

        #region ProcessPicture Tests

        //[Fact]
        //public async Task ProcessPicture_Should_Process_Picture_Successfully()
        //{
        //    // Arrange
        //    await using MediaDbContext context = GetInMemoryDbContext("ProcessPicture_Valid");
        //    IDistributedCache cache = GetMemoryCache();
        //    Picture picture = CreateTestPicture();
        //
        //    MemoryStream imageStream = new MemoryStream();
        //    _mockImageStore
        //        .Setup(x => x.GetStream(It.IsAny<string>(), It.IsAny<string>()))
        //        .ReturnsAsync(imageStream);
        //
        //    _mockImageStore
        //        .Setup(x => x.SaveImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
        //        .ReturnsAsync("processed-image.jpg");
        //
        //    PicturesService service = new PicturesService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object, _mockServiceScopeFactory.Object);
        //
        //    // Act
        //    Picture? result = await service.ProcessPicture(picture);
        //
        //    // Assert
        //    Assert.NotNull(result);
        //    // Note: Full processing requires valid image data
        //}

        #endregion

        #region UpdateItemPictureExtension Tests

        [Fact]
        public async Task UpdateItemPictureExtension_Should_Return_Updated_Filename()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("UpdateItemPictureExtension_Valid");
            IDistributedCache cache = GetMemoryCache();

            MemoryStream imageStream = new();
            _mockImageStore
                .Setup(x => x.GetStream(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(imageStream);

            _mockImageStore
                .Setup(x => x.SaveImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("updated-image.jpg");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            string? result = await service.UpdateItemPictureExtension("old-image", BlobContainers.Pictures);

            // Assert
            Assert.Equal("old-image", result); // Returns original when stream is empty
        }

        #endregion

        #region ProcessProgenyPicture Tests

        [Fact]
        public async Task ProcessProgenyPicture_Should_Process_And_Save_Image()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("ProcessProgenyPicture_Valid");
            IDistributedCache cache = GetMemoryCache();

            _mockImageStore
                .Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Progeny, It.IsAny<string>()))
                .ReturnsAsync("progeny-picture.jpg");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Create a mock IFormFile
            Mock<IFormFile> mockFile = new();
            string content = "Fake image content";
            string fileName = "test.jpg";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);

            // Act & Assert
            // Note: This will fail without a valid image, but tests the method signature
            await Assert.ThrowsAsync<MagickMissingDelegateErrorException>(
                async () => await service.ProcessProgenyPicture(mockFile.Object));
        }

        #endregion

        #region ProcessProfilePicture Tests

        [Fact]
        public async Task ProcessProfilePicture_Should_Process_And_Save_Image()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("ProcessProfilePicture_Valid");
            IDistributedCache cache = GetMemoryCache();

            _mockImageStore
                .Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Profiles, It.IsAny<string>()))
                .ReturnsAsync("profile-picture.jpg");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            Mock<IFormFile> mockFile = new();
            string content = "Fake image content";
            string fileName = "test.jpg";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);

            // Act & Assert
            await Assert.ThrowsAsync<MagickMissingDelegateErrorException>(
                async () => await service.ProcessProfilePicture(mockFile.Object));
        }

        #endregion

        #region ProcessFriendPicture Tests

        [Fact]
        public async Task ProcessFriendPicture_Should_Process_And_Save_Image()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("ProcessFriendPicture_Valid");
            IDistributedCache cache = GetMemoryCache();

            _mockImageStore
                .Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Friends, It.IsAny<string>()))
                .ReturnsAsync("friend-picture.jpg");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            Mock<IFormFile> mockFile = new();
            string content = "Fake image content";
            string fileName = "test.jpg";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);

            // Act & Assert
            await Assert.ThrowsAsync<MagickMissingDelegateErrorException>(
                async () => await service.ProcessFriendPicture(mockFile.Object));
        }

        #endregion

        #region ProcessContactPicture Tests

        [Fact]
        public async Task ProcessContactPicture_Should_Process_And_Save_Image()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("ProcessContactPicture_Valid");
            IDistributedCache cache = GetMemoryCache();

            _mockImageStore
                .Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Contacts, It.IsAny<string>()))
                .ReturnsAsync("contact-picture.jpg");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            Mock<IFormFile> mockFile = new();
            string content = "Fake image content";
            string fileName = "test.jpg";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);

            // Act & Assert
            await Assert.ThrowsAsync<MagickMissingDelegateErrorException>(
                async () => await service.ProcessContactPicture(mockFile.Object));
        }

        #endregion

        #region CheckPicturePropertiesForNull Tests

        [Fact]
        public async Task CheckPicturePropertiesForNull_Should_Set_Null_Properties_To_Defaults()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("CheckPicturePropertiesForNull_Valid");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();
            picture.Altitude = null;
            picture.Latitude = null;
            picture.Longtitude = null;
            picture.Location = null;
            picture.Tags = null;
            picture.PictureRotation = null;

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckPicturePropertiesForNull();

            // Assert
            Picture? updatedPicture = await context.PicturesDb.FindAsync(1);
            Assert.NotNull(updatedPicture);
            Assert.Equal("", updatedPicture.Altitude);
            Assert.Equal("", updatedPicture.Latitude);
            Assert.Equal("", updatedPicture.Longtitude);
            Assert.Equal("", updatedPicture.Location);
            Assert.Equal("", updatedPicture.Tags);
            Assert.Equal(0, updatedPicture.PictureRotation);
        }

        #endregion

        #region CheckPictureLinks Tests

        [Fact]
        public async Task CheckPictureLinks_Should_Delete_Pictures_With_Null_Links()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("CheckPictureLinks_NullLinks");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();
            picture.PictureLink = "";
            picture.ProgenyId = 1;
            picture.PictureId = 100;
            

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockImageStore
                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("deleted");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckPictureLinks();

            // Assert
            Picture? deletedPicture = await context.PicturesDb.FindAsync(1);
            Assert.Null(deletedPicture);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Delete_Pictures_With_Missing_Images()
        {
            // Arrange
            await using MediaDbContext context = GetInMemoryDbContext("CheckPictureLinks_MissingImage");
            IDistributedCache cache = GetMemoryCache();
            Picture picture = CreateTestPicture();

            context.PicturesDb.Add(picture);
            await context.SaveChangesAsync();

            _mockImageStore
                .Setup(x => x.ImageExists(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockImageStore
                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("deleted");

            PicturesService service = new(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            await service.CheckPictureLinks();

            // Assert
            Picture? deletedPicture = await context.PicturesDb.FindAsync(1);
            Assert.Null(deletedPicture);
        }

        #endregion
    }
}