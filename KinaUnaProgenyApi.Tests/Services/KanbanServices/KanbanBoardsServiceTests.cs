using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.KanbanServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.KanbanServices
{
    public class KanbanBoardsServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly KanbanBoardsService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public KanbanBoardsServiceTests()
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

            // Initialize service
            _service = new KanbanBoardsService(
                _progenyDbContext,
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
                AccessLevel = 1,
                Tags = "tag1,tag2",
                Context = "context1",
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
                AccessLevel = 2,
                Tags = "tag3",
                Context = "context2",
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(testBoard2);

            KanbanBoard deletedBoard = new()
            {
                KanbanBoardId = 3,
                UId = Guid.NewGuid().ToString(),
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Deleted Board",
                Description = "Deleted Description",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]",
                CreatedTime = DateTime.UtcNow.AddDays(-30),
                ModifiedTime = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "admin1",
                ModifiedBy = "admin1",
                AccessLevel = 1,
                IsDeleted = true
            };
            _progenyDbContext.KanbanBoardsDb.Add(deletedBoard);

            KanbanBoard familyBoard = new()
            {
                KanbanBoardId = 4,
                UId = Guid.NewGuid().ToString(),
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Board",
                Description = "Family Description",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]",
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "admin1",
                ModifiedBy = "admin1",
                AccessLevel = 1,
                IsDeleted = false
            };
            _progenyDbContext.KanbanBoardsDb.Add(familyBoard);

            // Add test KanbanItem records
            KanbanItem testItem1 = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = 1,
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(testItem1);

            KanbanItem testItem2 = new()
            {
                KanbanItemId = 2,
                KanbanBoardId = 1,
                IsDeleted = false
            };
            _progenyDbContext.KanbanItemsDb.Add(testItem2);

            _progenyDbContext.SaveChanges();
        }

        #region GetKanbanBoardById Tests

        [Fact]
        public async Task GetKanbanBoardById_WhenUserHasAccess_ReturnsBoardWithPermissions()
        {
            // Arrange
            int boardId = 1;
            TimelineItemPermission itemPermission = new()
            {
                TimelineType = KinaUnaTypes.TimeLineType.KanbanBoard,
                ItemId = boardId,
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, 1, 0, _testUser))
                .ReturnsAsync(itemPermission);

            // Act
            KanbanBoard result = await _service.GetKanbanBoardById(boardId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(boardId, result.KanbanBoardId);
            Assert.Equal("Test Board 1", result.Title);
            Assert.NotNull(result.ItemPerMission);
            Assert.Equal(PermissionLevel.View, result.ItemPerMission.PermissionLevel);
        }

        [Fact]
        public async Task GetKanbanBoardById_WhenUserHasNoAccess_ReturnsEmptyBoard()
        {
            // Arrange
            int boardId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            KanbanBoard result = await _service.GetKanbanBoardById(boardId, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanBoardId);
        }

        [Fact]
        public async Task GetKanbanBoardById_WhenBoardDoesNotExist_ReturnsEmptyKanbanBoard()
        {
            // Arrange
            int boardId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, boardId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.GetKanbanBoardById(boardId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanBoardId);
        }

        #endregion

        #region AddKanbanBoard Tests

        [Fact]
        public async Task AddKanbanBoard_WhenUserHasProgenyAccess_AddsBoard()
        {
            // Arrange
            KanbanBoard newBoard = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "New Board",
                Description = "New Description",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]",
                ModifiedBy = "user1",
                AccessLevel = 1,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            KanbanBoard result = await _service.AddKanbanBoard(newBoard, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.KanbanBoardId > 0);
            Assert.Equal("New Board", result.Title);
            Assert.NotNull(result.UId);
            Assert.NotEqual(Guid.Empty.ToString(), result.UId);
            Assert.True(result.CreatedTime <= DateTime.UtcNow);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);

            _mockAccessManagementService.Verify(
                x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser),
                Times.Once);
        }

        [Fact]
        public async Task AddKanbanBoard_WhenUserHasFamilyAccess_AddsBoard()
        {
            // Arrange
            KanbanBoard newBoard = new()
            {
                ProgenyId = 0,
                FamilyId = 1,
                Title = "New Family Board",
                Description = "New Description",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]",
                ModifiedBy = "user1",
                AccessLevel = 1,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), 0, 1, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            KanbanBoard result = await _service.AddKanbanBoard(newBoard, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.KanbanBoardId > 0);
            Assert.Equal("New Family Board", result.Title);
        }

        [Fact]
        public async Task AddKanbanBoard_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            KanbanBoard newBoard = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "New Board",
                Description = "New Description",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            KanbanBoard result = await _service.AddKanbanBoard(newBoard, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddKanbanBoard_WhenColumnsInvalid_EnsuresValidColumns()
        {
            // Arrange
            KanbanBoard newBoard = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                Title = "New Board",
                Description = "New Description",
                Columns = "",
                ModifiedBy = "user1",
                AccessLevel = 1,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            KanbanBoard result = await _service.AddKanbanBoard(newBoard, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Columns);
        }

        #endregion

        #region UpdateKanbanBoard Tests

        [Fact]
        public async Task UpdateKanbanBoard_WhenUserHasAccess_UpdatesBoard()
        {
            // Arrange
            KanbanBoard updatedBoard = new()
            {
                KanbanBoardId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title",
                Description = "Updated Description",
                Columns = "[{\"id\":1,\"name\":\"Updated Column\"}]",
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow,
                AccessLevel = 2,
                Tags = "newtag",
                Context = "newcontext",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            KanbanBoard result = await _service.UpdateKanbanBoard(updatedBoard, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(2, result.AccessLevel);
            Assert.Equal("newtag", result.Tags);
            Assert.Equal("newcontext", result.Context);

            _mockAccessManagementService.Verify(
                x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, 1, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser),
                Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanBoard_WhenUserHasNoAccess_ReturnsEmptyBoard()
        {
            // Arrange
            KanbanBoard updatedBoard = new()
            {
                KanbanBoardId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            KanbanBoard result = await _service.UpdateKanbanBoard(updatedBoard, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanBoardId);
        }

        [Fact]
        public async Task UpdateKanbanBoard_WhenBoardDoesNotExist_ReturnsNull()
        {
            // Arrange
            KanbanBoard updatedBoard = new()
            {
                KanbanBoardId = 999,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.UpdateKanbanBoard(updatedBoard, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKanbanBoard_WhenProgenyIdMismatch_ReturnsNull()
        {
            // Arrange
            KanbanBoard updatedBoard = new()
            {
                KanbanBoardId = 1,
                ProgenyId = 999,
                FamilyId = 0,
                Title = "Updated Title"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.UpdateKanbanBoard(updatedBoard, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKanbanBoard_WhenUIdIsNull_GeneratesNewUId()
        {
            // Arrange
            KanbanBoard boardWithoutUId = new()
            {
                KanbanBoardId = 5,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Board Without UId",
                Description = "Test",
                Columns = "[{\"id\":1,\"name\":\"To Do\"}]",
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                CreatedBy = "user1",
                ModifiedBy = "user1",
                UId = null
            };
            _progenyDbContext.KanbanBoardsDb.Add(boardWithoutUId);
            await _progenyDbContext.SaveChangesAsync();

            KanbanBoard updatedBoard = new()
            {
                KanbanBoardId = 5,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Updated Title",
                Description = "Updated Description",
                Columns = "[{\"id\":1,\"name\":\"Updated\"}]",
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow,
                AccessLevel = 1,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 5, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, 5, 1, 0, It.IsAny<List<ItemPermissionDto>>(), _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            KanbanBoard result = await _service.UpdateKanbanBoard(updatedBoard, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UId);
            Assert.NotEmpty(result.UId);
        }

        #endregion

        #region DeleteKanbanBoard Tests

        [Fact]
        public async Task DeleteKanbanBoard_WhenUserHasAccess_SoftDeletesBoard()
        {
            // Arrange
            KanbanBoard? boardToDelete = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.DeleteKanbanBoard(boardToDelete, _adminUser, hardDelete: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.KanbanBoardId);

            KanbanBoard? deletedBoard = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);
            Assert.NotNull(deletedBoard);
            Assert.True(deletedBoard.IsDeleted);

            // Check associated items are soft deleted
            List<KanbanItem> items = await _progenyDbContext.KanbanItemsDb
                .Where(ki => ki.KanbanBoardId == 1)
                .ToListAsync();
            Assert.All(items, item => Assert.True(item.IsDeleted));
        }

        [Fact]
        public async Task DeleteKanbanBoard_WhenHardDelete_PermanentlyDeletesBoard()
        {
            // Arrange
            KanbanBoard? boardToDelete = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.DeleteKanbanBoard(boardToDelete, _adminUser, hardDelete: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.KanbanBoardId);

            KanbanBoard? deletedBoard = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);
            Assert.Null(deletedBoard);

            // Check associated items are hard deleted
            List<KanbanItem> items = await _progenyDbContext.KanbanItemsDb
                .Where(ki => ki.KanbanBoardId == 1)
                .ToListAsync();
            Assert.Empty(items);
        }

        [Fact]
        public async Task DeleteKanbanBoard_WhenUserHasNoAccess_ReturnsEmptyBoard()
        {
            // Arrange
            KanbanBoard? boardToDelete = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            KanbanBoard result = await _service.DeleteKanbanBoard(boardToDelete, _otherUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.KanbanBoardId);

            KanbanBoard? board = await _progenyDbContext.KanbanBoardsDb.FindAsync(1);
            Assert.NotNull(board);
            Assert.False(board.IsDeleted);
        }

        [Fact]
        public async Task DeleteKanbanBoard_WhenBoardDoesNotExist_ReturnsNull()
        {
            // Arrange
            KanbanBoard nonExistentBoard = new()
            {
                KanbanBoardId = 999,
                ProgenyId = 1,
                Title = "Non-existent"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.DeleteKanbanBoard(nonExistentBoard, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteKanbanBoard_WhenNoAssociatedItems_DeletesSuccessfully()
        {
            // Arrange
            KanbanBoard? boardToDelete = await _progenyDbContext.KanbanBoardsDb.FindAsync(2);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, 2, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            KanbanBoard result = await _service.DeleteKanbanBoard(boardToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.KanbanBoardId);
        }

        #endregion

        #region GetKanbanBoardsForProgenyOrFamily Tests

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenProgenyIdProvided_ReturnsProgenyBoards()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = false
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, board => Assert.Equal(1, board.ProgenyId));
            Assert.All(result, board => Assert.False(board.IsDeleted));
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenFamilyIdProvided_ReturnsFamilyBoards()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                FamilyIds = new List<int> { 1 },
                IncludeDeleted = false
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(0, 1, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].FamilyId);
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenIncludeDeleted_ReturnsDeletedBoards()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = true
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, board => board.IsDeleted);
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenUserHasNoAccess_ReturnsEmptyList()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = false
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _otherUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenTagFilterProvided_FiltersCorrectly()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = false,
                TagFilter = "tag1"
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("tag1", result[0].Tags);
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenContextFilterProvided_FiltersCorrectly()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = false,
                ContextFilter = "context1"
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("context1", result[0].Context);
        }

        [Fact]
        public async Task GetKanbanBoardsForProgenyOrFamily_WhenMultipleTagsInFilter_FiltersCorrectly()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = new List<int> { 1 },
                IncludeDeleted = false,
                TagFilter = "tag1,tag3"
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsForProgenyOrFamily(1, 0, _testUser, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region CreateKanbanBoardsResponse Tests

        [Fact]
        public void CreateKanbanBoardsResponse_WhenValidRequest_ReturnsCorrectResponse()
        {
            // Arrange
            List<KanbanBoard> boards = new()
            {
                new KanbanBoard { KanbanBoardId = 1, Title = "Board 1", ModifiedTime = DateTime.UtcNow.AddDays(-1), CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new KanbanBoard { KanbanBoardId = 2, Title = "Board 2", ModifiedTime = DateTime.UtcNow, CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new KanbanBoard { KanbanBoardId = 3, Title = "Board 3", ModifiedTime = DateTime.UtcNow.AddDays(-2), CreatedTime = DateTime.UtcNow.AddDays(-15) }
            };

            KanbanBoardsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 2
            };

            // Act
            KanbanBoardsResponse response = _service.CreateKanbanBoardsResponse(boards, request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.TotalItems);
            Assert.Equal(2, response.TotalPages);
            Assert.Equal(2, response.KanbanBoards.Count);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(2, response.KanbanBoards[0].KanbanBoardId); // Most recently modified
        }

        [Fact]
        public void CreateKanbanBoardsResponse_WhenSkipProvided_ReturnsCorrectPage()
        {
            // Arrange
            List<KanbanBoard> boards = new()
            {
                new KanbanBoard { KanbanBoardId = 1, Title = "Board 1", ModifiedTime = DateTime.UtcNow.AddDays(-1), CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new KanbanBoard { KanbanBoardId = 2, Title = "Board 2", ModifiedTime = DateTime.UtcNow, CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new KanbanBoard { KanbanBoardId = 3, Title = "Board 3", ModifiedTime = DateTime.UtcNow.AddDays(-2), CreatedTime = DateTime.UtcNow.AddDays(-15) }
            };

            KanbanBoardsRequest request = new()
            {
                Skip = 1,
                NumberOfItems = 1
            };

            // Act
            KanbanBoardsResponse response = _service.CreateKanbanBoardsResponse(boards, request);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.KanbanBoards);
            Assert.Equal(2, response.PageNumber);
            Assert.Equal(1, response.KanbanBoards[0].KanbanBoardId); // Second most recently modified
        }

        [Fact]
        public void CreateKanbanBoardsResponse_WhenNumberOfItemsIsZero_ReturnsAllItems()
        {
            // Arrange
            List<KanbanBoard> boards = new()
            {
                new KanbanBoard { KanbanBoardId = 1, Title = "Board 1", ModifiedTime = DateTime.UtcNow, CreatedTime = DateTime.UtcNow },
                new KanbanBoard { KanbanBoardId = 2, Title = "Board 2", ModifiedTime = DateTime.UtcNow, CreatedTime = DateTime.UtcNow }
            };

            KanbanBoardsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 0
            };

            // Act
            KanbanBoardsResponse response = _service.CreateKanbanBoardsResponse(boards, request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.KanbanBoards.Count);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(2, response.TotalItems);
        }

        [Fact]
        public void CreateKanbanBoardsResponse_WhenEmptyList_ReturnsEmptyResponse()
        {
            // Arrange
            List<KanbanBoard> boards = new();
            KanbanBoardsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            KanbanBoardsResponse response = _service.CreateKanbanBoardsResponse(boards, request);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.KanbanBoards);
            Assert.Equal(0, response.TotalItems);
            Assert.Equal(0, response.TotalPages);
        }

        [Fact]
        public void CreateKanbanBoardsResponse_SortsCorrectly()
        {
            // Arrange
            List<KanbanBoard> boards = new()
            {
                new KanbanBoard { KanbanBoardId = 1, Title = "Board 1", ModifiedTime = DateTime.UtcNow.AddDays(-5), CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new KanbanBoard { KanbanBoardId = 2, Title = "Board 2", ModifiedTime = DateTime.UtcNow, CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new KanbanBoard { KanbanBoardId = 3, Title = "Board 3", ModifiedTime = DateTime.UtcNow.AddDays(-2), CreatedTime = DateTime.UtcNow.AddDays(-15) }
            };

            KanbanBoardsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            KanbanBoardsResponse response = _service.CreateKanbanBoardsResponse(boards, request);

            // Assert
            Assert.Equal(2, response.KanbanBoards[0].KanbanBoardId);
            Assert.Equal(3, response.KanbanBoards[1].KanbanBoardId);
            Assert.Equal(1, response.KanbanBoards[2].KanbanBoardId);
        }

        #endregion

        #region GetKanbanBoardsListForProgenyOrFamily Tests

        [Fact]
        public async Task GetKanbanBoardsListForProgenyOrFamily_WhenProgenyIdProvided_ReturnsNonDeletedBoards()
        {
            // Arrange
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsListForProgenyOrFamily(1, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, board => Assert.Equal(1, board.ProgenyId));
            Assert.All(result, board => Assert.False(board.IsDeleted));
        }

        [Fact]
        public async Task GetKanbanBoardsListForProgenyOrFamily_WhenFamilyIdProvided_ReturnsFamilyBoards()
        {
            // Arrange
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsListForProgenyOrFamily(0, 1, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].FamilyId);
        }

        [Fact]
        public async Task GetKanbanBoardsListForProgenyOrFamily_WhenUserHasNoAccess_ReturnsOnlyAccessibleBoards()
        {
            // Arrange
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
            List<KanbanBoard> result = await _service.GetKanbanBoardsListForProgenyOrFamily(1, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].KanbanBoardId);
        }

        [Fact]
        public async Task GetKanbanBoardsListForProgenyOrFamily_WhenNoBoards_ReturnsEmptyList()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsListForProgenyOrFamily(999, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKanbanBoardsListForProgenyOrFamily_ExcludesDeletedBoards()
        {
            // Arrange
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<KanbanBoard> result = await _service.GetKanbanBoardsListForProgenyOrFamily(1, 0, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, board => Assert.False(board.IsDeleted));
            Assert.DoesNotContain(result, board => board.KanbanBoardId == 3); // Deleted board
        }

        #endregion
    }
}