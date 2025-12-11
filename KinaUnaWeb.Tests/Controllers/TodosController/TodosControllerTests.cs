using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
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

namespace KinaUnaWeb.Tests.Controllers.TodosController
{
    public class TodosControllerTests
    {
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly Mock<IFamiliesHttpClient> _mockFamiliesHttpClient;
        private readonly Mock<ISubtasksHttpClient> _mockSubtasksHttpClient;
        private readonly Mock<IKanbanItemsHttpClient> _mockKanbanItemsHttpClient;
        private readonly Mock<IKanbanBoardsHttpClient> _mockKanbanBoardsHttpClient;
        private readonly KinaUnaWeb.Controllers.TodosController _controller;

        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";
        private const string TestUserTimezone = "Central Standard Time";
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestTodoItemId = 123;
        private const int TestKanbanBoardId = 456;
        private const int TestKanbanItemId = 789;

        public TodosControllerTests()
        {
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            _mockFamiliesHttpClient = new Mock<IFamiliesHttpClient>();
            _mockSubtasksHttpClient = new Mock<ISubtasksHttpClient>();
            _mockKanbanItemsHttpClient = new Mock<IKanbanItemsHttpClient>();
            _mockKanbanBoardsHttpClient = new Mock<IKanbanBoardsHttpClient>();

            _controller = new KinaUnaWeb.Controllers.TodosController(
                _mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                _mockSubtasksHttpClient.Object,
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
        public async Task Index_Should_Return_View_With_Model()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 0, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.Index(null);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoItemsListViewModel model = Assert.IsType<TodoItemsListViewModel>(viewResult.Model);
            Assert.Equal(0, model.PopUpTodoItemId);
        }

        [Fact]
        public async Task Index_Should_Load_TodoItem_When_TodoItemId_Provided()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem todoItem = CreateMockTodoItem();

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 0, 0, false))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult result = await _controller.Index(TestTodoItemId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoItemsListViewModel model = Assert.IsType<TodoItemsListViewModel>(viewResult.Model);
            Assert.Equal(TestTodoItemId, model.PopUpTodoItemId);
            Assert.Single(model.TodoItemsList);
            Assert.Equal(TestTodoItemId, model.TodoItemsList[0].TodoItemId);
        }

        #endregion

        #region GetTodoItemsList Tests

        [Fact]
        public async Task GetTodoItemsList_Should_Set_Default_LanguageId_When_Zero()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                LanguageId = 0,
                Progenies = [TestProgenyId],
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItemsResponse response = CreateMockTodoItemsResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodosPageResponse pageResponse = Assert.IsType<TodosPageResponse>(jsonResult.Value);
            Assert.NotEmpty(pageResponse.TodosList);

