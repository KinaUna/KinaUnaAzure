using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace KinaUnaWeb.Tests.Controllers.TodosController
{
    public class DeleteTodoTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public DeleteTodoTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            Mock<IUserInfosHttpClient> mockUserInfosHttpClient = new();
            Mock<IProgenyHttpClient> mockProgenyHttpClient = new();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                mockUserInfosHttpClient.Object,
                mockProgenyHttpClient.Object);

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

        #region DeleteTodo Tests

        [Fact]
        public async Task DeleteTodo_Get_Should_Return_View_When_User_Is_Admin()
        {
            // Arrange
            const int itemId = 123;
            TodoItem todoItem = CreateMockTodoItem(itemId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(itemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult? result = await _controller.DeleteTodo(itemId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(viewResult.Model);
            Assert.Equal(itemId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task DeleteTodo_Get_Should_Redirect_When_User_Not_Admin()
        {
            // Arrange
            const int itemId = 123;
            TodoItem todoItem = CreateMockTodoItem(itemId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.CurrentProgeny.Admins = "other@example.com";

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(itemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult? result = await _controller.DeleteTodo(itemId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteTodo_Post_Should_Delete_And_Redirect_When_Successful()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            TodoItem todoItem = CreateMockTodoItem(model.TodoItem.TodoItemId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(model.TodoItem.TodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.DeleteTodoItem(todoItem.TodoItemId))
                .ReturnsAsync(true);

            // Act
            IActionResult? result = await _controller.DeleteTodo(model);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Todos", redirectResult.ControllerName);

            _mockTodoItemsHttpClient.Verify(x => x.DeleteTodoItem(todoItem.TodoItemId), Times.Once);
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

        private static TodoViewModel CreateMockTodoViewModelUserIsAdmin()
        {
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            return new TodoViewModel(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = 123,
                    ProgenyId = 1,
                    Title = "Test Todo",
                    Description = "Test Description"
                },
                ProgenyList = [new SelectListItem("Test Child", "1")],
            };
        }

        #endregion
    }
}
