using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
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

namespace KinaUnaWeb.Tests.Controllers.SubtasksController
{
    public class SubtasksControllerTests
    {
        private readonly Mock<ISubtasksHttpClient> _mockSubtasksHttpClient;
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly KinaUnaWeb.Controllers.SubtasksController _controller;
        
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";
        private const string TestUserTimezone = "Central Standard Time";
        private const int TestProgenyId = 1;
        private const int TestSubtaskId = 123;
        private const int TestParentTodoItemId = 456;

        public SubtasksControllerTests()
        {
            _mockSubtasksHttpClient = new Mock<ISubtasksHttpClient>();
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new Mock<IKanbanItemsHttpClient>();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new Mock<IKanbanBoardsHttpClient>();

            _controller = new KinaUnaWeb.Controllers.SubtasksController(
                _mockSubtasksHttpClient.Object,
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
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

        #region GetSubtasksList Tests

        [Fact]
        public async Task GetSubtasksList_Should_Set_Default_LanguageId_When_Zero()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 0,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            SubtasksResponse response = CreateMockSubtasksResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockSubtasksHttpClient.Setup(x => x.GetSubtasksList(It.IsAny<SubtasksRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetSubtasksList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            SubtasksPageResponse pageResponse = Assert.IsType<SubtasksPageResponse>(jsonResult.Value);
            Assert.Equal(TestParentTodoItemId, pageResponse.ParentTodoItemId);
            
            _mockUserInfosHttpClient.Verify(x => x.GetUserInfo(TestUserEmail), Times.Once);
        }

        [Fact]
        public async Task GetSubtasksList_Should_Set_Default_CurrentPageNumber_When_Less_Than_One()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 1,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                CurrentPageNumber = 0,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            SubtasksResponse response = CreateMockSubtasksResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockSubtasksHttpClient.Setup(x => x.GetSubtasksList(It.IsAny<SubtasksRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetSubtasksList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<SubtasksPageResponse>(jsonResult.Value);

            _mockSubtasksHttpClient.Verify(x => x.GetSubtasksList(It.Is<SubtasksRequest>(r => 
                r.Skip == 0)), Times.Once);
        }

        [Fact]
        public async Task GetSubtasksList_Should_Set_Default_ItemsPerPage_When_Less_Than_One()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 1,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                CurrentPageNumber = 1,
                ItemsPerPage = 0
            };

            UserInfo userInfo = CreateMockUserInfo();
            SubtasksResponse response = CreateMockSubtasksResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockSubtasksHttpClient.Setup(x => x.GetSubtasksList(It.IsAny<SubtasksRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetSubtasksList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<SubtasksPageResponse>(jsonResult.Value);

            _mockSubtasksHttpClient.Verify(x => x.GetSubtasksList(It.Is<SubtasksRequest>(r => 
                r.NumberOfItems == 10)), Times.Once);
        }

        [Fact]
        public async Task GetSubtasksList_Should_Convert_Subtask_Dates_To_User_Timezone()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 1,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItem subtask = new()
            {
                TodoItemId = TestSubtaskId,
                DueDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                CompletedDate = DateTime.UtcNow.AddHours(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-2)
            };
            SubtasksResponse response = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = [subtask]
            };

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockSubtasksHttpClient.Setup(x => x.GetSubtasksList(It.IsAny<SubtasksRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetSubtasksList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            SubtasksPageResponse pageResponse = Assert.IsType<SubtasksPageResponse>(jsonResult.Value);
            TodoItem resultSubtask = pageResponse.SubtasksList.First();
            
            // Verify dates were converted (they should be different from UTC)
            Assert.NotNull(resultSubtask.DueDate);
            Assert.NotNull(resultSubtask.StartDate);
            Assert.NotNull(resultSubtask.CompletedDate);
        }

        #endregion

        #region SubtaskElement Tests

        [Fact]
        public async Task SubtaskElement_Should_Set_Default_LanguageId_When_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestSubtaskId,
                LanguageId = 0
            };

            TodoItem subtask = CreateMockSubtask();
            Progeny progeny = CreateMockProgeny();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_SubtaskElementPartial", partialViewResult.ViewName);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, response.TodoItemId);
        }

        [Fact]
        public async Task SubtaskElement_Should_Return_New_TodoItem_When_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = 0,
                LanguageId = 1
            };

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_SubtaskElementPartial", partialViewResult.ViewName);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(0, response.TodoItem.TodoItemId);
            Assert.Equal(1, response.LanguageId);
        }

