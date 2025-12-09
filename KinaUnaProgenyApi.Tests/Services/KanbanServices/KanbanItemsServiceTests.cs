using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.KanbanServices
{
    public class KanbanItemsServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly KanbanItemsService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public KanbanItemsServiceTests()
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
            _mockTodosService = new Mock<ITodosService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Initialize service
            _service = new KanbanItemsService(
                _progenyDbContext,
                _mockTodosService.Object,
                _mockAccessManagementService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test KanbanBoard records
            KanbanBoard testBoard1 = new()
            {
                KanbanBoardId = 1,
                UId = Guid.NewGuid().ToString(),
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Board 1",
                Description = "Test Description 1",
                Columns = "[{\"id\":1,\"name\":\"To Do\"},{\"id\":2,\"name\":\"In Progress\"},{\"id\":3,\"name\":\"Done\"}]",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "admin1",
                ModifiedBy = "admin1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(testBoard1);

            KanbanBoard testBoard2 = new()
            {
                KanbanBoardId = 2,
                UId = Guid.NewGuid().ToString(),
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Board 2",
                Description = "Test Description 2",
                Columns = "[{\"id\":1,\"name\":\"Backlog\"},{\"id\":2,\"name\":\"Active\"}]",
                CreatedTime = DateTime.UtcNow.AddDays(-20),
                ModifiedTime = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(testBoard2);

            // Add test TodoItem records
            TodoItem testTodo1 = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo 1",
                Description = "Test Todo Description 1",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(testTodo1);

            TodoItem testTodo2 = new()
            {
                TodoItemId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo 2",
                Description = "Test Todo Description 2",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedTime = DateTime.UtcNow.AddDays(-3),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(testTodo2);

            TodoItem testTodo3 = new()
            {
                TodoItemId = 3,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo 3",
                Description = "Test Todo Description 3",
                Status = 1,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = true
            };
            _progenyDbContext.TodoItemsDb.Add(testTodo3);

            TodoItem familyTodo = new()
            {
                TodoItemId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Todo",
                Description = "Family Todo Description",
                Status = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "admin1",
                ModifiedBy = "admin1",
                IsDeleted = false
            };
            _progenyDbContext.TodoItemsDb.Add(familyTodo);

            // Add test KanbanItem records
            KanbanItem testItem1 = new()
            {
                KanbanItemId = 1,
                UId = Guid.NewGuid().ToString(),
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 1,
                RowIndex = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(testItem1);

            KanbanItem testItem2 = new()
            {
                KanbanItemId = 2,
                UId = Guid.NewGuid().ToString(),
                KanbanBoardId = 1,
                TodoItemId = 2,
                ColumnId = 2,
                RowIndex = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedTime = DateTime.UtcNow.AddDays(-3),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(testItem2);

            KanbanItem deletedItem = new()
            {
                KanbanItemId = 3,
                UId = Guid.NewGuid().ToString(),
                KanbanBoardId = 1,
                TodoItemId = 3,
                ColumnId = 3,
                RowIndex = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = true
            };
            _progenyDbContext.KanbanItemsDb.Add(deletedItem);

            KanbanItem familyItem = new()
            {
                KanbanItemId = 4,
                UId = Guid.NewGuid().ToString(),
                KanbanBoardId = 2,
                TodoItemId = 4,
                ColumnId = 1,
                RowIndex = 0,
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "admin1",
                ModifiedBy = "admin1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(familyItem);

            _progenyDbContext.SaveChanges();
        }

        #region GetKanbanItemById Tests

        [Fact]
        public async Task GetKanbanItemById_WhenUserHasAccess_ReturnsItemWithTodoItem()
        {
            // Arrange
            int itemId = 1;
            TodoItem expectedTodo = new()
            {
                TodoItemId = 1,
                Title = "Test Todo 1",
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanItem, itemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(expectedTodo);

            // Act
            KanbanItem result = await _service.GetKanbanItemById(itemId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.KanbanItemId);
            Assert.NotNull(result.TodoItem);
            Assert.Equal("Test Todo 1", result.TodoItem.Title);
        }

        [Fact]
        public async Task GetKanbanItemById_WhenUserHasNoAccess_ReturnsEmptyItem()
        {
            // Arrange
            int itemId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanItem, itemId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            KanbanItem result = await _service.GetKanbanItemById(itemId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanItemId);
        }

        [Fact]
        public async Task GetKanbanItemById_WhenItemDoesNotExist_ReturnsEmptyItem()
        {
            // Arrange
            int itemId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanItem, itemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.GetKanbanItemById(itemId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanItemId);
        }

        #endregion

        #region AddKanbanItem Tests

        [Fact]
        public async Task AddKanbanItem_WhenUserHasProgenyAccess_AddsItem()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 1,
                ModifiedBy = "user1"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.KanbanItemId > 0);
            Assert.NotNull(result.UId);
            Assert.NotEqual(Guid.Empty.ToString(), result.UId);
            Assert.True(result.CreatedTime <= DateTime.UtcNow);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task AddKanbanItem_WhenUserHasFamilyAccess_AddsItem()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 2,
                TodoItemId = 4,
                ColumnId = 1,
                ModifiedBy = "user1"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(4, _testUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 2, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.KanbanItemId > 0);
        }

        [Fact]
        public async Task AddKanbanItem_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 1,
                ModifiedBy = "user2"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _otherUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(0, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddKanbanItem_WhenBoardDoesNotExist_ReturnsEmptyItem()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 999,
                TodoItemId = 1,
                ColumnId = 1,
                ModifiedBy = "user1"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanItemId);
        }

        [Fact]
        public async Task AddKanbanItem_WhenInvalidColumnId_SetsToFirstColumn()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 999,
                ModifiedBy = "user1"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ColumnId);
        }

        [Fact]
        public async Task AddKanbanItem_SetsCorrectRowIndex()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Test Todo"
            };

            KanbanItem newItem = new()
            {
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 1,
                ModifiedBy = "user1"
            };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todoItem);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.AddKanbanItem(newItem, _testUser);

            // Assert
            Assert.NotNull(result);
            // Should be 1 because there's already one item in column 1
            Assert.Equal(1, result.RowIndex);
        }

        #endregion

        #region UpdateKanbanItem Tests

        [Fact]
        public async Task UpdateKanbanItem_WhenUserHasAccess_UpdatesItem()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 1,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.ColumnId);
            Assert.Equal(1, result.RowIndex);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateKanbanItem_WhenUserHasNoTodoAccess_ReturnsNull()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 1,
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKanbanItem_WhenUserHasNoBoardAccess_ReturnsNull()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 1,
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKanbanItem_WhenItemDoesNotExist_ReturnsNull()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 999,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 1,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKanbanItem_WhenUIdIsNull_GeneratesNewUId()
        {
            // Arrange
            KanbanItem itemWithoutUId = new()
            {
                KanbanItemId = 5,
                UId = null,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 1,
                RowIndex = 0,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                CreatedBy = "user1",
                ModifiedBy = "user1",
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(itemWithoutUId);
            await _progenyDbContext.SaveChangesAsync();

            KanbanItem updatedItem = new()
            {
                KanbanItemId = 5,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 0,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UId);
            Assert.NotEmpty(result.UId);
        }

        [Fact]
        public async Task UpdateKanbanItem_WhenRowIndexNegative_SetsToLastPosition()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = -1,
                ModifiedBy = "user1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _testUser);

            // Assert
            Assert.NotNull(result);
            // Should be 1 because there's already one item in column 2
            Assert.Equal(1, result.RowIndex);
        }

        [Fact]
        public async Task UpdateKanbanItem_PreservesCreatedBy()
        {
            // Arrange
            KanbanItem updatedItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                TodoItemId = 1,
                ColumnId = 2,
                RowIndex = 1,
                ModifiedBy = "admin1"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.UpdateKanbanItem(updatedItem, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user1", result.CreatedBy);
        }

        #endregion

        #region DeleteKanbanItem Tests

        [Fact]
        public async Task DeleteKanbanItem_WhenUserHasAccess_SoftDeletesItem()
        {
            // Arrange
            KanbanItem? itemToDelete = await _progenyDbContext.KanbanItemsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.DeleteKanbanItem(itemToDelete, _adminUser, hardDelete: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.KanbanItemId);

            KanbanItem? deletedItem = await _progenyDbContext.KanbanItemsDb.FindAsync(1);
            Assert.NotNull(deletedItem);
            Assert.True(deletedItem.IsDeleted);
        }

        [Fact]
        public async Task DeleteKanbanItem_WhenHardDelete_PermanentlyDeletesItem()
        {
            // Arrange
            KanbanItem? itemToDelete = await _progenyDbContext.KanbanItemsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.DeleteKanbanItem(itemToDelete, _adminUser, hardDelete: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.KanbanItemId);

            KanbanItem? deletedItem = await _progenyDbContext.KanbanItemsDb.FindAsync(1);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task DeleteKanbanItem_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            KanbanItem? itemToDelete = await _progenyDbContext.KanbanItemsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            KanbanItem result = await _service.DeleteKanbanItem(itemToDelete, _otherUser);

            // Assert
            Assert.Null(result);

            KanbanItem? item = await _progenyDbContext.KanbanItemsDb.FindAsync(1);
            Assert.NotNull(item);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task DeleteKanbanItem_WhenItemDoesNotExist_ReturnsNull()
        {
            // Arrange
            KanbanItem nonExistentItem = new()
            {
                KanbanItemId = 999,
                TodoItemId = 1,
                KanbanBoardId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanItem result = await _service.DeleteKanbanItem(nonExistentItem, _adminUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetKanbanItemsForBoard Tests

        [Fact]
        public async Task GetKanbanItemsForBoard_WhenBoardExists_ReturnsItemsWithTodos()
        {
            // Arrange
            int boardId = 1;
            TodoItem todo1 = new() { TodoItemId = 1, Title = "Todo 1" };
            TodoItem todo2 = new() { TodoItemId = 2, Title = "Todo 2" };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todo1);
            _mockTodosService
                .Setup(x => x.GetTodoItem(2, _testUser))
                .ReturnsAsync(todo2);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForBoard(boardId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.NotNull(item.TodoItem));
            Assert.All(result, item => Assert.False(item.IsDeleted));
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_WhenIncludeDeleted_ReturnsDeletedItems()
        {
            // Arrange
            int boardId = 1;
            TodoItem todo1 = new() { TodoItemId = 1, Title = "Todo 1" };
            TodoItem todo2 = new() { TodoItemId = 2, Title = "Todo 2" };
            TodoItem todo3 = new() { TodoItemId = 3, Title = "Todo 3" };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todo1);
            _mockTodosService
                .Setup(x => x.GetTodoItem(2, _testUser))
                .ReturnsAsync(todo2);
            _mockTodosService
                .Setup(x => x.GetTodoItem(3, _testUser))
                .ReturnsAsync(todo3);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForBoard(boardId, _testUser, includeDeleted: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, item => item.IsDeleted);
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_WhenTodoItemNotFound_ExcludesItem()
        {
            // Arrange
            int boardId = 1;
            TodoItem todo1 = new() { TodoItemId = 1, Title = "Todo 1" };
            TodoItem emptyTodo = new() { TodoItemId = 0 };

            _mockTodosService
                .Setup(x => x.GetTodoItem(1, _testUser))
                .ReturnsAsync(todo1);
            _mockTodosService
                .Setup(x => x.GetTodoItem(2, _testUser))
                .ReturnsAsync(emptyTodo);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForBoard(boardId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].TodoItemId);
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_WhenNoBoardExists_ReturnsEmptyList()
        {
            // Arrange
            int boardId = 999;

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForBoard(boardId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetKanbanItemsForTodoItem Tests

        [Fact]
        public async Task GetKanbanItemsForTodoItem_WhenUserHasAccess_ReturnsItems()
        {
            // Arrange
            int todoItemId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForTodoItem(todoItemId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(todoItemId, result[0].TodoItemId);
            Assert.False(result[0].IsDeleted);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_WhenUserHasNoAccess_ReturnsEmptyList()
        {
            // Arrange
            int todoItemId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItemId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForTodoItem(todoItemId, _otherUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_WhenIncludeDeleted_ReturnsDeletedItems()
        {
            // Arrange
            int todoItemId = 3;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForTodoItem(todoItemId, _testUser, includeDeleted: true);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].IsDeleted);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_WhenNoItemsExist_ReturnsEmptyList()
        {
            // Arrange
            int todoItemId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForTodoItem(todoItemId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_ExcludesDeletedByDefault()
        {
            // Arrange
            int todoItemId = 3;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItemId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<KanbanItem> result = await _service.GetKanbanItemsForTodoItem(todoItemId, _testUser, includeDeleted: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}