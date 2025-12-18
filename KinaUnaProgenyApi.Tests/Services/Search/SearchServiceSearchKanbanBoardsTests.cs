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
    public class SearchServiceSearchKanbanBoardsTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchKanbanBoardsTests()
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
            MediaDbContext mediaDbContext = new(mediaOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            Mock<ITimelineService> mockTimelineService = new();

            // Initialize service
            _service = new SearchService(
                _progenyDbContext,
                mediaDbContext,
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

            // Add test KanbanBoard records for progeny
            KanbanBoard board1 = new()
            {
                KanbanBoardId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Project Alpha",
                Description = "<p>Development tasks for alpha</p>",
                Tags = "development,sprint1",
                Context = "work",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(board1);

            KanbanBoard board2 = new()
            {
                KanbanBoardId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Project Beta",
                Description = "<p>Beta testing phase</p>",
                Tags = "testing,qa",
                Context = "work",
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(board2);

            KanbanBoard deletedBoard = new()
            {
                KanbanBoardId = 3,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Deleted Board",
                Description = "Should not appear in results",
                Tags = "project",
                Context = "work",
                CreatedTime = DateTime.UtcNow.AddDays(-15),
                IsDeleted = true
            };
            _progenyDbContext.KanbanBoardsDb.Add(deletedBoard);

            // Add test KanbanBoard records for family
            KanbanBoard familyBoard1 = new()
            {
                KanbanBoardId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Chores",
                Description = "<p>Weekly household tasks</p>",
                Tags = "chores,household",
                Context = "home",
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(familyBoard1);

            KanbanBoard familyBoard2 = new()
            {
                KanbanBoardId = 5,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Vacation Planning",
                Description = "<p>Project for summer vacation</p>",
                Tags = "vacation,planning",
                Context = "travel",
                CreatedTime = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(familyBoard2);

            KanbanBoard deletedFamilyBoard = new()
            {
                KanbanBoardId = 6,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Deleted Family Board",
                Description = "Should not appear",
                Tags = "project",
                Context = "home",
                CreatedTime = DateTime.UtcNow.AddDays(-20),
                IsDeleted = true
            };
            _progenyDbContext.KanbanBoardsDb.Add(deletedFamilyBoard);

            // Board for different progeny (to test filtering)
            KanbanBoard otherProgenyBoard = new()
            {
                KanbanBoardId = 7,
                ProgenyId = 2,
                FamilyId = 0,
                Title = "Other Progeny Project",
                Description = "Belongs to different progeny",
                Tags = "project",
                Context = "other",
                CreatedTime = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(otherProgenyBoard);

            _progenyDbContext.SaveChanges();
        }

        #region Basic Search Tests

        [Fact]
        public async Task SearchKanbanBoards_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project"
            };

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchKanbanBoards_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = ""
            };

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchKanbanBoards_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "   "
            };

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Progeny Search Tests

        [Fact]
        public async Task SearchKanbanBoards_WithProgenyId_ReturnsMatchingBoardsFromProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.All(result.Results, board => Assert.Equal(1, board.ProgenyId));
            Assert.All(result.Results, board => Assert.False(board.IsDeleted));
        }

        [Fact]
        public async Task SearchKanbanBoards_WithProgenyId_ExcludesDeletedBoards()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "deleted",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchKanbanBoards_WithNoProgenyPermission_ReturnsEmptyForProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchKanbanBoards_WithMultipleProgenies_SearchesAllAccessibleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1, 2],
                Query = "project",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupProgenyPermissions(2);
            SetupItemPermissions([1, 2, 7]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
        }

        #endregion

        #region Family Search Tests

        [Fact]
        public async Task SearchKanbanBoards_WithFamilyId_ReturnsMatchingBoardsFromFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                FamilyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            SetupFamilyPermissions(1);
            SetupItemPermissions([5]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results[0].FamilyId);
            Assert.Equal("Vacation Planning", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchKanbanBoards_WithFamilyId_ExcludesDeletedBoards()
        {
            // Arrange
            SearchRequest request = new()
            {
                FamilyIds = [1],
                Query = "deleted",
                NumberOfItems = 10
            };

            SetupFamilyPermissions(1);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchKanbanBoards_WithNoFamilyPermission_ReturnsEmptyForFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                FamilyIds = [1],
                Query = "chores",
                NumberOfItems = 10
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Combined Progeny and Family Search Tests

        [Fact]
        public async Task SearchKanbanBoards_WithBothProgenyAndFamily_CombinesResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                FamilyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupFamilyPermissions(1);
            SetupItemPermissions([1, 2, 5]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
            Assert.Contains(result.Results, b => b.ProgenyId == 1);
            Assert.Contains(result.Results, b => b.FamilyId == 1);
        }

        #endregion

        #region Search Field Tests

        [Fact]
        public async Task SearchKanbanBoards_SearchesInTitle()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "alpha",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Project Alpha", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchKanbanBoards_SearchesInDescription()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "development",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Development", result.Results[0].Description);
        }

        [Fact]
        public async Task SearchKanbanBoards_SearchesInTags()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "sprint1",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("sprint1", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchKanbanBoards_SearchesInContext()
        {
            // Arrange
            SearchRequest request = new()
            {
                FamilyIds = [1],
                Query = "travel",
                NumberOfItems = 10
            };

            SetupFamilyPermissions(1);
            SetupItemPermissions([5]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("travel", result.Results[0].Context);
        }

        [Fact]
        public async Task SearchKanbanBoards_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "ALPHA",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Project Alpha", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchKanbanBoards_TrimsQueryWhitespace()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "  alpha  ",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task SearchKanbanBoards_FiltersOutBoardsWithoutItemPermission()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            
            // Only grant access to board 1, not board 2
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, 1, 1, 0, _testUser))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results[0].KanbanBoardId);
        }

        [Fact]
        public async Task SearchKanbanBoards_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "alpha",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            
            TimelineItemPermission expectedPermission = new() { PermissionLevel = PermissionLevel.Edit };
            
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, 1, 1, 0, _testUser))
                .ReturnsAsync(expectedPermission);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task SearchKanbanBoards_ReturnsTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 1
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchKanbanBoards_SkipsItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                Skip = 1,
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task SearchKanbanBoards_CalculatesRemainingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 1
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RemainingItems);
        }

        [Fact]
        public async Task SearchKanbanBoards_CalculatesPageNumber()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                Skip = 1,
                NumberOfItems = 1
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PageNumber);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task SearchKanbanBoards_SortsByCreatedTimeDescending()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                Sort = 0, // Descending (newest first)
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.True(result.Results[0].CreatedTime > result.Results[1].CreatedTime);
        }

        [Fact]
        public async Task SearchKanbanBoards_SortsByCreatedTimeAscending()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                Sort = 1, // Ascending (oldest first)
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.True(result.Results[0].CreatedTime < result.Results[1].CreatedTime);
        }

        #endregion

        #region DescriptionAsPlainText Tests

        [Fact]
        public async Task SearchKanbanBoards_SearchesDescriptionAsPlainText()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "tasks",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("tasks", result.Results[0].Description.ToLower());
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SearchKanbanBoards_WithEmptyProgenyAndFamilyLists_ReturnsEmptyResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [],
                FamilyIds = [],
                Query = "project",
                NumberOfItems = 10
            };

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchKanbanBoards_WithNoMatchingResults_ReturnsEmptyList()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "nonexistentquery",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchKanbanBoards_ReturnsSearchRequestInResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "project",
                NumberOfItems = 10
            };

            SetupProgenyPermissions(1);
            SetupItemPermissions([1, 2]);

            // Act
            SearchResponse<KanbanBoard> result = await _service.SearchKanbanBoards(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request, result.SearchRequest);
        }

        #endregion

        #region Helper Methods

        private void SetupProgenyPermissions(int progenyId)
        {
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(progenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
        }

        private void SetupFamilyPermissions(int familyId)
        {
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(familyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
        }

        private void SetupItemPermissions(int[] boardIds)
        {
            foreach (int boardId in boardIds)
            {
                _mockAccessManagementService
                    .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, _testUser, PermissionLevel.View))
                    .ReturnsAsync(true);
                
                KanbanBoard? board = _progenyDbContext.KanbanBoardsDb.Find(boardId);
                if (board != null)
                {
                    _mockAccessManagementService
                        .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, board.ProgenyId, board.FamilyId, _testUser))
                        .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });
                }
            }
        }

        #endregion
    }
}