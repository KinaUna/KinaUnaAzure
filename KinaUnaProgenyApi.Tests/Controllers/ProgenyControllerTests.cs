using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class ProgenyControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly ProgenyController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;
        private readonly Progeny _testProgeny;
        private readonly ProgenyInfo _testProgenyInfo;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const int TestProgenyId = 1;
        private const int OtherProgenyId = 2;

        public ProgenyControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                Id = 1,
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                LastName = "User"
            };

            _adminUser = new UserInfo
            {
                Id = 2,
                UserId = AdminUserId,
                UserEmail = AdminUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Admin",
                LastName = "User"
            };

            _otherUser = new UserInfo
            {
                Id = 3,
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                ViewChild = OtherProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Other",
                LastName = "User"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2),
                TimeZone = "UTC",
                PictureLink = Constants.ProfilePictureUrl,
                Admins = AdminUserEmail,
                Email = TestUserEmail,
                UserId = TestUserId,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow.AddMonths(-6),
                ModifiedBy = TestUserId,
                ModifiedTime = DateTime.UtcNow
            };
            
            _testProgenyInfo = new ProgenyInfo
            {
                ProgenyInfoId = 1,
                ProgenyId = TestProgenyId,
                Email = TestUserEmail,
                MobileNumber = "1234567890",
                Website = "https://test.com",
                Notes = "Test notes",
                ModifiedBy = TestUserId,
                ModifiedTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Setup controller with mocked user
            _controller = new ProgenyController(_mockProgenyService.Object, _mockUserInfoService.Object);
            SetupControllerUser(_controller, TestUserId, TestUserEmail);
        }

        private static void SetupControllerUser(ControllerBase controller, string userId, string userEmail)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Email, userEmail)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region GetProgeny Tests

        [Fact]
        public async Task GetProgeny_ValidId_ReturnsOkWithProgeny()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult? result = await _controller.GetProgeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Progeny returnedProgeny = Assert.IsType<Progeny>(okResult.Value);
            Assert.Equal(TestProgenyId, returnedProgeny.Id);
            Assert.Equal("Test Progeny", returnedProgeny.Name);
        }

        [Fact]
        public async Task GetProgeny_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.GetProgeny(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProgeny_CallsServicesWithCorrectParameters()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.GetProgeny(TestProgenyId);

            // Assert
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgeny(TestProgenyId, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_ValidProgeny_ReturnsOkWithCreatedProgeny()
        {
            // Arrange
            Progeny newProgeny = new()
            {
                Name = "New Progeny",
                NickName = "NewNick",
                BirthDay = DateTime.UtcNow.AddYears(-1),
                TimeZone = "UTC",
                PictureLink = "https://example.com/picture.jpg",
                Admins = AdminUserEmail
            };

            Progeny createdProgeny = new()
            {
                Id = 3,
                Name = newProgeny.Name,
                NickName = newProgeny.NickName,
                BirthDay = newProgeny.BirthDay,
                TimeZone = newProgeny.TimeZone,
                PictureLink = newProgeny.PictureLink,
                Admins = newProgeny.Admins,
                CreatedBy = TestUserId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.AddProgeny(It.IsAny<Progeny>(), _testUser))
                .ReturnsAsync(createdProgeny);

            // Act
            IActionResult? result = await _controller.Post(newProgeny);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Progeny returnedProgeny = Assert.IsType<Progeny>(okResult.Value);
            Assert.Equal(3, returnedProgeny.Id);
            Assert.Equal("New Progeny", returnedProgeny.Name);
            Assert.Equal(TestUserId, returnedProgeny.CreatedBy);
        }

        [Fact]
        public async Task Post_ProgenyWithoutPictureLink_SetsDefaultPictureLink()
        {
            // Arrange
            Progeny newProgeny = new()
            {
                Name = "New Progeny",
                NickName = "NewNick",
                BirthDay = DateTime.UtcNow.AddYears(-1),
                TimeZone = "UTC",
                PictureLink = null,
                Admins = AdminUserEmail
            };

            Progeny? capturedProgeny = null;

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.AddProgeny(It.IsAny<Progeny>(), _testUser))
                .Callback<Progeny, UserInfo>((p, _) => capturedProgeny = p)
                .ReturnsAsync((Progeny p, UserInfo _) => p);

            // Act
            await _controller.Post(newProgeny);

            // Assert
            Assert.NotNull(capturedProgeny);
            Assert.Equal(Constants.ProfilePictureUrl, capturedProgeny.PictureLink);
        }

        [Fact]
        public async Task Post_ProgenyWithRelativePictureLink_ResizesImage()
        {
            // Arrange
            Progeny newProgeny = new()
            {
                Name = "New Progeny",
                NickName = "NewNick",
                BirthDay = DateTime.UtcNow.AddYears(-1),
                TimeZone = "UTC",
                PictureLink = "local/path/image.jpg",
                Admins = AdminUserEmail
            };

            string resizedImagePath = "resized/path/image.jpg";

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.ResizeImage("local/path/image.jpg"))
                .ReturnsAsync(resizedImagePath);

            _mockProgenyService
                .Setup(x => x.AddProgeny(It.IsAny<Progeny>(), _testUser))
                .ReturnsAsync((Progeny p, UserInfo _) => p);

            // Act
            await _controller.Post(newProgeny);

            // Assert
            _mockProgenyService.Verify(x => x.ResizeImage("local/path/image.jpg"), Times.Once);
        }

        [Fact]
        public async Task Post_ProgenyWithHttpPictureLink_DoesNotResizeImage()
        {
            // Arrange
            Progeny newProgeny = new()
            {
                Name = "New Progeny",
                NickName = "NewNick",
                BirthDay = DateTime.UtcNow.AddYears(-1),
                TimeZone = "UTC",
                PictureLink = "https://example.com/image.jpg",
                Admins = AdminUserEmail
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.AddProgeny(It.IsAny<Progeny>(), _testUser))
                .ReturnsAsync((Progeny p, UserInfo _) => p);

            // Act
            await _controller.Post(newProgeny);

            // Assert
            _mockProgenyService.Verify(x => x.ResizeImage(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_ValidUpdateByAdmin_ReturnsOkWithUpdatedProgeny()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            Progeny updateProgeny = new()
            {
                Id = TestProgenyId,
                Name = "Updated Progeny",
                NickName = "UpdatedNick",
                BirthDay = DateTime.UtcNow.AddYears(-3),
                TimeZone = "PST",
                PictureLink = "https://example.com/updated.jpg"
            };

            Progeny updatedProgeny = new()
            {
                Id = TestProgenyId,
                Name = updateProgeny.Name,
                NickName = updateProgeny.NickName,
                BirthDay = updateProgeny.BirthDay,
                TimeZone = updateProgeny.TimeZone,
                PictureLink = updateProgeny.PictureLink,
                Admins = AdminUserEmail,
                ModifiedBy = AdminUserId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgeny(It.IsAny<Progeny>(), _adminUser))
                .ReturnsAsync(updatedProgeny);

            // Act
            IActionResult? result = await _controller.Put(TestProgenyId, updateProgeny);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Progeny returnedProgeny = Assert.IsType<Progeny>(okResult.Value);
            Assert.Equal("Updated Progeny", returnedProgeny.Name);
            Assert.Equal(AdminUserId, returnedProgeny.ModifiedBy);
        }

        [Fact]
        public async Task Put_ProgenyNotFound_ReturnsNotFound()
        {
            // Arrange
            Progeny updateProgeny = new() { Id = 999 };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.Put(999, updateProgeny);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_UserNotInAdminList_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, OtherUserId, OtherUserEmail);

            Progeny updateProgeny = new()
            {
                Id = TestProgenyId,
                Name = "Updated Progeny"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_otherUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _otherUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult? result = await _controller.Put(TestProgenyId, updateProgeny);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_ServiceReturnsNull_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            Progeny updateProgeny = new()
            {
                Id = TestProgenyId,
                Name = "Updated Progeny"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgeny(It.IsAny<Progeny>(), _adminUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.Put(TestProgenyId, updateProgeny);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_UpdatesAllProgenyProperties()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            Progeny updateProgeny = new()
            {
                Id = TestProgenyId,
                Name = "New Name",
                NickName = "New Nick",
                BirthDay = DateTime.UtcNow.AddYears(-5),
                TimeZone = "EST",
                PictureLink = "https://new.com/pic.jpg"
            };

            Progeny? capturedProgeny = null;

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgeny(It.IsAny<Progeny>(), _adminUser))
                .Callback<Progeny, UserInfo>((p, _) => capturedProgeny = p)
                .ReturnsAsync((Progeny p, UserInfo _) => p);

            // Act
            await _controller.Put(TestProgenyId, updateProgeny);

            // Assert
            Assert.NotNull(capturedProgeny);
            Assert.Equal("New Name", capturedProgeny.Name);
            Assert.Equal("New Nick", capturedProgeny.NickName);
            Assert.Equal(updateProgeny.BirthDay, capturedProgeny.BirthDay);
            Assert.Equal("EST", capturedProgeny.TimeZone);
            Assert.Equal("https://new.com/pic.jpg", capturedProgeny.PictureLink);
            Assert.Equal(AdminUserId, capturedProgeny.ModifiedBy);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_ValidDeleteByAdmin_ReturnsNoContent()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.DeleteProgeny(It.IsAny<Progeny>(), _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            _mockProgenyService
                .Setup(x => x.DeleteProgenyInfo(_testProgenyInfo, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            // Act
            IActionResult? result = await _controller.Delete(TestProgenyId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ProgenyNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_UserNotInAdminList_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, OtherUserId, OtherUserEmail);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_otherUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _otherUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult? result = await _controller.Delete(TestProgenyId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_ServiceReturnsNull_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.DeleteProgeny(It.IsAny<Progeny>(), _adminUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.Delete(TestProgenyId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_DeletesProgenyAndProgenyInfo()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.DeleteProgeny(It.IsAny<Progeny>(), _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            _mockProgenyService
                .Setup(x => x.DeleteProgenyInfo(_testProgenyInfo, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            // Act
            await _controller.Delete(TestProgenyId);

            // Assert
            _mockProgenyService.Verify(x => x.DeleteProgeny(It.IsAny<Progeny>(), _adminUser), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgenyInfo(TestProgenyId, _adminUser), Times.Once);
            _mockProgenyService.Verify(x => x.DeleteProgenyInfo(_testProgenyInfo, _adminUser), Times.Once);
        }

        [Fact]
        public async Task Delete_SetsModifiedByBeforeDeletion()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            Progeny? capturedProgeny = null;

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.DeleteProgeny(It.IsAny<Progeny>(), _adminUser))
                .Callback<Progeny, UserInfo>((p, _) => capturedProgeny = p)
                .ReturnsAsync((Progeny p, UserInfo _) => p);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            _mockProgenyService
                .Setup(x => x.DeleteProgenyInfo(_testProgenyInfo, _adminUser))
                .ReturnsAsync(_testProgenyInfo);

            // Act
            await _controller.Delete(TestProgenyId);

            // Assert
            Assert.NotNull(capturedProgeny);
            Assert.Equal(AdminUserId, capturedProgeny.ModifiedBy);
        }

        #endregion

        #region GetProgenyInfo Tests

        [Fact]
        public async Task GetProgenyInfo_ValidProgenyId_ReturnsOkWithProgenyInfo()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgenyInfo);

            // Act
            IActionResult? result = await _controller.GetProgenyInfo(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            ProgenyInfo returnedInfo = Assert.IsType<ProgenyInfo>(okResult.Value);
            Assert.Equal(TestProgenyId, returnedInfo.ProgenyId);
            Assert.Equal(TestUserEmail, returnedInfo.Email);
        }

        [Fact]
        public async Task GetProgenyInfo_InvalidProgenyId_ReturnsNotFound()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(999, _testUser))
                .ReturnsAsync((ProgenyInfo)null!);

            // Act
            IActionResult? result = await _controller.GetProgenyInfo(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProgenyInfo_CallsServicesWithCorrectParameters()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgenyInfo(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgenyInfo);

            // Act
            await _controller.GetProgenyInfo(TestProgenyId);

            // Assert
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgenyInfo(TestProgenyId, _testUser), Times.Once);
        }

        #endregion

        #region UpdateProgenyInfo Tests

        [Fact]
        public async Task UpdateProgenyInfo_ValidUpdateByAdmin_ReturnsOkWithUpdatedInfo()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyInfoId = 1,
                ProgenyId = TestProgenyId,
                Email = "updated@example.com",
                MobileNumber = "9876543210",
                Website = "https://updated.com",
                Notes = "Updated notes"
            };

            ProgenyInfo updatedProgenyInfo = new()
            {
                ProgenyInfoId = 1,
                ProgenyId = TestProgenyId,
                Email = updateProgenyInfo.Email,
                MobileNumber = updateProgenyInfo.MobileNumber,
                Website = updateProgenyInfo.Website,
                Notes = updateProgenyInfo.Notes,
                ModifiedBy = AdminUserId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgenyInfo(It.IsAny<ProgenyInfo>(), _adminUser))
                .ReturnsAsync(updatedProgenyInfo);

            // Act
            IActionResult? result = await _controller.UpdateProgenyInfo(TestProgenyId, updateProgenyInfo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            ProgenyInfo returnedInfo = Assert.IsType<ProgenyInfo>(okResult.Value);
            Assert.Equal("updated@example.com", returnedInfo.Email);
            Assert.Equal(AdminUserId, returnedInfo.ModifiedBy);
        }

        [Fact]
        public async Task UpdateProgenyInfo_ProgenyNotFound_ReturnsNotFound()
        {
            // Arrange
            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyId = 999
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.UpdateProgenyInfo(999, updateProgenyInfo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateProgenyInfo_UserNotInAdminList_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, OtherUserId, OtherUserEmail);

            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_otherUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _otherUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult? result = await _controller.UpdateProgenyInfo(TestProgenyId, updateProgenyInfo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateProgenyInfo_MismatchedProgenyId_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyId = OtherProgenyId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult? result = await _controller.UpdateProgenyInfo(TestProgenyId, updateProgenyInfo);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateProgenyInfo_ServiceReturnsNull_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgenyInfo(It.IsAny<ProgenyInfo>(), _adminUser))
                .ReturnsAsync((ProgenyInfo)null!);

            // Act
            IActionResult? result = await _controller.UpdateProgenyInfo(TestProgenyId, updateProgenyInfo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateProgenyInfo_SetsModifiedByProperty()
        {
            // Arrange
            SetupControllerUser(_controller, AdminUserId, AdminUserEmail);

            ProgenyInfo updateProgenyInfo = new()
            {
                ProgenyId = TestProgenyId,
                Email = "test@test.com"
            };

            ProgenyInfo? capturedProgenyInfo = null;

            _mockUserInfoService
                .Setup(x => x.GetUserInfoByUserId(It.IsAny<string>()))
                .ReturnsAsync(_adminUser);

            _mockProgenyService
                .Setup(x => x.GetProgeny(TestProgenyId, _adminUser))
                .ReturnsAsync(_testProgeny);

            _mockProgenyService
                .Setup(x => x.UpdateProgenyInfo(It.IsAny<ProgenyInfo>(), _adminUser))
                .Callback<ProgenyInfo, UserInfo>((pi, _) => capturedProgenyInfo = pi)
                .ReturnsAsync((ProgenyInfo pi, UserInfo _) => pi);

            // Act
            await _controller.UpdateProgenyInfo(TestProgenyId, updateProgenyInfo);

            // Assert
            Assert.NotNull(capturedProgenyInfo);
            Assert.Equal(AdminUserId, capturedProgenyInfo.ModifiedBy);
        }

        #endregion
    }
}