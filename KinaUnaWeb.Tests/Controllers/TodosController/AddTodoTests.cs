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
    public class AddTodoTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public AddTodoTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            Mock<IUserInfosHttpClient> mockUserInfosHttpClient = new();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object);

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

        #region AddTodo Tests

        [Fact]
        public async Task AddTodo_Get_Should_Return_PartialView_With_TodoViewModel()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            List<SelectListItem> progenySelectList = [];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(It.IsAny<UserInfo>(), 0))
                .ReturnsAsync(progenySelectList);

            // Act
            IActionResult? result = await _controller.AddTodo();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AddTodoPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.TodoItem);
        }

        [Fact]
        public async Task AddTodo_Get_Should_Return_NotFound_When_CurrentUser_Null()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentUser = null;

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult? result = await _controller.AddTodo();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddTodo_Post_Should_Return_TodoAddedPartial_When_Successful()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            List<Progeny> progenyList = [new() { Id = 1, Name = "Test Child" }];
            TodoItem addedTodoItem = new() { TodoItemId = 123, Title = "New Todo" };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, model.TodoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgenyAdminList(baseModel.CurrentUser.UserEmail))
                .ReturnsAsync(progenyList);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedTodoItem);

            // Act
            IActionResult? result = await _controller.AddTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoAddedPartial", partialViewResult.ViewName);
            TodoViewModel resultModel = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(123, resultModel.TodoItem.TodoItemId);

            _mockTodoItemsHttpClient.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task AddTodo_Post_Should_Redirect_When_No_Progeny_Admin_Access()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsNotAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            List<Progeny> emptyProgenyList = [];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, model.TodoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgenyAdminList(baseModel.CurrentUser.UserEmail))
                .ReturnsAsync(emptyProgenyList);

            // Act
            IActionResult? result = await _controller.AddTodo(model);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            _mockTodoItemsHttpClient.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Never);
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

        private static TodoViewModel CreateMockTodoViewModelUserIsNotAdmin()
        {
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
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
