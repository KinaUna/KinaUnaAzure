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
    public class SearchServiceTodoItemsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        private const int TestProgenyId = 1;
        private const int OtherProgenyId = 2;
        private const int TestFamilyId = 1;
        private const int OtherFamilyId = 2;

        public SearchServiceTodoItemsTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContexts
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
            // Add test users
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add progeny-based TodoItems
            TodoItem todo1 = new()
            {
                TodoItemId = 1,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Complete homework",
                Description = "Math assignment",
                Notes = "Due by Friday",
                Tags = "school,math",
                Context = "Education",
                Location = "Home",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo1);

            TodoItem todo2 = new()
            {
                TodoItemId = 2,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Doctor appointment",
                Description = "Annual checkup",
                Notes = "Bring vaccination records",
                Tags = "health,medical",
                Context = "Health",
                Location = "Clinic",
                Status = 1,
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                CreatedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo2);

            TodoItem todo3 = new()
            {
                TodoItemId = 3,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Deleted task",
                Description = "This is deleted",
                Notes = "",
                Tags = "deleted",
                Context = "",
                Location = "",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "user1",
                IsDeleted = true
            };
            _progenyDbContext.TodoItemsDb.Add(todo3);

            TodoItem todo4 = new()
            {
                TodoItemId = 4,
                ProgenyId = OtherProgenyId,
                FamilyId = 0,
                Title = "Other progeny task",
                Description = "Different progeny",
                Notes = "",
                Tags = "",
                Context = "",
                Location = "",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "user2",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo4);

            // Add family-based TodoItems
            TodoItem todo5 = new()
            {
                TodoItemId = 5,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family vacation planning",
                Description = "Book flights and hotel",
                Notes = "Check budget",
                Tags = "travel,vacation",
                Context = "Family",
                Location = "Online",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-4),
                CreatedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo5);

            TodoItem todo6 = new()
            {
                TodoItemId = 6,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Grocery shopping",
                Description = "Weekly groceries",
                Notes = "Don't forget milk",
                Tags = "shopping,food, home",
                Context = "Household",
                Location = "Supermarket",
                Status = 2,
                CreatedTime = DateTime.UtcNow.AddDays(-6),
                CreatedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo6);

            TodoItem todo7 = new()
            {
                TodoItemId = 7,
                ProgenyId = 0,
                FamilyId = OtherFamilyId,
                Title = "Other family task",
                Description = "Different family",
                Notes = "",
                Tags = "",
                Context = "",
                Location = "",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "user2",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo7);

            // Add a todo with HTML in description
            TodoItem todo8 = new()
            {
                TodoItemId = 8,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Task with HTML",
                Description = "<p>Important <strong>meeting</strong> notes</p>",
                Notes = "Plain text here",
                Tags = "html,test",
                Context = "Work",
                Location = "Home Office",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo8);

            _progenyDbContext.SaveChanges();
        }

        private void SetupDefaultMockBehaviors()
        {
            // Setup progeny permission
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(OtherProgenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Setup family permission
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(OtherFamilyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Setup item permissions
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Basic Functionality Tests

        [Fact]
        public async Task SearchTodoItems_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "homework"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchTodoItems_WhenQueryIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = null!
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = ""
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "   "
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Title Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesTitle_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "homework"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Complete homework", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchTodoItems_MatchesTitleCaseInsensitive_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "HOMEWORK"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Complete homework", result.Results[0].Title);
        }

        #endregion

        #region Description Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesDescription_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "checkup"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Doctor appointment", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchTodoItems_MatchesDescriptionAsPlainText_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "meeting"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Task with HTML", result.Results[0].Title);
        }

        #endregion

        #region Notes Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesNotes_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "vaccination"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Doctor appointment", result.Results[0].Title);
        }

        #endregion

        #region Tags Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesTags_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "school"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Complete homework", result.Results[0].Title);
        }

        #endregion

        #region Context Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesContext_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "Education"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Complete homework", result.Results[0].Title);
        }

        #endregion

        #region Location Search Tests

        [Fact]
        public async Task SearchTodoItems_MatchesLocation_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "Clinic"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Doctor appointment", result.Results[0].Title);
        }

        #endregion

        #region Progeny Access Tests

        [Fact]
        public async Task SearchTodoItems_WhenUserHasNoProgenyPermission_ExcludesThoseItems()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "homework"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_OnlyReturnsItemsFromAuthorizedProgenies()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId, OtherProgenyId],
                Query = "task"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.All(result.Results, item => Assert.Equal(TestProgenyId, item.ProgenyId));
            Assert.DoesNotContain(result.Results, item => item.ProgenyId == OtherProgenyId);
        }

        #endregion

        #region Family Search Tests

        [Fact]
        public async Task SearchTodoItems_SearchesFamilyItems_ReturnsMatchingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                FamilyIds = [TestFamilyId],
                Query = "vacation"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Family vacation planning", result.Results[0].Title);
            Assert.Equal(TestFamilyId, result.Results[0].FamilyId);
        }

        [Fact]
        public async Task SearchTodoItems_WhenUserHasNoFamilyPermission_ExcludesThoseItems()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchRequest request = new()
            {
                FamilyIds = [TestFamilyId],
                Query = "vacation"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_SearchesBothProgenyAndFamily_ReturnsCombinedResults()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [TestFamilyId],
                Query = "home"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Equal(3, result.Results.Count);
            Assert.Contains(result.Results, item => item.Location == "Home Office");
            Assert.Contains(result.Results, item => item.Context == "Household");
        }

        #endregion

        #region Deleted Items Tests

        [Fact]
        public async Task SearchTodoItems_ExcludesDeletedItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "deleted"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Empty(result.Results);
        }

        #endregion

        #region Item Permission Tests

        [Fact]
        public async Task SearchTodoItems_WhenUserHasNoItemPermission_ExcludesThoseItems()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "doctor"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Doctor appointment", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchTodoItems_SetsItemPermission_ForEachResult()
        {
            // Arrange
            TimelineItemPermission expectedPermission = new() { PermissionLevel = PermissionLevel.Edit };
            SetupDefaultMockBehaviors();
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(expectedPermission);

            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "homework"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task SearchTodoItems_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                Skip = 0,
                NumberOfItems = 1
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_WithSkip_SkipsItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                Skip = 1,
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_ReturnsTotalCount()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                NumberOfItems = 1
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchTodoItems_ReturnsRemainingItems()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                NumberOfItems = 1,
                Skip = 0
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Equal(1, result.RemainingItems);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task SearchTodoItems_DefaultSort_OrdersByNewestFirst()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                Sort = 0
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.True(result.Results[0].CreatedTime >= result.Results[^1].CreatedTime);
        }

        [Fact]
        public async Task SearchTodoItems_SortAscending_OrdersByOldestFirst()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "home",
                Sort = 1
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.True(result.Results[0].CreatedTime <= result.Results[^1].CreatedTime);
        }

        #endregion

        #region Query Trimming Tests

        [Fact]
        public async Task SearchTodoItems_TrimsQueryWhitespace()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "  homework  "
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Complete homework", result.Results[0].Title);
        }

        #endregion

        #region Empty Results Tests

        [Fact]
        public async Task SearchTodoItems_WhenNoMatches_ReturnsEmptyResults()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                Query = "nonexistentquery12345"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchTodoItems_WhenNoProgenyOrFamilyIdsProvided_ReturnsEmptyResults()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [],
                FamilyIds = [],
                Query = "homework"
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Empty(result.Results);
        }

        #endregion

        #region Search Request Preservation Tests

        [Fact]
        public async Task SearchTodoItems_PreservesSearchRequest()
        {
            // Arrange
            SetupDefaultMockBehaviors();
            SearchRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [TestFamilyId],
                Query = "homework",
                Skip = 5,
                NumberOfItems = 10,
                Sort = 1
            };

            // Act
            SearchResponse<TodoItem> result = await _service.SearchTodoItems(request, _testUser);

            // Assert
            Assert.Equal(request, result.SearchRequest);
            Assert.Equal(request.Query, result.SearchRequest.Query);
            Assert.Equal(request.Skip, result.SearchRequest.Skip);
            Assert.Equal(request.NumberOfItems, result.SearchRequest.NumberOfItems);
        }

        #endregion
    }
}