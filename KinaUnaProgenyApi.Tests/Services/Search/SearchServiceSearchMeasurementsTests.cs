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
    public class SearchServiceSearchMeasurementsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchMeasurementsTests()
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

            // Add test Measurements for progeny 1
            Measurement measurement1 = new()
            {
                MeasurementId = 1,
                ProgenyId = 1,
                Weight = 10.5,
                Height = 75.0,
                Circumference = 45.0,
                EyeColor = "Blue",
                HairColor = "Blonde",
                Date = DateTime.UtcNow.AddMonths(-6),
                CreatedDate = DateTime.UtcNow.AddMonths(-6),
                CreatedTime = DateTime.UtcNow.AddMonths(-6),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.MeasurementsDb.Add(measurement1);

            Measurement measurement2 = new()
            {
                MeasurementId = 2,
                ProgenyId = 1,
                Weight = 12.0,
                Height = 80.0,
                Circumference = 47.0,
                EyeColor = "Brown",
                HairColor = "Brown",
                Date = DateTime.UtcNow.AddMonths(-3),
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                CreatedTime = DateTime.UtcNow.AddMonths(-3),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.MeasurementsDb.Add(measurement2);

            Measurement measurement3 = new()
            {
                MeasurementId = 3,
                ProgenyId = 1,
                Weight = 14.0,
                Height = 85.0,
                Circumference = 49.0,
                EyeColor = "Green",
                HairColor = "Red",
                Date = DateTime.UtcNow.AddMonths(-1),
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                CreatedTime = DateTime.UtcNow.AddMonths(-1),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.MeasurementsDb.Add(measurement3);

            // Add test Measurement for progeny 2
            Measurement measurement4 = new()
            {
                MeasurementId = 4,
                ProgenyId = 2,
                Weight = 11.0,
                Height = 78.0,
                Circumference = 46.0,
                EyeColor = "Hazel",
                HairColor = "Black",
                Date = DateTime.UtcNow.AddMonths(-2),
                CreatedDate = DateTime.UtcNow.AddMonths(-2),
                CreatedTime = DateTime.UtcNow.AddMonths(-2),
                Author = "user2",
                CreatedBy = "user2"
            };
            _progenyDbContext.MeasurementsDb.Add(measurement4);

            // Add measurement with null EyeColor and HairColor
            Measurement measurement5 = new()
            {
                MeasurementId = 5,
                ProgenyId = 1,
                Weight = 15.0,
                Height = 88.0,
                Circumference = 50.0,
                EyeColor = null,
                HairColor = null,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow,
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.MeasurementsDb.Add(measurement5);

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

        #region SearchMeasurements Tests

        [Fact]
        public async Task SearchMeasurements_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchMeasurements_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchMeasurements_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchMeasurements_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Blue", result.Results[0].EyeColor);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchMeasurements_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchMeasurements_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchMeasurements_SearchesEyeColorField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "green",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 3, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Green", result.Results[0].EyeColor);
        }

        [Fact]
        public async Task SearchMeasurements_SearchesHairColorField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "blonde",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Blonde", result.Results[0].HairColor);
        }

        [Fact]
        public async Task SearchMeasurements_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "BLUE",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Blue", result.Results[0].EyeColor);
        }

        [Fact]
        public async Task SearchMeasurements_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  Blue  ",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Blue", result.Results[0].EyeColor);
        }

        [Fact]
        public async Task SearchMeasurements_HandlesNullEyeColorAndHairColor()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nonexistent",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act - Should not throw exception when EyeColor or HairColor is null
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchMeasurements_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "o", // Matches "Brown", "Blonde", "Brown" - multiple items
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.True(result.RemainingItems > 0);
        }

        [Fact]
        public async Task SearchMeasurements_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "brown",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Results.Count, result.TotalCount);
        }

        [Fact]
        public async Task SearchMeasurements_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "o", // Matches multiple items
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentDate = result.Results[i].Date;
                    DateTime nextDate = result.Results[i + 1].Date;
                    Assert.True(currentDate >= nextDate, "Results should be sorted in descending order by Date");
                }
            }
        }

        [Fact]
        public async Task SearchMeasurements_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "o", // Matches multiple items
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentDate = result.Results[i].Date;
                    DateTime nextDate = result.Results[i + 1].Date;
                    Assert.True(currentDate <= nextDate, "Results should be sorted in ascending order by Date");
                }
            }
        }

        [Fact]
        public async Task SearchMeasurements_SkipsItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "o",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // First get all results
            SearchRequest allRequest = new()
            {
                Query = "o",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Skip = 0
            };
            SearchResponse<Measurement> allResults = await _service.SearchMeasurements(allRequest, _testUser);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].MeasurementId, result.Results[0].MeasurementId);
            }
        }

        [Fact]
        public async Task SearchMeasurements_ReturnsAllItemsWhenNumberOfItemsIsZero()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "o",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchMeasurements_SearchesMultipleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "hazel",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 4, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 4, 2, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Hazel", result.Results[0].EyeColor);
        }

        [Fact]
        public async Task SearchMeasurements_ReturnsEmptyWhenNoMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nonexistentcolor12345",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchMeasurements_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "blue",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        [Fact]
        public async Task SearchMeasurements_MatchesPartialEyeColor()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "ow", // Matches "Brown"
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Results.Count > 0);
            Assert.All(result.Results, m =>
                Assert.True(
                    (m.EyeColor?.ToLower().Contains("ow") ?? false) ||
                    (m.HairColor?.ToLower().Contains("ow") ?? false)));
        }

        [Fact]
        public async Task SearchMeasurements_MatchesPartialHairColor()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "lon", // Matches "Blonde"
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("lon", result.Results[0].HairColor.ToLower());
        }

        [Fact]
        public async Task SearchMeasurements_DoesNotSearchFamilyItems()
        {
            // Arrange - Measurements don't have FamilyId support, so FamilyIds should be ignored
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchMeasurements_WithEmptyProgenyIds_ReturnsEmptyResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Blue",
                ProgenyIds = [],
                FamilyIds = [],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchMeasurements_MatchesEitherEyeColorOrHairColor()
        {
            // Arrange - "Red" only appears in HairColor for measurement 3
            SearchRequest request = new()
            {
                Query = "red",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 3, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Measurement> result = await _service.SearchMeasurements(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Red", result.Results[0].HairColor);
            Assert.Equal("Green", result.Results[0].EyeColor); // Confirms it matched HairColor, not EyeColor
        }

        #endregion
    }
}