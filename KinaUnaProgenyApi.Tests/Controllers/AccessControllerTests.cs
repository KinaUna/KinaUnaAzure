using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
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
    public class AccessControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IUserGroupsService> _mockUserGroupsService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IFamilyMembersService> _mockFamilyMembersService;
        private readonly AccessController _controller;

        private readonly UserInfo _testUser;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string OldEmail = "old@test.com";
        private const string NewEmail = "new@test.com";
        private const int TestProgenyId = 1;

        public AccessControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = OldEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User",
                ProfilePicture = "profile.jpg"
            };

            Progeny testProgeny = new()
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2),
                Admins = OldEmail
            };

            // Seed database
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.ProgenyDb.Add(testProgeny);
            _progenyDbContext.SaveChanges();

            // Setup mocks
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockUserGroupsService = new Mock<IUserGroupsService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockFamilyMembersService = new Mock<IFamilyMembersService>();

            _controller = new AccessController(
                _mockProgenyService.Object,
                _mockUserInfoService.Object,
                _mockAccessManagementService.Object,
                _mockUserGroupsService.Object,
                _mockFamiliesService.Object,
                _mockFamilyMembersService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, TestUserEmail),
                new(ClaimTypes.NameIdentifier, TestUserId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region UpdateUsersEmail Tests

        [Fact]
        public async Task UpdateUsersEmail_Should_Return_NotFound_When_User_Not_Found()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = "non-existent-user-id",
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User with given id and email could not be found.", notFoundResult.Value);

            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(It.IsAny<UserInfo>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Return_NotFound_When_Email_Does_Not_Match()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = "wrong@test.com",
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User with given id and email could not be found.", notFoundResult.Value);

            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(It.IsAny<UserInfo>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Update_Permissions_When_Valid()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.ChangeUsersEmailForPermissions(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(_testUser, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Update_UserGroups_When_Valid()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockUserGroupsService.Setup(x => x.ChangeUsersEmailForGroupMembers(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockUserGroupsService.Verify(
                x => x.ChangeUsersEmailForGroupMembers(_testUser, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Update_FamilyMembers_When_Valid()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.ChangeUsersEmailForFamilyMembers(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockFamilyMembersService.Verify(
                x => x.ChangeUsersEmailForFamilyMembers(_testUser, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Update_Families_When_Valid()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.ChangeUsersEmailForFamilies(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockFamiliesService.Verify(
                x => x.ChangeUsersEmailForFamilies(_testUser, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Update_Progenies_When_Valid()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.ChangeUsersEmailForProgenies(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockProgenyService.Verify(
                x => x.ChangeUsersEmailForProgenies(_testUser, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Call_All_Services_In_Correct_Order()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            List<string> callOrder = [];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);

            _mockAccessManagementService.Setup(x => x.ChangeUsersEmailForPermissions(_testUser, NewEmail))
                .Callback(() => callOrder.Add("permissions"))
                .Returns(Task.CompletedTask);

            _mockUserGroupsService.Setup(x => x.ChangeUsersEmailForGroupMembers(_testUser, NewEmail))
                .Callback(() => callOrder.Add("userGroups"))
                .Returns(Task.CompletedTask);

            _mockFamilyMembersService.Setup(x => x.ChangeUsersEmailForFamilyMembers(_testUser, NewEmail))
                .Callback(() => callOrder.Add("familyMembers"))
                .Returns(Task.CompletedTask);

            _mockFamiliesService.Setup(x => x.ChangeUsersEmailForFamilies(_testUser, NewEmail))
                .Callback(() => callOrder.Add("families"))
                .Returns(Task.CompletedTask);

            _mockProgenyService.Setup(x => x.ChangeUsersEmailForProgenies(_testUser, NewEmail))
                .Callback(() => callOrder.Add("progenies"))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.Equal(5, callOrder.Count);
            Assert.Equal("permissions", callOrder[0]);
            Assert.Equal("userGroups", callOrder[1]);
            Assert.Equal("familyMembers", callOrder[2]);
            Assert.Equal("families", callOrder[3]);
            Assert.Equal("progenies", callOrder[4]);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Be_Case_Insensitive_For_Email_Comparison()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = "OLD@TEST.COM", // Different case
                NewEmail = NewEmail
            };

            UserInfo userWithLowerCaseEmail = new()
            {
                UserId = TestUserId,
                UserEmail = "old@test.com" // Lower case
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(userWithLowerCaseEmail);
            _mockAccessManagementService.Setup(x => x.ChangeUsersEmailForPermissions(userWithLowerCaseEmail, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(userWithLowerCaseEmail, NewEmail),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Handle_Null_NewEmail()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = null!
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(_testUser, null!),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Handle_Empty_NewEmail()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = string.Empty
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAccessManagementService.Verify(
                x => x.ChangeUsersEmailForPermissions(_testUser, string.Empty),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Handle_Whitespace_In_Emails()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = " old@test.com ",
                NewEmail = " new@test.com "
            };

            UserInfo userWithTrimmedEmail = new()
            {
                UserId = TestUserId,
                UserEmail = "old@test.com"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(userWithTrimmedEmail);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            // Should not match because of whitespace
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User with given id and email could not be found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUsersEmail_Should_Return_Ok_When_All_Services_Complete_Successfully()
        {
            // Arrange
            UpdateUserEmailModel model = new()
            {
                UserId = TestUserId,
                OldEmail = OldEmail,
                NewEmail = NewEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(model.UserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.ChangeUsersEmailForPermissions(_testUser, NewEmail))
                .Returns(Task.CompletedTask);
            _mockUserGroupsService.Setup(x => x.ChangeUsersEmailForGroupMembers(_testUser, NewEmail))
                .Returns(Task.CompletedTask);
            _mockFamilyMembersService.Setup(x => x.ChangeUsersEmailForFamilyMembers(_testUser, NewEmail))
                .Returns(Task.CompletedTask);
            _mockFamiliesService.Setup(x => x.ChangeUsersEmailForFamilies(_testUser, NewEmail))
                .Returns(Task.CompletedTask);
            _mockProgenyService.Setup(x => x.ChangeUsersEmailForProgenies(_testUser, NewEmail))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.UpdateUsersEmail(model);

            // Assert
            OkResult okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion
    }
}