        [Fact]
        public async Task SubtaskElement_Should_Load_Full_Subtask_Data_When_TodoItemId_Not_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestSubtaskId,
                LanguageId = 1
            };

            TodoItem subtask = CreateMockSubtask();
            Progeny progeny = CreateMockProgeny();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, response.TodoItem.TodoItemId);
            Assert.Equal(TestSubtaskId, response.TodoItemId);
            Assert.True(response.IsCurrentUserProgenyAdmin);
            Assert.Equal("John Doe", response.TodoItem.CreatedBy);
            
            _mockSubtasksHttpClient.Verify(x => x.GetSubtask(TestSubtaskId), Times.Once);
            _mockProgenyHttpClient.Verify(x => x.GetProgeny(TestProgenyId), Times.Once);
        }

        #endregion

        #region ViewSubtask Tests

        [Fact]
        public async Task ViewSubtask_Should_Return_View_When_PartialView_False()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.ViewSubtask(TestSubtaskId, false);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(viewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
            Assert.Equal("John Doe", model.TodoItem.CreatedBy);
            Assert.NotNull(model.TodoItem.Progeny);
        }

        [Fact]
        public async Task ViewSubtask_Should_Return_PartialView_When_PartialView_True()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.ViewSubtask(TestSubtaskId, true);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_SubtaskDetailsPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
        }

        #endregion

        #region AddSubtask Tests

        [Fact]
        public async Task AddSubtask_Get_Should_Return_PartialView_When_User_Is_Authenticated()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<SelectListItem> progenySelectList = [new SelectListItem { Text = "Test Child", Value = "1" }];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(baseModel.CurrentUser, 0))
                .ReturnsAsync(progenySelectList);

            // Act
            IActionResult result = await _controller.AddSubtask();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AddSubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.CurrentUser);
            Assert.Equal(progenySelectList, model.ProgenyList);
        }

        [Fact]
        public async Task AddSubtask_Get_Should_Return_NotFound_When_User_Is_Null()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.CurrentUser = null;
            baseModel.SetCurrentUsersAccessLevel();
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, 0))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.AddSubtask();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddSubtask_Post_Should_Add_Subtask_And_Return_PartialView()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<Progeny> progAdminList = [CreateMockProgeny()];
            TodoItem addedSubtask = CreateMockSubtask();

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgenyAdminList(TestUserEmail))
                .ReturnsAsync(progAdminList);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.AddSubtask(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_SubtaskAddedPartial", partialViewResult.ViewName);
            TodoViewModel resultModel = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, resultModel.TodoItem.TodoItemId);
            
            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task AddSubtask_Post_Should_Redirect_When_User_Has_No_Admin_Access()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsNotAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<Progeny> progAdminList = [];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgenyAdminList(TestUserEmail))
                .ReturnsAsync(progAdminList);

            // Act
            IActionResult result = await _controller.AddSubtask(model);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Todos", redirectResult.ControllerName);
        }

        #endregion

        #region AddSubtaskInline Tests

        [Fact]
        public async Task AddSubtaskInline_Should_Add_Subtask_And_Return_Json()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "Test Inline Subtask"
            };
            TodoItem parentTodoItem = CreateMockTodoItem(TestParentTodoItemId);
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<Progeny> progAdminList = [CreateMockProgeny()];
            TodoItem addedSubtask = CreateMockSubtask();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgenyAdminList(TestUserEmail))
                .ReturnsAsync(progAdminList);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.AddSubtaskInline(todoItem);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal(TestSubtaskId, resultTodoItem.TodoItemId);
            
            _mockTodoItemsHttpClient.Verify(x => x.GetTodoItem(TestParentTodoItemId), Times.Once);
            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        #endregion

        #region EditSubtask Tests

        [Fact]
        public async Task EditSubtask_Get_Should_Return_PartialView_When_User_Is_Admin()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<SelectListItem> progenySelectList = [new SelectListItem { Text = "Test Child", Value = "1" }];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(baseModel.CurrentUser, 0))
                .ReturnsAsync(progenySelectList);

            // Act
            IActionResult result = await _controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_EditSubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Return_AccessDenied_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Update_Subtask_And_Return_PartialView()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            TodoItem updatedSubtask = CreateMockSubtask();
            TodoItem parentTodoItem = CreateMockTodoItem(TestParentTodoItemId);

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            // Act
            IActionResult result = await _controller.EditSubtask(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("../Todos/_TodoDetailsPartial", partialViewResult.ViewName);
            
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Return_AccessDenied_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsNotAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.EditSubtask(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        #endregion

        #region DeleteSubtask Tests

        [Fact]
        public async Task DeleteSubtask_Get_Should_Return_PartialView_When_User_Is_Admin()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DeleteSubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Redirect_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Todos", redirectResult.ControllerName);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Delete_Subtask_And_Return_Json()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            TodoItem parentTodoItem = CreateMockTodoItem(TestParentTodoItemId);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            // Act
            IActionResult result = await _controller.DeleteSubtask(model);

            // Assert
            Assert.IsType<JsonResult>(result);
            
            _mockSubtasksHttpClient.Verify(x => x.DeleteSubtask(TestSubtaskId), Times.Once);
        }

        #endregion

        #region CopySubtask Tests

        [Fact]
        public async Task CopySubtask_Get_Should_Return_PartialView_When_User_Has_Access()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            List<SelectListItem> progenySelectList = [new SelectListItem { Text = "Test Child", Value = "1" }];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(baseModel.CurrentUser, 0))
                .ReturnsAsync(progenySelectList);

            // Act
            IActionResult result = await _controller.CopySubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_CopySubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task CopySubtask_Get_Should_Return_AccessDenied_When_User_Has_No_Access()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            subtask.AccessLevel = 0; // Private access
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.CopySubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task CopySubtask_Post_Should_Create_Copy_And_Return_PartialView()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModelUserIsAdmin();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            TodoItem copiedSubtask = CreateMockSubtask();

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(copiedSubtask);

            // Act
            IActionResult result = await _controller.CopySubtask(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_SubtaskCopiedPartial", partialViewResult.ViewName);
            TodoViewModel resultModel = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, resultModel.TodoItem.TodoItemId);
            
            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        #endregion

        #region Status Change Tests

        [Theory]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsNotStarted), KinaUnaTypes.TodoStatusType.NotStarted)]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsInProgress), KinaUnaTypes.TodoStatusType.InProgress)]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCancelled), KinaUnaTypes.TodoStatusType.Cancelled)]
        public async Task SetSubtaskStatus_Should_Update_Status_And_Return_Json(string methodName, KinaUnaTypes.TodoStatusType expectedStatus)
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            TodoItem updatedSubtask = CreateMockSubtask();
            updatedSubtask.Status = (int)expectedStatus;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = methodName switch
            {
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsNotStarted) => await _controller.SetSubtaskAsNotStarted(TestSubtaskId),
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsInProgress) => await _controller.SetSubtaskAsInProgress(TestSubtaskId),
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCancelled) => await _controller.SetSubtaskAsCancelled(TestSubtaskId),
                _ => throw new ArgumentException($"Unknown method: {methodName}")
            };

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)expectedStatus, resultTodoItem.Status);
            
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t => 
                t.Status == (int)expectedStatus)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Set_CompletedDate_And_Return_Json()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsAdmin();
            baseModel.SetCurrentUsersAccessLevel();
            TodoItem updatedSubtask = CreateMockSubtask();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            updatedSubtask.CompletedDate = DateTime.UtcNow;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Completed, resultTodoItem.Status);
            Assert.NotNull(resultTodoItem.CompletedDate);
            
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t => 
                t.Status == (int)KinaUnaTypes.TodoStatusType.Completed && 
                t.CompletedDate.HasValue)), Times.Once);
        }

        [Theory]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsNotStarted))]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsInProgress))]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCompleted))]
        [InlineData(nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCancelled))]
        public async Task SetSubtaskStatus_Should_Return_Unauthorized_When_User_Is_Not_Admin(string methodName)
        {
            // Arrange
            TodoItem subtask = CreateMockSubtask();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelUserIsNotAdmin();
            baseModel.SetCurrentUsersAccessLevel();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, TestProgenyId))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = methodName switch
            {
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsNotStarted) => await _controller.SetSubtaskAsNotStarted(TestSubtaskId),
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsInProgress) => await _controller.SetSubtaskAsInProgress(TestSubtaskId),
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCompleted) => await _controller.SetSubtaskAsCompleted(TestSubtaskId),
                nameof(KinaUnaWeb.Controllers.SubtasksController.SetSubtaskAsCancelled) => await _controller.SetSubtaskAsCancelled(TestSubtaskId),
                _ => throw new ArgumentException($"Unknown method: {methodName}")
            };

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Access denied.", unauthorizedResult.Value);
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
                    Timezone = TestUserTimezone
                },
                CurrentProgeny = new Progeny
                {
                    Id = TestProgenyId,
                    Name = "Test Child",
                    Admins = TestUserEmail
                },
                CurrentProgenyAccessList =
                [
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
                    Timezone = TestUserTimezone
                },
                CurrentProgeny = new Progeny
                {
                    Id = TestProgenyId,
                    Name = "Test Child",
                    Admins = ""
                },
                CurrentProgenyAccessList =
                [
                    new UserAccess { UserId = TestUserEmail, AccessLevel = (int)AccessLevel.Family }
                ],
                IsCurrentUserProgenyAdmin = false,
                LanguageId = 1
            };
        }

        private static UserInfo CreateMockUserInfo()
        {
            return new UserInfo
            {
                UserEmail = TestUserEmail,
                UserId = TestUserId,
                FirstName = "John",
                LastName = "Doe",
                Timezone = TestUserTimezone
            };
        }

        private static TodoItem CreateMockSubtask()
        {
            return new TodoItem
            {
                TodoItemId = TestSubtaskId,
                ProgenyId = TestProgenyId,
                ParentTodoItemId = TestParentTodoItemId,
                Title = "Test Subtask",
                Description = "Test Description",
                CreatedBy = TestUserId,
                AccessLevel = 1,
                Status = 0,
                CreatedTime = DateTime.UtcNow
            };
        }

        private static TodoItem CreateMockTodoItem(int todoItemId)
        {
            return new TodoItem
            {
                TodoItemId = todoItemId,
                ProgenyId = TestProgenyId,
                Title = "Test Todo Item",
                Description = "Test Description",
                CreatedBy = TestUserId,
                AccessLevel = 1,
                Status = 0,
                CreatedTime = DateTime.UtcNow
            };
        }

        private static Progeny CreateMockProgeny()
        {
            return new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Child",
                Admins = TestUserEmail
            };
        }

        private static SubtasksResponse CreateMockSubtasksResponse()
        {
            return new SubtasksResponse
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = [CreateMockSubtask()],
                PageNumber = 1,
                TotalPages = 1,
                TotalItems = 1
            };
        }

        private static TodoViewModel CreateMockTodoViewModelUserIsAdmin()
        {
            TodoViewModel model = new(CreateMockBaseItemsViewModelUserIsAdmin())
            {
                TodoItem = CreateMockSubtask()
            };

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }

            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }

            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }

            return model;
        }

        private static TodoViewModel CreateMockTodoViewModelUserIsNotAdmin()
        {
            TodoViewModel model = new(CreateMockBaseItemsViewModelUserIsNotAdmin())
            {
                TodoItem = CreateMockSubtask()
            };

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }

            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }
            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));
            }

            return model;
        }

        #endregion
    }
}