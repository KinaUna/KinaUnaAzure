using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.Subtasks
{
    public class SubtasksServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly SubtasksService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public SubtasksServiceTests()
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
            _mockKinaUnaCacheService = new Mock<IKinaUnaCacheService>();
            _mockDistributedCache = new Mock<IDistributedCache>();

            // Setup default cache behavior to return null (cache miss)
            _mockDistributedCache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Initialize service
            _service = new SubtasksService(
                _progenyDbContext,
                _mockAccessManagementService.Object,
                _mockKinaUnaCacheService.Object,
                _mockDistributedCache.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test parent TodoItem records
            TodoItem parentTodo1 = new()
            {
                TodoItemId = 100,
                Title = "Parent Todo 1",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 0,
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(parentTodo1);

            TodoItem parentTodo2 = new()
            {
                TodoItemId = 200,
                Title = "Parent Todo 2",
                ProgenyId = 0,
                FamilyId = 1,
                ParentTodoItemId = 0,
                CreatedBy = "admin1",
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(parentTodo2);

            // Add test subtask records
            TodoItem subtask1 = new()
            {
                TodoItemId = 1,
                Title = "Subtask 1",
                Description = "Test Description 1",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 100,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                StartDate = null,
                DueDate = DateTime.UtcNow.AddDays(5),
                CompletedDate = null,
                Tags = "tag1,tag2",
                Context = "home",
                Location = "kitchen",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-8),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(subtask1);

            TodoItem subtask2 = new()
            {
                TodoItemId = 2,
                Title = "Subtask 2",
                Description = "Test Description 2",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 100,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                StartDate = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(3),
                CompletedDate = null,
                Tags = "tag2,tag3",
                Context = "work",
                Location = "office",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-6),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-6),
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(subtask2);

            TodoItem deletedSubtask = new()
            {
                TodoItemId = 3,
                Title = "Deleted Subtask",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 100,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-4),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                IsDeleted = true
            };
            _progenyDbContext.TodoItemsDb.Add(deletedSubtask);

            TodoItem familySubtask = new()
            {
                TodoItemId = 4,
                Title = "Family Subtask",
                ProgenyId = 0,
                FamilyId = 1,
                ParentTodoItemId = 200,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                StartDate = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(-1),
                CompletedDate = DateTime.UtcNow,
                CreatedBy = "admin1",
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                ModifiedBy = "admin1",
                ModifiedTime = DateTime.UtcNow,
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(familySubtask);

            _progenyDbContext.SaveChanges();
        }

        #region AddSubtask Tests

        [Fact]
        public async Task AddSubtask_WhenUserHasProgenyAccess_AddsSubtask()
        {
            // Arrange
            TodoItem newSubtask = new()
            {
                Title = "New Subtask",
                Description = "New Description",
                ProgenyId = 1,
                FamilyId = 0,
                ParentTodoItemId = 100,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, 100, _testUser))
                .ReturnsAsync([]);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TodoItemId > 0);
            Assert.Equal("New Subtask", result.Title);
            Assert.Equal("New Description", result.Description);
            Assert.Equal("user1", result.CreatedBy);
            Assert.Equal("user1", result.ModifiedBy);
            Assert.False(result.IsDeleted);
            Assert.True(result.CreatedTime <= DateTime.UtcNow);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);

            // Verify cache operations were called
            _mockDistributedCache.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem), Times.Once);
        }

        [Fact]
        public async Task AddSubtask_WhenUserHasFamilyAccess_AddsSubtask()
        {
            // Arrange
            TodoItem newSubtask = new()
            {
                Title = "New Family Subtask",
                ProgenyId = 0,
                FamilyId = 1,
                ParentTodoItemId = 200,
                CreatedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, 200, _adminUser))
                .ReturnsAsync([]);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(0, 1, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TodoItemId > 0);
            Assert.Equal("New Family Subtask", result.Title);
        }

        [Fact]
        public async Task AddSubtask_WhenUserIsNull_ReturnsNull()
        {
            // Arrange
            TodoItem newSubtask = new()
            {
                Title = "New Subtask",
                ProgenyId = 1,
                CreatedBy = "user1"
            };

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddSubtask_WhenUserHasNoProgenyAccess_ReturnsNull()
        {
            // Arrange
            TodoItem newSubtask = new()
            {
                Title = "New Subtask",
                ProgenyId = 1,
                FamilyId = 0,
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddSubtask_WhenUserHasNoFamilyAccess_ReturnsNull()
        {
            // Arrange
            TodoItem newSubtask = new()
            {
                Title = "New Family Subtask",
                ProgenyId = 0,
                FamilyId = 1,
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddSubtask_SetsTimestampsCorrectly()
        {
            // Arrange
            DateTime beforeAdd = DateTime.UtcNow;
            TodoItem newSubtask = new()
            {
                Title = "Timestamp Test",
                ProgenyId = 1,
                ParentTodoItemId = 100,
                CreatedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, 100, _testUser))
                .ReturnsAsync([]);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.AddSubtask(newSubtask, _testUser);
            DateTime afterAdd = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedTime >= beforeAdd && result.CreatedTime <= afterAdd);
            Assert.True(result.ModifiedTime >= beforeAdd && result.ModifiedTime <= afterAdd);
        }

        #endregion

        #region GetSubtask Tests

        [Fact]
        public async Task GetSubtask_WhenUserHasAccess_ReturnsSubtask()
        {
            // Arrange
            int subtaskId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, subtaskId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, subtaskId, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            TodoItem result = await _service.GetSubtask(subtaskId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(subtaskId, result.TodoItemId);
            Assert.Equal("Subtask 1", result.Title);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetSubtask_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            int subtaskId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, subtaskId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.GetSubtask(subtaskId, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSubtask_WhenSubtaskDoesNotExist_ReturnsNull()
        {
            // Arrange
            int subtaskId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, subtaskId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.GetSubtask(subtaskId, _testUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetSubtasksForTodoItem Tests

        [Fact]
        public async Task GetSubtasksForTodoItem_ReturnsNonDeletedSubtasksWithAccess()
        {
            // Arrange
            int parentTodoId = 100;
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
            List<TodoItem> result = await _service.GetSubtasksForTodoItem(parentTodoId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.False(item.IsDeleted));
            Assert.All(result, item => Assert.Equal(parentTodoId, item.ParentTodoItemId));
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_FiltersOutDeletedSubtasks()
        {
            // Arrange
            int parentTodoId = 100;
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            List<TodoItem> result = await _service.GetSubtasksForTodoItem(parentTodoId, _testUser);

            // Assert
            Assert.DoesNotContain(result, item => item.TodoItemId == 3); // Deleted subtask
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_FiltersOutSubtasksWithoutAccess()
        {
            // Arrange
            int parentTodoId = 100;
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
            List<TodoItem> result = await _service.GetSubtasksForTodoItem(parentTodoId, _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].TodoItemId);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_WhenNoSubtasksExist_ReturnsEmptyList()
        {
            // Arrange
            int parentTodoId = 999;

            // Act
            List<TodoItem> result = await _service.GetSubtasksForTodoItem(parentTodoId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UpdateSubtask Tests

        [Fact]
        public async Task UpdateSubtask_WhenUserHasAccess_UpdatesSubtask()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                ModifiedBy = "user1",
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, result.Status);

            // Verify cache operations
            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem), Times.Once);
        }

        [Fact]
        public async Task UpdateSubtask_WhenUserIsNull_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Title = "Updated Title"
            };

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSubtask_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Title = "Updated Title",
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSubtask_WhenSubtaskDoesNotExist_ReturnsNull()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 999,
                Title = "Updated Title"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSubtask_WhenStatusChangesToCompleted_SetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.CompletedDate);
            Assert.True(result.CompletedDate.Value <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateSubtask_WhenStatusChangesToNotStarted_ResetsCompletedDateAndStartDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 2,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateSubtask_WhenStatusChangesToInProgress_SetsStartDateAndResetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.StartDate);
            Assert.Null(result.CompletedDate);
            Assert.True(result.StartDate.Value <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateSubtask_WhenStatusChangesToCancelled_ResetsCompletedDate()
        {
            // Arrange
            TodoItem updateValues = new()
            {
                TodoItemId = 4,
                Status = (int)KinaUnaTypes.TodoStatusType.Cancelled,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 4, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(0, 1, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateSubtask_WhenStatusUnchanged_PreservesCompletedDate()
        {
            // Arrange
            await _progenyDbContext.TodoItemsDb.FindAsync([4], TestContext.Current.CancellationToken);

            TodoItem updateValues = new()
            {
                TodoItemId = 4,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                Title = "Updated Family Subtask",
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 4, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(0, 1, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            TodoItem result = await _service.UpdateSubtask(updateValues, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Family Subtask", result.Title);
        }

        #endregion

        #region DeleteSubtask Tests

        [Fact]
        public async Task DeleteSubtask_WhenUserHasAccess_SoftDeletesByDefault()
        {
            // Arrange
            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser))
                .ReturnsAsync([]);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            bool result = await _service.DeleteSubtask(subtaskToDelete, _adminUser);

            // Assert
            Assert.True(result);

            TodoItem? deletedSubtask = await _progenyDbContext.TodoItemsDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.NotNull(deletedSubtask);
            Assert.True(deletedSubtask.IsDeleted);
            Assert.Equal("admin1", deletedSubtask.ModifiedBy);
            Assert.True(deletedSubtask.ModifiedTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task DeleteSubtask_WhenHardDelete_PermanentlyDeletesSubtask()
        {
            // Arrange
            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 2,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 2, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, 2, _adminUser))
                .ReturnsAsync([]);
            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.TodoItem))
                .Returns(Task.CompletedTask);

            // Act
            bool result = await _service.DeleteSubtask(subtaskToDelete, _adminUser, hardDelete: true);

            // Assert
            Assert.True(result);

            TodoItem? deletedSubtask = await _progenyDbContext.TodoItemsDb.FindAsync([2], TestContext.Current.CancellationToken);
            Assert.Null(deletedSubtask);
        }

        [Fact]
        public async Task DeleteSubtask_WhenUserIsNull_ReturnsFalse()
        {
            // Arrange
            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 1
            };

            // Act
            bool result = await _service.DeleteSubtask(subtaskToDelete, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteSubtask_WhenUserHasNoAccess_ReturnsFalse()
        {
            // Arrange
            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            bool result = await _service.DeleteSubtask(subtaskToDelete, _otherUser);

            // Assert
            Assert.False(result);

            TodoItem? subtask = await _progenyDbContext.TodoItemsDb.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.NotNull(subtask);
            Assert.False(subtask.IsDeleted);
        }

        [Fact]
        public async Task DeleteSubtask_WhenSubtaskDoesNotExist_ReturnsFalse()
        {
            // Arrange
            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 999,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            bool result = await _service.DeleteSubtask(subtaskToDelete, _adminUser);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CreateSubtaskResponseForTodoItem Tests

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithValidRequest_ReturnsFilteredResponse()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(100, response.ParentTodoItemId);
            Assert.Equal(2, response.Subtasks.Count);
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(1, response.TotalPages);
        }

        [Fact]
        public void CreateSubtaskResponseForTodoItem_WhenRequestIsNull_ThrowsArgumentException()
        {
            // Arrange
            List<TodoItem> subtasks = [];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CreateSubtaskResponseForTodoItem(subtasks, null, _testUser));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithStartDateFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            DateTime startDate = DateTime.UtcNow.AddDays(-3);
            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                StartYear = startDate.Year,
                StartMonth = startDate.Month,
                StartDay = startDate.Day,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.All(response.Subtasks, s => Assert.True(s.StartDate >= request.StartDate));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithEndDateFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            DateTime endDate = DateTime.UtcNow.AddDays(4);
            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                EndYear = endDate.Year,
                EndMonth = endDate.Month,
                EndDay = endDate.Day,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.All(response.Subtasks, s => Assert.True(s.DueDate <= request.EndDate));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithTagFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                TagFilter = "tag2",
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.Subtasks.Count);
            Assert.All(response.Subtasks, s => Assert.Contains("tag2", s.Tags));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithContextFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                ContextFilter = "home",
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.All(response.Subtasks, s => Assert.Contains("home", s.Context));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithLocationFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                LocationFilter = "office",
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.All(response.Subtasks, s => Assert.Contains("office", s.Location));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithStatusFilter_FiltersCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                StatusFilter = [KinaUnaTypes.TodoStatusType.InProgress],
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.All(response.Subtasks, s => Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, s.Status));
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                Skip = 1,
                NumberOfItems = 1
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Subtasks);
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(2, response.PageNumber);
            Assert.Equal(2, response.TotalPages);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithGroupByStatus_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                GroupBy = 1,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.Subtasks.Count);
            Assert.True(response.Subtasks[0].Status <= response.Subtasks[1].Status);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithGroupByProgeny_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                GroupBy = 2,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.Subtasks.Count);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_WithGroupByLocation_SortsCorrectly()
        {
            // Arrange
            List<TodoItem> subtasks = await _progenyDbContext.TodoItemsDb
                .Where(t => t.ParentTodoItemId == 100 && !t.IsDeleted)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            SubtasksRequest request = new()
            {
                ParentTodoItemId = 100,
                GroupBy = 3,
                NumberOfItems = 10
            };

            // Act
            SubtasksResponse response = _service.CreateSubtaskResponseForTodoItem(subtasks, request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.Subtasks.Count);
        }

        #endregion
    }
}