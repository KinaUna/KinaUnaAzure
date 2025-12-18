using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.Search;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.Search
{
    public class SearchServiceSearchLocationsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchLocationsTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContexts (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            DbContextOptions<MediaDbContext> mediaOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _mediaDbContext = new MediaDbContext(mediaOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            Mock<ITimelineService> mockTimelineService = new();

            // Initialize service
            _service = new SearchService(
                _progenyDbContext,
                _mediaDbContext,
                _mockAccessManagementService.Object,
                mockTimelineService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test Location items for progeny search
            Location location1 = new()
            {
                LocationId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Name = "Central Park",
                StreetName = "Fifth Avenue",
                City = "New York",
                District = "Manhattan",
                County = "New York County",
                State = "New York",
                Country = "USA",
                PostalCode = "10022",
                Notes = "Beautiful park in the city",
                Tags = "park,outdoor,nature",
                Date = DateTime.UtcNow.AddDays(-7),
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.LocationsDb.Add(location1);

            Location location2 = new()
            {
                LocationId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Name = "Golden Gate Bridge",
                StreetName = "Golden Gate Bridge",
                City = "San Francisco",
                District = "Presidio",
                County = "San Francisco County",
                State = "California",
                Country = "USA",
                PostalCode = "94129",
                Notes = "Famous landmark bridge",
                Tags = "bridge,landmark,tourist",
                Date = DateTime.UtcNow.AddDays(-14),
                CreatedTime = DateTime.UtcNow.AddDays(-15),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.LocationsDb.Add(location2);

            Location location3 = new()
            {
                LocationId = 3,
                ProgenyId = 2,
                FamilyId = 0,
                Name = "Eiffel Tower",
                StreetName = "Champ de Mars",
                City = "Paris",
                District = "7th arrondissement",
                County = "Paris",
                State = "Île-de-France",
                Country = "France",
                PostalCode = "75007",
                Notes = "Iconic iron lattice tower",
                Tags = "tower,landmark,france",
                Date = DateTime.UtcNow.AddDays(-21),
                CreatedTime = DateTime.UtcNow.AddDays(-22),
                Author = "user2",
                CreatedBy = "user2"
            };
            _progenyDbContext.LocationsDb.Add(location3);

            // Add test Location items for family search
            Location familyLocation1 = new()
            {
                LocationId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Name = "Beach Resort",
                StreetName = "Ocean Drive",
                City = "Miami",
                District = "South Beach",
                County = "Miami-Dade County",
                State = "Florida",
                Country = "USA",
                PostalCode = "33139",
                Notes = "Family vacation spot",
                Tags = "beach,resort,vacation",
                Date = DateTime.UtcNow.AddDays(-30),
                CreatedTime = DateTime.UtcNow.AddDays(-35),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.LocationsDb.Add(familyLocation1);

            Location familyLocation2 = new()
            {
                LocationId = 5,
                ProgenyId = 0,
                FamilyId = 1,
                Name = "Mountain Cabin",
                StreetName = "Pine Tree Lane",
                City = "Aspen",
                District = "Pitkin",
                County = "Pitkin County",
                State = "Colorado",
                Country = "USA",
                PostalCode = "81611",
                Notes = "Cozy cabin in the mountains",
                Tags = "cabin,mountain,winter",
                Date = DateTime.UtcNow.AddDays(-45),
                CreatedTime = DateTime.UtcNow.AddDays(-50),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.LocationsDb.Add(familyLocation2);

            _progenyDbContext.SaveChanges();
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region SearchLocations Tests

        [Fact]
        public async Task SearchLocations_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Central",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchLocations_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchLocations_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchLocations_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Central",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Central Park", result.Results[0].Name);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchLocations_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Central",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchLocations_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Central",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchLocations_SearchesNameField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "park",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Central Park", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchLocations_SearchesStreetNameField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "fifth",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Fifth", result.Results[0].StreetName);
        }

        [Fact]
        public async Task SearchLocations_SearchesCityField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "san francisco",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("San Francisco", result.Results[0].City);
        }

        [Fact]
        public async Task SearchLocations_SearchesDistrictField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "manhattan",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Manhattan", result.Results[0].District);
        }

        [Fact]
        public async Task SearchLocations_SearchesCountyField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "new york county",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("New York County", result.Results[0].County);
        }

        [Fact]
        public async Task SearchLocations_SearchesStateField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "california",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("California", result.Results[0].State);
        }

        [Fact]
        public async Task SearchLocations_SearchesCountryField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "usa",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.All(result.Results, r => Assert.Equal("USA", r.Country));
        }

        [Fact]
        public async Task SearchLocations_SearchesPostalCodeField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "10022",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("10022", result.Results[0].PostalCode);
        }

        [Fact]
        public async Task SearchLocations_SearchesNotesField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "beautiful",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Beautiful", result.Results[0].Notes);
        }

        [Fact]
        public async Task SearchLocations_SearchesTagsField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "landmark",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("landmark", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchLocations_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "CENTRAL PARK",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Central Park", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchLocations_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  Central Park  ",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Central Park", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchLocations_SearchesFamilyItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "beach",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 4, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 4, 0, 1, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Beach Resort", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchLocations_WhenUserHasNoFamilyPermission_SkipsFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "beach",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchLocations_SearchesBothProgenyAndFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "usa",
                ProgenyIds = [1],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Results.Count); // 2 progeny locations + 2 family locations in USA
        }

        [Fact]
        public async Task SearchLocations_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 1,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.True(result.RemainingItems > 0);
        }

        [Fact]
        public async Task SearchLocations_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Results.Count, result.TotalCount);
        }

        [Fact]
        public async Task SearchLocations_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Sort = 0 // Descending (newest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].Date ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].Date ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime >= nextTime, "Results should be sorted in descending order");
                }
            }
        }

        [Fact]
        public async Task SearchLocations_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Sort = 1 // Ascending (oldest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].Date ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].Date ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime <= nextTime, "Results should be sorted in ascending order");
                }
            }
        }

        [Fact]
        public async Task SearchLocations_SkipsItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 1,
                Skip = 1
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // First get all results
            SearchRequest allRequest = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Skip = 0
            };
            SearchResponse<Location> allResults = await _service.SearchLocations(allRequest, _testUser);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].LocationId, result.Results[0].LocationId);
            }
        }

        [Fact]
        public async Task SearchLocations_ReturnsAllItemsWhenNumberOfItemsIsZero()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 0,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchLocations_DoesNotReturnFamilyItemsForProgenySearch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "miami", // Only matches family item
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchLocations_DoesNotReturnProgenyItemsForFamilySearch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "central park", // Only matches progeny item
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchLocations_SearchesMultipleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "eiffel",
                ProgenyIds = [1, 2],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 3, 2, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Eiffel Tower", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchLocations_ReturnsEmptyWhenNoMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nonexistentquery12345",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchLocations_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "central",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.Edit,
                TimelineItemPermissionId = 100
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        [Fact]
        public async Task SearchLocations_SearchesMultipleFamilies()
        {
            // Arrange - Add a location for a second family
            Location familyLocation3 = new()
            {
                LocationId = 6,
                ProgenyId = 0,
                FamilyId = 2,
                Name = "Lake House",
                StreetName = "Lake View Road",
                City = "Lake Tahoe",
                District = "Douglas",
                County = "Douglas County",
                State = "Nevada",
                Country = "USA",
                PostalCode = "89449",
                Notes = "Summer lake retreat",
                Tags = "lake,summer,retreat",
                Date = DateTime.UtcNow.AddDays(-60),
                CreatedTime = DateTime.UtcNow.AddDays(-65),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.LocationsDb.Add(familyLocation3);
            await _progenyDbContext.SaveChangesAsync();

            SearchRequest request = new()
            {
                Query = "lake",
                ProgenyIds = [],
                FamilyIds = [1, 2],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 6, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 6, 0, 2, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Location> result = await _service.SearchLocations(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Lake House", result.Results[0].Name);
        }

        #endregion
    }
}