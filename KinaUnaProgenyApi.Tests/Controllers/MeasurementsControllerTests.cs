using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class MeasurementsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IMeasurementService> _mockMeasurementService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly MeasurementsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Measurement _testMeasurement;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestMeasurementId = 100;

        public MeasurementsControllerTests()
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

            _testMeasurement = new Measurement
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId,
                Height = 100.5,
                Weight = 15.5,
                Circumference = 50.0,
                EyeColor = "Blue",
                HairColor = "Blonde",
                Date = DateTime.UtcNow.AddDays(-30),
                CreatedDate = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Measurement,
                ItemId = TestMeasurementId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockMeasurementService = new Mock<IMeasurementService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new MeasurementsController(
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockMeasurementService.Object,
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
        public async Task Progeny_Should_Return_Ok_With_Measurements_List()
        {
            // Arrange
            List<Measurement> measurements = [_testMeasurement];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(measurements);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Measurement> returnedMeasurements = Assert.IsType<List<Measurement>>(okResult.Value);
            Assert.Single(returnedMeasurements);
            Assert.Equal(TestMeasurementId, returnedMeasurements[0].MeasurementId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockMeasurementService.Verify(x => x.GetMeasurementsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Measurements()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Measurement> returnedMeasurements = Assert.IsType<List<Measurement>>(okResult.Value);
            Assert.Empty(returnedMeasurements);
        }

        #endregion

        #region GetMeasurementItem Tests

        [Fact]
        public async Task GetMeasurementItem_Should_Return_Ok_When_Measurement_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);

            // Act
            IActionResult result = await _controller.GetMeasurementItem(TestMeasurementId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Measurement returnedMeasurement = Assert.IsType<Measurement>(okResult.Value);
            Assert.Equal(TestMeasurementId, returnedMeasurement.MeasurementId);
            Assert.Equal(_testMeasurement.Height, returnedMeasurement.Height);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockMeasurementService.Verify(x => x.GetMeasurement(TestMeasurementId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetMeasurementItem_Should_Return_NotFound_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(999, _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.GetMeasurementItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockMeasurementService.Verify(x => x.GetMeasurement(999, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetMeasurementItem_Should_Return_NotFound_When_MeasurementId_Is_Zero()
        {
            // Arrange
            Measurement measurement = new()
            {
                MeasurementId = 0,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(measurement);

            // Act
            IActionResult result = await _controller.GetMeasurementItem(TestMeasurementId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Measurement_And_Return_Ok()
        {
            // Arrange
            Measurement newMeasurement = new()
            {
                ProgenyId = TestProgenyId,
                Height = 105.0,
                Weight = 16.0,
                Circumference = 51.0,
                EyeColor = "Brown",
                HairColor = "Black",
                Date = DateTime.UtcNow
            };

            Measurement createdMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId,
                Height = 105.0,
                Weight = 16.0,
                Circumference = 51.0,
                EyeColor = "Brown",
                HairColor = "Black",
                Date = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockMeasurementService.Setup(x => x.AddMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(createdMeasurement);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(createdMeasurement);

            // Act
            IActionResult result = await _controller.Post(newMeasurement);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Measurement returnedMeasurement = Assert.IsType<Measurement>(okResult.Value);
            Assert.Equal(TestMeasurementId, returnedMeasurement.MeasurementId);
            Assert.Equal(TestUserId, returnedMeasurement.Author);

            _mockMeasurementService.Verify(x => x.AddMeasurement(It.Is<Measurement>(m =>
                m.Author == TestUserId && m.CreatedBy == TestUserId && m.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendMeasurementNotification(
                It.IsAny<Measurement>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddMeasurement_Returns_Null()
        {
            // Arrange
            Measurement newMeasurement = new()
            {
                ProgenyId = TestProgenyId,
                Height = 105.0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockMeasurementService.Setup(x => x.AddMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.Post(newMeasurement);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_MeasurementId_Is_Zero()
        {
            // Arrange
            Measurement newMeasurement = new()
            {
                ProgenyId = TestProgenyId,
                Height = 105.0
            };

            Measurement createdMeasurement = new()
            {
                MeasurementId = 0,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockMeasurementService.Setup(x => x.AddMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(createdMeasurement);

            // Act
            IActionResult result = await _controller.Post(newMeasurement);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Set_Author_CreatedBy_And_ModifiedBy()
        {
            // Arrange
            Measurement newMeasurement = new()
            {
                ProgenyId = TestProgenyId,
                Height = 105.0
            };

            Measurement createdMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockMeasurementService.Setup(x => x.AddMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(createdMeasurement);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(createdMeasurement);

            // Act
            IActionResult result = await _controller.Post(newMeasurement);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockMeasurementService.Verify(x => x.AddMeasurement(It.Is<Measurement>(m =>
                m.Author == TestUserId &&
                m.CreatedBy == TestUserId &&
                m.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Progeny_NickName()
        {
            // Arrange
            Measurement newMeasurement = new()
            {
                ProgenyId = TestProgenyId,
                Height = 105.0
            };

            Measurement createdMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockMeasurementService.Setup(x => x.AddMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(createdMeasurement);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(createdMeasurement);

            // Act
            await _controller.Post(newMeasurement);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendMeasurementNotification(
                It.IsAny<Measurement>(),
                It.IsAny<UserInfo>(),
                It.Is<string>(s => s.Contains(_testProgeny.NickName))), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Measurement_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId,
                Height = 110.0,
                Weight = 17.0
            };

            Measurement returnedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId,
                Height = 110.0,
                Weight = 17.0,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.UpdateMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(returnedMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(returnedMeasurement);

            // Act
            IActionResult result = await _controller.Put(TestMeasurementId, updatedMeasurement);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Measurement returnedValue = Assert.IsType<Measurement>(okResult.Value);
            Assert.Equal(110.0, returnedValue.Height);
            Assert.Equal(TestUserId, returnedValue.ModifiedBy);

            _mockMeasurementService.Verify(x => x.GetMeasurement(TestMeasurementId, _testUser), Times.Exactly(2));
            _mockMeasurementService.Verify(x => x.UpdateMeasurement(It.Is<Measurement>(m =>
                m.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = 999,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(999, _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.Put(999, updatedMeasurement);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockMeasurementService.Verify(x => x.UpdateMeasurement(It.IsAny<Measurement>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateMeasurement_Returns_Null()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.UpdateMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.Put(TestMeasurementId, updatedMeasurement);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdatedMeasurementId_Is_Zero()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            Measurement returnedMeasurement = new()
            {
                MeasurementId = 0,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.UpdateMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(returnedMeasurement);

            // Act
            IActionResult result = await _controller.Put(TestMeasurementId, updatedMeasurement);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            Measurement returnedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.UpdateMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(returnedMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(null as TimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(returnedMeasurement);

            // Act
            IActionResult result = await _controller.Put(TestMeasurementId, updatedMeasurement);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Set_ModifiedBy()
        {
            // Arrange
            Measurement updatedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            Measurement returnedMeasurement = new()
            {
                MeasurementId = TestMeasurementId,
                ProgenyId = TestProgenyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.UpdateMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(returnedMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockMeasurementService.Setup(x => x.GetMeasurement(It.IsAny<int>(), _testUser))
                .ReturnsAsync(returnedMeasurement);

            // Act
            await _controller.Put(TestMeasurementId, updatedMeasurement);

            // Assert
            _mockMeasurementService.Verify(x => x.UpdateMeasurement(It.Is<Measurement>(m =>
                m.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Measurement_And_Timeline_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestMeasurementId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockMeasurementService.Verify(x => x.GetMeasurement(TestMeasurementId, _testUser), Times.Once);
            _mockMeasurementService.Verify(x => x.DeleteMeasurement(It.Is<Measurement>(m =>
                m.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendMeasurementNotification(
                It.IsAny<Measurement>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(999, _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockMeasurementService.Verify(x => x.DeleteMeasurement(It.IsAny<Measurement>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteMeasurement_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(null as Measurement);

            // Act
            IActionResult result = await _controller.Delete(TestMeasurementId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestMeasurementId);

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
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestMeasurementId);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendMeasurementNotification(
                It.IsAny<Measurement>(),
                It.IsAny<UserInfo>(),
                It.Is<string>(s => s.Contains(_testProgeny.NickName))), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext();

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestMeasurementId);

            // Assert
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Set_ModifiedBy()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurement(TestMeasurementId, _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockMeasurementService.Setup(x => x.DeleteMeasurement(It.IsAny<Measurement>(), _testUser))
                .ReturnsAsync(_testMeasurement);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestMeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestMeasurementId);

            // Assert
            _mockMeasurementService.Verify(x => x.DeleteMeasurement(It.Is<Measurement>(m =>
                m.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        #endregion

        #region GetMeasurementsListPage Tests

        [Fact]
        public async Task GetMeasurementsListPage_Should_Return_Ok_With_Paged_Results()
        {
            // Arrange
            List<Measurement> allMeasurements =
            [
                _testMeasurement,
                new() { MeasurementId = 2, ProgenyId = TestProgenyId, Height = 105.0, Date = DateTime.UtcNow.AddDays(-20) },
                new() { MeasurementId = 3, ProgenyId = TestProgenyId, Height = 110.0, Date = DateTime.UtcNow.AddDays(-10) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(2, 1, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(2, model.MeasurementsList.Count);
            Assert.Equal(2, model.TotalPages);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(1, model.SortBy);
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Set_PageIndex_To_One_When_Less_Than_One()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 0, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Sort_By_Oldest_First_When_SortBy_Is_Zero()
        {
            // Arrange
            List<Measurement> allMeasurements =
            [
                new() { MeasurementId = 1, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-30) },
                new() { MeasurementId = 2, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-20) },
                new() { MeasurementId = 3, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-10) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 1, TestProgenyId, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(1, model.MeasurementsList[0].MeasurementNumber);
            Assert.Equal(2, model.MeasurementsList[1].MeasurementNumber);
            Assert.Equal(3, model.MeasurementsList[2].MeasurementNumber);
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Sort_By_Newest_First_When_SortBy_Is_One()
        {
            // Arrange
            List<Measurement> allMeasurements =
            [
                new() { MeasurementId = 1, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-30) },
                new() { MeasurementId = 2, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-20) },
                new() { MeasurementId = 3, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-10) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 1, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(3, model.MeasurementsList[0].MeasurementNumber);
            Assert.Equal(2, model.MeasurementsList[1].MeasurementNumber);
            Assert.Equal(1, model.MeasurementsList[2].MeasurementNumber);
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Calculate_TotalPages_Correctly()
        {
            // Arrange
            List<Measurement> allMeasurements = [];
            for (int i = 0; i < 25; i++)
            {
                allMeasurements.Add(new Measurement { MeasurementId = i, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-i) });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 1, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(4, model.TotalPages); // 25 items / 8 per page = 3.125 -> 4 pages
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Return_Correct_Page_Items()
        {
            // Arrange
            List<Measurement> allMeasurements = [];
            for (int i = 0; i < 25; i++)
            {
                allMeasurements.Add(new Measurement { MeasurementId = i, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-i) });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 2, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Equal(8, model.MeasurementsList.Count);
            Assert.Equal(2, model.PageNumber);
        }

        [Fact]
        public async Task GetMeasurementsListPage_Should_Return_Empty_List_For_Page_Beyond_Total()
        {
            // Arrange
            List<Measurement> allMeasurements =
            [
                new() { MeasurementId = 1, ProgenyId = TestProgenyId, Date = DateTime.UtcNow.AddDays(-30) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockMeasurementService.Setup(x => x.GetMeasurementsList(TestProgenyId, _testUser))
                .ReturnsAsync(allMeasurements);

            // Act
            IActionResult result = await _controller.GetMeasurementsListPage(8, 5, TestProgenyId, 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            MeasurementsListPage model = Assert.IsType<MeasurementsListPage>(okResult.Value);
            Assert.Empty(model.MeasurementsList);
            Assert.Equal(5, model.PageNumber);
        }

        #endregion
    }
}