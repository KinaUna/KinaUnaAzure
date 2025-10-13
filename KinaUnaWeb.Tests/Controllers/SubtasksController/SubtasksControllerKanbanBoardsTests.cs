using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
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

namespace KinaUnaWeb.Tests.Controllers.SubtasksController
{
    public class SubtasksControllerKanbanBoardsTests
    {
        private readonly Mock<ISubtasksHttpClient> _mockSubtasksHttpClient;
        private readonly Mock<IViewModelSetupService> _mockViewModelSetupService;
        private readonly Mock<IUserInfosHttpClient> _mockUserInfosHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly Mock<IFamiliesHttpClient> _mockFamiliesHttpClient;
        private readonly KinaUnaWeb.Controllers.SubtasksController _controller;

        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";
        private const string TestUserTimezone = "Central Standard Time";
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestSubtaskId = 123;
        private const int TestParentTodoItemId = 999;

        public SubtasksControllerKanbanBoardsTests()
        {
            _mockSubtasksHttpClient = new Mock<ISubtasksHttpClient>();
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            _mockViewModelSetupService = new Mock<IViewModelSetupService>();
            _mockUserInfosHttpClient = new Mock<IUserInfosHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            _mockFamiliesHttpClient = new Mock<IFamiliesHttpClient>();
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            _controller = new KinaUnaWeb.Controllers.SubtasksController(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new(OpenIddictConstants.Claims.Email, TestUserEmail),
                new(OpenIddictConstants.Claims.Subject, TestUserId)
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

        #region Helper Methods

        private static BaseItemsViewModel CreateMockBaseItemsViewModelForProgeny()
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
                    PictureLink = "/images/progeny.jpg",
                    ProgenyPerMission = new ProgenyPermission { PermissionLevel = PermissionLevel.Admin }
                },
                LanguageId = 1
            };
        }


        private static TodoItem CreateMockSubtaskForProgeny()
        {
            return new TodoItem
            {
                TodoItemId = TestSubtaskId,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Subtask",
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
                NickName = "Testy",
                Admins = TestUserEmail,
                PictureLink = "/images/progeny.jpg"
            };
        }

