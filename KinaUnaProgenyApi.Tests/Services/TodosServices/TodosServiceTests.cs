using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.TodosServices
{
    public class TodosServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<ISubtasksService> _mockSubtasksService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly TodosService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public TodosServiceTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _adminUser = new UserInfo { UserId = "admin1", UserEmail = "admin@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContext (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockSubtasksService = new Mock<ISubtasksService>();
            _mockKinaUnaCacheService = new Mock<IKinaUnaCacheService>();
            _mockDistributedCache = new Mock<IDistributedCache>();

            // Setup default mock behaviors
            SetupDefaultMockBehaviors();

            // Initialize service
            _service = new TodosService(
                _progenyDbContext,
                _mockAccessManagementService.Object,
                _mockSubtasksService.Object,
                _mockKinaUnaCacheService.Object,
                _mockDistributedCache.Object);

            // Seed test data
            SeedTestData();
        }

        private void SetupDefaultMockBehaviors()
        {
            // Default behavior for IDistributedCache - return null (cache miss)
            _mockDistributedCache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            _mockDistributedCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockDistributedCache
                .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default behavior for ISubtasksService - return empty list
            _mockSubtasksService
                .Setup(x => x.GetSubtasksForTodoItem(It.IsAny<int>(), It.IsAny<UserInfo>()))
                .ReturnsAsync(new List<TodoItem>());

            // Default behavior for IKinaUnaCacheService - return null (cache miss)
            _mockKinaUnaCacheService
                .Setup(x => x.GetTodosListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((TodosListCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.SetTodoItemsListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TodoItem[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);

            // Default behavior for IAccessManagementService - permission handling
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), It.IsAny<UserInfo>()))
                .ReturnsAsync([]);

            _mockAccessManagementService
                .Setup(x => x.CopyItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<TimelineItemPermission>>(), It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<UserInfo>()))
                .ReturnsAsync([]);

            _mockAccessManagementService
                .Setup(x => x.RevokeItemPermission(It.IsAny<TimelineItemPermission>(), It.IsAny<UserInfo>()))
                .ReturnsAsync(true);
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test TodoItem records
            TodoItem todo1 = new()
            {
                TodoItemId = 1,
                Title = "Todo 1",
                Description = "Description 1",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                DueDate = DateTime.UtcNow.AddDays(5),
                Tags = "tag1,tag2",
                Context = "home",
                Location = "kitchen",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo1);

            TodoItem todo2 = new()
            {
                TodoItemId = 2,
                Title = "Todo 2",
                Description = "Description 2",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                StartDate = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(3),
                Tags = "tag2,tag3",
                Context = "work",
                Location = "office",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedTime = DateTime.UtcNow.AddDays(-8),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(todo2);

            TodoItem deletedTodo = new()
            {
                TodoItemId = 3,
                Title = "Deleted Todo",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                IsDeleted = true
            };
            _progenyDbContext.TodoItemsDb.Add(deletedTodo);

            TodoItem familyTodo = new()
            {
                TodoItemId = 4,
                Title = "Family Todo",
                ProgenyId = 0,
                FamilyId = 1,
                ParentTodoItemId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                StartDate = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(-1),
                CompletedDate = DateTime.UtcNow,
                CreatedBy = "admin1",
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                ModifiedTime = DateTime.UtcNow,
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(familyTodo);

            // Add subtasks for calculating subtask counts
            TodoItem subtask1 = new()
            {
                TodoItemId = 5,
                Title = "Subtask 1",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-9),
                ModifiedTime = DateTime.UtcNow.AddDays(-9),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(subtask1);

            TodoItem subtask2 = new()
            {
                TodoItemId = 6,
                Title = "Subtask 2",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-9),
                ModifiedTime = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(subtask2);

            _progenyDbContext.SaveChanges();
        }

        #region AddTodoItem Tests

        [Fact]
        public async Task AddTodoItem_WhenUserHasProgenyAccess_AddsTodoItem()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "New Todo",
                Description = "New Description",
                ProgenyId = 1,
                FamilyId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TodoItemId > 0);
            Assert.Equal("New Todo", result.Title);
            Assert.Equal("New Description", result.Description);
            Assert.Equal("user1", result.CreatedBy);
            Assert.Equal("user1", result.ModifiedBy);
            Assert.False(result.IsDeleted);
            Assert.True(result.CreatedTime <= DateTime.UtcNow);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task AddTodoItem_WhenUserHasFamilyAccess_AddsTodoItem()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "New Family Todo",
                ProgenyId = 0,
                FamilyId = 1,
                CreatedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TodoItemId > 0);
            Assert.Equal("New Family Todo", result.Title);
            Assert.Equal(0, result.ProgenyId);
            Assert.Equal(1, result.FamilyId);
        }

        [Fact]
        public async Task AddTodoItem_WhenUserIsNull_ReturnsNull()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "New Todo",
                ProgenyId = 1,
                CreatedBy = "user1"
            };

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddTodoItem_WhenBothProgenyIdAndFamilyIdAreSet_ReturnsNull()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "Invalid Todo",
                ProgenyId = 1,
                FamilyId = 1,
                CreatedBy = "user1"
            };

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddTodoItem_WhenNeitherProgenyIdNorFamilyIdAreSet_ReturnsNull()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "Invalid Todo",
                ProgenyId = 0,
                FamilyId = 0,
                CreatedBy = "user1"
            };

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddTodoItem_WhenUserHasNoProgenyAccess_ReturnsNull()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "New Todo",
                ProgenyId = 1,
                FamilyId = 0,
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddTodoItem_WhenUserHasNoFamilyAccess_ReturnsNull()
        {
            // Arrange
            TodoItem newTodo = new()
            {
                Title = "New Family Todo",
                ProgenyId = 0,
                FamilyId = 1,
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.AddTodoItem(newTodo, _otherUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteTodoItem Tests

        [Fact]
        public async Task DeleteTodoItem_WhenUserHasAccess_SoftDeletesByDefault()
        {
            // Arrange
            TodoItem todoToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteTodoItem(todoToDelete, _adminUser);

            // Assert
            Assert.True(result);

            TodoItem? deletedTodo = await _progenyDbContext.TodoItemsDb.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
            Assert.NotNull(deletedTodo);
            Assert.True(deletedTodo.IsDeleted);
            Assert.Equal("admin1", deletedTodo.ModifiedBy);
        }

        [Fact]
        public async Task DeleteTodoItem_WhenHardDelete_PermanentlyDeletesTodoItem()
        {
            // Arrange
            TodoItem todoToDelete = new()
            {
                TodoItemId = 2,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteTodoItem(todoToDelete, _adminUser, hardDelete: true);

            // Assert
            Assert.True(result);

            TodoItem? deletedTodo = await _progenyDbContext.TodoItemsDb.FindAsync(2);
            Assert.Null(deletedTodo);
        }

        [Fact]
        public async Task DeleteTodoItem_WhenUserIsNull_ReturnsFalse()
        {
            // Arrange
            TodoItem todoToDelete = new()
            {
                TodoItemId = 1
            };

            // Act
            bool result = await _service.DeleteTodoItem(todoToDelete, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteTodoItem_WhenUserHasNoAccess_ReturnsFalse()
        {
            // Arrange
            TodoItem todoToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            bool result = await _service.DeleteTodoItem(todoToDelete, _otherUser);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteTodoItem_WhenTodoDoesNotExist_ReturnsFalse()
        {
            // Arrange
            TodoItem todoToDelete = new()
            {
                TodoItemId = 999,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteTodoItem(todoToDelete, _adminUser);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetTodoItem Tests

        [Fact]
        public async Task GetTodoItem_WhenUserHasAccess_ReturnsTodoItemWithSubtaskCounts()
        {
            // Arrange
            int todoId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            List<TodoItem> subtasks = [
                new TodoItem { TodoItemId = 5, Status = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                new TodoItem { TodoItemId = 6, Status = (int)KinaUnaTypes.TodoStatusType.Completed }
            ];

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, todoId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);
            _mockSubtasksService
                .Setup(x => x.GetSubtasksForTodoItem(todoId, _testUser))
                .ReturnsAsync(subtasks);

            // Act
            TodoItem result = await _service.GetTodoItem(todoId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(todoId, result.TodoItemId);
            Assert.Equal("Todo 1", result.Title);
            Assert.Equal(2, result.SubtaskCount);
            Assert.Equal(1, result.CompletedSubtaskCount);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetTodoItem_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            int todoId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.GetTodoItem(todoId, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTodoItem_WhenTodoDoesNotExist_ReturnsNull()
        {
            // Arrange
            int todoId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.GetTodoItem(todoId, _testUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetTodosForProgenyOrFamily Tests

        [Fact]
        public async Task GetTodosForProgenyOrFamily_ReturnsNonDeletedTodosWithAccess()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.False(item.IsDeleted));
            Assert.All(result, item => Assert.Equal(0, item.ParentTodoItemId));
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithStartDateFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            DateTime startDate = DateTime.UtcNow.AddDays(4);
            TodoItemsRequest request = new()
            {
                StartDate = startDate,
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithEndDateFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            DateTime endDate = DateTime.UtcNow.AddDays(4);
            TodoItemsRequest request = new()
            {
                EndDate = endDate,
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithLocationFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                LocationFilter = "office",
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, item => Assert.Contains("office", item.Location));
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithTagFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                TagFilter = "tag1",
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, item => Assert.Contains("tag1", item.Tags));
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithContextFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                ContextFilter = "home",
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, item => Assert.Contains("home", item.Context));
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_WithStatusFilter_FiltersCorrectly()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                StatusFilter = [KinaUnaTypes.TodoStatusType.InProgress],
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, item => Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, item.Status));
        }

        [Fact]
        public async Task GetTodosForProgenyOrFamily_FiltersOutItemsWithoutAccess()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TodoItemsRequest request = new()
            {
                NumberOfItems = 10
            };
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetTodosForProgenyOrFamily(progenyId, familyId, _testUser, request);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].TodoItemId);
        }

        #endregion

        #region CreateTodoItemsResponseForTodoPage Tests

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_ReturnsResponseWithCorrectMetadata()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1, Title = "Test 1", DueDate = DateTime.UtcNow.AddDays(1), Tags = "tag1,tag2", Context = "home" },
                new TodoItem { TodoItemId = 2, Title = "Test 2", DueDate = DateTime.UtcNow.AddDays(2), Tags = "tag2,tag3", Context = "work" }
            ];
            TodoItemsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(1, response.TotalPages);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(2, response.TodoItems.Count);
            Assert.Contains("tag1", response.TagsList);
            Assert.Contains("tag2", response.TagsList);
            Assert.Contains("tag3", response.TagsList);
            Assert.Contains("home", response.ContextsList);
            Assert.Contains("work", response.ContextsList);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1, DueDate = DateTime.UtcNow.AddDays(1) },
                new TodoItem { TodoItemId = 2, DueDate = DateTime.UtcNow.AddDays(2) },
                new TodoItem { TodoItemId = 3, DueDate = DateTime.UtcNow.AddDays(3) }
            ];
            TodoItemsRequest request = new()
            {
                Skip = 1,
                NumberOfItems = 1
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.Equal(3, response.TotalItems);
            Assert.Equal(3, response.TotalPages);
            Assert.Equal(2, response.PageNumber);
            Assert.Single(response.TodoItems);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_SortByDueDate_Ascending_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1, DueDate = DateTime.UtcNow.AddDays(3), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 2, DueDate = DateTime.UtcNow.AddDays(1), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 3, DueDate = DateTime.UtcNow.AddDays(2), CreatedTime = DateTime.UtcNow }
            ];
            TodoItemsRequest request = new()
            {
                SortBy = 0,
                Sort = 0,
                NumberOfItems = 10
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.Equal(2, response.TodoItems[0].TodoItemId);
            Assert.Equal(3, response.TodoItems[1].TodoItemId);
            Assert.Equal(1, response.TodoItems[2].TodoItemId);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_SortByDueDate_Descending_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1, DueDate = DateTime.UtcNow.AddDays(3), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 2, DueDate = DateTime.UtcNow.AddDays(1), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 3, DueDate = DateTime.UtcNow.AddDays(2), CreatedTime = DateTime.UtcNow }
            ];
            TodoItemsRequest request = new()
            {
                SortBy = 0,
                Sort = 1,
                NumberOfItems = 10
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.Equal(1, response.TodoItems[0].TodoItemId);
            Assert.Equal(3, response.TodoItems[1].TodoItemId);
            Assert.Equal(2, response.TodoItems[2].TodoItemId);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_GroupByStatus_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1, Status = 2, DueDate = DateTime.UtcNow.AddDays(1), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 2, Status = 0, DueDate = DateTime.UtcNow.AddDays(2), CreatedTime = DateTime.UtcNow },
                new TodoItem { TodoItemId = 3, Status = 1, DueDate = DateTime.UtcNow.AddDays(3), CreatedTime = DateTime.UtcNow }
            ];
            TodoItemsRequest request = new()
            {
                GroupBy = 1,
                NumberOfItems = 10
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.Equal(0, response.TodoItems[0].Status);
            Assert.Equal(1, response.TodoItems[1].Status);
            Assert.Equal(2, response.TodoItems[2].Status);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_WithNoPagination_ReturnsAllItems()
        {
            // Arrange
            List<TodoItem> todos = [
                new TodoItem { TodoItemId = 1 },
                new TodoItem { TodoItemId = 2 },
                new TodoItem { TodoItemId = 3 }
            ];
            TodoItemsRequest request = new()
            {
                NumberOfItems = 0
            };

            // Act
            TodoItemsResponse response = _service.CreateTodoItemsResponseForTodoPage(todos, request);

            // Assert
            Assert.Equal(3, response.TodoItems.Count);
        }

        #endregion

        #region GetTodosList Tests

        [Fact]
        public async Task GetTodosList_ReturnsNonDeletedTodosWithAccess()
        {
            // Arrange
            int progenyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<TodoItem> result = await _service.GetTodosList(progenyId, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only parent todos, not subtasks
            Assert.All(result, item => Assert.False(item.IsDeleted));
        }

        [Fact]
        public async Task GetTodosList_FiltersOutItemsWithoutAccess()
        {
            // Arrange
            int progenyId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.Is<int>(id => id != 1), _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<TodoItem> result = await _service.GetTodosList(progenyId, 0, _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].TodoItemId);
        }

        [Fact]
        public async Task GetTodosList_WhenNoTodosExist_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 999;

            // Act
            List<TodoItem> result = await _service.GetTodosList(progenyId, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UpdateTodoItem Tests

        [Fact]
        public async Task UpdateTodoItem_WhenUserHasAccess_UpdatesTodoItem()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, result.Status);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenUserIsNull_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Title = "Updated Title"
            };

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenBothProgenyIdAndFamilyIdAreSet_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 1,
                Title = "Updated Title"
            };

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenNeitherProgenyIdNorFamilyIdAreSet_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 0,
                FamilyId = 0,
                Title = "Updated Title"
            };

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title",
                ModifiedBy = "user2"
            };

            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny"
            };
            _progenyDbContext.ProgenyDb.Add(progeny);
            await _progenyDbContext.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenTodoDoesNotExist_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 999,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenStatusChangesToCompleted_SetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.CompletedDate);
            Assert.True(result.CompletedDate.Value <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenStatusChangesToNotStarted_ResetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenStatusChangesToInProgress_SetsStartDateAndResetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.StartDate);
            Assert.Null(result.CompletedDate);
            Assert.True(result.StartDate.Value <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateTodoItem_WhenStatusChangesToCancelled_ResetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Cancelled,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 4, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 4, _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateTodoItem(updateValues, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        #endregion
    }
}