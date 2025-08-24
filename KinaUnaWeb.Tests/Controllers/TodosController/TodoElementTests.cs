using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace KinaUnaWeb.Tests.Controllers.TodosController
{
    public class TodoElementTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public TodoElementTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            Mock<ISubtasksHttpClient> mockSubtasksHttpClient = new();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                mockSubtasksHttpClient.Object);

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

        #region TodoElement Tests

        [Fact]
        public async Task TodoElement_Should_Return_PartialView_For_New_TodoItem()
        {
            // Arrange
            TodoItemParameters parameters = new() { TodoItemId = 0, LanguageId = 1 };

            // Act
            IActionResult? result = await _controller.TodoElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoItemElementPartial", partialViewResult.ViewName);

            TodoItemResponse model = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(0, model.TodoItem.TodoItemId);
            Assert.Equal(1, model.LanguageId);
        }

        [Fact]
        public async Task TodoElement_Should_Return_PartialView_For_Existing_TodoItem()
        {
            // Arrange
            const int todoItemId = 123;
            TodoItemParameters parameters = new() { TodoItemId = todoItemId, LanguageId = 1 };

            TodoItem todoItem = new()
            {
                TodoItemId = todoItemId,
                ProgenyId = 1,
                CreatedBy = "user123"
            };
            Progeny progeny = new() { Id = 1, Name = "Test Child" };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            UserInfo userInfo = new() { FirstName = "John", LastName = "Doe" };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoItemId))
                .ReturnsAsync(todoItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(progeny);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 1))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId("user123"))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult? result = await _controller.TodoElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoItemElementPartial", partialViewResult.ViewName);

            TodoItemResponse model = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(todoItemId, model.TodoItem.TodoItemId);
            Assert.Equal(progeny, model.TodoItem.Progeny);
            Assert.Equal("John Doe", model.TodoItem.CreatedBy);

            _mockTodoItemsHttpClient.Verify(x => x.GetTodoItem(todoItemId), Times.Once);
            _mockProgenyHttpClient.Verify(x => x.GetProgeny(1), Times.Once);
            _mockUserInfosHttpClient.Verify(x => x.GetUserInfoByUserId("user123"), Times.Once);
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