        private static Family CreateMockFamily()
        {
            return new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family",
                PictureLink = "/images/family.jpg"
            };
        }
        
        private void SetupControllerContextForController(KinaUnaWeb.Controllers.SubtasksController controller)
        {
            List<Claim> claims =
            [
                new(OpenIddictConstants.Claims.Email, TestUserEmail),
                new(OpenIddictConstants.Claims.Subject, TestUserId)
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

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        #endregion

        #region AddSubtaskToKanbanBoard (GET) Tests

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Return_NotFoundPartial_When_Subtask_Is_Null()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Return_NotFoundPartial_When_TodoItemId_Is_Zero()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Return_AccessDeniedPartial_When_Permission_Level_Too_Low()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.View;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Return_PartialView_With_Model()
        {
            // Arrange
            const int kanbanBoardId1 = 1;
            const int kanbanBoardId2 = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList =
            [
                new() { Text = "Child 1", Value = "1" }
            ];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList =
            [
                new() { Text = "Family 1", Value = "1" }
            ];

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards =
                [
                    new() { KanbanBoardId = kanbanBoardId1, Title = "Board 1", ProgenyId = TestProgenyId, FamilyId = 0 },
                    new() { KanbanBoardId = kanbanBoardId2, Title = "Board 2", ProgenyId = 0, FamilyId = TestFamilyId }
                ]
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForBoard(It.IsAny<int>()))
                .ReturnsAsync([]);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(CreateMockProgeny());
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(TestFamilyId))
                .ReturnsAsync(CreateMockFamily());

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AddSubtaskToKanbanBoardPartial", partialViewResult.ViewName);
            KanbanItemViewModel model = Assert.IsType<KanbanItemViewModel>(partialViewResult.Model);
            Assert.NotNull(model.KanbanItem);
            Assert.Equal(TestSubtaskId, model.KanbanItem.TodoItem.TodoItemId);
            Assert.NotNull(model.KanbanBoardsList);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Populate_KanbanBoardsList_For_Progenies()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList =
            [
                new() { Text = "Child 1", Value = "1" }
            ];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];

            Progeny progeny = CreateMockProgeny();
            progeny.NickName = "TestNick";

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards =
                [
                    new() { KanbanBoardId = kanbanBoardId, Title = "Progeny Board", ProgenyId = TestProgenyId, FamilyId = 0 }
                ]
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForBoard(kanbanBoardId))
                .ReturnsAsync([]);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            KanbanItemViewModel model = Assert.IsType<KanbanItemViewModel>(partialViewResult.Model);
            Assert.Single(model.KanbanBoardsList);
            Assert.Contains("TestNick", model.KanbanBoardsList[0].Text);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Populate_KanbanBoardsList_For_Families()
        {
            // Arrange
            const int kanbanBoardId = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList = [];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList =
            [
                new() { Text = "Family 1", Value = "1" }
            ];

            Family family = CreateMockFamily();
            family.Name = "TestFamily";

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards =
                [
                    new() { KanbanBoardId = kanbanBoardId, Title = "Family Board", ProgenyId = 0, FamilyId = TestFamilyId }
                ]
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForBoard(kanbanBoardId))
                .ReturnsAsync([]);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(TestFamilyId))
                .ReturnsAsync(family);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            KanbanItemViewModel model = Assert.IsType<KanbanItemViewModel>(partialViewResult.Model);
            Assert.Single(model.KanbanBoardsList);
            Assert.Contains("TestFamily", model.KanbanBoardsList[0].Text);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Exclude_Boards_Already_Containing_Subtask()
        {
            // Arrange
            const int kanbanBoardId1 = 1;
            const int kanbanBoardId2 = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList =
            [
                new() { Text = "Child 1", Value = "1" }
            ];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards =
                [
                    new() { KanbanBoardId = kanbanBoardId1, Title = "Board 1", ProgenyId = TestProgenyId, FamilyId = 0 },
                    new() { KanbanBoardId = kanbanBoardId2, Title = "Board 2", ProgenyId = TestProgenyId, FamilyId = 0 }
                ]
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);

            // Board 1 already contains the subtask
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForBoard(kanbanBoardId1))
                .ReturnsAsync([new KanbanItem { TodoItemId = TestSubtaskId }]);

            // Board 2 does not contain the subtask
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForBoard(kanbanBoardId2))
                .ReturnsAsync([]);

            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(CreateMockProgeny());

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            KanbanItemViewModel model = Assert.IsType<KanbanItemViewModel>(partialViewResult.Model);
            Assert.Single(model.KanbanBoardsList); // Only Board 2 should be included
            Assert.Equal(kanbanBoardId2.ToString(), model.KanbanBoardsList[0].Value);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Set_Status_List()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList = [];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards = []
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            KanbanItemViewModel model = Assert.IsType<KanbanItemViewModel>(partialViewResult.Model);
            Assert.NotNull(model.StatusList);
            Assert.NotEmpty(model.StatusList);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Get_Should_Request_All_KanbanBoards_With_NumberOfItems_Zero()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList =
            [
                new() { Text = "Child 1", Value = "1" }
            ];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];

            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoards = []
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoardsList(It.IsAny<KanbanBoardsRequest>()))
                .ReturnsAsync(kanbanBoardsResponse);

            // Act
            await controller.AddSubtaskToKanbanBoard(TestSubtaskId);

            // Assert
            mockKanbanBoardsHttpClient.Verify(x => x.GetKanbanBoardsList(It.Is<KanbanBoardsRequest>(r =>
                r.NumberOfItems == 0 &&
                r.IncludeDeleted == false)), Times.Once);
        }

        #endregion

        #region AddSubtaskToKanbanBoard (POST) Tests

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_NotFound_When_Subtask_Is_Null()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_NotFound_When_TodoItemId_Is_Zero()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_Unauthorized_When_Subtask_Permission_Too_Low()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.View;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_NotFound_When_KanbanBoard_Is_Null()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync((KanbanBoard)null!);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_NotFound_When_KanbanBoardId_Is_Zero()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            KanbanBoard kanbanBoard = new() { KanbanBoardId = 0 };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Return_Unauthorized_When_KanbanBoard_Permission_Too_Low()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View }
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Add_KanbanItem_And_Return_Json()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();


            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted }
                })
            };

            KanbanItem addedKanbanItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = kanbanBoardId,
                ColumnId = columnId,
                TodoItemId = TestSubtaskId,
                RowIndex = -1
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            IActionResult result = await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            KanbanItem resultKanbanItem = Assert.IsType<KanbanItem>(jsonResult.Value);
            Assert.Equal(1, resultKanbanItem.KanbanItemId);
            Assert.Equal(kanbanBoardId, resultKanbanItem.KanbanBoardId);
            Assert.Equal(TestSubtaskId, resultKanbanItem.TodoItemId);

            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.IsAny<KanbanItem>()), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Set_ColumnId_To_First_Column_When_No_Status_Match()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int firstColumnId = 0;
            const int secondColumnId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = firstColumnId, ColumnIndex = 0, SetStatus = -1 }, // No status
                    new() { Id = secondColumnId, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.Completed }
                })
            };

            KanbanItem addedKanbanItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = kanbanBoardId,
                ColumnId = firstColumnId,
                TodoItemId = TestSubtaskId
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.ColumnId == firstColumnId)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Set_ColumnId_To_Matching_Status_Column()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int firstColumnId = 0;
            const int matchingColumnId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = firstColumnId, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                    new() { Id = matchingColumnId, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress }
                })
            };

            KanbanItem addedKanbanItem = new()
            {
                KanbanItemId = 1,
                KanbanBoardId = kanbanBoardId,
                ColumnId = matchingColumnId,
                TodoItemId = TestSubtaskId
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.ColumnId == matchingColumnId)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Set_RowIndex_To_Minus_One()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 0;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId, ColumnIndex = 0, SetStatus = -1 }
                })
            };

            KanbanItem addedKanbanItem = new() { KanbanItemId = 1 };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.RowIndex == -1)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Set_CreatedTime_To_UTC()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 0;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId, ColumnIndex = 0, SetStatus = -1 }
                })
            };

            KanbanItem addedKanbanItem = new() { KanbanItemId = 1 };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.CreatedTime.Kind == DateTimeKind.Utc)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskToKanbanBoard_Post_Should_Set_CreatedBy_To_Current_User_Email()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 0;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            KanbanItemViewModel inputModel = new()
            {
                KanbanItem = new KanbanItem
                {
                    KanbanBoardId = kanbanBoardId,
                    TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Add },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId, ColumnIndex = 0, SetStatus = -1 }
                })
            };

            KanbanItem addedKanbanItem = new() { KanbanItemId = 1 };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.AddKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(addedKanbanItem);

            // Act
            await controller.AddSubtaskToKanbanBoard(inputModel);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.AddKanbanItem(It.Is<KanbanItem>(k =>
                k.CreatedBy == TestUserEmail)), Times.Once);
        }

        #endregion

        #region UpdateKanbanItemsStatus Tests

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Not_Update_When_No_KanbanItems_Exist()
        {
            // Arrange
            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            mockKanbanBoardsHttpClient.Verify(x => x.GetKanbanBoard(It.IsAny<int>()), Times.Never);
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Skip_KanbanItem_When_Board_Is_Null()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync((KanbanBoard)null!);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Skip_KanbanItem_When_Board_Id_Is_Zero()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new() { KanbanBoardId = 0 };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Skip_KanbanItem_When_Board_Permission_Too_Low()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View }
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Update_KanbanItem_Column_When_Status_Matches()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId, ColumnId = 0 }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 1, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress },
                    new() { Id = columnId, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.Completed }
                })
            };

            KanbanItem updatedKanbanItem = new() { KanbanItemId = 1, ColumnId = columnId };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync(updatedKanbanItem);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.ColumnId == columnId &&
                k.RowIndex == -1 &&
                k.TodoItem.Status == (int)KinaUnaTypes.TodoStatusType.Completed)), Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Not_Update_When_No_Matching_Status_Column()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 1, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                    new() { Id = 2, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Update_Multiple_KanbanItems_Across_Different_Boards()
        {
            // Arrange
            const int kanbanBoardId1 = 1;
            const int kanbanBoardId2 = 2;
            const int columnId1 = 1;
            const int columnId2 = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId1, TodoItemId = TestSubtaskId, ColumnId = 0 },
                new() { KanbanItemId = 2, KanbanBoardId = kanbanBoardId2, TodoItemId = TestSubtaskId, ColumnId = 0 }
            ];

            KanbanBoard kanbanBoard1 = new()
            {
                KanbanBoardId = kanbanBoardId1,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 0, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                    new() { Id = columnId1, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress }
                })
            };

            KanbanBoard kanbanBoard2 = new()
            {
                KanbanBoardId = kanbanBoardId2,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 0, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                    new() { Id = columnId2, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId1))
                .ReturnsAsync(kanbanBoard1);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId2))
                .ReturnsAsync(kanbanBoard2);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanBoardId == kanbanBoardId1 && k.ColumnId == columnId1)), Times.Once);
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanBoardId == kanbanBoardId2 && k.ColumnId == columnId2)), Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Skip_Some_Boards_And_Update_Others()
        {
            // Arrange
            const int kanbanBoardId1 = 1; // Will be skipped - no permission
            const int kanbanBoardId2 = 2; // Will be updated
            const int columnId2 = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId1, TodoItemId = TestSubtaskId },
                new() { KanbanItemId = 2, KanbanBoardId = kanbanBoardId2, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard1 = new()
            {
                KanbanBoardId = kanbanBoardId1,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View } // Too low
            };

            KanbanBoard kanbanBoard2 = new()
            {
                KanbanBoardId = kanbanBoardId2,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId2, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.Completed }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId1))
                .ReturnsAsync(kanbanBoard1);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId2))
                .ReturnsAsync(kanbanBoard2);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanBoardId == kanbanBoardId2)), Times.Once);
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.KanbanBoardId == kanbanBoardId1)), Times.Never);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Assign_Updated_Subtask_To_KanbanItem_TodoItem_Property()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int columnId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = columnId, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.NotStarted }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.TodoItem.TodoItemId == TestSubtaskId)), Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Break_After_Finding_Matching_Column()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int firstMatchingColumnId = 1;
            const int secondMatchingColumnId = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            // Two columns with the same status - should use the first one
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = firstMatchingColumnId, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress },
                    new() { Id = secondMatchingColumnId, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.InProgress }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.ColumnId == firstMatchingColumnId)), Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Skip_Columns_With_Negative_SetStatus()
        {
            // Arrange
            const int kanbanBoardId = 1;
            const int matchingColumnId = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 1, ColumnIndex = 0, SetStatus = -1 }, // Should be skipped
                    new() { Id = matchingColumnId, ColumnIndex = 1, SetStatus = (int)KinaUnaTypes.TodoStatusType.Completed }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.Is<KanbanItem>(k =>
                k.ColumnId == matchingColumnId)), Times.Once);
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Call_SetColumnsListFromColumns_For_Each_Board()
        {
            // Arrange
            const int kanbanBoardId1 = 1;
            const int kanbanBoardId2 = 2;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId1, TodoItemId = TestSubtaskId },
                new() { KanbanItemId = 2, KanbanBoardId = kanbanBoardId2, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard1 = new()
            {
                KanbanBoardId = kanbanBoardId1,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 1, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.Cancelled }
                })
            };

            KanbanBoard kanbanBoard2 = new()
            {
                KanbanBoardId = kanbanBoardId2,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>
                {
                    new() { Id = 2, ColumnIndex = 0, SetStatus = (int)KinaUnaTypes.TodoStatusType.Cancelled }
                })
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId1))
                .ReturnsAsync(kanbanBoard1);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId2))
                .ReturnsAsync(kanbanBoard2);
            mockKanbanItemsHttpClient.Setup(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()))
                .ReturnsAsync((KanbanItem k) => k);

            // Act
            await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            mockKanbanBoardsHttpClient.Verify(x => x.GetKanbanBoard(kanbanBoardId1), Times.Once);
            mockKanbanBoardsHttpClient.Verify(x => x.GetKanbanBoard(kanbanBoardId2), Times.Once);
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateKanbanItemsStatus_Should_Handle_Empty_Columns_List()
        {
            // Arrange
            const int kanbanBoardId = 1;

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            Mock<IKanbanBoardsHttpClient> mockKanbanBoardsHttpClient = new();

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                mockKanbanBoardsHttpClient.Object);
            SetupControllerContextForController(controller);

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;

            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, KanbanBoardId = kanbanBoardId, TodoItemId = TestSubtaskId }
            ];

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = kanbanBoardId,
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit },
                Columns = System.Text.Json.JsonSerializer.Serialize(new List<KanbanBoardColumn>())
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);
            mockKanbanBoardsHttpClient.Setup(x => x.GetKanbanBoard(kanbanBoardId))
                .ReturnsAsync(kanbanBoard);

            // Act
            await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.UpdateKanbanItem(It.IsAny<KanbanItem>()), Times.Never);
        }

        #endregion
    }
}
