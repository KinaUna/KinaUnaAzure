using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
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
    public class GetTodoItemsListTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public GetTodoItemsListTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            Mock<IViewModelSetupService> mockViewModelSetupService = new();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            Mock<IProgenyHttpClient> mockProgenyHttpClient = new();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
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

        #region GetTodoItemsList Tests

        [Fact]
        public async Task GetTodoItemsList_Should_Return_Json_With_TodosPageResponse()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                Progenies = [1, 2],
                CurrentPageNumber = 1,
                ItemsPerPage = 10,
                LanguageId = 1
            };

            UserInfo userInfo = new() { Timezone = "UTC" };
            List<TodoItem> todoItems =
            [
                new() { TodoItemId = 1, DueDate = DateTime.UtcNow.AddDays(1) },
                new() { TodoItemId = 2, StartDate = DateTime.UtcNow.AddDays(-1) }
            ];

            TodoItemsResponse todoItemsResponse = new() { TodoItems = todoItems };

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(todoItemsResponse);

            // Act
            IActionResult? result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodosPageResponse response = Assert.IsType<TodosPageResponse>(jsonResult.Value);

            Assert.Equal(response.TodosList.Count, todoItems.Count);
            _mockUserInfosHttpClient.Verify(x => x.GetUserInfo(TestUserEmail), Times.Once);
            _mockTodoItemsHttpClient.Verify(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()), Times.Once);
        }

        [Fact]
        public async Task GetTodoItemsList_Should_Set_Default_Values_For_Invalid_Parameters()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                LanguageId = 0,
                CurrentPageNumber = 0,
                ItemsPerPage = 0
            };

            UserInfo userInfo = new() { Timezone = "UTC" };
            TodoItemsResponse todoItemsResponse = new() { TodoItems = [] };

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(todoItemsResponse);

            // Act
            IActionResult? result = await _controller.GetTodoItemsList(parameters);

            // Assert
            Assert.Equal(1, parameters.CurrentPageNumber);
            Assert.Equal(10, parameters.ItemsPerPage);

            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<TodosPageResponse>(jsonResult.Value);
        }

        [Fact]
        public async Task GetTodoItemsList_Should_Convert_Timezones_For_TodoItems()
        {
            // Arrange
            TodoItemsPageParameters parameters = new() { LanguageId = 1 };
            UserInfo userInfo = new() { Timezone = "Eastern Standard Time" };
            DateTime utcTime = new(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            List<TodoItem> todoItems =
            [
                new()
                {
                    TodoItemId = 1,
                    DueDate = utcTime,
                    StartDate = utcTime,
                    CompletedDate = utcTime,
                    CreatedTime = utcTime
                }
            ];

            TodoItemsResponse todoItemsResponse = new() { TodoItems = todoItems };

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(todoItemsResponse);

            // Act
            IActionResult? result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodosPageResponse response = Assert.IsType<TodosPageResponse>(jsonResult.Value);

            // Verify timezone conversion occurred (dates should be different from UTC)
            var convertedTodoItem = response.TodosList[0];
            Assert.NotEqual(utcTime, convertedTodoItem.DueDate);
            Assert.NotEqual(utcTime, convertedTodoItem.StartDate);
            Assert.NotEqual(utcTime, convertedTodoItem.CompletedDate);
            Assert.NotEqual(utcTime, convertedTodoItem.CreatedTime);
        }

        #endregion
    }
}
