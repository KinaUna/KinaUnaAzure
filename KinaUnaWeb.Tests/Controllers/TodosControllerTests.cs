using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Controllers;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace KinaUnaWeb.Tests.Controllers
{
    public class TodosControllerTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public TodosControllerTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();

            _controller = new TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
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

        #region EditTodo Tests

        [Fact]
        public async Task EditTodo_Get_Should_Return_PartialView_When_User_Is_Admin()
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
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(It.IsAny<UserInfo>(), It.IsAny<int>()))
                .ReturnsAsync([new("Test Child", "1")]);

            // Act
            IActionResult? result = await _controller.EditTodo(itemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_EditTodoPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(itemId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task EditTodo_Get_Should_Return_AccessDenied_When_User_Not_Admin()
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
            IActionResult? result = await _controller.EditTodo(itemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditTodo_Post_Should_Return_TodoUpdatedPartial_When_Successful()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentProgeny.Admins = TestUserEmail;
            TodoItem updatedTodoItem = new() { TodoItemId = 123, Title = "Updated Todo" };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, model.TodoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);

            // Act
            IActionResult? result = await _controller.EditTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoUpdatedPartial", partialViewResult.ViewName);
            TodoViewModel resultModel = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(123, resultModel.TodoItem.TodoItemId);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.IsAny<TodoItem>()), Times.Once);
        }

        #endregion

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

        #region Status Change Tests

        [Theory]
        [InlineData(nameof(TodosController.SetTodoAsNotStarted), KinaUnaTypes.TodoStatusType.NotStarted)]
        [InlineData(nameof(TodosController.SetTodoAsInProgress), KinaUnaTypes.TodoStatusType.InProgress)]
        [InlineData(nameof(TodosController.SetTodoAsCancelled), KinaUnaTypes.TodoStatusType.Cancelled)]
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
                nameof(TodosController.SetTodoAsNotStarted) => await _controller.SetTodoAsNotStarted(todoId),
                nameof(TodosController.SetTodoAsInProgress) => await _controller.SetTodoAsInProgress(todoId),
                nameof(TodosController.SetTodoAsCancelled) => await _controller.SetTodoAsCancelled(todoId),
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
        [InlineData(nameof(TodosController.SetTodoAsNotStarted))]
        [InlineData(nameof(TodosController.SetTodoAsInProgress))]
        [InlineData(nameof(TodosController.SetTodoAsCompleted))]
        [InlineData(nameof(TodosController.SetTodoAsCancelled))]
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
                nameof(TodosController.SetTodoAsNotStarted) => await _controller.SetTodoAsNotStarted(todoId),
                nameof(TodosController.SetTodoAsInProgress) => await _controller.SetTodoAsInProgress(todoId),
                nameof(TodosController.SetTodoAsCompleted) => await _controller.SetTodoAsCompleted(todoId),
                nameof(TodosController.SetTodoAsCancelled) => await _controller.SetTodoAsCancelled(todoId),
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



        private void SetupMocksForViewTodo(TodoItem todoItem, BaseItemsViewModel baseModel, UserInfo userInfo)
        {
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(todoItem.TodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, todoItem.ProgenyId))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(todoItem.CreatedBy))
                .ReturnsAsync(userInfo);
        }

        #endregion
    }
}