using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Tests.Controllers.TodosController
{
    public class TodosControllerTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly Mock<IKanbanItemsHttpClient> _mockKanbanItemsHttpClient;
        private readonly Mock<IKanbanBoardsHttpClient> _mockKanbanBoardsHttpClient;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public TodosControllerTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            Mock<ISubtasksHttpClient> mockSubtasksHttpClient = new();
            _mockKanbanItemsHttpClient = new Mock<IKanbanItemsHttpClient>();
            _mockKanbanBoardsHttpClient = new Mock<IKanbanBoardsHttpClient>();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object, 
                mockSubtasksHttpClient.Object,
                _mockKanbanItemsHttpClient.Object,
                _mockKanbanBoardsHttpClient.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new Claim(OpenIddictConstants.Claims.Email, TestUserEmail),
                new Claim(OpenIddictConstants.Claims.Subject, TestUserId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            DefaultHttpContext httpContext = new()
            {
                User = claimsPrincipal
            };

            // Mock cookies for language
            Mock<IRequestCookieCollection> mockRequestCookies = new();
            mockRequestCookies.Setup(x => x["KinaUnaLanguage"]).Returns("1");
            httpContext.Request.Cookies = mockRequestCookies.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        #region Index Tests

        [Fact]
        public async Task Index_Should_Return_View_With_TodoItemsListViewModel()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult? result = await _controller.Index(null);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoItemsListViewModel model = Assert.IsType<TodoItemsListViewModel>(viewResult.Model);
            Assert.Equal(0, model.PopUpTodoItemId);
            
            _mockViewModelSetupService.Verify(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0), Times.Once);
        }

        [Fact]
        public async Task Index_With_TodoItemId_Should_Add_TodoItem_To_List()
        {
            // Arrange
            const int todoItemId = 123;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            TodoItem todoItem = new() { TodoItemId = todoItemId, Title = "Test Todo" };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult? result = await _controller.Index(todoItemId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoItemsListViewModel model = Assert.IsType<TodoItemsListViewModel>(viewResult.Model);
            Assert.Equal(todoItemId, model.PopUpTodoItemId);
            Assert.Single(model.TodoItemsList);
            Assert.Equal(todoItem, model.TodoItemsList[0]);

            _mockTodoItemsHttpClient.Verify(x => x.GetTodoItem(todoItemId), Times.Once);
        }

        [Fact]
        public async Task Index_With_ChildId_Should_Pass_ChildId_To_SetupViewModel()
        {
            // Arrange
            const int childId = 5;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, childId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult? result = await _controller.Index(null, childId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<TodoItemsListViewModel>(viewResult.Model);
            
            _mockViewModelSetupService.Verify(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, childId), Times.Once);
        }

        #endregion
        
        

        #region ViewTodo Tests

        [Fact]
        public async Task ViewTodo_Should_Return_View_When_PartialView_False()
        {
            // Arrange
            const int todoId = 123;
            TodoItem todoItem = CreateMockTodoItem(todoId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            UserInfo userInfo = new() { FirstName = "John", LastName = "Doe" };

            SetupMocksForViewTodo(todoItem, baseModel, userInfo);

            // Act
            IActionResult? result = await _controller.ViewTodo(todoId, false);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(viewResult.Model);
            Assert.Equal(todoId, model.TodoItem.TodoItemId);
            Assert.Equal("John Doe", model.TodoItem.CreatedBy);
        }

        [Fact]
        public async Task ViewTodo_Should_Return_PartialView_When_PartialView_True()
        {
            // Arrange
            const int todoId = 123;
            TodoItem todoItem = CreateMockTodoItem(todoId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            UserInfo userInfo = new() { FirstName = "John", LastName = "Doe" };

            SetupMocksForViewTodo(todoItem, baseModel, userInfo);

            // Act
            IActionResult? result = await _controller.ViewTodo(todoId, true);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoDetailsPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(todoId, model.TodoItem.TodoItemId);
        }

        #endregion

        

        

        #region Helper Methods

        private static BaseItemsViewModel CreateMockBaseItemsViewModelUserIsAdmin()
        {
            return new BaseItemsViewModel
            {
                CurrentUser = new UserInfo 
                { 
                    UserEmail = TestUserEmail, 
                    UserId = TestUserId,
                    Timezone = "UTC"
                },
                CurrentProgeny = new Progeny 
                { 
                    Id = 1, 
                    Name = "Test Child",
                    Admins = TestUserEmail
                },
                CurrentProgenyAccessList = [
                    new UserAccess { UserId = TestUserEmail, AccessLevel = (int)AccessLevel.Private }
                ],
                IsCurrentUserProgenyAdmin = true,
                LanguageId = 1
            };
        }

        private static BaseItemsViewModel CreateMockBaseItemsViewModelUserIsNotAdmin()
        {
            return new BaseItemsViewModel
            {
                CurrentUser = new UserInfo
                {
                    UserEmail = TestUserEmail,
                    UserId = TestUserId,
                    Timezone = "UTC"
                },
                CurrentProgeny = new Progeny
                {
                    Id = 1,
                    Name = "Test Child",
                    Admins = ""
                },
                CurrentProgenyAccessList =
                [
                    new UserAccess { UserId = TestUserEmail, AccessLevel = (int)AccessLevel.Family }
                ],
                IsCurrentUserProgenyAdmin = true,
                LanguageId = 1
            };
        }

        private static TodoItem CreateMockTodoItem(int todoItemId)
        {
            return new TodoItem
            {
                TodoItemId = todoItemId,
                ProgenyId = 1,
                Title = "Test Todo",
                Description = "Test Description",
                CreatedBy = "user123",
                AccessLevel = 1,
                Status = 0
            };
        }

        private void SetupMocksForViewTodo(TodoItem todoItem, BaseItemsViewModel baseModel, UserInfo userInfo)
        {
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoItem.TodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(It.IsAny<UserInfo>(), It.IsAny<int>())).ReturnsAsync(new List<SelectListItem>());
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(todoItem.CreatedBy))
                .ReturnsAsync(userInfo);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(It.IsAny<int>())).ReturnsAsync(new Progeny());
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(It.IsAny<int>())).ReturnsAsync(new KanbanBoard());
            _mockKanbanBoardsHttpClient.Setup(x => x.GetProgeniesKanbanBoardsList(It.IsAny<KanbanBoardsRequest>())).ReturnsAsync(new KanbanBoardsResponse());
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(It.IsAny<int>())).ReturnsAsync([]);
        }

        #endregion
    }
}