using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class VocabularyServiceTests
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService = new();
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService = new();

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

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        #region GetVocabularyItem Tests

        [Fact]
        public async Task GetVocabularyItem_Should_Return_VocabularyItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyItem_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Test Word",
                Description = "Test Description",
                Language = "English",
                SoundsLike = "test werd",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                Author = "testuser@test.com",
                CreatedBy = "testuser@test.com",
                CreatedTime = DateTime.UtcNow
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.GetVocabularyItem(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.WordId);
            Assert.Equal("Test Word", result.Word);
            Assert.Equal("Test Description", result.Description);
            Assert.NotNull(result.ItemPermission);
        }

        [Fact]
        public async Task GetVocabularyItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyItem_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Test Word"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.GetVocabularyItem(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVocabularyItem_Should_Return_Null_When_VocabularyItem_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyItem_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.GetVocabularyItem(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVocabularyItem_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyItem_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Test Word",
                Description = "Test Description"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result1 = await service.GetVocabularyItem(1, userInfo);
            VocabularyItem? result2 = await service.GetVocabularyItem(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Word, result2.Word);
            Assert.Equal(result1.Description, result2.Description);
        }

        #endregion

        #region AddVocabularyItem Tests

        [Fact]
        public async Task AddVocabularyItem_Should_Add_VocabularyItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddVocabularyItem_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                ProgenyId = 1,
                Word = "New Word",
                Description = "New Description",
                Language = "English",
                SoundsLike = "noo werd",
                Date = DateTime.UtcNow,
                Author = "testuser@test.com",
                CreatedBy = "testuser@test.com",
                ItemPermissionsDtoList = []
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.AddVocabularyItem(vocabularyItem);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.WordId);
            Assert.Equal("New Word", result.Word);
            Assert.Equal("New Description", result.Description);
            Assert.NotEqual(default(DateTime), result.CreatedTime);
            Assert.NotEqual(default(DateTime), result.DateAdded);

            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.FindAsync([result.WordId], TestContext.Current.CancellationToken);
            Assert.NotNull(dbVocabularyItem);
            Assert.Equal("New Word", dbVocabularyItem.Word);
        }

        [Fact]
        public async Task AddVocabularyItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddVocabularyItem_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                ProgenyId = 1,
                Word = "New Word",
                CreatedBy = "testuser@test.com"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.AddVocabularyItem(vocabularyItem);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.VocabularyDb);
        }

        [Fact]
        public async Task AddVocabularyItem_Should_Set_Timestamps()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddVocabularyItem_Timestamps");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            DateTime beforeAdd = DateTime.UtcNow;

            VocabularyItem vocabularyItem = new()
            {
                ProgenyId = 1,
                Word = "New Word",
                Description = "New Description",
                CreatedBy = "testuser@test.com",
                ItemPermissionsDtoList = []
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.AddVocabularyItem(vocabularyItem);
            DateTime afterAdd = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedTime >= beforeAdd);
            Assert.True(result.CreatedTime <= afterAdd);
            Assert.True(result.DateAdded >= beforeAdd);
            Assert.True(result.DateAdded <= afterAdd);
            Assert.True(result.ModifiedTime >= beforeAdd);
            Assert.True(result.ModifiedTime <= afterAdd);
        }

        #endregion

        #region UpdateVocabularyItem Tests

        [Fact]
        public async Task UpdateVocabularyItem_Should_Update_VocabularyItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateVocabularyItem_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Original Word",
                Description = "Original Description",
                Language = "English"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Entry(vocabularyItem).State = EntityState.Detached;

            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Updated Word",
                Description = "Updated Description",
                Language = "Spanish",
                ModifiedBy = "testuser@test.com",
                ItemPermissionsDtoList = []
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync([]);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.UpdateVocabularyItem(updatedVocabularyItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Word", result.Word);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("Spanish", result.Language);

            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.NotNull(dbVocabularyItem);
            Assert.Equal("Updated Word", dbVocabularyItem.Word);
            Assert.Equal("Updated Description", dbVocabularyItem.Description);
        }

        [Fact]
        public async Task UpdateVocabularyItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateVocabularyItem_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Original Word",
                Description = "Original Description"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Updated Word",
                Description = "Updated Description",
                ModifiedBy = "testuser@test.com"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.UpdateVocabularyItem(updatedVocabularyItem);

            // Assert
            Assert.Null(result);

            // Verify original vocabulary item unchanged
            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.Equal("Original Word", dbVocabularyItem!.Word);
        }

        [Fact]
        public async Task UpdateVocabularyItem_Should_Return_Null_When_VocabularyItem_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateVocabularyItem_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = 999,
                ProgenyId = 1,
                Word = "Updated Word",
                Description = "Updated Description",
                ModifiedBy = "testuser@test.com"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.UpdateVocabularyItem(updatedVocabularyItem);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateVocabularyItem_Should_Update_ModifiedTime()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateVocabularyItem_ModifiedTime");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            DateTime originalTime = DateTime.UtcNow.AddDays(-1);

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Original Word",
                ModifiedTime = originalTime
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Entry(vocabularyItem).State = EntityState.Detached;

            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Updated Word",
                ModifiedBy = "testuser@test.com",
                ItemPermissionsDtoList = []
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync([]);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);
            DateTime beforeUpdate = DateTime.UtcNow;

            // Act
            VocabularyItem? result = await service.UpdateVocabularyItem(updatedVocabularyItem);
            DateTime afterUpdate = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ModifiedTime >= beforeUpdate);
            Assert.True(result.ModifiedTime <= afterUpdate);
            Assert.True(result.ModifiedTime > originalTime);
        }

        #endregion

        #region DeleteVocabularyItem Tests

        [Fact]
        public async Task DeleteVocabularyItem_Should_Delete_VocabularyItem_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteVocabularyItem_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Test Word",
                Description = "Test Description",
                ModifiedBy = "testuser@test.com"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, userInfo))
                .ReturnsAsync([]);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.DeleteVocabularyItem(vocabularyItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.WordId);

            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.Null(dbVocabularyItem);
        }

        [Fact]
        public async Task DeleteVocabularyItem_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteVocabularyItem_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Test Word",
                ModifiedBy = "testuser@test.com"
            };

            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.DeleteVocabularyItem(vocabularyItem);

            // Assert
            Assert.Null(result);

            // Verify vocabulary item still exists
            VocabularyItem? dbVocabularyItem = await context.VocabularyDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.NotNull(dbVocabularyItem);
        }

        [Fact]
        public async Task DeleteVocabularyItem_Should_Return_Null_When_VocabularyItem_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteVocabularyItem_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new()
            {
                WordId = 999,
                ProgenyId = 1,
                Word = "Test Word",
                ModifiedBy = "testuser@test.com"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            VocabularyItem? result = await service.DeleteVocabularyItem(vocabularyItem);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteVocabularyItem_Should_Remove_From_Cache()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteVocabularyItem_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem1 = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Word 1",
                ModifiedBy = "testuser@test.com"
            };

            VocabularyItem vocabularyItem2 = new()
            {
                WordId = 2,
                ProgenyId = 1,
                Word = "Word 2"
            };

            context.VocabularyDb.AddRange(vocabularyItem1, vocabularyItem2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("testuser@test.com"))
                .ReturnsAsync(userInfo);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, userInfo))
                .ReturnsAsync([]);

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Prime the cache
            await service.GetVocabularyList(1, userInfo);

            // Act
            await service.DeleteVocabularyItem(vocabularyItem1);

            // Get list again - should be updated
            List<VocabularyItem>? result = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].WordId);
        }

        #endregion

        #region GetVocabularyList Tests

        [Fact]
        public async Task GetVocabularyList_Should_Return_List_Of_Accessible_VocabularyItems()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyList_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, ProgenyId = 1, Word = "Word 1", Description = "Description 1" },
                new() { WordId = 2, ProgenyId = 1, Word = "Word 2", Description = "Description 2" },
                new() { WordId = 3, ProgenyId = 1, Word = "Word 3", Description = "Description 3" }
            ];

            context.VocabularyDb.AddRange(vocabularyItems);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<VocabularyItem>? result = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetVocabularyList_Should_Return_Only_Accessible_VocabularyItems()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyList_PartialAccess");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, ProgenyId = 1, Word = "Word 1" },
                new() { WordId = 2, ProgenyId = 1, Word = "Word 2" },
                new() { WordId = 3, ProgenyId = 1, Word = "Word 3" }
            ];

            context.VocabularyDb.AddRange(vocabularyItems);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<VocabularyItem>? result = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, v => v.WordId == 1);
            Assert.Contains(result, v => v.WordId == 3);
            Assert.DoesNotContain(result, v => v.WordId == 2);
        }

        [Fact]
        public async Task GetVocabularyList_Should_Return_Empty_List_When_No_VocabularyItems_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyList_Empty");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<VocabularyItem>? result = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetVocabularyList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyList_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            VocabularyItem vocabularyItem = new() { WordId = 1, ProgenyId = 1, Word = "Word 1" };
            context.VocabularyDb.Add(vocabularyItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<VocabularyItem>? result1 = await service.GetVocabularyList(1, userInfo);
            List<VocabularyItem>? result2 = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);
        }

        [Fact]
        public async Task GetVocabularyList_Should_Not_Include_VocabularyItems_From_Other_Progenies()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetVocabularyList_FilterByProgeny");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, ProgenyId = 1, Word = "Word 1" },
                new() { WordId = 2, ProgenyId = 2, Word = "Word 2" },
                new() { WordId = 3, ProgenyId = 1, Word = "Word 3" }
            ];

            context.VocabularyDb.AddRange(vocabularyItems);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockKinaUnaCacheService
                .Setup(x => x.GetVocabularyItemsListCache(userInfo.UserId, 1))
                .ReturnsAsync((VocabularyListCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vocabulary))
                .ReturnsAsync((TimelineUpdatedCacheEntry?)null);

            _mockKinaUnaCacheService
                .Setup(x => x.SetVocabularyItemsListCache(userInfo.UserId, 1, It.IsAny<VocabularyItem[]>()))
                .Returns(Task.CompletedTask);

            VocabularyService service = new(context, cache, _mockUserInfoService.Object, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<VocabularyItem>? result = await service.GetVocabularyList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(1, v.ProgenyId));
        }

        #endregion
    }
}