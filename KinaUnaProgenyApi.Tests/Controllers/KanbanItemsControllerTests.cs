using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class KanbanItemsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IKanbanItemsService> _mockKanbanItemsService;
        private readonly Mock<IKanbanBoardsService> _mockKanbanBoardsService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly KanbanItemsController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;
        private readonly Progeny _testProgeny;
        private readonly TodoItem _testTodoItem;
        private readonly KanbanBoard _testKanbanBoard;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const int TestProgenyId = 1;
        private const int TestKanbanBoardId = 100;
        private const int TestKanbanItemId = 200;
        private const int TestTodoItemId = 300;

        public KanbanItemsControllerTests()
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
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _adminUser = new UserInfo
            {
                UserId = AdminUserId,
                UserEmail = AdminUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _otherUser = new UserInfo
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                Admins = AdminUserEmail
            };

            _testTodoItem = new TodoItem
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                Title = "Test Todo",
                Description = "Test Description",
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow
            };

            _testKanbanBoard = new KanbanBoard
            {
                KanbanBoardId = TestKanbanBoardId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Kanban Board",
                Description = "Test Description",
                Columns = "[{\"Id\":1,\"Name\":\"To Do\"},{\"Id\":2,\"Name\":\"In Progress\"},{\"Id\":3,\"Name\":\"Done\"}]",
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow
            };

            // Seed database
            SeedTestData();

            // Setup mocks
            _mockKanbanItemsService = new Mock<IKanbanItemsService>();
            _mockKanbanBoardsService = new Mock<IKanbanBoardsService>();
            _mockTodosService = new Mock<ITodosService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Default mock setups
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(AdminUserId))
                .ReturnsAsync(_adminUser);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);

            // Initialize controller
            _controller = new KanbanItemsController(
                _mockKanbanItemsService.Object,
                _mockKanbanBoardsService.Object,
                _mockTodosService.Object,
                _mockUserInfoService.Object);

            SetupControllerContext();
        }

        private void SeedTestData()
        {
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);
            _progenyDbContext.ProgenyDb.Add(_testProgeny);
            _progenyDbContext.SaveChanges();
        }

        private void SetupControllerContext(string userId = TestUserId, string userEmail = TestUserEmail)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Email, userEmail),
                new Claim(ClaimTypes.NameIdentifier, userId)
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

        private static KanbanItem CreateTestKanbanItem(
            int kanbanItemId,
            int kanbanBoardId,
            int todoItemId,
            int columnId = 1,
            int rowIndex = 0)
        {
            return new KanbanItem
            {
                KanbanItemId = kanbanItemId,
                KanbanBoardId = kanbanBoardId,
                TodoItemId = todoItemId,
                ColumnId = columnId,
                RowIndex = rowIndex,
                UId = Guid.NewGuid().ToString(),
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        #region GetKanbanItem Tests

        [Fact]
        public async Task GetKanbanItem_Should_Return_Ok_When_Item_Exists()
        {
            // Arrange
            KanbanItem kanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            kanbanItem.TodoItem = _testTodoItem;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(kanbanItem);

            // Act
            IActionResult result = await _controller.GetKanbanItem(TestKanbanItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanItem returnedItem = Assert.IsType<KanbanItem>(okResult.Value);
            Assert.Equal(TestKanbanItemId, returnedItem.KanbanItemId);
            Assert.NotNull(returnedItem.TodoItem);
            Assert.Equal(TestTodoItemId, returnedItem.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task GetKanbanItem_Should_Return_NotFound_When_Item_Not_Exists()
        {
            // Arrange
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.GetKanbanItem(TestKanbanItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetKanbanItem_Should_Return_BadRequest_When_TodoItem_Is_Null()
        {
            // Arrange
            KanbanItem kanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            kanbanItem.TodoItem = null;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(kanbanItem);

            // Act
            IActionResult result = await _controller.GetKanbanItem(TestKanbanItemId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The Kanban item is not linked to a valid Todo item.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetKanbanItem_Should_Use_Current_User_From_Claims()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);
            KanbanItem kanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            kanbanItem.TodoItem = _testTodoItem;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _adminUser))
                .ReturnsAsync(kanbanItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(It.IsAny<string>())).ReturnsAsync(_adminUser);
            // Act
            IActionResult result = await _controller.GetKanbanItem(TestKanbanItemId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(It.IsAny<string>()), Times.Once);
            _mockKanbanItemsService.Verify(x => x.GetKanbanItemById(TestKanbanItemId, _adminUser), Times.Once);
        }

        #endregion

        #region GetKanbanItemsForBoard Tests

        [Fact]
        public async Task GetKanbanItemsForBoard_Should_Return_Ok_When_Board_Exists()
        {
            // Arrange
            List<KanbanItem> kanbanItems =
            [
                CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId),
                CreateTestKanbanItem(TestKanbanItemId + 1, TestKanbanBoardId, TestTodoItemId + 1)
            ];
            kanbanItems[0].TodoItem = _testTodoItem;
            kanbanItems[1].TodoItem = new TodoItem { TodoItemId = TestTodoItemId + 1, Title = "Another Todo" };

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(_testKanbanBoard);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForBoard(TestKanbanBoardId, _testUser, false))
                .ReturnsAsync(kanbanItems);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForBoard(TestKanbanBoardId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
            Assert.All(returnedItems, item => Assert.NotNull(item.TodoItem));
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_Should_Return_NotFound_When_Board_Not_Exists()
        {
            // Arrange
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForBoard(TestKanbanBoardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_Should_Include_Deleted_Items_When_IncludeDeleted_True()
        {
            // Arrange
            List<KanbanItem> kanbanItems =
            [
                CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId),
                CreateTestKanbanItem(TestKanbanItemId + 1, TestKanbanBoardId, TestTodoItemId + 1)
            ];
            kanbanItems[0].TodoItem = _testTodoItem;
            kanbanItems[1].TodoItem = new TodoItem { TodoItemId = TestTodoItemId + 1, Title = "Deleted Todo", IsDeleted = true };
            kanbanItems[1].IsDeleted = true;

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(_testKanbanBoard);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForBoard(TestKanbanBoardId, _testUser, true))
                .ReturnsAsync(kanbanItems);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForBoard(TestKanbanBoardId, true);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
            Assert.Contains(returnedItems, item => item.IsDeleted);

            _mockKanbanItemsService.Verify(x => x.GetKanbanItemsForBoard(TestKanbanBoardId, _testUser, true), Times.Once);
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_Should_Return_Empty_List_When_No_Items()
        {
            // Arrange
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(_testKanbanBoard);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForBoard(TestKanbanBoardId, _testUser, false))
                .ReturnsAsync(new List<KanbanItem>());

            // Act
            IActionResult result = await _controller.GetKanbanItemsForBoard(TestKanbanBoardId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region GetKanbanItemsForTodoItem Tests

        [Fact]
        public async Task GetKanbanItemsForTodoItem_Should_Return_Ok_When_TodoItem_Exists()
        {
            // Arrange
            List<KanbanItem> kanbanItems =
            [
                CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId),
                CreateTestKanbanItem(TestKanbanItemId + 1, TestKanbanBoardId + 1, TestTodoItemId)
            ];
            kanbanItems[0].TodoItem = _testTodoItem;
            kanbanItems[1].TodoItem = _testTodoItem;

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId, _testUser, false))
                .ReturnsAsync(kanbanItems);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForTodoItem(TestTodoItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
            Assert.All(returnedItems, item => Assert.Equal(TestTodoItemId, item.TodoItemId));
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_Should_Return_NotFound_When_TodoItem_Not_Exists()
        {
            // Arrange
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForTodoItem(TestTodoItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_Should_Include_Deleted_Items_When_IncludeDeleted_True()
        {
            // Arrange
            List<KanbanItem> kanbanItems =
            [
                CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId),
                CreateTestKanbanItem(TestKanbanItemId + 1, TestKanbanBoardId, TestTodoItemId)
            ];
            kanbanItems[0].TodoItem = _testTodoItem;
            kanbanItems[1].TodoItem = _testTodoItem;
            kanbanItems[1].IsDeleted = true;

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId, _testUser, true))
                .ReturnsAsync(kanbanItems);

            // Act
            IActionResult result = await _controller.GetKanbanItemsForTodoItem(TestTodoItemId, true);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
            _mockKanbanItemsService.Verify(x => x.GetKanbanItemsForTodoItem(TestTodoItemId, _testUser, true), Times.Once);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_Should_Return_Empty_List_When_No_Items()
        {
            // Arrange
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId, _testUser, false))
                .ReturnsAsync(new List<KanbanItem>());

            // Act
            IActionResult result = await _controller.GetKanbanItemsForTodoItem(TestTodoItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KanbanItem> returnedItems = Assert.IsType<List<KanbanItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_KanbanItem()
        {
            // Arrange
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, TestTodoItemId);
            KanbanItem addedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>(), _testUser))
                .ReturnsAsync(addedKanbanItem);

            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanItem returnedItem = Assert.IsType<KanbanItem>(okResult.Value);
            Assert.Equal(TestKanbanItemId, returnedItem.KanbanItemId);
            Assert.NotNull(returnedItem.TodoItem);
            Assert.Equal(TestTodoItemId, returnedItem.TodoItem.TodoItemId);

            _mockKanbanItemsService.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.CreatedBy == TestUserId &&
                k.ModifiedBy == TestUserId &&
                k.TodoItemId == TestTodoItemId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_KanbanItem_Is_Null()
        {
            // Act
            IActionResult result = await _controller.Post(null!);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_TodoItemId_Is_Zero()
        {
            // Arrange
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, 0);

            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The Kanban item must be linked to a valid Todo item.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_TodoItem_Not_Found()
        {
            // Arrange
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, TestTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The Kanban item must be linked to a valid Todo item.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_TodoItem_Has_Zero_Id()
        {
            // Arrange
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, TestTodoItemId);
            TodoItem invalidTodoItem = new() { TodoItemId = 0 };

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(invalidTodoItem);

            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The Kanban item must be linked to a valid Todo item.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_AddKanbanItem_Returns_Null()
        {
            // Arrange
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, TestTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>(), _testUser))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_Should_Set_CreatedBy_And_ModifiedBy_To_Current_User()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);
            KanbanItem kanbanItemToAdd = CreateTestKanbanItem(0, TestKanbanBoardId, TestTodoItemId);
            KanbanItem addedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _adminUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>(), _adminUser))
                .ReturnsAsync(addedKanbanItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(It.IsAny<string>())).ReturnsAsync(_adminUser);
            // Act
            IActionResult result = await _controller.Post(kanbanItemToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKanbanItemsService.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.CreatedBy == AdminUserId &&
                k.ModifiedBy == AdminUserId), _adminUser), Times.Once);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_Should_Return_Ok_When_Valid_Update()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            updateValues.ColumnId = 2;
            updateValues.RowIndex = 5;
            KanbanItem updatedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            updatedKanbanItem.ColumnId = 2;
            updatedKanbanItem.RowIndex = 5;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>(), _testUser))
                .ReturnsAsync(updatedKanbanItem);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanItem returnedItem = Assert.IsType<KanbanItem>(okResult.Value);
            Assert.Equal(TestKanbanItemId, returnedItem.KanbanItemId);
            Assert.Equal(2, returnedItem.ColumnId);
            Assert.Equal(5, returnedItem.RowIndex);
            Assert.NotNull(returnedItem.TodoItem);

            _mockKanbanItemsService.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanItemId == TestKanbanItemId &&
                k.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_KanbanItem_Is_Null()
        {
            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, null!);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_Id_Mismatch()
        {
            // Arrange
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            // Act
            IActionResult result = await _controller.Update(999, updateValues);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_Existing_KanbanItem_Not_Found()
        {
            // Arrange
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_TodoItemId_Is_Zero()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, 0);

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The Kanban item must be linked to a valid Todo item.", badRequestResult.Value);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_TodoItem_Not_Found()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The linked Todo item could not be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_TodoItem_Has_Zero_Id()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            TodoItem invalidTodoItem = new() { TodoItemId = 0 };

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(invalidTodoItem);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The linked Todo item could not be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Update_Should_Return_Unauthorized_When_UpdateKanbanItem_Returns_Null()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>(), _testUser))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Update_Should_Set_ModifiedBy_To_Current_User()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem updateValues = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            KanbanItem updatedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _adminUser))
                .ReturnsAsync(existingKanbanItem);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _adminUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>(), _adminUser))
                .ReturnsAsync(updatedKanbanItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(It.IsAny<string>())).ReturnsAsync(_adminUser);
            // Act
            IActionResult result = await _controller.Update(TestKanbanItemId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKanbanItemsService.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.ModifiedBy == AdminUserId), _adminUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_Ok_When_Successfully_Deleted()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem deletedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            deletedKanbanItem.TodoItem = _testTodoItem;
            deletedKanbanItem.IsDeleted = true;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockKanbanItemsService.Setup(x => x.DeleteKanbanItem(It.IsAny<KanbanItem>(), _testUser, false))
                .ReturnsAsync(deletedKanbanItem);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanItem returnedItem = Assert.IsType<KanbanItem>(okResult.Value);
            Assert.Equal(TestKanbanItemId, returnedItem.KanbanItemId);
            Assert.NotNull(returnedItem.TodoItem);

            _mockKanbanItemsService.Verify(x => x.DeleteKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanItemId == TestKanbanItemId &&
                k.ModifiedBy == TestUserId), _testUser, false), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_KanbanItem_Not_Exists()
        {
            // Arrange
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteKanbanItem_Returns_Null()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockKanbanItemsService.Setup(x => x.DeleteKanbanItem(It.IsAny<KanbanItem>(), _testUser, false))
                .ReturnsAsync((KanbanItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanItemId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Set_ModifiedBy_To_Current_User()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem deletedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            deletedKanbanItem.TodoItem = _testTodoItem;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _adminUser))
                .ReturnsAsync(existingKanbanItem);
            _mockKanbanItemsService.Setup(x => x.DeleteKanbanItem(It.IsAny<KanbanItem>(), _adminUser, false))
                .ReturnsAsync(deletedKanbanItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(It.IsAny<string>())).ReturnsAsync(_adminUser);
            
            // Act
            IActionResult result = await _controller.Delete(TestKanbanItemId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKanbanItemsService.Verify(x => x.DeleteKanbanItem(It.Is<KanbanItem>(k =>
                k.ModifiedBy == AdminUserId), _adminUser, false), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Preserve_Existing_TodoItem()
        {
            // Arrange
            KanbanItem existingKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            existingKanbanItem.TodoItem = _testTodoItem;
            KanbanItem deletedKanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            deletedKanbanItem.TodoItem = null; // Service might not return TodoItem

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(existingKanbanItem);
            _mockKanbanItemsService.Setup(x => x.DeleteKanbanItem(It.IsAny<KanbanItem>(), _testUser, false))
                .ReturnsAsync(deletedKanbanItem);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanItem returnedItem = Assert.IsType<KanbanItem>(okResult.Value);
            Assert.NotNull(returnedItem.TodoItem); // Should be set from existing item
            Assert.Equal(_testTodoItem.TodoItemId, returnedItem.TodoItem.TodoItemId);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task All_Methods_Should_Use_Current_User_From_UserInfoService()
        {
            // Arrange
            KanbanItem kanbanItem = CreateTestKanbanItem(TestKanbanItemId, TestKanbanBoardId, TestTodoItemId);
            kanbanItem.TodoItem = _testTodoItem;

            _mockKanbanItemsService.Setup(x => x.GetKanbanItemById(TestKanbanItemId, _testUser))
                .ReturnsAsync(kanbanItem);

            // Act
            await _controller.GetKanbanItem(TestKanbanItemId);

            // Assert
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
        }

        [Fact]
        public async Task GetKanbanItemsForBoard_Should_Validate_Board_Access()
        {
            // Arrange
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(_testKanbanBoard);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForBoard(TestKanbanBoardId, _testUser, false))
                .ReturnsAsync(new List<KanbanItem>());

            // Act
            IActionResult result = await _controller.GetKanbanItemsForBoard(TestKanbanBoardId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKanbanBoardsService.Verify(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetKanbanItemsForTodoItem_Should_Validate_TodoItem_Access()
        {
            // Arrange
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockKanbanItemsService.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId, _testUser, false))
                .ReturnsAsync(new List<KanbanItem>());

            // Act
            IActionResult result = await _controller.GetKanbanItemsForTodoItem(TestTodoItemId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTodosService.Verify(x => x.GetTodoItem(TestTodoItemId, _testUser), Times.Once);
        }

        #endregion

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}