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
    public class VaccinationServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;
        private readonly VaccinationService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public VaccinationServiceTests()
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

            // Initialize service
            _service = new VaccinationService(_progenyDbContext, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test Vaccination records
            Vaccination vaccination1 = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                VaccinationName = "MMR Vaccine",
                VaccinationDescription = "Measles, Mumps, and Rubella vaccine",
                VaccinationDate = DateTime.Parse("2024-01-15"),
                Author = "user1"
            };
            _progenyDbContext.VaccinationsDb.Add(vaccination1);

            Vaccination vaccination2 = new()
            {
                VaccinationId = 2,
                ProgenyId = 1,
                VaccinationName = "Hepatitis B",
                VaccinationDescription = "First dose",
                VaccinationDate = DateTime.Parse("2024-02-20"),
                Author = "user1"
            };
            _progenyDbContext.VaccinationsDb.Add(vaccination2);

            Vaccination vaccination3 = new()
            {
                VaccinationId = 3,
                ProgenyId = 2,
                VaccinationName = "Polio Vaccine",
                VaccinationDescription = "Another progeny's vaccination",
                VaccinationDate = DateTime.Parse("2024-03-10"),
                Author = "user2"
            };
            _progenyDbContext.VaccinationsDb.Add(vaccination3);

            _progenyDbContext.SaveChanges();
        }

        #region GetVaccination Tests

        [Fact]
        public async Task GetVaccination_WhenUserHasAccess_ReturnsVaccinationWithPermission()
        {
            // Arrange
            int vaccinationId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            Vaccination result = await _service.GetVaccination(vaccinationId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vaccinationId, result.VaccinationId);
            Assert.Equal(1, result.ProgenyId);
            Assert.Equal("MMR Vaccine", result.VaccinationName);
            Assert.Equal("Measles, Mumps, and Rubella vaccine", result.VaccinationDescription);
            Assert.NotNull(result.ItemPermission);
            Assert.Equal(PermissionLevel.View, result.ItemPermission.PermissionLevel);
        }

        [Fact]
        public async Task GetVaccination_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            int vaccinationId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            Vaccination result = await _service.GetVaccination(vaccinationId, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVaccination_WhenVaccinationDoesNotExist_ReturnsNull()
        {
            // Arrange
            int vaccinationId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            Vaccination result = await _service.GetVaccination(vaccinationId, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVaccination_WhenCalledMultipleTimes_UsesCacheOnSecondCall()
        {
            // Arrange
            int vaccinationId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, vaccinationId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            Vaccination firstCall = await _service.GetVaccination(vaccinationId, _testUser);
            Vaccination secondCall = await _service.GetVaccination(vaccinationId, _testUser);

            // Assert
            Assert.NotNull(firstCall);
            Assert.NotNull(secondCall);
            Assert.Equal(firstCall.VaccinationId, secondCall.VaccinationId);
            Assert.Equal(firstCall.VaccinationName, secondCall.VaccinationName);
        }

        #endregion

        #region AddVaccination Tests

        [Fact]
        public async Task AddVaccination_WhenUserHasAccess_AddsVaccinationToDatabase()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = 1,
                VaccinationName = "DTaP Vaccine",
                VaccinationDescription = "Diphtheria, Tetanus, and Pertussis vaccine",
                VaccinationDate = DateTime.Parse("2024-04-15"),
                Author = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.AddVaccination(newVaccination, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.VaccinationId > 0);
            Assert.Equal("DTaP Vaccine", result.VaccinationName);
            Assert.Equal("Diphtheria, Tetanus, and Pertussis vaccine", result.VaccinationDescription);
            Assert.Equal(1, result.ProgenyId);

            // Verify it was added to the database
            Vaccination? dbVaccination = await _progenyDbContext.VaccinationsDb.FindAsync(result.VaccinationId);
            Assert.NotNull(dbVaccination);
            Assert.Equal(result.VaccinationName, dbVaccination.VaccinationName);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(
                x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination),
                Times.Once);
        }

        [Fact]
        public async Task AddVaccination_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = 1,
                VaccinationName = "Test Vaccine",
                VaccinationDate = DateTime.UtcNow,
                Author = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            Vaccination result = await _service.AddVaccination(newVaccination, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddVaccination_CopiesPropertiesCorrectly()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = 1,
                VaccinationName = "Influenza Vaccine",
                VaccinationDescription = "Annual flu shot",
                VaccinationDate = DateTime.Parse("2024-10-01"),
                Author = "user1",
                Notes = "No side effects",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.AddVaccination(newVaccination, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newVaccination.ProgenyId, result.ProgenyId);
            Assert.Equal(newVaccination.VaccinationName, result.VaccinationName);
            Assert.Equal(newVaccination.VaccinationDescription, result.VaccinationDescription);
            Assert.Equal(newVaccination.VaccinationDate, result.VaccinationDate);
            Assert.Equal(newVaccination.Author, result.Author);
        }

        [Fact]
        public async Task AddVaccination_CallsAddItemPermissions()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = 1,
                VaccinationName = "Varicella Vaccine",
                VaccinationDescription = "Chickenpox vaccine",
                VaccinationDate = DateTime.UtcNow,
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.AddVaccination(newVaccination, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(
                x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, result.VaccinationId, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser),
                Times.Once);
        }

        #endregion

        #region UpdateVaccination Tests

        [Fact]
        public async Task UpdateVaccination_WhenUserHasAccess_UpdatesVaccination()
        {
            // Arrange
            Vaccination updateValues = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                VaccinationName = "MMR Vaccine - Updated",
                VaccinationDescription = "Updated description",
                VaccinationDate = DateTime.Parse("2024-01-15"),
                Author = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.UpdateVaccination(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VaccinationId);
            Assert.Equal("MMR Vaccine - Updated", result.VaccinationName);
            Assert.Equal("Updated description", result.VaccinationDescription);

            // Verify database was updated
            Vaccination? dbVaccination = await _progenyDbContext.VaccinationsDb.FindAsync(1);
            Assert.NotNull(dbVaccination);
            Assert.Equal("MMR Vaccine - Updated", dbVaccination.VaccinationName);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(
                x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination),
                Times.Once);
        }

        [Fact]
        public async Task UpdateVaccination_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Vaccination updateValues = new()
            {
                VaccinationId = 1,
                VaccinationName = "Should not update"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            Vaccination result = await _service.UpdateVaccination(updateValues, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateVaccination_WhenVaccinationDoesNotExist_ReturnsNull()
        {
            // Arrange
            Vaccination updateValues = new()
            {
                VaccinationId = 999,
                VaccinationName = "Non-existent"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Vaccination result = await _service.UpdateVaccination(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateVaccination_UpdatesCacheAfterUpdate()
        {
            // Arrange
            Vaccination updateValues = new()
            {
                VaccinationId = 2,
                ProgenyId = 1,
                VaccinationName = "Hepatitis B - Updated",
                VaccinationDescription = "Cache should update",
                VaccinationDate = DateTime.Parse("2024-02-20"),
                Author = "user1",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 2, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, 2, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.UpdateVaccination(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cache should update", result.VaccinationDescription);
        }

        [Fact]
        public async Task UpdateVaccination_CallsUpdateItemPermissions()
        {
            // Arrange
            Vaccination updateValues = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                VaccinationName = "Test",
                ItemPermissionsDtoList = []
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.UpdateVaccination(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(
                x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser),
                Times.Once);
        }

        #endregion

        #region DeleteVaccination Tests

        [Fact]
        public async Task DeleteVaccination_WhenUserHasAccess_RemovesVaccination()
        {
            // Arrange
            Vaccination vaccinationToDelete = new()
            {
                VaccinationId = 1,
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, _adminUser))
                .ReturnsAsync(new List<TimelineItemPermission>());
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            int countBefore = await _progenyDbContext.VaccinationsDb.CountAsync();

            // Act
            Vaccination result = await _service.DeleteVaccination(vaccinationToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VaccinationId);

            int countAfter = await _progenyDbContext.VaccinationsDb.CountAsync();
            Assert.Equal(countBefore - 1, countAfter);

            Vaccination? deletedVaccination = await _progenyDbContext.VaccinationsDb.FindAsync(1);
            Assert.Null(deletedVaccination);

            // Verify cache was updated
            _mockKinaUnaCacheService.Verify(
                x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination),
                Times.Once);
        }

        [Fact]
        public async Task DeleteVaccination_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Vaccination vaccinationToDelete = new()
            {
                VaccinationId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            Vaccination result = await _service.DeleteVaccination(vaccinationToDelete, _otherUser);

            // Assert
            Assert.Null(result);

            // Verify vaccination still exists
            Vaccination? vaccination = await _progenyDbContext.VaccinationsDb.FindAsync(1);
            Assert.NotNull(vaccination);
        }

        [Fact]
        public async Task DeleteVaccination_WhenVaccinationDoesNotExist_ReturnsNull()
        {
            // Arrange
            Vaccination vaccinationToDelete = new()
            {
                VaccinationId = 999
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            Vaccination result = await _service.DeleteVaccination(vaccinationToDelete, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteVaccination_RemovesFromCache()
        {
            // Arrange
            Vaccination vaccinationToDelete = new()
            {
                VaccinationId = 2,
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 2, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 2, _adminUser))
                .ReturnsAsync(new List<TimelineItemPermission>());
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .Returns(Task.CompletedTask);

            // Act
            Vaccination result = await _service.DeleteVaccination(vaccinationToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);

            // Verify it's removed from database
            Vaccination? deletedVaccination = await _progenyDbContext.VaccinationsDb.FindAsync(2);
            Assert.Null(deletedVaccination);
        }

        #endregion

        #region GetVaccinationsList Tests

        [Fact]
        public async Task GetVaccinationsList_ReturnsOnlyVaccinationsWithAccess()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_testUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_testUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, vaccination => Assert.Equal(progenyId, vaccination.ProgenyId));
            Assert.All(result, vaccination => Assert.NotNull(vaccination.ItemPermission));
        }

        [Fact]
        public async Task GetVaccinationsList_FiltersOutVaccinationsWithoutAccess()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_testUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_testUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].VaccinationId);
        }

        [Fact]
        public async Task GetVaccinationsList_WhenProgenyHasNoVaccinations_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 999;

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_testUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_testUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetVaccinationsList_UsesCache_OnSecondCall()
        {
            // Arrange
            int progenyId = 1;
            
            Vaccination[] cachedVaccinations =
            [
                new() { VaccinationId = 1, ProgenyId = 1, VaccinationName = "Cached Vaccine" }
            ];
            VaccinationsListCacheEntry cacheEntry = new()
            {
                VaccinationsList = cachedVaccinations,
                UpdateTime = DateTime.UtcNow
            };
            TimelineUpdatedCacheEntry timelineEntry = new()
            {
                UpdateTime = DateTime.UtcNow.AddMinutes(-5)
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_testUser.UserId, progenyId))
                .ReturnsAsync(cacheEntry);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync(timelineEntry);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Cached Vaccine", result[0].VaccinationName);
        }

        [Fact]
        public async Task GetVaccinationsList_WhenUserHasNoAccessToAnyVaccination_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 1;

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_otherUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_otherUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetVaccinationsList_OnlyReturnsForRequestedProgeny()
        {
            // Arrange
            const int progenyId = 2;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_otherUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_otherUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 3, _otherUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 3, 2, 0, _otherUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _otherUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result[0].VaccinationId);
            Assert.Equal(progenyId, result[0].ProgenyId);
        }

        [Fact]
        public async Task GetVaccinationsList_SetsItemPermissionForEachVaccination()
        {
            // Arrange
            int progenyId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetVaccinationsListCache(_testUser.UserId, progenyId))
                .ReturnsAsync((VaccinationsListCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vaccination))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);
            _mockKinaUnaCacheService
                .Setup(x => x.SetVaccinationsListCache(_testUser.UserId, progenyId, It.IsAny<Vaccination[]>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<Vaccination> result = await _service.GetVaccinationsList(progenyId, _testUser);

            // Assert
            Assert.NotEmpty(result);
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            Assert.All(result, vaccination =>
            {
                Assert.NotNull(vaccination.ItemPermission);
                Assert.Equal(PermissionLevel.View, vaccination.ItemPermission.PermissionLevel);
            });
        }

        #endregion
    }
}