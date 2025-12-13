using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models;
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
    public class LocationsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly LocationsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;
        private readonly Location _testLocation;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestLocationId = 100;

        public LocationsControllerTests()
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
                LastName = "User",
                ProfilePicture = "profile.jpg"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family"
            };

            _testLocation = new Location
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Test Location",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                Country = "USA",
                Tags = "Tag1, Tag2",
                Date = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                ItemPerMission = new TimelineItemPermission(){PermissionLevel = PermissionLevel.Edit}
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Location,
                ItemId = TestLocationId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockLocationService = new Mock<ILocationService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            _controller = new LocationsController(
                _mockUserInfoService.Object,
                _mockLocationService.Object,
                _mockTimelineService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                _mockWebNotificationsService.Object,
                _mockAccessManagementService.Object);

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
        public async Task Progeny_Should_Return_Ok_With_Locations_List()
        {
            // Arrange
            List<Location> locations = [_testLocation];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Location> returnedLocations = Assert.IsType<List<Location>>(okResult.Value);
            Assert.Single(returnedLocations);
            Assert.Equal(TestLocationId, returnedLocations[0].LocationId);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Locations_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Location> returnedLocations = Assert.IsType<List<Location>>(okResult.Value);
            Assert.Empty(returnedLocations);
        }

        #endregion

        #region Family Tests

        [Fact]
        public async Task Family_Should_Return_Ok_With_Locations_List()
        {
            // Arrange
            Location familyLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Name = "Family Location"
            };
            List<Location> locations = [familyLocation];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(0, TestFamilyId, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.Family(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Location> returnedLocations = Assert.IsType<List<Location>>(okResult.Value);
            Assert.Single(returnedLocations);
            Assert.Equal(TestFamilyId, returnedLocations[0].FamilyId);
        }

        [Fact]
        public async Task Family_Should_Return_Empty_List_When_No_Locations_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(0, TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Family(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Location> returnedLocations = Assert.IsType<List<Location>>(okResult.Value);
            Assert.Empty(returnedLocations);
        }

        #endregion

        #region GetLocationItem Tests

        [Fact]
        public async Task GetLocationItem_Should_Return_Ok_When_Location_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(_testLocation);

            // Act
            IActionResult result = await _controller.GetLocationItem(TestLocationId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Location returnedLocation = Assert.IsType<Location>(okResult.Value);
            Assert.Equal(TestLocationId, returnedLocation.LocationId);
            Assert.Equal("Test Location", returnedLocation.Name);
        }

        [Fact]
        public async Task GetLocationItem_Should_Return_NotFound_When_Location_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(999, _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.GetLocationItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Location_For_Progeny_And_Return_Ok()
        {
            // Arrange
            Location newLocation = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "New Location",
                City = "Boston"
            };

            Location createdLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "New Location",
                City = "Boston",
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockLocationService.Setup(x => x.AddLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(createdLocation);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockLocationService.Setup(x => x.GetLocation(It.IsAny<int>(), _testUser))
                .ReturnsAsync(createdLocation);

            // Act
            IActionResult result = await _controller.Post(newLocation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Location returnedLocation = Assert.IsType<Location>(okResult.Value);
            Assert.Equal(TestLocationId, returnedLocation.LocationId);
            Assert.Equal(TestUserId, returnedLocation.Author);

            _mockAccessManagementService.Verify(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add), Times.Once);
            _mockLocationService.Verify(x => x.AddLocation(It.Is<Location>(l =>
                l.Author == TestUserId && l.CreatedBy == TestUserId && l.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendLocationNotification(
                It.IsAny<Location>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Create_Location_For_Family_And_Return_Ok()
        {
            // Arrange
            Location newLocation = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Name = "Family Location"
            };

            Location createdLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Name = "Family Location",
                Author = TestUserId,
                CreatedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockLocationService.Setup(x => x.AddLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(createdLocation);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockLocationService.Setup(x => x.GetLocation(It.IsAny<int>(), _testUser))
                .ReturnsAsync(createdLocation);

            // Act
            IActionResult result = await _controller.Post(newLocation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Location returnedLocation = Assert.IsType<Location>(okResult.Value);
            Assert.Equal(TestFamilyId, returnedLocation.FamilyId);

            _mockAccessManagementService.Verify(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            Location invalidLocation = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                Name = "Invalid Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Post(invalidLocation);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A location must have either a ProgenyId or a FamilyId set, but not both.", badRequestResult.Value);

            _mockLocationService.Verify(x => x.AddLocation(It.IsAny<Location>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Neither_ProgenyId_Nor_FamilyId_Set()
        {
            // Arrange
            Location invalidLocation = new()
            {
                ProgenyId = 0,
                FamilyId = 0,
                Name = "Invalid Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Post(invalidLocation);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A location board must have either a ProgenyId or a FamilyId set.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Progeny_Permission()
        {
            // Arrange
            Location newLocation = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Unauthorized Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newLocation);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLocationService.Verify(x => x.AddLocation(It.IsAny<Location>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Family_Permission()
        {
            // Arrange
            Location newLocation = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Name = "Unauthorized Family Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newLocation);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_LocationService_Returns_Null()
        {
            // Arrange
            Location newLocation = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Failed Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockLocationService.Setup(x => x.AddLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.Post(newLocation);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Location_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Updated Location",
                City = "Updated City"
            };

            Location returnedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Updated Location",
                City = "Updated City",
                ModifiedBy = TestUserId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(_testLocation);
            _mockLocationService.Setup(x => x.UpdateLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(returnedLocation);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestLocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, updatedLocation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<Location>(okResult.Value);
            
            _mockLocationService.Verify(x => x.GetLocation(TestLocationId, _testUser), Times.Exactly(2));
            _mockLocationService.Verify(x => x.UpdateLocation(It.IsAny<Location>(), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Id_Mismatch()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = 999,
                ProgenyId = TestProgenyId,
                Name = "Mismatched Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, updatedLocation);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("LocationId in the URL must match the LocationId in the body of the request.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Location_Does_Not_Exist()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = 999,
                ProgenyId = TestProgenyId,
                Name = "Non-existent Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(999, _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.Put(999, updatedLocation);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLocationService.Verify(x => x.UpdateLocation(It.IsAny<Location>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Location_Has_Both_ProgenyId_And_FamilyId()
        {
            // Arrange
            Location invalidLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                Name = "Invalid Location"
            };

            Location existingLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(existingLocation);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, invalidLocation);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A location must have either a ProgenyId or a FamilyId set, but not both.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Permission_Level_Too_Low()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Updated Location"
            };

            Location existingLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(existingLocation);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, updatedLocation);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateLocation_Returns_Null()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Failed Update"
            };

            Location existingLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(existingLocation);
            _mockLocationService.Setup(x => x.UpdateLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, updatedLocation);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Location updatedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Name = "Updated Location",
                ItemPerMission = new TimelineItemPermission{ PermissionLevel = PermissionLevel.Edit}
            };

            Location returnedLocation = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                Name = "Updated Location",
                ModifiedBy = TestUserId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(_testLocation);
            _mockLocationService.Setup(x => x.UpdateLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(returnedLocation);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestLocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestLocationId, updatedLocation);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Location_And_Timeline_For_Progeny_And_Return_NoContent()
        {
            // Arrange
            Location locationToDelete = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(locationToDelete);
            _mockLocationService.Setup(x => x.DeleteLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(locationToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestLocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestLocationId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockLocationService.Verify(x => x.GetLocation(TestLocationId, _testUser), Times.Once);
            _mockLocationService.Verify(x => x.DeleteLocation(It.Is<Location>(l =>
                l.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendLocationNotification(
                It.IsAny<Location>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Location_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(999, _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLocationService.Verify(x => x.DeleteLocation(It.IsAny<Location>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Permission_Level_Too_Low()
        {
            // Arrange
            Location locationToDelete = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(locationToDelete);

            // Act
            IActionResult result = await _controller.Delete(TestLocationId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLocationService.Verify(x => x.DeleteLocation(It.IsAny<Location>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteLocation_Returns_Null()
        {
            // Arrange
            Location locationToDelete = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(locationToDelete);
            _mockLocationService.Setup(x => x.DeleteLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(null as Location);

            // Act
            IActionResult result = await _controller.Delete(TestLocationId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Location locationToDelete = new()
            {
                LocationId = TestLocationId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(locationToDelete);
            _mockLocationService.Setup(x => x.DeleteLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(locationToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestLocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestLocationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Handle_Family_Location()
        {
            // Arrange
            Location familyLocationToDelete = new()
            {
                LocationId = TestLocationId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocation(TestLocationId, _testUser))
                .ReturnsAsync(familyLocationToDelete);
            _mockLocationService.Setup(x => x.DeleteLocation(It.IsAny<Location>(), _testUser))
                .ReturnsAsync(familyLocationToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestLocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.Delete(TestLocationId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify that progeny-specific notifications are not sent for family locations
            _mockProgenyService.Verify(x => x.GetProgeny(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
            _mockFamiliesService.Verify(x => x.GetFamilyById(TestFamilyId, _testUser), Times.Once);
        }

        #endregion

        #region GetLocationsListPage Tests

        [Fact]
        public async Task GetLocationsListPage_Should_Return_Ok_With_Paged_Data()
        {
            // Arrange
            List<Location> locations =
            [
                new() { LocationId = 1, ProgenyId = TestProgenyId, Name = "Location 1", Date = DateTime.UtcNow.AddDays(-2) },
                new() { LocationId = 2, ProgenyId = TestProgenyId, Name = "Location 2", Date = DateTime.UtcNow.AddDays(-1) },
                new() { LocationId = 3, ProgenyId = TestProgenyId, Name = "Location 3", Date = DateTime.UtcNow }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.GetLocationsListPage(2, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            LocationsListPage model = Assert.IsType<LocationsListPage>(okResult.Value);
            Assert.Equal(2, model.LocationsList.Count);
            Assert.Equal(2, model.TotalPages);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task GetLocationsListPage_Should_Set_PageIndex_To_1_When_Less_Than_1()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetLocationsListPage(8, 0, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            LocationsListPage model = Assert.IsType<LocationsListPage>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task GetLocationsListPage_Should_Sort_By_Date_Ascending_When_SortBy_0()
        {
            // Arrange
            List<Location> locations =
            [
                new() { LocationId = 1, ProgenyId = TestProgenyId, Name = "Location 1", Date = DateTime.UtcNow.AddDays(-2) },
                new() { LocationId = 2, ProgenyId = TestProgenyId, Name = "Location 2", Date = DateTime.UtcNow.AddDays(-1) },
                new() { LocationId = 3, ProgenyId = TestProgenyId, Name = "Location 3", Date = DateTime.UtcNow }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.GetLocationsListPage(10, 1, TestProgenyId, 0, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            LocationsListPage model = Assert.IsType<LocationsListPage>(okResult.Value);
            Assert.Equal(3, model.LocationsList.Count);
            Assert.Equal(1, model.LocationsList[0].LocationNumber);
            Assert.Equal(2, model.LocationsList[1].LocationNumber);
            Assert.Equal(3, model.LocationsList[2].LocationNumber);
        }

        [Fact]
        public async Task GetLocationsListPage_Should_Sort_By_Date_Descending_When_SortBy_1()
        {
            // Arrange
            List<Location> locations =
            [
                new() { LocationId = 1, ProgenyId = TestProgenyId, Name = "Location 1", Date = DateTime.UtcNow.AddDays(-2) },
                new() { LocationId = 2, ProgenyId = TestProgenyId, Name = "Location 2", Date = DateTime.UtcNow.AddDays(-1) },
                new() { LocationId = 3, ProgenyId = TestProgenyId, Name = "Location 3", Date = DateTime.UtcNow }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.GetLocationsListPage(10, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            LocationsListPage model = Assert.IsType<LocationsListPage>(okResult.Value);
            Assert.Equal(3, model.LocationsList.Count);
            Assert.Equal(3, model.LocationsList[0].LocationNumber);
            Assert.Equal(2, model.LocationsList[1].LocationNumber);
            Assert.Equal(1, model.LocationsList[2].LocationNumber);
        }

        [Fact]
        public async Task GetLocationsListPage_Should_Calculate_TotalPages_Correctly()
        {
            // Arrange
            List<Location> locations = [];
            for (int i = 1; i <= 25; i++)
            {
                locations.Add(new Location { LocationId = i, ProgenyId = TestProgenyId, Name = $"Location {i}", Date = DateTime.UtcNow.AddDays(-i) });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(locations);

            // Act
            IActionResult result = await _controller.GetLocationsListPage(10, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            LocationsListPage model = Assert.IsType<LocationsListPage>(okResult.Value);
            Assert.Equal(3, model.TotalPages); // 25 items / 10 per page = 3 pages
            Assert.Equal(10, model.LocationsList.Count);
        }

        #endregion
    }
}