            _mockUserInfosHttpClient.Verify(x => x.GetUserInfo(TestUserEmail), Times.Once);
        }

        [Fact]
        public async Task GetTodoItemsList_Should_Set_Default_CurrentPageNumber_When_Less_Than_One()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                LanguageId = 1,
                Progenies = [TestProgenyId],
                CurrentPageNumber = 0,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItemsResponse response = CreateMockTodoItemsResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<TodosPageResponse>(jsonResult.Value);

            _mockTodoItemsHttpClient.Verify(x => x.GetProgeniesTodoItemsList(It.Is<TodoItemsRequest>(r =>
                r.Skip == 0)), Times.Once);
        }

        [Fact]
        public async Task GetTodoItemsList_Should_Set_Default_ItemsPerPage_When_Less_Than_One()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                LanguageId = 1,
                Progenies = [TestProgenyId],
                CurrentPageNumber = 1,
                ItemsPerPage = 0
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItemsResponse response = CreateMockTodoItemsResponse();

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<TodosPageResponse>(jsonResult.Value);

            _mockTodoItemsHttpClient.Verify(x => x.GetProgeniesTodoItemsList(It.Is<TodoItemsRequest>(r =>
                r.NumberOfItems == 10)), Times.Once);
        }

        [Fact]
        public async Task GetTodoItemsList_Should_Convert_TodoItem_Dates_To_User_Timezone()
        {
            // Arrange
            TodoItemsPageParameters parameters = new()
            {
                LanguageId = 1,
                Progenies = [TestProgenyId],
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItem todoItem = new()
            {
                TodoItemId = TestTodoItemId,
                DueDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                CompletedDate = DateTime.UtcNow.AddHours(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-2)
            };
            TodoItemsResponse response = new()
            {
                TodoItems = [todoItem]
            };

            _mockUserInfosHttpClient.Setup(x => x.GetUserInfo(TestUserEmail))
                .ReturnsAsync(userInfo);
            _mockTodoItemsHttpClient.Setup(x => x.GetProgeniesTodoItemsList(It.IsAny<TodoItemsRequest>()))
                .ReturnsAsync(response);

            // Act
            IActionResult result = await _controller.GetTodoItemsList(parameters);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodosPageResponse pageResponse = Assert.IsType<TodosPageResponse>(jsonResult.Value);
            TodoItem resultTodoItem = pageResponse.TodosList.First();

            Assert.NotNull(resultTodoItem.DueDate);
            Assert.NotNull(resultTodoItem.StartDate);
            Assert.NotNull(resultTodoItem.CompletedDate);
        }

        #endregion

        #region TodoElement Tests

        [Fact]
        public async Task TodoElement_Should_Return_New_TodoItem_When_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = 0,
                LanguageId = 1
            };

            // Act
            IActionResult result = await _controller.TodoElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoItemElementPartial", partialViewResult.ViewName);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(0, response.TodoItem.TodoItemId);
            Assert.Equal(1, response.LanguageId);
        }

        [Fact]
        public async Task TodoElement_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestTodoItemId,
                LanguageId = 1
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.TodoElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task TodoElement_Should_Load_Full_TodoItem_Data()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestTodoItemId,
                LanguageId = 1
            };

            TodoItem todoItem = CreateMockTodoItem();
            Progeny progeny = CreateMockProgeny();
            UserInfo userInfo = CreateMockUserInfo();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);
            _mockUserInfosHttpClient.Setup(x => x.GetExtendedUserInfoByUserId(todoItem.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.TodoElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(TestTodoItemId, response.TodoItem.TodoItemId);
            Assert.NotNull(response.TodoItem.Progeny);
        }

        #endregion

        #region ViewTodo Tests

        [Fact]
        public async Task ViewTodo_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.ViewTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task ViewTodo_Should_Return_View_When_PartialView_False()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetExtendedUserInfoByUserId(todoItem.CreatedBy))
                .ReturnsAsync(userInfo);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(new KanbanBoardsResponse { KanbanBoards = [] });

            // Act
            IActionResult result = await _controller.ViewTodo(TestTodoItemId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(viewResult.Model);
            Assert.Equal(TestTodoItemId, model.TodoItem.TodoItemId);
        }

        [Fact]
        public async Task ViewTodo_Should_Return_PartialView_When_PartialView_True()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            UserInfo userInfo = CreateMockUserInfo();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockUserInfosHttpClient.Setup(x => x.GetExtendedUserInfoByUserId(todoItem.CreatedBy))
                .ReturnsAsync(userInfo);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(new KanbanBoardsResponse { KanbanBoards = [] });
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(CreateMockProgeny());
            // Act
            IActionResult result = await _controller.ViewTodo(TestTodoItemId, true);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoDetailsPartial", partialViewResult.ViewName);
        }

        #endregion

        #region AddTodo Tests

        [Fact]
        public async Task AddTodo_Get_Should_Return_PartialView()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            List<SelectListItem> progenyList = [new SelectListItem { Text = "Test Child", Value = "1" }];
            List<SelectListItem> familyList = [new SelectListItem { Text = "Test Family", Value = "1" }];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 0, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(new KanbanBoardsResponse { KanbanBoards = [] });

            // Act
            IActionResult result = await _controller.AddTodo();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AddTodoPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.CurrentUser);
        }

        [Fact]
        public async Task AddTodo_Get_Should_Return_NotFound_When_User_Is_Null()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            baseModel.CurrentUser = null;

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 0, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.AddTodo();

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddTodo_Post_Should_Add_TodoItem_For_Progeny()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem addedTodoItem = CreateMockTodoItem();
            List<Progeny> progenies = [CreateMockProgeny()];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedTodoItem);

            // Act
            IActionResult result = await _controller.AddTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoAddedPartial", partialViewResult.ViewName);

            _mockTodoItemsHttpClient.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task AddTodo_Post_Should_Return_NotFound_When_User_Cannot_Add()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync([]);
            _mockFamiliesHttpClient.Setup(x => x.GetFamiliesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.AddTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddTodo_Post_Should_Add_TodoItem_To_KanbanBoard_When_Specified()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            model.AddToKanbanBoardId = TestKanbanBoardId;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem addedTodoItem = CreateMockTodoItem();
            List<Progeny> progenies = [CreateMockProgeny()];
            KanbanItem addedKanbanItem = new() { KanbanItemId = TestKanbanItemId };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedTodoItem);
            _mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            IActionResult result = await _controller.AddTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoAddedPartial", partialViewResult.ViewName);

            _mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanBoardId == TestKanbanBoardId && k.TodoItemId == TestTodoItemId)), Times.Once);
        }

        #endregion

        #region EditTodo Tests

        [Fact]
        public async Task EditTodo_Get_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.EditTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditTodo_Get_Should_Return_AccessDenied_When_User_Lacks_Permission()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult result = await _controller.EditTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditTodo_Get_Should_Return_PartialView()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.EditTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_EditTodoPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditTodo_Post_Should_Update_TodoItem()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            TodoItem existingTodoItem = CreateMockTodoItem();
            existingTodoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem updatedTodoItem = CreateMockTodoItem();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(existingTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.EditTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoUpdatedPartial", partialViewResult.ViewName);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.IsAny<TodoItem>()), Times.Once);
        }
        
        #endregion

        #region DeleteTodo Tests

        [Fact]
        public async Task DeleteTodo_Get_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.DeleteTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteTodo_Get_Should_Return_AccessDenied_When_User_Not_Admin()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult result = await _controller.DeleteTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteTodo_Get_Should_Return_PartialView()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DeleteTodoPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteTodo_Post_Should_Delete_TodoItem()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.DeleteTodoItem(TestTodoItemId))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteTodo(model);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<TodoItem>(jsonResult.Value);

            _mockTodoItemsHttpClient.Verify(x => x.DeleteTodoItem(TestTodoItemId), Times.Once);
        }

        #endregion

        #region CopyTodo Tests

        [Fact]
        public async Task CopyTodo_Get_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.CopyTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task CopyTodo_Get_Should_Return_PartialView()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.CopyTodo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_CopyTodoPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task CopyTodo_Post_Should_Create_Copy_And_Return_PartialView()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            model.CopyFromTodoId = 999;
            model.CopySubtasks = false;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem copiedTodoItem = CreateMockTodoItem();
            List<Progeny> progenies = [CreateMockProgeny()];

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(copiedTodoItem);

            // Act
            IActionResult result = await _controller.CopyTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoCopiedPartial", partialViewResult.ViewName);

            _mockTodoItemsHttpClient.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task CopyTodo_Post_Should_Copy_Subtasks_When_Specified()
        {
            // Arrange
            TodoViewModel model = CreateMockTodoViewModel();
            model.CopyFromTodoId = 999;
            model.CopySubtasks = true;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            TodoItem copiedTodoItem = CreateMockTodoItem();
            List<Progeny> progenies = [CreateMockProgeny()];
            SubtasksResponse subtasksResponse = new() { Subtasks = [CreateMockTodoItem()] };

            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockProgenyHttpClient.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);
            _mockTodoItemsHttpClient.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(copiedTodoItem);
            _mockSubtasksHttpClient.Setup(x => x.GetSubtasksList(It.IsAny<SubtasksRequest>()))
                .ReturnsAsync(subtasksResponse);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(CreateMockTodoItem());

            // Act
            IActionResult result = await _controller.CopyTodo(model);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TodoCopiedPartial", partialViewResult.ViewName);

            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        #endregion

        #region Status Change Tests

        [Theory]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted), KinaUnaTypes.TodoStatusType.NotStarted)]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress), KinaUnaTypes.TodoStatusType.InProgress)]
        [InlineData(nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled), KinaUnaTypes.TodoStatusType.Cancelled)]
        public async Task SetTodoStatus_Should_Update_Status_And_Return_Json(string methodName, KinaUnaTypes.TodoStatusType expectedStatus)
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            TodoItem updatedTodoItem = CreateMockTodoItem();
            updatedTodoItem.Status = (int)expectedStatus;

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = methodName switch
            {
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsNotStarted) => await _controller.SetTodoAsNotStarted(TestTodoItemId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsInProgress) => await _controller.SetTodoAsInProgress(TestTodoItemId),
                nameof(KinaUnaWeb.Controllers.TodosController.SetTodoAsCancelled) => await _controller.SetTodoAsCancelled(TestTodoItemId),
                _ => throw new ArgumentException($"Unknown method: {methodName}")
            };

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)expectedStatus, resultTodoItem.Status);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.Status == (int)expectedStatus)), Times.Once);
        }

        [Fact]
        public async Task SetTodoAsCompleted_Should_Set_CompletedDate_And_Return_Json()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            TodoItem updatedTodoItem = CreateMockTodoItem();
            updatedTodoItem.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            updatedTodoItem.CompletedDate = DateTime.UtcNow;

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);
            _mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestTodoItemId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.SetTodoAsCompleted(TestTodoItemId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Completed, resultTodoItem.Status);
            Assert.NotNull(resultTodoItem.CompletedDate);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.Completed &&
                t.CompletedDate.HasValue)), Times.Once);
        }

        [Fact]
        public async Task SetTodoStatus_Should_Return_Empty_TodoItem_When_Permission_Insufficient()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult result = await _controller.SetTodoAsCompleted(TestTodoItemId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal(0, resultTodoItem.TodoItemId);
        }

        #endregion

        #region AddTodoItemToKanbanBoard Tests

        [Fact]
        public async Task AddTodoItemToKanbanBoard_Get_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.AddTodoItemToKanbanBoard(TestTodoItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddTodoItemToKanbanBoard_Get_Should_Return_PartialView()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(new KanbanBoardsResponse { KanbanBoards = [] });

            // Act
            IActionResult result = await _controller.AddTodoItemToKanbanBoard(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AddTodoItemToKanbanBoardPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddTodoItemToKanbanBoard_Post_Should_Add_KanbanItem()
        {
            // Arrange
            KanbanItemViewModel model = new(CreateMockBaseItemsViewModel())
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = TestKanbanBoardId,
                    TodoItem = CreateMockTodoItem()
                }
            };
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = TestKanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = "[{\"Id\":1,\"ColumnIndex\":0,\"SetStatus\":0}]"
            };
            KanbanItem addedKanbanItem = new() { KanbanItemId = TestKanbanItemId };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(TestKanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            _mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            IActionResult result = await _controller.AddTodoItemToKanbanBoard(model);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            KanbanItem resultKanbanItem = Assert.IsType<KanbanItem>(jsonResult.Value);
            Assert.Equal(TestKanbanItemId, resultKanbanItem.KanbanItemId);

            _mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.IsAny<KanbanItem>()), Times.Once);
        }

        [Fact]
        public async Task AddTodoItemToKanbanBoard_Post_Should_Return_Unauthorized_When_Permission_Insufficient()
        {
            // Arrange
            KanbanItemViewModel model = new(CreateMockBaseItemsViewModel())
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = TestKanbanBoardId,
                    TodoItem = CreateMockTodoItem()
                }
            };
            TodoItem todoItem = CreateMockTodoItem();
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = TestKanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View }
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(TestKanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await _controller.AddTodoItemToKanbanBoard(model);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region AssignTodoItemTo Tests

        [Fact]
        public async Task AssignTodoItemTo_Get_Should_Return_NotFound_When_TodoItem_Not_Found()
        {
            // Arrange
            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.AssignTodoItemTo(TestTodoItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AssignTodoItemTo_Get_Should_Return_Unauthorized_When_Permission_Insufficient()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);

            // Act
            IActionResult result = await _controller.AssignTodoItemTo(TestTodoItemId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AssignTodoItemTo_Get_Should_Return_PartialView()
        {
            // Arrange
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync([]);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.AssignTodoItemTo(TestTodoItemId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AssignTodoItemToPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AssignTodoItemTo_Post_Should_Update_Assignment()
        {
            // Arrange
            
            TodoItem todoItem = CreateMockTodoItem();
            todoItem.ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit };
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModel();
            baseModel.CurrentProgeny.ProgenyPerMission = new ProgenyPermission { PermissionLevel = PermissionLevel.Add };
            TodoItem updatedTodoItem = CreateMockTodoItem();
            updatedTodoItem.ProgenyId = 2;
            TodoViewModel model = CreateMockTodoViewModelFromBase(baseModel);
            model.TodoItem.ProgenyId = 2; // Assign to different progeny

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestTodoItemId))
                .ReturnsAsync(todoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(It.IsAny<int>(), TestUserEmail, It.IsAny<int>(), 0, false))
                .ReturnsAsync(baseModel);
            _mockTodoItemsHttpClient.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedTodoItem);

            // Act
            IActionResult result = await _controller.AssignTodoItemTo(model);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal(2, resultTodoItem.ProgenyId);

            _mockTodoItemsHttpClient.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t =>
                t.ProgenyId == 2)), Times.Once);
        }

        #endregion

        #region Helper Methods

        private static BaseItemsViewModel CreateMockBaseItemsViewModel()
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
                    Admins = TestUserEmail,
                    ProgenyPerMission = new ProgenyPermission { PermissionLevel = PermissionLevel.Admin }
                },
                CurrentFamily = new Family
                {
                    FamilyId = TestFamilyId,
                    Name = "Test Family",
                    FamilyPermission = new FamilyPermission { PermissionLevel = PermissionLevel.Admin }
                },
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

        private static TodoItem CreateMockTodoItem()
        {
            return new TodoItem
            {
                TodoItemId = TestTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Todo Item",
                Description = "Test Description",
                CreatedBy = TestUserId,
                Status = 0,
                CreatedTime = DateTime.UtcNow,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Admin }
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

        private static TodoItemsResponse CreateMockTodoItemsResponse()
        {
            return new TodoItemsResponse
            {
                TodoItems = [CreateMockTodoItem()],
                PageNumber = 1,
                TotalPages = 1,
                TotalItems = 1
            };
        }

        private static TodoViewModel CreateMockTodoViewModel()
        {
            TodoViewModel model = new(CreateMockBaseItemsViewModel())
            {
                TodoItem = CreateMockTodoItem()
            };

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime,
                TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));

            return model;
        }

        private static TodoViewModel CreateMockTodoViewModelFromBase(BaseItemsViewModel baseItemsViewModel)
        {
            TodoViewModel model = new(baseItemsViewModel)
            {
                TodoItem = CreateMockTodoItem()
            };

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime,
                TimeZoneInfo.FindSystemTimeZoneById(TestUserTimezone));

            return model;
        }

        #endregion
    }
}