using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.FamilyServices
{
    public class FamilyMembersServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IFamilyAuditLogsService> _mockFamilyAuditLogService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly FamilyMembersService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public FamilyMembersServiceTests()
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
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockFamilyAuditLogService = new Mock<IFamilyAuditLogsService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize service
            _service = new FamilyMembersService(
                _progenyDbContext,
                _mockAccessManagementService.Object,
                _mockFamilyAuditLogService.Object,
                _mockProgenyService.Object);

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

            // Add test Progeny records
            Progeny testProgeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                BirthDay = DateTime.UtcNow.AddYears(-3),
                Admins = "admin@example.com"
            };
            _progenyDbContext.ProgenyDb.Add(testProgeny);

            // Add test FamilyMember records
            FamilyMember member1 = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                UserId = "admin1",
                Email = "admin@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0,
                PermissionLevel = PermissionLevel.Admin,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1"
            };
            _progenyDbContext.FamilyMembersDb.Add(member1);

            FamilyMember member2 = new()
            {
                FamilyMemberId = 2,
                FamilyId = 1,
                UserId = "user1",
                Email = "user1@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 1,
                PermissionLevel = PermissionLevel.Edit,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1"
            };
            _progenyDbContext.FamilyMembersDb.Add(member2);

            FamilyMember member3 = new()
            {
                FamilyMemberId = 3,
                FamilyId = 2,
                UserId = "user1",
                Email = "user1@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0,
                PermissionLevel = PermissionLevel.Admin,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "user1",
                ModifiedBy = "user1"
            };
            _progenyDbContext.FamilyMembersDb.Add(member3);

            // Add test FamilyPermission records
            FamilyPermission permission1 = new()
            {
                FamilyPermissionId = 1,
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Admin,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1"
            };
            _progenyDbContext.FamilyPermissionsDb.Add(permission1);

            FamilyPermission permission2 = new()
            {
                FamilyPermissionId = 2,
                FamilyId = 1,
                PermissionLevel = PermissionLevel.Edit,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1"
            };
            _progenyDbContext.FamilyPermissionsDb.Add(permission2);

            _progenyDbContext.SaveChanges();
        }

        #region GetFamilyMember Tests

        [Fact]
        public async Task GetFamilyMember_WhenUserHasAccess_ReturnsFamilyMember()
        {
            // Arrange
            int familyMemberId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("admin1"))
                .ReturnsAsync(_adminUser);

            // Act
            FamilyMember result = await _service.GetFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(familyMemberId, result.FamilyMemberId);
            Assert.Equal("admin@example.com", result.Email);
            Assert.NotNull(result.UserInfo);
        }

        [Fact]
        public async Task GetFamilyMember_WhenFamilyMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            int familyMemberId = 999;

            // Act
            FamilyMember result = await _service.GetFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFamilyMember_WhenUserHasNoAccessToFamily_ReturnsNull()
        {
            // Arrange
            int familyMemberId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            FamilyMember result = await _service.GetFamilyMember(familyMemberId, _otherUser);

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetFamilyMember_WhenHasProgenyAndUserHasAccess_ReturnsWithProgeny()
        {
            // Arrange
            int familyMemberId = 2;
            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _testUser))
                .ReturnsAsync(progeny);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("user1"))
                .ReturnsAsync(_testUser);

            // Act
            FamilyMember result = await _service.GetFamilyMember(familyMemberId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Progeny);
            Assert.Equal(1, result.Progeny.Id);
        }

        [Fact]
        public async Task GetFamilyMember_WhenProgenyDoesNotExist_ReturnsNull()
        {
            // Arrange
            int familyMemberId = 2;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            FamilyMember result = await _service.GetFamilyMember(familyMemberId, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFamilyMember_WhenNoUserId_ReturnsWithoutUserInfo()
        {
            // Arrange
            FamilyMember memberWithoutUserId = new()
            {
                FamilyMemberId = 10,
                FamilyId = 1,
                UserId = "",
                Email = "nouserid@example.com",
                MemberType = FamilyMemberType.Pet,
                ProgenyId = 0
            };
            _progenyDbContext.FamilyMembersDb.Add(memberWithoutUserId);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            FamilyMember result = await _service.GetFamilyMember(10, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UserInfo);
            Assert.Equal(0, result.UserInfo.Id);
            Assert.Empty(result.UserInfo.UserId);
        }

        #endregion

        #region AddFamilyMember Tests

        [Fact]
        public async Task AddFamilyMember_WhenUserIsAdmin_AddsMember()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FamilyMemberId > 0);
            Assert.Equal("newmember@example.com", result.Email);
            Assert.Equal(_adminUser.UserId, result.CreatedBy);
            Assert.Equal(_adminUser.UserId, result.ModifiedBy);

            _mockFamilyAuditLogService.Verify(
                x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser),
                Times.Once);
        }

        [Fact]
        public async Task AddFamilyMember_WhenUserHasAddPermission_AddsMember()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _testUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FamilyMemberId > 0);
        }

        [Fact]
        public async Task AddFamilyMember_WhenFamilyDoesNotExist_ReturnsNull()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 999,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddFamilyMember_WhenUserHasNoPermission_ReturnsNull()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddFamilyMember_WhenEmailMatchesExistingUser_AssignsUserId()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "user1@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user1", result.UserId);
        }

        [Fact]
        public async Task AddFamilyMember_WhenEmailHasWhitespace_TrimsEmail()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "  newmember@example.com  ",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newmember@example.com", result.Email);
        }

        [Fact]
        public async Task AddFamilyMember_WhenEmailIsEmpty_DoesNotCreatePermission()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "",
                MemberType = FamilyMemberType.Pet,
                ProgenyId = 0
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(
                x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser),
                Times.Never);
        }

        [Fact]
        public async Task AddFamilyMember_WhenPermissionAlreadyExists_UpdatesPermission()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync((FamilyPermission)null!);

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddFamilyMember_WhenHasProgenyAndNoAccess_ReturnsNull()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 1
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddFamilyMember_WhenHasProgenyAndHasAccess_AddsMember()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = 1,
                Email = "newmember@example.com",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 1
            };

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync(progeny);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberAddedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GrantFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.AddFamilyMember(newMember, _adminUser);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region UpdateFamilyMember Tests

        [Fact]
        public async Task UpdateFamilyMember_WhenUserIsAdmin_UpdatesMember()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                Email = "updatedemail@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberUpdatedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionsList(1, _adminUser))
                .ReturnsAsync([
                    new()
                    {
                        FamilyPermissionId = 1,
                        FamilyId = 1,
                        PermissionLevel = PermissionLevel.Admin
                    }
                ]);

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("updatedemail@example.com", result.Email);
            Assert.Equal(PermissionLevel.Edit, result.PermissionLevel);
            Assert.Equal(_adminUser.UserId, result.ModifiedBy);

            _mockFamilyAuditLogService.Verify(
                x => x.AddFamilyMemberUpdatedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser),
                Times.Once);
            _mockFamilyAuditLogService.Verify(
                x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenUserHasEditPermission_UpdatesMember()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 2,
                FamilyId = 1,
                Email = "user1@example.com",
                UserId = "user1",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 1,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _testUser))
                .ReturnsAsync(progeny);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberUpdatedAuditLogEntry(It.IsAny<FamilyMember>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionsList(1, _testUser))
                .ReturnsAsync([]);

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testUser.UserId, result.ModifiedBy);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenFamilyDoesNotExist_ReturnsNull()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 1,
                FamilyId = 999,
                Email = "updatedemail@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent
            };

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenUserHasNoPermission_ReturnsNull()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                Email = "updatedemail@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 999,
                FamilyId = 1,
                Email = "updatedemail@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent
            };

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenEmailChanged_UpdatesUserIdIfUserExists()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                Email = "user1@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0,
                PermissionLevel = PermissionLevel.Admin
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberUpdatedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionsList(1, _adminUser))
                .ReturnsAsync([
                    new()
                    {
                        FamilyPermissionId = 1,
                        FamilyId = 1,
                        PermissionLevel = PermissionLevel.Admin
                    }
                ]);

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user1", result.UserId);
            Assert.Equal("user1@example.com", result.Email);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenPermissionLevelChanged_UpdatesPermission()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 1,
                FamilyId = 1,
                Email = "admin@example.com",
                UserId = "admin1",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 0,
                PermissionLevel = PermissionLevel.View
            };

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberUpdatedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog { FamilyAuditLogId = 1 });

            _mockFamilyAuditLogService
                .Setup(x => x.UpdateFamilyAuditLogEntry(It.IsAny<FamilyAuditLog>()))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.GetFamilyPermissionsList(1, _adminUser))
                .ReturnsAsync([
                    new()
                    {
                        FamilyPermissionId = 1,
                        FamilyId = 1,
                        PermissionLevel = PermissionLevel.Admin
                    }
                ]);

            _mockAccessManagementService
                .Setup(x => x.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(new FamilyPermission());

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PermissionLevel.View, result.PermissionLevel);
        }

        [Fact]
        public async Task UpdateFamilyMember_WhenProgenyDoesNotExist_ReturnsNull()
        {
            // Arrange
            FamilyMember updatedMember = new()
            {
                FamilyMemberId = 2,
                FamilyId = 1,
                Email = "user1@example.com",
                UserId = "user1",
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 1,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            FamilyMember result = await _service.UpdateFamilyMember(updatedMember, _testUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteFamilyMember Tests

        [Fact]
        public async Task DeleteFamilyMember_WhenUserIsAdmin_DeletesMember()
        {
            // Arrange
            int familyMemberId = 1;

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberDeletedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.True(result);
            FamilyMember deletedMember = (await _progenyDbContext.FamilyMembersDb.FindAsync([familyMemberId], TestContext.Current.CancellationToken))!;
            Assert.Null(deletedMember);

            _mockFamilyAuditLogService.Verify(
                x => x.AddFamilyMemberDeletedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser),
                Times.Once);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenUserHasAdminPermission_DeletesMember()
        {
            // Arrange
            int familyMemberId = 3;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(2, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberDeletedAuditLogEntry(It.IsAny<FamilyMember>(), _testUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _testUser))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _testUser);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenMemberDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int familyMemberId = 999;

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenFamilyDoesNotExist_ReturnsFalse()
        {
            // Arrange
            FamilyMember memberWithInvalidFamily = new()
            {
                FamilyMemberId = 20,
                FamilyId = 999,
                UserId = "user1",
                Email = "user1@example.com"
            };
            _progenyDbContext.FamilyMembersDb.Add(memberWithInvalidFamily);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            bool result = await _service.DeleteFamilyMember(20, _adminUser);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenUserHasNoPermission_ReturnsFalse()
        {
            // Arrange
            int familyMemberId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _otherUser);

            // Assert
            Assert.False(result);
            FamilyMember member = (await _progenyDbContext.FamilyMembersDb.FindAsync([familyMemberId], TestContext.Current.CancellationToken))!;
            Assert.NotNull(member);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenHasProgenyAndNoAccess_ReturnsFalse()
        {
            // Arrange
            int familyMemberId = 2;

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFamilyMember_WhenMemberHasMultiplePermissions_DeletesAllPermissions()
        {
            // Arrange
            int familyMemberId = 1;

            // Add extra permission for the user
            FamilyPermission extraPermission = new()
            {
                FamilyPermissionId = 10,
                FamilyId = 1,
                PermissionLevel = PermissionLevel.View
            };
            _progenyDbContext.FamilyPermissionsDb.Add(extraPermission);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockFamilyAuditLogService
                .Setup(x => x.AddFamilyMemberDeletedAuditLogEntry(It.IsAny<FamilyMember>(), _adminUser))
                .ReturnsAsync(new FamilyAuditLog());

            _mockAccessManagementService
                .Setup(x => x.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), _adminUser))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteFamilyMember(familyMemberId, _adminUser);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region GetFamilyMembersForFamily Tests

        [Fact]
        public async Task GetFamilyMembersForFamily_WhenUserHasAccess_ReturnsList()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("admin1"))
                .ReturnsAsync(_adminUser);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("user1"))
                .ReturnsAsync(_testUser);

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync(progeny);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, m => Assert.Equal(familyId, m.FamilyId));
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_WhenUserHasNoAccess_ReturnsEmptyList()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_WhenNoMembers_ReturnsEmptyList()
        {
            // Arrange
            int familyId = 2;

            // Remove existing member from family 2
            FamilyMember memberToRemove = (await _progenyDbContext.FamilyMembersDb.FindAsync([3], TestContext.Current.CancellationToken))!;
            _progenyDbContext.FamilyMembersDb.Remove(memberToRemove);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_WhenMemberHasNoProgeny_IncludesInList()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync(progeny);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, m => m.ProgenyId == 0);
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_WhenMemberHasProgenyWithNoAccess_ExcludesFromList()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("admin1"))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, m => Assert.Equal(0, m.ProgenyId));
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_LoadsUserInfoForMembers()
        {
            // Arrange
            int familyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("admin1"))
                .ReturnsAsync(_adminUser);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId("user1"))
                .ReturnsAsync(_testUser);

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                Admins = "other@example.com"
            };

            _mockProgenyService
                .Setup(x => x.GetProgeny(1, _adminUser))
                .ReturnsAsync(progeny);

            // Act
            List<FamilyMember> result = await _service.GetFamilyMembersForFamily(familyId, _adminUser);

            // Assert
            Assert.All(result, m =>
            {
                if (!string.IsNullOrWhiteSpace(m.UserId))
                {
                    Assert.NotNull(m.UserInfo);
                }
            });
        }

        #endregion

        #region ChangeUsersEmailForFamilyMembers Tests

        [Fact]
        public async Task ChangeUsersEmailForFamilyMembers_WhenUserHasMembers_UpdatesEmail()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            // Act
            await _service.ChangeUsersEmailForFamilyMembers(_adminUser, newEmail);

            // Assert
            FamilyMember updatedMember = (await _progenyDbContext.FamilyMembersDb.FindAsync([1], TestContext.Current.CancellationToken))!;
            Assert.NotNull(updatedMember);
            Assert.Equal(newEmail, updatedMember.Email);
        }

        [Fact]
        public async Task ChangeUsersEmailForFamilyMembers_WhenUserHasMultipleMembers_UpdatesAll()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            // Act
            await _service.ChangeUsersEmailForFamilyMembers(_testUser, newEmail);

            // Assert
            List<FamilyMember> members = await _progenyDbContext.FamilyMembersDb
                .Where(fm => fm.UserId == _testUser.UserId)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotEmpty(members);
            Assert.All(members, m => Assert.Equal(newEmail, m.Email));
        }

        [Fact]
        public async Task ChangeUsersEmailForFamilyMembers_WhenUserHasNoMembers_NoChanges()
        {
            // Arrange
            string newEmail = "newemail@example.com";
            UserInfo newUser = new() { UserId = "newuser", UserEmail = "newuser@example.com" };

            // Act
            await _service.ChangeUsersEmailForFamilyMembers(newUser, newEmail);

            // Assert - No exceptions should be thrown
            List<FamilyMember> allMembers = await _progenyDbContext.FamilyMembersDb.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.All(allMembers, m => Assert.NotEqual(newEmail, m.Email));
        }

        #endregion

        #region UpdateFamilyMembersForNewUser Tests

        [Fact]
        public async Task UpdateFamilyMembersForNewUser_WhenEmailMatches_AssignsUserId()
        {
            // Arrange
            FamilyMember memberWithEmail = new()
            {
                FamilyMemberId = 30,
                FamilyId = 1,
                UserId = "",
                Email = "newuser@example.com",
                MemberType = FamilyMemberType.Parent
            };
            _progenyDbContext.FamilyMembersDb.Add(memberWithEmail);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo newUser = new()
            {
                UserId = "newuser123",
                UserEmail = "newuser@example.com"
            };

            // Act
            await _service.UpdateFamilyMembersForNewUser(newUser);

            // Assert
            FamilyMember updatedMember = (await _progenyDbContext.FamilyMembersDb.FindAsync([30], TestContext.Current.CancellationToken))!;
            Assert.NotNull(updatedMember);
            Assert.Equal("newuser123", updatedMember.UserId);
        }

        [Fact]
        public async Task UpdateFamilyMembersForNewUser_WhenCaseInsensitiveMatch_AssignsUserId()
        {
            // Arrange
            FamilyMember memberWithEmail = new()
            {
                FamilyMemberId = 31,
                FamilyId = 1,
                UserId = "",
                Email = "NEWUSER@EXAMPLE.COM",
                MemberType = FamilyMemberType.Parent
            };
            _progenyDbContext.FamilyMembersDb.Add(memberWithEmail);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo newUser = new()
            {
                UserId = "newuser123",
                UserEmail = "newuser@example.com"
            };

            // Act
            await _service.UpdateFamilyMembersForNewUser(newUser);

            // Assert
            FamilyMember updatedMember = (await _progenyDbContext.FamilyMembersDb.FindAsync([31], TestContext.Current.CancellationToken))!;
            Assert.NotNull(updatedMember);
            Assert.Equal("newuser123", updatedMember.UserId);
        }

        [Fact]
        public async Task UpdateFamilyMembersForNewUser_WhenNoMatches_NoChanges()
        {
            // Arrange
            UserInfo newUser = new()
            {
                UserId = "newuser123",
                UserEmail = "nomatch@example.com"
            };

            // Act
            await _service.UpdateFamilyMembersForNewUser(newUser);

            // Assert - No exceptions should be thrown
            List<FamilyMember> allMembers = await _progenyDbContext.FamilyMembersDb.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.All(allMembers, m => Assert.NotEqual("newuser123", m.UserId));
        }

        [Fact]
        public async Task UpdateFamilyMembersForNewUser_WhenMultipleMatches_UpdatesAll()
        {
            // Arrange
            FamilyMember member1 = new()
            {
                FamilyMemberId = 32,
                FamilyId = 1,
                UserId = "",
                Email = "newuser@example.com",
                MemberType = FamilyMemberType.Parent
            };
            FamilyMember member2 = new()
            {
                FamilyMemberId = 33,
                FamilyId = 2,
                UserId = "",
                Email = "newuser@example.com",
                MemberType = FamilyMemberType.Parent
            };
            _progenyDbContext.FamilyMembersDb.AddRange(member1, member2);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo newUser = new()
            {
                UserId = "newuser123",
                UserEmail = "newuser@example.com"
            };

            // Act
            await _service.UpdateFamilyMembersForNewUser(newUser);

            // Assert
            List<FamilyMember> updatedMembers = await _progenyDbContext.FamilyMembersDb
                .Where(fm => fm.FamilyMemberId == 32 || fm.FamilyMemberId == 33)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.All(updatedMembers, m => Assert.Equal("newuser123", m.UserId));
        }

        #endregion
    }
}