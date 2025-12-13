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
    public class VaccinationsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IVaccinationService> _mockVaccinationService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly VaccinationsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Vaccination _testVaccination;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestVaccinationId = 100;

        public VaccinationsControllerTests()
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
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testVaccination = new Vaccination
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "MMR Vaccine",
                VaccinationDescription = "Measles, Mumps, and Rubella vaccine",
                VaccinationDate = DateTime.UtcNow.AddDays(-30),
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination,
                ItemId = TestVaccinationId.ToString(),
                ProgenyTime = _testVaccination.VaccinationDate
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockVaccinationService = new Mock<IVaccinationService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new VaccinationsController(
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockVaccinationService.Object,
                _mockProgenyService.Object,
                _mockWebNotificationsService.Object);

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

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Vaccinations_List()
        {
            // Arrange
            List<Vaccination> vaccinationsList =
            [
                _testVaccination,
                new() { VaccinationId = 101, ProgenyId = TestProgenyId, VaccinationName = "DTaP" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccinationsList(TestProgenyId, _testUser))
                .ReturnsAsync(vaccinationsList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Vaccination> returnedVaccinations = Assert.IsType<List<Vaccination>>(okResult.Value);
            Assert.Equal(2, returnedVaccinations.Count);
            Assert.Contains(returnedVaccinations, v => v.VaccinationId == TestVaccinationId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVaccinationService.Verify(x => x.GetVaccinationsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_NotFound_When_Vaccinations_List_Is_Empty()
        {
            // Arrange
            List<Vaccination> emptyList = [];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccinationsList(TestProgenyId, _testUser))
                .ReturnsAsync(emptyList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVaccinationService.Verify(x => x.GetVaccinationsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Handle_Multiple_Progenies()
        {
            // Arrange
            const int otherProgenyId = 2;
            List<Vaccination> vaccinationsList =
            [
                _testVaccination
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccinationsList(otherProgenyId, _testUser))
                .ReturnsAsync(vaccinationsList);

            // Act
            IActionResult result = await _controller.Progeny(otherProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Vaccination> returnedVaccinations = Assert.IsType<List<Vaccination>>(okResult.Value);
            Assert.Single(returnedVaccinations);

            _mockVaccinationService.Verify(x => x.GetVaccinationsList(otherProgenyId, _testUser), Times.Once);
        }

        #endregion

        #region GetVaccinationItem Tests

        [Fact]
        public async Task GetVaccinationItem_Should_Return_Ok_When_Vaccination_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);

            // Act
            IActionResult result = await _controller.GetVaccinationItem(TestVaccinationId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Vaccination returnedVaccination = Assert.IsType<Vaccination>(okResult.Value);
            Assert.Equal(TestVaccinationId, returnedVaccination.VaccinationId);
            Assert.Equal(_testVaccination.VaccinationName, returnedVaccination.VaccinationName);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVaccinationService.Verify(x => x.GetVaccination(TestVaccinationId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVaccinationItem_Should_Return_NotFound_When_Vaccination_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(999, _testUser))
                .ReturnsAsync((Vaccination)null!);

            // Act
            IActionResult result = await _controller.GetVaccinationItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVaccinationService.Verify(x => x.GetVaccination(999, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVaccinationItem_Should_Return_NotFound_When_VaccinationId_Is_Zero()
        {
            // Arrange
            Vaccination vaccinationWithZeroId = new()
            {
                VaccinationId = 0,
                VaccinationName = "Test"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(vaccinationWithZeroId);

            // Act
            IActionResult result = await _controller.GetVaccinationItem(TestVaccinationId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Vaccination_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = TestProgenyId,
                VaccinationName = "Hepatitis B",
                VaccinationDescription = "First dose",
                VaccinationDate = DateTime.UtcNow
            };

            Vaccination createdVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Hepatitis B",
                VaccinationDescription = "First dose",
                VaccinationDate = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.AddVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(createdVaccination);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(createdVaccination);

            // Act
            IActionResult result = await _controller.Post(newVaccination);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Vaccination returnedVaccination = Assert.IsType<Vaccination>(okResult.Value);
            Assert.Equal(TestVaccinationId, returnedVaccination.VaccinationId);
            Assert.Equal(TestUserId, returnedVaccination.Author);
            Assert.Equal(TestUserId, returnedVaccination.CreatedBy);
            Assert.Equal(TestUserId, returnedVaccination.ModifiedBy);

            _mockVaccinationService.Verify(x => x.AddVaccination(It.Is<Vaccination>(v =>
                v.Author == TestUserId &&
                v.CreatedBy == TestUserId &&
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                createdVaccination, _testUser, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_VaccinationService_Returns_Null()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = TestProgenyId,
                VaccinationName = "Failed Vaccination"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.AddVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync((Vaccination)null!);

            // Act
            IActionResult result = await _controller.Post(newVaccination);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                It.IsAny<Vaccination>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Progeny_NickName()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine"
            };

            Vaccination createdVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine",
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.AddVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(createdVaccination);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(createdVaccination);

            // Act
            IActionResult result = await _controller.Post(newVaccination);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                createdVaccination, _testUser, $"Vaccination added for {_testProgeny.NickName}"), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Set_Author_CreatedBy_And_ModifiedBy_From_User()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine",
                Author = "wrong-user",
                CreatedBy = "wrong-user",
                ModifiedBy = "wrong-user"
            };

            Vaccination createdVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.AddVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(createdVaccination);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(createdVaccination);

            // Act
            await _controller.Post(newVaccination);

            // Assert
            _mockVaccinationService.Verify(x => x.AddVaccination(It.Is<Vaccination>(v =>
                v.Author == TestUserId &&
                v.CreatedBy == TestUserId &&
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Retrieve_Updated_Vaccination_After_Adding()
        {
            // Arrange
            Vaccination newVaccination = new()
            {
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine"
            };

            Vaccination createdVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Test Vaccine"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.AddVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(createdVaccination);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(createdVaccination);

            // Act
            await _controller.Post(newVaccination);

            // Assert
            _mockVaccinationService.Verify(x => x.GetVaccination(TestVaccinationId, _testUser), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Vaccination_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Updated Vaccine",
                VaccinationDescription = "Updated Description"
            };

            Vaccination returnedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                ProgenyId = TestProgenyId,
                VaccinationName = "Updated Vaccine",
                VaccinationDescription = "Updated Description",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockVaccinationService.Setup(x => x.UpdateVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(returnedVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestVaccinationId, updatedVaccination);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Vaccination resultVaccination = Assert.IsType<Vaccination>(okResult.Value);
            Assert.Equal(TestUserId, resultVaccination.ModifiedBy);

            _mockVaccinationService.Verify(x => x.GetVaccination(TestVaccinationId, _testUser), Times.Exactly(2)); // Once for check, once after update
            _mockVaccinationService.Verify(x => x.UpdateVaccination(It.Is<Vaccination>(v =>
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Vaccination_Does_Not_Exist()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = 999,
                VaccinationName = "Non-existent Vaccine"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(999, _testUser))
                .ReturnsAsync((Vaccination)null!);

            // Act
            IActionResult result = await _controller.Put(999, updatedVaccination);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVaccinationService.Verify(x => x.UpdateVaccination(It.IsAny<Vaccination>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateVaccination_Returns_Null()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Failed Update"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockVaccinationService.Setup(x => x.UpdateVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync((Vaccination)null!);

            // Act
            IActionResult result = await _controller.Put(TestVaccinationId, updatedVaccination);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine"
            };

            Vaccination returnedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockVaccinationService.Setup(x => x.UpdateVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(returnedVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            IActionResult result = await _controller.Put(TestVaccinationId, updatedVaccination);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Set_ModifiedBy_From_User()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine",
                ModifiedBy = "wrong-user"
            };

            Vaccination returnedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockVaccinationService.Setup(x => x.UpdateVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(returnedVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Put(TestVaccinationId, updatedVaccination);

            // Assert
            _mockVaccinationService.Verify(x => x.UpdateVaccination(It.Is<Vaccination>(v =>
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Retrieve_Updated_Vaccination_After_Update()
        {
            // Arrange
            Vaccination updatedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine"
            };

            Vaccination returnedVaccination = new()
            {
                VaccinationId = TestVaccinationId,
                VaccinationName = "Updated Vaccine"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockVaccinationService.Setup(x => x.UpdateVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(returnedVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Put(TestVaccinationId, updatedVaccination);

            // Assert
            _mockVaccinationService.Verify(x => x.GetVaccination(TestVaccinationId, _testUser), Times.Exactly(2));
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Vaccination_And_Timeline_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.DeleteVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(_testVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestVaccinationId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockVaccinationService.Verify(x => x.GetVaccination(TestVaccinationId, _testUser), Times.Once);
            _mockVaccinationService.Verify(x => x.DeleteVaccination(It.Is<Vaccination>(v =>
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                _testVaccination, _testUser, $"Vaccination deleted for {_testProgeny.NickName}"), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Vaccination_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(999, _testUser))
                .ReturnsAsync((Vaccination)null!);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVaccinationService.Verify(x => x.DeleteVaccination(It.IsAny<Vaccination>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.DeleteVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(_testVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            IActionResult result = await _controller.Delete(TestVaccinationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Send_Notification_With_Progeny_NickName()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.DeleteVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(_testVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestVaccinationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                _testVaccination, _testUser, $"Vaccination deleted for {_testProgeny.NickName}"), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Set_ModifiedBy_From_User()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.DeleteVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(_testVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Delete(TestVaccinationId);

            // Assert
            _mockVaccinationService.Verify(x => x.DeleteVaccination(It.Is<Vaccination>(v =>
                v.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Not_Send_Notification_When_TimelineItem_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVaccinationService.Setup(x => x.GetVaccination(TestVaccinationId, _testUser))
                .ReturnsAsync(_testVaccination);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVaccinationService.Setup(x => x.DeleteVaccination(It.IsAny<Vaccination>(), _testUser))
                .ReturnsAsync(_testVaccination);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestVaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            IActionResult result = await _controller.Delete(TestVaccinationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockWebNotificationsService.Verify(x => x.SendVaccinationNotification(
                It.IsAny<Vaccination>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Never);
        }

        #endregion
    }
}