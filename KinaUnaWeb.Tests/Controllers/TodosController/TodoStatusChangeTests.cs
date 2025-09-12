using KinaUna.Data.Models;
using KinaUnaWeb.Models;
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
    public class TodoStatusChangeTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public TodoStatusChangeTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            Mock<IUserInfosHttpClient> mockUserInfosHttpClient = new();
            Mock<IProgenyHttpClient> mockProgenyHttpClient = new();
            Mock<ISubtasksHttpClient> mockSubtasksHttpClient = new();
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                mockUserInfosHttpClient.Object,
                mockProgenyHttpClient.Object,
                mockSubtasksHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);

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

        #region Status Change Tests

        [Theory]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted), KinaUnaTypes.TodoStatusType.NotStarted)]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress), KinaUnaTypes.TodoStatusType.InProgress)]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled), KinaUnaTypes.TodoStatusType.Cancelled)]
        public async Task SetTodoStatus_Should_Update_Status_And_Return_Json_When_User_Is_Admin(string methodName, KinaUnaTypes.TodoStatusType expectedStatus)
        {
            // Arrange
            const int todoId = 123;
            TodoItem todoItem = CreateMockTodoItem(todoId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;
            TodoItem updatedTodoItem = new() { TodoItemId = todoId, Status = (int)expectedStatus };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);

            // Act
            IActionResult result = methodName switch
            {
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted) => await _controller.SetTodoAsNotStarted(todoId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress) => await _controller.SetTodoAsInProgress(todoId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled) => await _controller.SetTodoAsCancelled(todoId),
                _ => throw new ArgumentException($"Unknown method: {methodName}")
            };

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem returnedTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)expectedStatus, returnedTodoItem.Status);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.Status == (int)expectedStatus)), Times.Once);
        }

        [Fact]
        public async Task SetTodoAsCompleted_Should_Set_CompletedDate_And_Return_Json()
        {
            // Arrange
            const int todoId = 123;
            TodoItem todoItem = CreateMockTodoItem(todoId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;
            TodoItem updatedTodoItem = new()
            {
                TodoItemId = todoId,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CompletedDate = DateTime.UtcNow
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);

            // Act
            IActionResult? result = await _controller.SetTodoAsCompleted(todoId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem returnedTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Completed, returnedTodoItem.Status);
            Assert.NotNull(returnedTodoItem.CompletedDate);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.Completed &&
                t.CompletedDate.HasValue)), Times.Once);
        }

        [Theory]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted))]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress))]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCompleted))]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled))]
        public async Task SetTodoStatus_Should_Return_Unauthorized_When_User_Not_Admin(string methodName)
        {
            // Arrange
            const int todoId = 123;
            TodoItem todoItem = CreateMockTodoItem(todoId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.CurrentProgeny.Admins = "other@example.com";

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = methodName switch
            {
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted) => await _controller.SetTodoAsNotStarted(todoId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress) => await _controller.SetTodoAsInProgress(todoId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCompleted) => await _controller.SetTodoAsCompleted(todoId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled) => await _controller.SetTodoAsCancelled(todoId),
                _ => throw new ArgumentException($"Unknown method: {methodName}")
            };

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Access denied.", unauthorizedResult.Value);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.IsAny<TodoItem>()), Times.Never);
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

        #endregion
    }
}
