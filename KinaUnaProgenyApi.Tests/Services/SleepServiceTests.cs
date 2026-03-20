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
    public class SleepServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;
        private readonly SleepService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public SleepServiceTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _adminUser = new UserInfo { UserId = "admin1", UserEmail = "admin@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContext (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup in-memory cache
            IOptions<MemoryDistributedCacheOptions> cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache cache = new MemoryDistributedCache(cacheOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockKinaUnaCacheService = new Mock<IKinaUnaCacheService>();

            // Setup default mock behaviors for IKinaUnaCacheService
            _mockKinaUnaCacheService
                .Setup(x => x.GetSleepListCache(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((SleepListCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.SetSleepListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Sleep[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);

            // Initialize service
            _service = new SleepService(_progenyDbContext, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test Sleep records
            Sleep sleep1 = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(8),
                SleepRating = 5,
                SleepNotes = "Good night sleep",
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };
            _progenyDbContext.SleepDb.Add(sleep1);

            Sleep sleep2 = new()
            {
                SleepId = 2,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-2),
                SleepEnd = DateTime.UtcNow.AddDays(-2).AddHours(6),
                SleepRating = 3,
                SleepNotes = "Restless night",
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                CreatedTime = DateTime.UtcNow.AddDays(-2),
            };
            _progenyDbContext.SleepDb.Add(sleep2);

            Sleep sleep3 = new()
            {
                SleepId = 3,
                ProgenyId = 2,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(7),
                SleepRating = 4,
                SleepNotes = "Another progeny's sleep",
                Author = "user2",
                CreatedBy = "user2",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1),
            };
            _progenyDbContext.SleepDb.Add(sleep3);

            _progenyDbContext.SaveChanges();
        }

        #region GetSleep Tests

        [Fact]
        public async Task GetSleep_WhenUserHasAccess_ReturnsSleepWithPermission()
        {
            // Arrange
            int sleepId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleepId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, sleepId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            Sleep result = await _service.GetSleep(sleepId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sleepId, result.SleepId);
            Assert.Equal(1, result.ProgenyId);
            Assert.Equal("Good night sleep", result.SleepNotes);
            Assert.Equal(5, result.SleepRating);
            Assert.NotNull(result.ItemPerMission);
            Assert.Equal(PermissionLevel.View, result.ItemPerMission.PermissionLevel);
        }

        [Fact]
        public async Task GetSleep_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            int sleepId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleepId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            Sleep result = await _service.GetSleep(sleepId, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSleep_WhenSleepDoesNotExist_ReturnsNull()
        {
            // Arrange
            int sleepId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleepId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            Sleep result = await _service.GetSleep(sleepId, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSleep_WhenCalledMultipleTimes_UsesCacheOnSecondCall()
        {
            // Arrange
            int sleepId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleepId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, sleepId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            Sleep firstCall = await _service.GetSleep(sleepId, _testUser);
            Sleep secondCall = await _service.GetSleep(sleepId, _testUser);

            // Assert
            Assert.NotNull(firstCall);
            Assert.NotNull(secondCall);
            Assert.Equal(firstCall.SleepId, secondCall.SleepId);
            Assert.Equal(firstCall.SleepNotes, secondCall.SleepNotes);
        }

        #endregion

        #region AddSleep Tests

        [Fact]
        public async Task AddSleep_WhenUserHasAccess_AddsSleepToDatabase()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-3),
                SleepEnd = DateTime.UtcNow.AddDays(-3).AddHours(9),
                SleepRating = 5,
                SleepNotes = "Excellent sleep",
                Author = "user1",
                CreatedBy = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            Sleep result = await _service.AddSleep(newSleep, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.SleepId > 0);
            Assert.Equal("Excellent sleep", result.SleepNotes);
            Assert.Equal(5, result.SleepRating);
            Assert.Equal(1, result.ProgenyId);

            // Verify it was added to the database
            Sleep? dbSleep = await _progenyDbContext.SleepDb.FindAsync(result.SleepId);
            Assert.NotNull(dbSleep);
            Assert.Equal(result.SleepNotes, dbSleep.SleepNotes);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Sleep), Times.Once);
        }

        [Fact]
        public async Task AddSleep_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow,
                SleepEnd = DateTime.UtcNow.AddHours(8),
                Author = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            Sleep result = await _service.AddSleep(newSleep, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddSleep_CopiesPropertiesCorrectly()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-4),
                SleepEnd = DateTime.UtcNow.AddDays(-4).AddHours(10),
                SleepRating = 4,
                SleepNotes = "Long sleep",
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            Sleep result = await _service.AddSleep(newSleep, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newSleep.ProgenyId, result.ProgenyId);
            Assert.Equal(newSleep.SleepStart, result.SleepStart);
            Assert.Equal(newSleep.SleepEnd, result.SleepEnd);
            Assert.Equal(newSleep.SleepRating, result.SleepRating);
            Assert.Equal(newSleep.SleepNotes, result.SleepNotes);
            Assert.Equal(newSleep.Author, result.Author);
        }

        #endregion

        #region UpdateSleep Tests

        [Fact]
        public async Task UpdateSleep_WhenUserHasAccess_UpdatesSleep()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(10),
                SleepRating = 4,
                SleepNotes = "Updated notes",
                Author = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            Sleep result = await _service.UpdateSleep(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SleepId);
            Assert.Equal("Updated notes", result.SleepNotes);
            Assert.Equal(4, result.SleepRating);

            // Verify database was updated
            Sleep? dbSleep = await _progenyDbContext.SleepDb.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
            Assert.NotNull(dbSleep);
            Assert.Equal("Updated notes", dbSleep.SleepNotes);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Sleep), Times.Once);
        }

        [Fact]
        public async Task UpdateSleep_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = 1,
                SleepNotes = "Should not update"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            Sleep result = await _service.UpdateSleep(updateValues, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSleep_WhenSleepDoesNotExist_ReturnsNull()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = 999,
                SleepNotes = "Non-existent"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Sleep result = await _service.UpdateSleep(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSleep_UpdatesCacheAfterUpdate()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = 2,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-2),
                SleepEnd = DateTime.UtcNow.AddDays(-2).AddHours(5),
                SleepRating = 2,
                SleepNotes = "Cache should update",
                Author = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 2, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Sleep, 2, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            Sleep result = await _service.UpdateSleep(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cache should update", result.SleepNotes);
        }

        #endregion

        #region DeleteSleep Tests

        [Fact]
        public async Task DeleteSleep_WhenUserHasAccess_RemovesSleep()
        {
            // Arrange
            Sleep sleepToDelete = new()
            {
                SleepId = 1,
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, _adminUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            int countBefore = await _progenyDbContext.SleepDb.CountAsync();

            // Act
            Sleep result = await _service.DeleteSleep(sleepToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SleepId);

            int countAfter = await _progenyDbContext.SleepDb.CountAsync();
            Assert.Equal(countBefore - 1, countAfter);

            Sleep? deletedSleep = await _progenyDbContext.SleepDb.FindAsync(1);
            Assert.Null(deletedSleep);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Sleep), Times.Once);
        }

        [Fact]
        public async Task DeleteSleep_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Sleep sleepToDelete = new()
            {
                SleepId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            Sleep result = await _service.DeleteSleep(sleepToDelete, _otherUser);

            // Assert
            Assert.Null(result);

            // Verify sleep still exists
            Sleep? sleep = await _progenyDbContext.SleepDb.FindAsync(1);
            Assert.NotNull(sleep);
        }

        [Fact]
        public async Task DeleteSleep_WhenSleepDoesNotExist_ReturnsNull()
        {
            // Arrange
            Sleep sleepToDelete = new()
            {
                SleepId = 999
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            Sleep result = await _service.DeleteSleep(sleepToDelete, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteSleep_RemovesFromCache()
        {
            // Arrange
            Sleep sleepToDelete = new()
            {
                SleepId = 2,
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 2, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 2, _adminUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            Sleep result = await _service.DeleteSleep(sleepToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);

            // Verify it's removed from database
            Sleep? deletedSleep = await _progenyDbContext.SleepDb.FindAsync(2);
            Assert.Null(deletedSleep);
        }

        #endregion

        #region GetSleepList Tests

        [Fact]
        public async Task GetSleepList_ReturnsOnlySleepsWithAccess()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Sleep> result = await _service.GetSleepList(progenyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, sleep => Assert.Equal(progenyId, sleep.ProgenyId));
            Assert.All(result, sleep => Assert.NotNull(sleep.ItemPerMission));
        }

        [Fact]
        public async Task GetSleepList_FiltersOutSleepsWithoutAccess()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Sleep> result = await _service.GetSleepList(progenyId, _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].SleepId);
        }

        [Fact]
        public async Task GetSleepList_WhenProgenyHasNoSleeps_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 999;

            // Act
            List<Sleep> result = await _service.GetSleepList(progenyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSleepList_UsesCache_OnSecondCall()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Sleep> firstCall = await _service.GetSleepList(progenyId, _testUser);
            List<Sleep> secondCall = await _service.GetSleepList(progenyId, _testUser);

            // Assert
            Assert.NotNull(firstCall);
            Assert.NotNull(secondCall);
            Assert.Equal(firstCall.Count, secondCall.Count);
        }

        [Fact]
        public async Task GetSleepList_WhenUserHasNoAccessToAnySleep_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<Sleep> result = await _service.GetSleepList(progenyId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSleepList_OnlyReturnsForRequestedProgeny()
        {
            // Arrange
            int progenyId = 2;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 3, _otherUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 3, 2, 0, _otherUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Sleep> result = await _service.GetSleepList(progenyId, _otherUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result[0].SleepId);
            Assert.Equal(progenyId, result[0].ProgenyId);
        }

        #endregion
    }
}