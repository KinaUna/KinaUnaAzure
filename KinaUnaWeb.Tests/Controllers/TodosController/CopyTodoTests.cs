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
    public class CopyTodoTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public CopyTodoTests()
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

        #region CopyTodo Tests

        [Fact]
        public async Task CopyTodo_Get_Should_Return_PartialView_When_Access_Allowed()
        {
            // Arrange
            const int itemId = 123;
            TodoItem todoItem = CreateMockTodoItem(itemId);
            todoItem.AccessLevel = 1;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(itemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(It.IsAny<UserInfo>(), It.IsAny<int>()))
                .ReturnsAsync([new("Test Child", "1")]);

            // Act
            IActionResult? result = await _controller.CopyTodo(itemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_CopyTodoPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(itemId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task CopyTodo_Get_Should_Return_AccessDenied_When_Access_Not_Allowed()
        {
            // Arrange
            const int itemId = 123;
            TodoItem todoItem = CreateMockTodoItem(itemId);
            todoItem.AccessLevel = 0;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(itemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(It.IsAny<UserInfo>(), It.IsAny<int>()))
                .ReturnsAsync([new("Test Child", "1")]);

            // Act
            IActionResult? result = await _controller.CopyTodo(itemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task CopyTodo_Post_Should_Return_TodoCopiedPartial_When_Successful()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;
            TodoItem copiedTodoItem = new() { TodoItemId = 456, Title = "Copied Todo" };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, model.TodoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(copiedTodoItem);

            // Act
            IActionResult? result = await _controller.CopyTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoCopiedPartial", partialViewResult.ViewName);
            TodoViewModel resultModel = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(456, resultModel.TodoItem.TodoItemId);

            _mockTodoItemsHttpClient.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Once);
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
