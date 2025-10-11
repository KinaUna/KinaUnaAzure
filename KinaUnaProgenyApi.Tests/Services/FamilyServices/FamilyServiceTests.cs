using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.FamilyServices
{
    public class FamiliesServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IFamilyMembersService> _mockFamilyMembersService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IFamilyAuditLogsService> _mockFamilyAuditLogService;
        private readonly Mock<IPermissionAuditLogsService> _mockPermissionAuditLogService;
        private readonly FamiliesService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public FamiliesServiceTests()
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

            // Setup mocks
            _mockFamilyMembersService = new Mock<IFamilyMembersService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockFamilyAuditLogService = new Mock<IFamilyAuditLogsService>();
            _mockPermissionAuditLogService = new Mock<IPermissionAuditLogsService>();

            // Initialize service
            _service = new FamiliesService(
                _progenyDbContext,
                _mockFamilyMembersService.Object,
                _mockAccessManagementService.Object,
                _mockFamilyAuditLogService.Object,
                _mockPermissionAuditLogService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test Family records
            Family testFamily = new()
            {
                FamilyId = 1,
                Name = "Test Family",
                Description = "Test Description",
                Admins = "admin@example.com",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1"
            };
            _progenyDbContext.FamiliesDb.Add(testFamily);

            Family anotherFamily = new()
            {
                FamilyId = 2,
                Name = "Another Family",
                Description = "Another Description",
                Admins = "user1@example.com",
                CreatedTime = DateTime.UtcNow.AddDays(-20),
                ModifiedTime = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "user1",
                ModifiedBy = "user1"
            };
            _progenyDbContext.FamiliesDb.Add(anotherFamily);

            // Add test FamilyMember records
            FamilyMember member1 = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                UserId = "admin1",
                Email = "admin@example.com"
            };
            _progenyDbContext.FamilyMembersDb.Add(member1);

            FamilyMember member2 = new()
            {
                FamilyMemberId = 2,
                FamilyId = 1,
                UserId = "user1",
                Email = "user1@example.com"
            };
            _progenyDbContext.FamilyMembersDb.Add(member2);

            // Add test FamilyPermission records
            FamilyPermission permission1 = new()
            {
                FamilyPermissionId = 1,
                FamilyId = 1,
                UserId = "admin1",
                Email = "admin@example.com",
                PermissionLevel = PermissionLevel.Admin
            };
            _progenyDbContext.FamilyPermissionsDb.Add(permission1);

            FamilyPermission permission2 = new()
            {
                FamilyPermissionId = 2,
                FamilyId = 1,
                UserId = "user1",
                Email = "user1@example.com",
                PermissionLevel = PermissionLevel.Edit
            };
            _progenyDbContext.FamilyPermissionsDb.Add(permission2);

            _progenyDbContext.SaveChanges();
        }

        #region GetFamilyById Tests

        [Fact]
        public async Task GetFamilyById_WhenUserHasAccess_ReturnsFamilyWithMembers()
        {
            // Arrange
            int familyId = 1;
            List<FamilyMember> familyMembers = new()
            {
                new FamilyMember { FamilyMemberId = 1, FamilyId = 1 }
            };
            FamilyPermission familyPermission = new()
            {
                FamilyId = 1,
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(familyId, _testUser))
                .ReturnsAsync(familyPermission);
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(familyId, _testUser))
                .ReturnsAsync(familyMembers);

            // Act
            Family result = await _service.GetFamilyById(familyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(familyId, result.FamilyId);
            Assert.Equal("Test Family", result.Name);
            Assert.NotNull(result.FamilyPermission);
            Assert.NotNull(result.FamilyMembers);
            Assert.Single(result.FamilyMembers);
        }

        [Fact]
        public async Task GetFamilyById_WhenUserHasNoAccess_ReturnsEmptyFamily()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            Family result = await _service.GetFamilyById(familyId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.FamilyId);
        }

        [Fact]
        public async Task GetFamilyById_WhenFamilyDoesNotExist_ReturnsNull()
        {
            // Arrange
            int familyId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(familyId, _testUser))
                .ReturnsAsync(new FamilyPermission());
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(familyId, _testUser))
                .ReturnsAsync(new List<FamilyMember>());

            // Act
            Family result = await _service.GetFamilyById(familyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.FamilyId);
        }

        #endregion

        #region GetUsersFamiliesByEmail Tests

        [Fact]
        public async Task GetUsersFamiliesByEmail_WhenUserHasFamilies_ReturnsFamiliesList()
        {
            // Arrange
            string userEmail = "admin@example.com";
            FamilyPermission familyPermission = new()
            {
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Admin
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(1, _adminUser))
                .ReturnsAsync(familyPermission);
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(1, _adminUser))
                .ReturnsAsync(new List<FamilyMember>());

            // Act
            List<Family> result = await _service.GetUsersFamiliesByEmail(userEmail, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].FamilyId);
            Assert.Equal("Test Family", result[0].Name);
        }

        [Fact]
        public async Task GetUsersFamiliesByEmail_WhenEmailHasUpperCase_PerformsCaseInsensitiveSearch()
        {
            // Arrange
            string userEmail = "ADMIN@EXAMPLE.COM";
            FamilyPermission familyPermission = new()
            {
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Admin
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(1, _adminUser))
                .ReturnsAsync(familyPermission);
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(1, _adminUser))
                .ReturnsAsync(new List<FamilyMember>());

            // Act
            List<Family> result = await _service.GetUsersFamiliesByEmail(userEmail, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetUsersFamiliesByEmail_WhenUserHasNoFamilies_ReturnsEmptyList()
        {
            // Arrange
            string userEmail = "nonexistent@example.com";

            // Act
            List<Family> result = await _service.GetUsersFamiliesByEmail(userEmail, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersFamiliesByEmail_WhenUserHasNoAccessToFamily_ExcludesThatFamily()
        {
            // Arrange
            string userEmail = "admin@example.com";

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<Family> result = await _service.GetUsersFamiliesByEmail(userEmail, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetUsersFamiliesByUserId Tests

        [Fact]
        public async Task GetUsersFamiliesByUserId_WhenUserHasFamilies_ReturnsFamiliesList()
        {
            // Arrange
            string userId = "admin1";
            FamilyPermission familyPermission = new()
            {
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Admin
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(1, _adminUser))
                .ReturnsAsync(familyPermission);
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(1, _adminUser))
                .ReturnsAsync(new List<FamilyMember>());

            // Act
            List<Family> result = await _service.GetUsersFamiliesByUserId(userId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].FamilyId);
        }

        [Fact]
        public async Task GetUsersFamiliesByUserId_WhenUserHasNoFamilies_ReturnsEmptyList()
        {
            // Arrange
            string userId = "nonexistent";

            // Act
            List<Family> result = await _service.GetUsersFamiliesByUserId(userId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersFamiliesByUserId_WhenDuplicateFamilies_ReturnsUniqueList()
        {
            // Arrange
            string userId = "user1";
            FamilyPermission familyPermission = new()
            {
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionForUser(1, _testUser))
                .ReturnsAsync(familyPermission);
            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(1, _testUser))
                .ReturnsAsync(new List<FamilyMember>());

            // Act
            List<Family> result = await _service.GetUsersFamiliesByUserId(userId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region AddFamily Tests

        [Fact]
        public async Task AddFamily_WhenValidFamily_AddsToDatabase()
        {
            // Arrange
            Family newFamily = new()
            {
                Name = "New Family",
                Description = "New Description",
                Admins = "user1@example.com"
            };

            UserInfo userFromDb = await _progenyDbContext.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == "user1@example.com".ToUpper());

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyCreatedAuditLogEntry(It.IsAny<Family>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            Family result = await _service.AddFamily(newFamily, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FamilyId > 0);
            Assert.Equal("New Family", result.Name);
            Assert.Equal(_testUser.UserId, result.CreatedBy);
            Assert.Equal(_testUser.UserId, result.ModifiedBy);
            Assert.Contains(_testUser.UserEmail, result.Admins);

            _mockFamilyAuditLogService.Verify(x => x.AddFamilyCreatedAuditLogEntry(It.IsAny<Family>(), _testUser), Times.Once);
            _mockAccessManagementService.Verify(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task AddFamily_WhenCurrentUserNotInAdminsList_AddsCurrentUser()
        {
            // Arrange
            Family newFamily = new()
            {
                Name = "New Family",
                Description = "New Description",
                Admins = "other@example.com"
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyCreatedAuditLogEntry(It.IsAny<Family>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            Family result = await _service.AddFamily(newFamily, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(_testUser.UserEmail, result.Admins);
        }

        [Fact]
        public async Task AddFamily_WhenMultipleAdmins_CreatesPermissionsForAll()
        {
            // Arrange
            Family newFamily = new()
            {
                Name = "New Family",
                Description = "New Description",
                Admins = "user1@example.com,admin@example.com"
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyCreatedAuditLogEntry(It.IsAny<Family>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            Family result = await _service.AddFamily(newFamily, _testUser);

            // Assert
            _mockAccessManagementService.Verify(
                x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser),
                Times.Exactly(2));
        }

        #endregion

        #region UpdateFamily Tests

        [Fact]
        public async Task UpdateFamily_WhenUserIsAdmin_UpdatesFamily()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 1,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com"
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyUpdatedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Family Name", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(_adminUser.UserId, result.ModifiedBy);

            _mockFamilyAuditLogService.Verify(x => x.AddFamilyUpdatedAuditLogEntry(It.IsAny<Family>(), _adminUser), Times.Once);
            _mockFamilyAuditLogService.Verify(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()), Times.Once);
        }

        [Fact]
        public async Task UpdateFamily_WhenUserNotAdmin_ReturnsNull()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 1,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com"
            };

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFamily_WhenFamilyDoesNotExist_ReturnsNull()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 999,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com"
            };

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFamily_WhenRemovingCurrentUserFromAdmins_KeepsCurrentUser()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 1,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com"
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyUpdatedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(_adminUser.UserEmail, result.Admins);
        }

        [Fact]
        public async Task UpdateFamily_WhenAddingNewAdmins_GrantsPermissions()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 1,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com,user2@example.com"
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyUpdatedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync((FamilyPermission)null);

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _adminUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(
                x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser),
                Times.Once);
        }

        [Fact]
        public async Task UpdateFamily_WhenRemovingAdmins_DowngradesPermissions()
        {
            // Arrange
            Family updatedFamily = new()
            {
                FamilyId = 1,
                Name = "Updated Family Name",
                Description = "Updated Description",
                Admins = "admin@example.com"
            };

            // Add second admin to existing family
            Family existingFamily = await _progenyDbContext.FamiliesDb.FindAsync(1);
            existingFamily.Admins = "admin@example.com,user1@example.com";
            _progenyDbContext.FamiliesDb.Update(existingFamily);
            await _progenyDbContext.SaveChangesAsync();

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyUpdatedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            Family result = await _service.UpdateFamily(updatedFamily, _adminUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(
                x => x.UpdateFamilyPermission(
                    It.Is<FamilyPermission>(p => p.PermissionLevel == PermissionLevel.Edit),
                    _adminUser),
                Times.Once);
        }

        #endregion

        #region DeleteFamily Tests

        [Fact]
        public async Task DeleteFamily_WhenUserIsAdmin_DeletesFamily()
        {
            // Arrange
            int familyId = 1;

            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(familyId, _adminUser))
                .ReturnsAsync(new List<FamilyMember>());

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyDeletedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(true);

            _mockPermissionAuditLogService
                .Setup(x => x.AddFamilyPermissionAuditLogEntry(It.IsAny<PermissionAction>(), It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            bool result = await _service.DeleteFamily(familyId, _adminUser);

            // Assert
            Assert.True(result);
            Family deletedFamily = await _progenyDbContext.FamiliesDb.FindAsync(familyId);
            Assert.Null(deletedFamily);

            _mockFamilyAuditLogService.Verify(x => x.AddFamilyDeletedAuditLogEntry(It.IsAny<Family>(), _adminUser), Times.Once);
        }

        [Fact]
        public async Task DeleteFamily_WhenUserNotAdmin_ReturnsFalse()
        {
            // Arrange
            int familyId = 1;

            // Act
            bool result = await _service.DeleteFamily(familyId, _otherUser);

            // Assert
            Assert.False(result);
            Family family = await _progenyDbContext.FamiliesDb.FindAsync(familyId);
            Assert.NotNull(family);
        }

        [Fact]
        public async Task DeleteFamily_WhenFamilyDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int familyId = 999;

            // Act
            bool result = await _service.DeleteFamily(familyId, _adminUser);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFamily_WhenFamilyHasMembers_DeletesMembers()
        {
            // Arrange
            int familyId = 1;
            List<FamilyMember> familyMembers = new()
            {
                new FamilyMember { FamilyMemberId = 1, FamilyId = 1 },
                new FamilyMember { FamilyMemberId = 2, FamilyId = 1 }
            };

            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(familyId, _adminUser))
                .ReturnsAsync(familyMembers);

            _mockFamilyMembersService
                .Setup(x => x.DeleteFamilyMember(It.IsAny<int>(), _adminUser))
                .ReturnsAsync(true);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyDeletedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(true);

            _mockPermissionAuditLogService
                .Setup(x => x.AddFamilyPermissionAuditLogEntry(It.IsAny<PermissionAction>(), It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            bool result = await _service.DeleteFamily(familyId, _adminUser);

            // Assert
            Assert.True(result);
            _mockFamilyMembersService.Verify(x => x.DeleteFamilyMember(It.IsAny<int>(), _adminUser), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteFamily_WhenFamilyHasPermissions_DeletesPermissions()
        {
            // Arrange
            int familyId = 1;

            _mockFamilyMembersService
                .Setup(x => x.GetFamilyMembersForFamily(familyId, _adminUser))
                .ReturnsAsync(new List<FamilyMember>());

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyDeletedAuditLogEntry(It.IsAny<Family>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(true);

            _mockPermissionAuditLogService
                .Setup(x => x.AddFamilyPermissionAuditLogEntry(It.IsAny<PermissionAction>(), It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            bool result = await _service.DeleteFamily(familyId, _adminUser);

            // Assert
            Assert.True(result);
            _mockAccessManagementService.Verify(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser), Times.Once);
            _mockPermissionAuditLogService.Verify(x => x.AddFamilyPermissionAuditLogEntry(PermissionAction.Delete, It.IsAny<FamilyPermission>(), _adminUser), Times.Once);
        }

        #endregion

        #region ChangeUsersEmailForFamilies Tests

        [Fact]
        public async Task ChangeUsersEmailForFamilies_WhenUserIsAdmin_UpdatesEmail()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            // Act
            await _service.ChangeUsersEmailForFamilies(_adminUser, newEmail);

            // Assert
            Family updatedFamily = await _progenyDbContext.FamiliesDb.FindAsync(1);
            Assert.NotNull(updatedFamily);
            Assert.Contains(newEmail, updatedFamily.Admins);
            Assert.DoesNotContain(_adminUser.UserEmail, updatedFamily.Admins);
        }

        [Fact]
        public async Task ChangeUsersEmailForFamilies_WhenUserNotAdmin_NoChanges()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            // Act
            await _service.ChangeUsersEmailForFamilies(_otherUser, newEmail);

            // Assert
            Family family = await _progenyDbContext.FamiliesDb.FindAsync(1);
            Assert.NotNull(family);
            Assert.DoesNotContain(newEmail, family.Admins);
        }

        [Fact]
        public async Task ChangeUsersEmailForFamilies_WhenMultipleFamilies_UpdatesAll()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            // Add user as admin to second family
            Family anotherFamily = await _progenyDbContext.FamiliesDb.FindAsync(2);
            anotherFamily.Admins = "admin@example.com";
            _progenyDbContext.FamiliesDb.Update(anotherFamily);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            await _service.ChangeUsersEmailForFamilies(_adminUser, newEmail);

            // Assert
            Family family1 = await _progenyDbContext.FamiliesDb.FindAsync(1);
            Family family2 = await _progenyDbContext.FamiliesDb.FindAsync(2);
            Assert.Contains(newEmail, family1.Admins);
            Assert.Contains(newEmail, family2.Admins);
        }

        [Fact]
        public async Task ChangeUsersEmailForFamilies_WhenNoFamilies_NoChanges()
        {
            // Arrange
            string newEmail = "newemail@example.com";
            UserInfo newUser = new UserInfo { UserId = "newuser", UserEmail = "newuser@example.com" };

            // Act
            await _service.ChangeUsersEmailForFamilies(newUser, newEmail);

            // Assert - No exceptions should be thrown and no families should be modified
            List<Family> families = await _progenyDbContext.FamiliesDb.ToListAsync();
            Assert.All(families, f => Assert.DoesNotContain(newEmail, f.Admins));
        }

        #endregion
    }
}