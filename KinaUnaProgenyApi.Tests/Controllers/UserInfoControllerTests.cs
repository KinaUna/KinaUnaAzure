using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class UserInfoControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<INotificationsService> _mockNotificationsService;
        private readonly UserInfoController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;
        private readonly UserInfo _adminUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;
        private readonly ApplicationUser _testApplicationUser;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string TestUserName = "testuser";
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;

        public UserInfoControllerTests()
        {
            // Setup in-memory ApplicationDbContext
            DbContextOptions<ApplicationDbContext> appOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _applicationDbContext = new ApplicationDbContext(appOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                UserName = TestUserName,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User",
                PhoneNumber = "1234567890",
                ProfilePicture = "profile.jpg",
                Timezone = Constants.DefaultTimezone,
                Deleted = false,
                DeletedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow,
                CanUserAddItems = false,
                ProgenyList = [],
                FamilyList = [],
                AccessList = []
            };

            _otherUser = new UserInfo
            {
                Id = 2,
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                UserName = "otheruser",
                ViewChild = 0,
                IsKinaUnaAdmin = false,
                FirstName = "Other",
                MiddleName = "O",
                LastName = "User",
                PhoneNumber = "0987654321",
                ProfilePicture = "other-profile.jpg",
                Timezone = Constants.DefaultTimezone,
                Deleted = false,
                DeletedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow,
                CanUserAddItems = false,
                ProgenyList = [],
                FamilyList = [],
                AccessList = []
            };

            _adminUser = new UserInfo
            {
                Id = 3,
                UserId = AdminUserId,
                UserEmail = AdminUserEmail,
                UserName = "adminuser",
                ViewChild = 0,
                IsKinaUnaAdmin = true,
                FirstName = "Admin",
                MiddleName = "A",
                LastName = "User",
                PhoneNumber = "5555555555",
                ProfilePicture = "admin-profile.jpg",
                Timezone = Constants.DefaultTimezone,
                Deleted = false,
                DeletedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow,
                CanUserAddItems = false,
                ProgenyList = [],
                FamilyList = [],
                AccessList = []
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "Testy",
                BirthDay = DateTime.UtcNow.AddYears(-2),
                Admins = TestUserEmail
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family",
                Description = "Test family description",
                Admins = TestUserEmail,
                FamilyMembers = []
            };

            _testApplicationUser = new ApplicationUser
            {
                Id = TestUserId,
                UserName = TestUserName,
                Email = TestUserEmail,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User",
                TimeZone = Constants.DefaultTimezone
            };

            // Setup mocks
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockNotificationsService = new Mock<INotificationsService>();

            // Initialize controller
            _controller = new UserInfoController(
                _applicationDbContext,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                _mockUserInfoService.Object,
                _mockAccessManagementService.Object,
                _mockNotificationsService.Object
            );

            SetupControllerContext(TestUserEmail, TestUserId, TestUserName);
        }

        private void SetupControllerContext(string email, string userId, string userName)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, email),
                new("sub", userId),
                new("name", userName)
            ];

            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal principal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        public void Dispose()
        {
            _applicationDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region UserInfoByEmail Tests

        [Fact]
        public async Task UserInfoByEmail_WhenUserAccessesOwnEmail_ReturnsUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([TestProgenyId]);
            _mockProgenyService.Setup(s => s.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([TestFamilyId]);
            _mockFamiliesService.Setup(s => s.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.UserInfoByEmail(TestUserEmail);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(TestUserEmail, returnedUserInfo.UserEmail);
            Assert.True(returnedUserInfo.CanUserAddItems);
            Assert.Single(returnedUserInfo.ProgenyList);
            Assert.Single(returnedUserInfo.FamilyList);
        }

        [Fact]
        public async Task UserInfoByEmail_WhenUserDoesNotExist_CreatesNewUserInfo()
        {
            // Arrange
            string newUserEmail = TestUserEmail;
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(Constants.DefaultUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(newUserEmail))
                .ReturnsAsync((UserInfo)null!);
            _mockUserInfoService.Setup(s => s.AddUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync(new UserInfo()
                {
                    Id = 100,
                    UserId = TestUserId,
                    UserEmail = newUserEmail
                });
            _mockUserInfoService.Setup(s => s.SetUserInfoByEmail(newUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(It.IsAny<UserInfo>(), PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(It.IsAny<UserInfo>(), PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.UserInfoByEmail(newUserEmail);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<UserInfo>(okResult.Value);
            _mockUserInfoService.Verify(s => s.AddUserInfo(It.Is<UserInfo>(u => 
                u.UserEmail == newUserEmail && 
                u.UserId == TestUserId && 
                u.Timezone == Constants.DefaultTimezone
            )), Times.Once);
        }

        [Fact]
        public async Task UserInfoByEmail_WhenUserHasNoAccess_ReturnsUnknownUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(OtherUserEmail))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.UserInfoByEmail(OtherUserEmail);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("Unknown", returnedUserInfo.UserEmail);
            Assert.Equal("Unknown", returnedUserInfo.UserId);
            Assert.False(returnedUserInfo.CanUserAddItems);
        }

        [Fact]
        public async Task UserInfoByEmail_WhenUsersShareFamily_ReturnsUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(OtherUserEmail))
                .ReturnsAsync(_otherUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([TestFamilyId]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([TestFamilyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.UserInfoByEmail(OtherUserEmail);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(OtherUserEmail, returnedUserInfo.UserEmail);
        }

        [Fact]
        public async Task UserInfoByEmail_WhenUsersShareProgeny_ReturnsUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(OtherUserEmail))
                .ReturnsAsync(_otherUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([TestProgenyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([TestProgenyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.UserInfoByEmail(OtherUserEmail);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(OtherUserEmail, returnedUserInfo.UserEmail);
        }

        #endregion

        #region GetInfo Tests

        [Fact]
        public async Task GetInfo_WhenUserHasAccess_ReturnsUserInfo()
        {
            // Arrange
            int userInfoId = 2;
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoById(userInfoId))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(OtherUserEmail))
                .ReturnsAsync(_otherUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([TestFamilyId]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([TestFamilyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetInfo(userInfoId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(OtherUserEmail, returnedUserInfo.UserEmail);
        }

        [Fact]
        public async Task GetInfo_WhenUserHasNoAccess_ReturnsUnknownUserInfo()
        {
            // Arrange
            int userInfoId = 2;
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoById(userInfoId))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(OtherUserEmail))
                .ReturnsAsync(_otherUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_otherUser, PermissionLevel.View))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetInfo(userInfoId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("Unknown", returnedUserInfo.UserEmail);
            Assert.Equal("Unknown", returnedUserInfo.UserId);
        }

        #endregion

        #region ByUserIdPost Tests

        [Fact]
        public async Task ByUserIdPost_WhenUserExists_ReturnsUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([TestFamilyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([TestProgenyId]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([TestProgenyId]);
            _mockProgenyService.Setup(s => s.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.Add))
                .ReturnsAsync([TestFamilyId]);
            _mockFamiliesService.Setup(s => s.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.ByUserIdPost(TestUserId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(TestUserId, returnedUserInfo.UserId);
            Assert.True(returnedUserInfo.CanUserAddItems);
        }

        [Fact]
        public async Task ByUserIdPost_WhenUserDoesNotExist_ReturnsUnknownUserInfo()
        {
            // Arrange
            string unknownUserId = "unknown-user-id";
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(unknownUserId))
                .ReturnsAsync((UserInfo)null!);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.ByUserIdPost(unknownUserId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("Unknown", returnedUserInfo.UserEmail);
            Assert.Equal("Unknown", returnedUserInfo.UserId);
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WhenUserIsAdmin_ReturnsAllUserInfos()
        {
            // Arrange
            SetupControllerContext(AdminUserEmail, AdminUserId, "adminuser");
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);
            List<UserInfo> allUsers = [_testUser, _otherUser, _adminUser];
            _mockUserInfoService.Setup(s => s.GetAllUserInfos())
                .ReturnsAsync(allUsers);

            // Act
            IActionResult result = await _controller.GetAll();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<UserInfo> returnedUsers = Assert.IsType<List<UserInfo>>(okResult.Value);
            Assert.Equal(3, returnedUsers.Count);
        }

        [Fact]
        public async Task GetAll_WhenUserIsNotAdmin_ReturnsEmptyList()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.GetAll();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<UserInfo> returnedUsers = Assert.IsType<List<UserInfo>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }

        #endregion

        #region CheckCurrentUser Tests

        [Fact]
        public async Task CheckCurrentUser_WhenUserIsValid_ReturnsUserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.CheckCurrentUser();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(TestUserEmail, returnedUserInfo.UserEmail);
        }

        [Fact]
        public async Task CheckCurrentUser_WhenUserIsDeleted_ReturnsUnauthorized()
        {
            // Arrange
            UserInfo deletedUser = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                Deleted = true
            };
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(deletedUser);

            // Act
            IActionResult result = await _controller.CheckCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CheckCurrentUser_WhenEmailMismatch_ReturnsUnauthorized()
        {
            // Arrange
            UserInfo userWithDifferentEmail = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = "different@example.com",
                Deleted = false
            };
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(userWithDifferentEmail);

            // Act
            IActionResult result = await _controller.CheckCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_WhenValidUserInfo_CreatesAndReturnsUserInfo()
        {
            // Arrange
            UserInfo newUserInfo = new()
            {
                UserEmail = TestUserEmail,
                UserId = "",
                ViewChild = 0,
                Timezone = "America/New_York",
                FirstName = "New",
                MiddleName = "N",
                LastName = "User",
                PhoneNumber = "9999999999",
                ProfilePicture = "new-profile.jpg",
                UserName = "newuser"
            };

            _mockUserInfoService.Setup(s => s.AddUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync(new UserInfo() {
                    Id = 100, UserEmail = TestUserEmail, UserId = TestUserId

                });
            _mockUserInfoService.Setup(s => s.SetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(new UserInfo()
                {
                    Id = 100,
                    UserEmail = TestUserEmail,
                    UserId = TestUserId

                });
            _mockAccessManagementService.Setup(s => s.ProgeniesUserCanAccess(It.IsAny<UserInfo>(), PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockAccessManagementService.Setup(s => s.FamiliesUserCanAccess(It.IsAny<UserInfo>(), PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Post(newUserInfo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(TestUserEmail, returnedUserInfo.UserEmail);
            Assert.False(returnedUserInfo.IsKinaUnaAdmin);
            Assert.False(returnedUserInfo.Deleted);
            _mockUserInfoService.Verify(s => s.AddUserInfo(It.Is<UserInfo>(u => 
                u.UserEmail == TestUserEmail && 
                u.IsKinaUnaAdmin == false && 
                u.Deleted == false
            )), Times.Once);
        }

        [Fact]
        public async Task Post_WhenEmailMismatch_ReturnsUnauthorized()
        {
            // Arrange
            UserInfo newUserInfo = new()
            {
                UserEmail = OtherUserEmail,
                UserId = OtherUserId,
                ViewChild = 0,
                Timezone = Constants.DefaultTimezone
            };

            // Act
            IActionResult result = await _controller.Post(newUserInfo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_WhenUserUpdatesOwnInfo_UpdatesAndReturnsUserInfo()
        {
            // Arrange
            await _applicationDbContext.Users.AddAsync(_testApplicationUser);
            await _applicationDbContext.SaveChangesAsync();

            UserInfo updatedUserInfo = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                FirstName = "Updated",
                MiddleName = "U",
                LastName = "Name",
                UserName = "updateduser",
                PhoneNumber = "1111111111",
                ViewChild = 2,
                Timezone = "Europe/London",
                Deleted = false,
                UpdateIsAdmin = false
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(s => s.UpdateUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync((UserInfo ui) => ui);

            // Act
            IActionResult result = await _controller.Put(TestUserId, updatedUserInfo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("Updated", returnedUserInfo.FirstName);
            Assert.Equal("updateduser", returnedUserInfo.UserName);
            _mockUserInfoService.Verify(s => s.UpdateUserInfo(It.IsAny<UserInfo>()), Times.Once);
        }

        [Fact]
        public async Task Put_WhenAdminUpdatesUser_UpdatesAndReturnsUserInfo()
        {
            // Arrange
            SetupControllerContext(AdminUserEmail, AdminUserId, "adminuser");
            await _applicationDbContext.Users.AddAsync(new ApplicationUser { Id = OtherUserId, Email = OtherUserEmail, UserName = "otheruser" });
            await _applicationDbContext.SaveChangesAsync();

            UserInfo updatedUserInfo = new()
            {
                Id = 2,
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                FirstName = "AdminUpdated",
                LastName = "User",
                UserName = "adminupdated",
                Deleted = false,
                UpdateIsAdmin = false
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_adminUser);
            _mockUserInfoService.Setup(s => s.UpdateUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync((UserInfo ui) => ui);

            // Act
            IActionResult result = await _controller.Put(OtherUserId, updatedUserInfo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("AdminUpdated", returnedUserInfo.FirstName);
        }

        [Fact]
        public async Task Put_WhenUserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync((UserInfo)null!);

            UserInfo updatedUserInfo = new()
            {
                UserId = "nonexistent-id"
            };

            // Act
            IActionResult result = await _controller.Put("nonexistent-id", updatedUserInfo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_WhenUnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            UserInfo updatedUserInfo = new()
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Put(OtherUserId, updatedUserInfo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_WhenAdminFlagUpdate_OnlyAdminCanUpdate()
        {
            // Arrange
            SetupControllerContext(AdminUserEmail, AdminUserId, "adminuser");
            await _applicationDbContext.Users.AddAsync(new ApplicationUser { Id = OtherUserId, Email = OtherUserEmail, UserName = "otheruser" });
            await _applicationDbContext.SaveChangesAsync();

            UserInfo updatedUserInfo = new()
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                IsKinaUnaAdmin = true,
                UpdateIsAdmin = true
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_adminUser);
            _mockUserInfoService.Setup(s => s.UpdateUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync((UserInfo ui) => ui);

            // Act
            IActionResult result = await _controller.Put(OtherUserId, updatedUserInfo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.True(returnedUserInfo.IsKinaUnaAdmin);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WhenUserDeletesOwnAccountAfter30Days_DeletesSuccessfully()
        {
            // Arrange
            UserInfo deletedUser = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                Deleted = true,
                DeletedTime = DateTime.UtcNow.AddDays(-31)
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoById(1))
                .ReturnsAsync(deletedUser);
            _mockNotificationsService.Setup(s => s.GetUsersMobileNotifications(TestUserId, ""))
                .ReturnsAsync([]);
            _mockUserInfoService.Setup(s => s.DeleteUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync(deletedUser);

            // Act
            IActionResult result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockUserInfoService.Verify(s => s.DeleteUserInfo(deletedUser), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenUserNotDeleted_ReturnsNotFound()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoById(1))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_WhenDeletedLessThan30DaysAgo_ReturnsNotFound()
        {
            // Arrange
            UserInfo recentlyDeletedUser = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                Deleted = true,
                DeletedTime = DateTime.UtcNow.AddDays(-15)
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoById(1))
                .ReturnsAsync(recentlyDeletedUser);

            // Act
            IActionResult result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_WhenUnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            UserInfo deletedOtherUser = new()
            {
                Id = 2,
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                Deleted = true,
                DeletedTime = DateTime.UtcNow.AddDays(-31)
            };

            _mockUserInfoService.Setup(s => s.GetUserInfoById(2))
                .ReturnsAsync(deletedOtherUser);

            // Act
            IActionResult result = await _controller.Delete(2);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_WhenUserHasNotifications_DeletesNotifications()
        {
            // Arrange
            UserInfo deletedUser = new()
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                Deleted = true,
                DeletedTime = DateTime.UtcNow.AddDays(-31)
            };

            List<MobileNotification> notifications =
            [
                new() { NotificationId = 1, UserId = TestUserId },
                new() { NotificationId = 2, UserId = TestUserId }
            ];

            _mockUserInfoService.Setup(s => s.GetUserInfoById(1))
                .ReturnsAsync(deletedUser);
            _mockNotificationsService.Setup(s => s.GetUsersMobileNotifications(TestUserId, ""))
                .ReturnsAsync(notifications);
            _mockNotificationsService.Setup(s => s.DeleteMobileNotification(It.IsAny<MobileNotification>()))
                .ReturnsAsync((MobileNotification n) => n);
            _mockUserInfoService.Setup(s => s.DeleteUserInfo(It.IsAny<UserInfo>()))
                .ReturnsAsync(deletedUser);

            // Act
            IActionResult result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockNotificationsService.Verify(s => s.DeleteMobileNotification(It.IsAny<MobileNotification>()), Times.Exactly(2));
        }

        #endregion

        #region GetDeletedUserInfos Tests

        [Fact]
        public async Task GetDeletedUserInfos_WhenUserIsAdmin_ReturnsDeletedUsers()
        {
            // Arrange
            SetupControllerContext(AdminUserEmail, AdminUserId, "adminuser");
            UserInfo deletedUser1 = new() { Id = 10, UserEmail = "deleted1@example.com", Deleted = true };
            UserInfo deletedUser2 = new() { Id = 11, UserEmail = "deleted2@example.com", Deleted = true };
            List<UserInfo> deletedUsers = [deletedUser1, deletedUser2];

            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);
            _mockUserInfoService.Setup(s => s.GetDeletedUserInfos())
                .ReturnsAsync(deletedUsers);

            // Act
            IActionResult result = await _controller.GetDeletedUserInfos();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<UserInfo> returnedUsers = Assert.IsType<List<UserInfo>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
        }

        [Fact]
        public async Task GetDeletedUserInfos_WhenUserIsNotAdmin_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserInfoService.Setup(s => s.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.GetDeletedUserInfos();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region AddUserInfoToDeletedUserInfos Tests

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_WhenValidUserInfo_AddsAndReturnsUserInfo()
        {
            // Arrange
            UserInfo userToDelete = new()
            {
                Id = 5,
                UserId = "user-to-delete",
                UserEmail = "todelete@example.com",
                Deleted = true
            };

            _mockUserInfoService.Setup(s => s.AddUserInfoToDeletedUserInfos(userToDelete))
                .ReturnsAsync(userToDelete);

            // Act
            IActionResult result = await _controller.AddUserInfoToDeletedUserInfos(userToDelete);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(userToDelete.UserEmail, returnedUserInfo.UserEmail);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_WhenNullUserInfo_ReturnsBadRequest()
        {
            // Act
            IActionResult result = await _controller.AddUserInfoToDeletedUserInfos(null);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid UserInfo object.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_WhenUserInfoIdIsZero_ReturnsBadRequest()
        {
            // Arrange
            UserInfo invalidUserInfo = new() { Id = 0 };

            // Act
            IActionResult result = await _controller.AddUserInfoToDeletedUserInfos(invalidUserInfo);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid UserInfo object.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_WhenServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            UserInfo userInfo = new() { Id = 5, UserEmail = "test@example.com" };
            _mockUserInfoService.Setup(s => s.AddUserInfoToDeletedUserInfos(userInfo))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.AddUserInfoToDeletedUserInfos(userInfo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region UpdateDeletedUserInfo Tests

        [Fact]
        public async Task UpdateDeletedUserInfo_WhenValidUserInfo_UpdatesAndReturnsUserInfo()
        {
            // Arrange
            UserInfo updatedDeletedUser = new()
            {
                Id = 5,
                UserId = "deleted-user",
                UserEmail = "deleted@example.com",
                Deleted = true,
                FirstName = "Updated"
            };

            _mockUserInfoService.Setup(s => s.UpdateDeletedUserInfo(updatedDeletedUser))
                .ReturnsAsync(updatedDeletedUser);

            // Act
            IActionResult result = await _controller.UpdateDeletedUserInfo(updatedDeletedUser);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal("Updated", returnedUserInfo.FirstName);
        }

        [Fact]
        public async Task UpdateDeletedUserInfo_WhenNullUserInfo_ReturnsBadRequest()
        {
            // Act
            IActionResult result = await _controller.UpdateDeletedUserInfo(null);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid UserInfo object.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateDeletedUserInfo_WhenUserInfoIdIsZero_ReturnsBadRequest()
        {
            // Arrange
            UserInfo invalidUserInfo = new() { Id = 0 };

            // Act
            IActionResult result = await _controller.UpdateDeletedUserInfo(invalidUserInfo);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid UserInfo object.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateDeletedUserInfo_WhenServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            UserInfo userInfo = new() { Id = 5, UserEmail = "test@example.com" };
            _mockUserInfoService.Setup(s => s.UpdateDeletedUserInfo(userInfo))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.UpdateDeletedUserInfo(userInfo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region RemoveUserInfoFromDeletedUserInfos Tests

        [Fact]
        public async Task RemoveUserInfoFromDeletedUserInfos_WhenValidUserInfo_RemovesAndReturnsUserInfo()
        {
            // Arrange
            UserInfo userToRemove = new()
            {
                Id = 5,
                UserId = "removed-user",
                UserEmail = "removed@example.com"
            };

            _mockUserInfoService.Setup(s => s.RemoveUserInfoFromDeletedUserInfos(userToRemove))
                .ReturnsAsync(userToRemove);

            // Act
            IActionResult result = await _controller.RemoveUserInfoFromDeletedUserInfos(userToRemove);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo returnedUserInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(userToRemove.UserEmail, returnedUserInfo.UserEmail);
        }

        [Fact]
        public async Task RemoveUserInfoFromDeletedUserInfos_WhenServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            UserInfo userInfo = new() { Id = 5, UserEmail = "test@example.com" };
            _mockUserInfoService.Setup(s => s.RemoveUserInfoFromDeletedUserInfos(userInfo))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.RemoveUserInfoFromDeletedUserInfos(userInfo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion
    }
}