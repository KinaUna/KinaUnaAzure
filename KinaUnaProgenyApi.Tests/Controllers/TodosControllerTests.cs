using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class TodosControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly TodosController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly TodoItem _testTodoItem;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestTodoItemId = 100;

        public TodosControllerTests()
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
            
            _testTodoItem = new TodoItem
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Todo",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                UId = Guid.NewGuid().ToString()
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = TestTodoItemId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockProgenyService = new Mock<IProgenyService>();
            Mock<IFamiliesService> mockFamilyService = new();
            _mockTodosService = new Mock<ITodosService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            _controller = new TodosController(
                _mockProgenyService.Object,
                mockFamilyService.Object,
                _mockTodosService.Object,
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockWebNotificationsService.Object,
                _mockAccessManagementService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Email, TestUserEmail),
                new Claim(ClaimTypes.NameIdentifier, TestUserId)
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

        #region GetProgeniesTodoItemsList Tests

        [Fact]
        public async Task GetProgeniesTodoItemsList_Should_Return_Ok_With_TodoItems_When_Valid_Request()
        {
            // Arrange
            TodoItemsRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [],
                Skip = 0,
                NumberOfItems = 10
            };

            List<TodoItem> todoItems = [_testTodoItem];
            TodoItemsResponse expectedResponse = new()
            {
                TodoItems = todoItems,
                TotalItems = 1,
                TotalPages = 1,
                PageNumber = 1,
                ProgenyList = [_testProgeny]
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodosForProgenyOrFamily(TestProgenyId, 0, _testUser, request))
                .ReturnsAsync(todoItems);
            _mockTodosService.Setup(x => x.CreateTodoItemsResponseForTodoPage(It.IsAny<List<TodoItem>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetProgeniesTodoItemsList(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItemsResponse response = Assert.IsType<TodoItemsResponse>(okResult.Value);
            Assert.Equal(1, response.TotalItems);
            Assert.Single(response.TodoItems);
            Assert.Equal(_testTodoItem.TodoItemId, response.TodoItems[0].TodoItemId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockTodosService.Verify(x => x.GetTodosForProgenyOrFamily(TestProgenyId, 0, _testUser, request), Times.Once);
        }

        [Fact]
        public async Task GetProgeniesTodoItemsList_Should_Handle_Multiple_Progenies_And_Families()
        {
            // Arrange
            int secondProgenyId = 2;
            TodoItemsRequest request = new()
            {
                ProgenyIds = [TestProgenyId, secondProgenyId],
                FamilyIds = [TestFamilyId],
                Skip = 0,
                NumberOfItems = 20
            };

            List<TodoItem> progenyTodos1 = [_testTodoItem];
            List<TodoItem> progenyTodos2 = [new TodoItem { TodoItemId = 101, ProgenyId = secondProgenyId, Title = "Todo 2" }];
            List<TodoItem> familyTodos = [new TodoItem { TodoItemId = 102, FamilyId = TestFamilyId, Title = "Family Todo" }];

            TodoItemsResponse expectedResponse = new()
            {
                TodoItems = [.. progenyTodos1, .. progenyTodos2, .. familyTodos],
                TotalItems = 3
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodosForProgenyOrFamily(TestProgenyId, 0, _testUser, request))
                .ReturnsAsync(progenyTodos1);
            _mockTodosService.Setup(x => x.GetTodosForProgenyOrFamily(secondProgenyId, 0, _testUser, request))
                .ReturnsAsync(progenyTodos2);
            _mockTodosService.Setup(x => x.GetTodosForProgenyOrFamily(0, TestFamilyId, _testUser, request))
                .ReturnsAsync(familyTodos);
            _mockTodosService.Setup(x => x.CreateTodoItemsResponseForTodoPage(It.IsAny<List<TodoItem>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetProgeniesTodoItemsList(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItemsResponse response = Assert.IsType<TodoItemsResponse>(okResult.Value);
            Assert.Equal(3, response.TotalItems);

            _mockTodosService.Verify(x => x.GetTodosForProgenyOrFamily(TestProgenyId, 0, _testUser, request), Times.Once);
            _mockTodosService.Verify(x => x.GetTodosForProgenyOrFamily(secondProgenyId, 0, _testUser, request), Times.Once);
            _mockTodosService.Verify(x => x.GetTodosForProgenyOrFamily(0, TestFamilyId, _testUser, request), Times.Once);
        }

        [Fact]
        public async Task GetProgeniesTodoItemsList_Should_Set_Skip_To_Zero_When_Negative()
        {
            // Arrange
            TodoItemsRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [],
                Skip = -5,
                NumberOfItems = 10
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodosForProgenyOrFamily(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserInfo>(), It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync([]);
            _mockTodosService.Setup(x => x.CreateTodoItemsResponseForTodoPage(It.IsAny<List<TodoItem>>(), It.IsAny<TodoItemsRequest>()))
                .Returns(new TodoItemsResponse());

            // Act
            IActionResult result = await _controller.GetProgeniesTodoItemsList(request);

            // Assert
            Assert.Equal(0, request.Skip);
            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetTodoItem Tests

        [Fact]
        public async Task GetTodoItem_Should_Return_Ok_When_TodoItem_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);

            // Act
            IActionResult result = await _controller.GetTodoItem(TestTodoItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestTodoItemId, returnedTodo.TodoItemId);
            Assert.Equal(_testTodoItem.Title, returnedTodo.Title);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockTodosService.Verify(x => x.GetTodoItem(TestTodoItemId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetTodoItem_Should_Return_NotFound_When_TodoItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(999, _testUser))
                .ReturnsAsync(null as TodoItem);

            // Act
            IActionResult result = await _controller.GetTodoItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockTodosService.Verify(x => x.GetTodoItem(999, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_TodoItem_For_Progeny_And_Return_Ok()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Todo",
                Description = "New Description"
            };

            TodoItem createdTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Todo",
                Description = "New Description",
                CreatedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(createdTodoItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestTodoItemId, returnedTodo.TodoItemId);
            Assert.Equal(TestUserId, returnedTodo.CreatedBy);

            _mockAccessManagementService.Verify(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add), Times.Once);
            _mockTodosService.Verify(x => x.AddTodoItem(It.Is<TodoItem>(t =>
                t.CreatedBy == TestUserId && !string.IsNullOrWhiteSpace(t.UId)), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendTodoItemNotification(
                It.IsAny<TodoItem>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Create_TodoItem_For_Family_And_Return_Ok()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Todo"
            };

            TodoItem createdTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Todo",
                CreatedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(createdTodoItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestFamilyId, returnedTodo.FamilyId);

            _mockAccessManagementService.Verify(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            TodoItem invalidTodoItem = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                Title = "Invalid Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Post(invalidTodoItem);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A TodoItem must have either a ProgenyId or a FamilyId set, but not both.", badRequestResult.Value);

            _mockTodosService.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Neither_ProgenyId_Nor_FamilyId_Set()
        {
            // Arrange
            TodoItem invalidTodoItem = new()
            {
                ProgenyId = 0,
                FamilyId = 0,
                Title = "Invalid Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.Post(invalidTodoItem);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A TodoItem must have either a ProgenyId or a FamilyId set.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Progeny_Permission()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Unauthorized Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTodosService.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Family_Permission()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Unauthorized Family Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Todo without UId"
            };

            TodoItem createdTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                Title = "Todo without UId",
                CreatedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(createdTodoItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTodosService.Verify(x => x.AddTodoItem(It.Is<TodoItem>(t =>
                !string.IsNullOrWhiteSpace(t.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_TodosService_Returns_Null()
        {
            // Arrange
            TodoItem newTodoItem = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Failed Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(null as TodoItem);

            // Act
            IActionResult result = await _controller.Post(newTodoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_TodoItem_And_Timeline_And_Return_Ok()
        {
            // Arrange
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Title",
                Description = "Updated Description"
            };

            TodoItem returnedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Title",
                Description = "Updated Description",
                ModifiedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(returnedTodoItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, updatedTodoItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal("Updated Title", returnedTodo.Title);
            Assert.Equal(TestUserId, returnedTodo.ModifiedBy);

            _mockTodosService.Verify(x => x.GetTodoItem(TestTodoItemId, _testUser), Times.Once);
            _mockTodosService.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.ModifiedBy == TestUserId && !string.IsNullOrWhiteSpace(t.UId)), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_TodoItem_Does_Not_Exist()
        {
            // Arrange
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = 999,
                ProgenyId = TestProgenyId,
                Title = "Non-existent Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(999, _testUser))
                .ReturnsAsync(null as TodoItem);

            // Act
            IActionResult result = await _controller.Put(999, updatedTodoItem);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockTodosService.Verify(x => x.UpdateTodoItem(It.IsAny<TodoItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_TodoItem_Has_Both_ProgenyId_And_FamilyId()
        {
            // Arrange
            TodoItem invalidTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                Title = "Invalid Todo"
            };

            TodoItem existingTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(existingTodoItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, invalidTodoItem);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("TodoItems cannot be assigned to both a progeny and a family.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_TodoItem_Has_Neither_ProgenyId_Nor_FamilyId()
        {
            // Arrange
            TodoItem invalidTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = 0,
                FamilyId = 0,
                Title = "Invalid Todo"
            };

            TodoItem existingTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = 0,
                FamilyId = 0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(existingTodoItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, invalidTodoItem);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("TodoItems must be assigned to either a progeny or a family.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated without UId"
            };

            TodoItem returnedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                Title = "Updated without UId",
                ModifiedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(returnedTodoItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, updatedTodoItem);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTodosService.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                !string.IsNullOrWhiteSpace(t.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateTodoItem_Returns_Null()
        {
            // Arrange
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Failed Update"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(null as TodoItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, updatedTodoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Title"
            };

            TodoItem returnedTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                ModifiedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(returnedTodoItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestTodoItemId, updatedTodoItem);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_TodoItem_And_Timeline_For_Progeny_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(true);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestTodoItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockTodosService.Verify(x => x.GetTodoItem(TestTodoItemId, _testUser), Times.Once);
            _mockTodosService.Verify(x => x.DeleteTodoItem(It.Is<TodoItem>(t =>
                t.ModifiedBy == TestUserId), _testUser, false), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendTodoItemNotification(
                It.IsAny<TodoItem>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_TodoItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(999, _testUser))
                .ReturnsAsync(null as TodoItem);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockTodosService.Verify(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), It.IsAny<UserInfo>(), false), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_BadRequest_When_DeleteTodoItem_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestTodoItemId);

            // Assert
            Assert.IsType<BadRequestResult>(result);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(true);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestTodoItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Set_AccessLevel_To_Zero_For_Notifications()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(_testTodoItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(true);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestTodoItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockWebNotificationsService.Verify(x => x.SendTodoItemNotification(
                It.Is<TodoItem>(t => t.AccessLevel == 0),
                It.IsAny<UserInfo>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Handle_Family_TodoItem()
        {
            // Arrange
            TodoItem familyTodoItem = new()
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Todo"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockTodosService.Setup(x => x.GetTodoItem(TestTodoItemId, _testUser))
                .ReturnsAsync(familyTodoItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(true);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestTodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestTodoItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify that progeny-specific notifications are not sent for family todos
            _mockProgenyService.Verify(x => x.GetProgeny(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion
    }
}