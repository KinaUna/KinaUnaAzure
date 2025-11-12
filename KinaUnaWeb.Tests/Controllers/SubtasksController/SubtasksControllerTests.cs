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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace KinaUnaWeb.Tests.Controllers.SubtasksController
{
    public class SubtasksControllerTests
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

        public SubtasksControllerTests()
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

        #region GetSubtasksList Tests

        [Fact]
        public async Task GetSubtasksList_Should_Set_Default_LanguageId_When_Zero()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 0,
                ParentTodoItemId = TestParentTodoItemId,
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
            Assert.NotEmpty(pageResponse.SubtasksList);

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
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            TodoItem subtask = new()
            {
                TodoItemId = TestSubtaskId,
                ParentTodoItemId = TestParentTodoItemId,
                DueDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                CompletedDate = DateTime.UtcNow.AddHours(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-2)
            };
            SubtasksResponse response = new()
            {
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

            Assert.NotNull(resultSubtask.DueDate);
            Assert.NotNull(resultSubtask.StartDate);
            Assert.NotNull(resultSubtask.CompletedDate);
        }

        [Fact]
        public async Task GetSubtasksList_Should_Map_Request_Parameters_Correctly()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 1,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                CurrentPageNumber = 2,
                ItemsPerPage = 20,
                StartYear = 2024,
                StartMonth = 1,
                StartDay = 1,
                EndYear = 2024,
                EndMonth = 12,
                EndDay = 31,
                LocationFilter = "Home",
                TagFilter = "urgent",
                ContextFilter = "work",
                StatusFilter = [KinaUnaTypes.TodoStatusType.InProgress],
                Sort = 1,
                SortBy = 1,
                GroupBy = 1
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
                r.ParentTodoItemId == TestParentTodoItemId &&
                r.ProgenyId == TestProgenyId &&
                r.FamilyId == TestFamilyId &&
                r.Skip == 20 && // (2-1) * 20
                r.NumberOfItems == 20 &&
                r.LocationFilter == "Home" &&
                r.TagFilter == "urgent" &&
                r.ContextFilter == "work" &&
                r.StatusFilter == parameters.StatusFilter &&
                r.Sort == 1 &&
                r.SortBy == 1 &&
                r.GroupBy == 1)), Times.Once);
        }

        [Fact]
        public async Task GetSubtasksList_Should_Return_Empty_List_When_No_Subtasks()
        {
            // Arrange
            SubtasksPageParameters parameters = new()
            {
                LanguageId = 1,
                ParentTodoItemId = TestParentTodoItemId,
                CurrentPageNumber = 1,
                ItemsPerPage = 10
            };

            UserInfo userInfo = CreateMockUserInfo();
            SubtasksResponse response = new()
            {
                Subtasks = []
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
            Assert.Empty(pageResponse.SubtasksList);
        }

        #endregion

        #region SubtaskElement Tests

        [Fact]
        public async Task SubtaskElement_Should_Return_New_Subtask_When_TodoItemId_Is_Zero()
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
        public async Task SubtaskElement_Should_Set_Default_LanguageId_When_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = 0,
                LanguageId = 0
            };

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(1, response.LanguageId); // Should be set from cookie
        }

        [Fact]
        public async Task SubtaskElement_Should_Load_Full_Subtask_Data()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestSubtaskId,
                LanguageId = 1
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            Progeny progeny = CreateMockProgeny();
            Family family = CreateMockFamily();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(TestFamilyId))
                .ReturnsAsync(family);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, response.TodoItem.TodoItemId);
            Assert.NotNull(response.TodoItem.Progeny);
            Assert.Equal(progeny.Id, response.TodoItem.Progeny.Id);
            Assert.NotNull(response.TodoItem.Family);
            Assert.Equal(0, response.TodoItem.Family.FamilyId);
            Assert.Equal("John Doe", response.TodoItem.CreatedBy);
        }

        [Fact]
        public async Task SubtaskElement_Should_Only_Load_Progeny_When_ProgenyId_Greater_Than_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestSubtaskId,
                LanguageId = 1
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ProgenyId = TestProgenyId;
            subtask.FamilyId = 0;
            Progeny progeny = CreateMockProgeny();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(progeny);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.NotNull(response.TodoItem.Progeny);
            Assert.Equal(progeny.Id, response.TodoItem.Progeny.Id);
            Assert.NotNull(response.TodoItem.Family);
            Assert.Equal(0, response.TodoItem.Family.FamilyId);

            _mockProgenyHttpClient.Verify(x => x.GetProgeny(TestProgenyId), Times.Once);
            _mockFamiliesHttpClient.Verify(x => x.GetFamily(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SubtaskElement_Should_Only_Load_Family_When_FamilyId_Greater_Than_Zero()
        {
            // Arrange
            TodoItemParameters parameters = new()
            {
                TodoItemId = TestSubtaskId,
                LanguageId = 1
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ProgenyId = 0;
            subtask.FamilyId = TestFamilyId;
            Family family = CreateMockFamily();
            UserInfo userInfo = CreateMockUserInfo();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(TestFamilyId))
                .ReturnsAsync(family);
            _mockUserInfosHttpClient.Setup(x => x.GetUserInfoByUserId(subtask.CreatedBy))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.SubtaskElement(parameters);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoItemResponse response = Assert.IsType<TodoItemResponse>(partialViewResult.Model);
            Assert.NotNull(response.TodoItem.Progeny);
            Assert.Equal(0, response.TodoItem.Progeny.Id);
            Assert.NotNull(response.TodoItem.Family);
            Assert.Equal(family.FamilyId, response.TodoItem.Family.FamilyId);

            _mockFamiliesHttpClient.Verify(x => x.GetFamily(TestFamilyId), Times.Once);
            _mockProgenyHttpClient.Verify(x => x.GetProgeny(It.IsAny<int>()), Times.Never);
        }

        #endregion

        

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

        private static BaseItemsViewModel CreateMockBaseItemsViewModelForFamily()
        {
            return new BaseItemsViewModel
            {
                CurrentUser = new UserInfo
                {
                    UserEmail = TestUserEmail,
                    UserId = TestUserId,
                    Timezone = TestUserTimezone
                },
                CurrentFamily = new Family
                {
                    FamilyId = TestFamilyId,
                    Name = "Test Family",
                    PictureLink = "/images/family.jpg",
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

        private static TodoItem CreateMockSubtaskForFamily()
        {
            return new TodoItem
            {
                TodoItemId = TestSubtaskId,
                ParentTodoItemId = TestParentTodoItemId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
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

        private static SubtasksResponse CreateMockSubtasksResponse()
        {
            return new SubtasksResponse
            {
                Subtasks = [CreateMockSubtaskForProgeny()],
                SubtasksRequest = new SubtasksRequest
                {
                    ParentTodoItemId = TestParentTodoItemId
                }
            };
        }

        #endregion

        

        

        #region AddSubtaskInline Tests

        [Fact]
        public async Task AddSubtaskInline_Should_Return_NotFound_When_Parent_TodoItem_Is_Null()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Subtask"
            };

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Return_NotFound_When_Parent_TodoItemId_Is_Zero()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Subtask"
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = 0
            };

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Subtask"
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.None
                }
            };

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Add_Subtask_And_Return_Json()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Inline Subtask",
                Description = "Description"
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Add
                }
            };

            TodoItem addedSubtask = CreateMockSubtaskForProgeny();
            addedSubtask.Title = "New Inline Subtask";

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal("New Inline Subtask", resultTodoItem.Title);
            Assert.Equal(TestSubtaskId, resultTodoItem.TodoItemId);

            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.IsAny<TodoItem>()), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Set_ProgenyId_And_FamilyId_From_Parent()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Inline Subtask",
                ProgenyId = 999, // Should be overwritten
                FamilyId = 0 // Should be overwritten
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Add
                }
            };

            TodoItem addedSubtask = CreateMockSubtaskForProgeny();

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(TestProgenyId))
                .ReturnsAsync(CreateMockProgeny());
            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            Assert.IsType<TodoItem>(jsonResult.Value);

            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.Is<TodoItem>(t =>
                t.ProgenyId == TestProgenyId &&
                t.FamilyId == 0)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Set_CreatedTime_To_UTC()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Inline Subtask"
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Add
                }
            };

            TodoItem addedSubtask = CreateMockSubtaskForProgeny();

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.AddSubtask(It.Is<TodoItem>(t =>
                t.CreatedTime.Kind == DateTimeKind.Utc)), Times.Once);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Convert_Result_Dates_To_User_Timezone()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Inline Subtask"
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Add
                }
            };

            TodoItem addedSubtask = CreateMockSubtaskForProgeny();
            addedSubtask.CreatedTime = DateTime.UtcNow;
            addedSubtask.StartDate = DateTime.UtcNow.AddDays(-1);
            addedSubtask.DueDate = DateTime.UtcNow.AddDays(7);
            addedSubtask.CompletedDate = DateTime.UtcNow;

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);

            // All dates should be converted from UTC to user timezone
            Assert.NotEqual(default, resultTodoItem.CreatedTime);
            Assert.NotNull(resultTodoItem.StartDate);
            Assert.NotNull(resultTodoItem.DueDate);
            Assert.NotNull(resultTodoItem.CompletedDate);
        }

        [Fact]
        public async Task AddSubtaskInline_Should_Handle_Null_Optional_Dates()
        {
            // Arrange
            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());

            SetupControllerContextForController(controller);

            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoItem inputTodoItem = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Title = "New Inline Subtask",
                StartDate = null,
                DueDate = null,
                CompletedDate = null
            };

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Add
                }
            };

            TodoItem addedSubtask = CreateMockSubtaskForProgeny();
            addedSubtask.StartDate = null;
            addedSubtask.DueDate = null;
            addedSubtask.CompletedDate = null;

            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);
            _mockSubtasksHttpClient.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await controller.AddSubtaskInline(inputTodoItem);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);

            Assert.Null(resultTodoItem.StartDate);
            Assert.Null(resultTodoItem.DueDate);
            Assert.Null(resultTodoItem.CompletedDate);
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

        #region EditSubtask (GET) Tests

        [Fact]
        public async Task EditSubtask_Get_Should_Return_NotFoundPartial_When_Subtask_Is_Null()
        {
            // Arrange
            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Return_NotFoundPartial_When_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Return_AccessDeniedPartial_When_Permission_Level_Too_Low()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.View;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Return_PartialView_With_Model()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList = [];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];
            List<KanbanItem> kanbanItems = [];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, true))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_EditSubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
            Assert.NotNull(model.ProgenyList);
            Assert.NotNull(model.FamilyList);
            Assert.NotNull(model.KanbanItems);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Populate_Progeny_And_Family_Lists()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList =
            [
                new() { Text = "Child 1", Value = "1" },
                new() { Text = "Child 2", Value = "2" }
            ];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList =
            [
                new() { Text = "Family 1", Value = "1" }
            ];
            List<KanbanItem> kanbanItems = [];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, true))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(2, model.ProgenyList.Count);
            Assert.Single(model.FamilyList);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Load_KanbanItems_For_TodoItem()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList = [];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];
            List<KanbanItem> kanbanItems =
            [
                new() { KanbanItemId = 1, TodoItemId = TestSubtaskId },
                new() { KanbanItemId = 2, TodoItemId = TestSubtaskId }
            ];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, true))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.KanbanItems);
            Assert.Equal(2, model.KanbanItems.Count);
            mockKanbanItemsHttpClient.Verify(x => x.GetKanbanItemsForTodoItem(TestSubtaskId), Times.Once);
        }

        [Fact]
        public async Task EditSubtask_Get_Should_Set_Status_List()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> progenyList = [];
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> familyList = [];
            List<KanbanItem> kanbanItems = [];

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, true))
                .ReturnsAsync(baseModel);
            _mockViewModelSetupService.Setup(x => x.GetProgenySelectList(0))
                .ReturnsAsync(progenyList);
            _mockViewModelSetupService.Setup(x => x.GetFamilySelectList(0))
                .ReturnsAsync(familyList);

            Mock<IKanbanItemsHttpClient> mockKanbanItemsHttpClient = new();
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync(kanbanItems);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                Mock.Of<ITodoItemsHttpClient>(),
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                mockKanbanItemsHttpClient.Object,
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.StatusList);
            Assert.NotEmpty(model.StatusList);
        }

        #endregion

        #region EditSubtask (POST) Tests

        [Fact]
        public async Task EditSubtask_Post_Should_Return_NotFoundPartial_When_Existing_Subtask_Is_Null()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Return_NotFoundPartial_When_Existing_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);

            // Act
            IActionResult result = await _controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Return_AccessDeniedPartial_When_Permission_Level_Too_Low()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.ItemPerMission.PermissionLevel = PermissionLevel.View;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);

            // Act
            IActionResult result = await _controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Update_Subtask_And_Return_Parent_TodoItem()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0,
                    Title = "Updated Subtask",
                    Description = "Updated Description",
                    Status = 1,
                    CreatedTime = DateTime.Now
                }
            };

            inputModel.TodoItem.CreatedTime = DateTime.SpecifyKind(inputModel.TodoItem.CreatedTime, DateTimeKind.Unspecified);
            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Title = "Updated Subtask";
            updatedSubtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                CreatedTime = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("../Todos/_TodoDetailsPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestParentTodoItemId, model.TodoItem.TodoItemId);

            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.IsAny<TodoItem>()), Times.Once);
            mockTodoItemsHttpClient.Verify(x => x.GetTodoItem(TestParentTodoItemId), Times.Once);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Convert_Parent_TodoItem_Dates_To_User_Timezone()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = TestFamilyId,
                    Title = "Updated Subtask"
                }
            };

            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                CreatedTime = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(7),
                CompletedDate = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);

            // All dates should be converted from UTC to user timezone
            Assert.NotEqual(default, model.TodoItem.CreatedTime);
            Assert.NotNull(model.TodoItem.StartDate);
            Assert.NotNull(model.TodoItem.DueDate);
            Assert.NotNull(model.TodoItem.CompletedDate);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Handle_Null_Optional_Dates_For_Parent_TodoItem()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = TestFamilyId,
                    Title = "Updated Subtask"
                }
            };

            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                CreatedTime = DateTime.UtcNow,
                StartDate = null,
                DueDate = null,
                CompletedDate = null
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.EditSubtask(inputModel);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);

            Assert.Null(model.TodoItem.StartDate);
            Assert.Null(model.TodoItem.DueDate);
            Assert.Null(model.TodoItem.CompletedDate);
        }

        [Fact]
        public async Task EditSubtask_Post_Should_Call_UpdateSubtask_With_Correct_Data()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0,
                    Title = "Updated Subtask Title",
                    Description = "Updated Description",
                    Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                    CreatedTime = DateTime.Now
                }
            };

            inputModel.TodoItem.CreatedTime = DateTime.SpecifyKind(inputModel.TodoItem.CreatedTime, DateTimeKind.Unspecified);
            TodoItem existingSubtask = CreateMockSubtaskForProgeny();
            existingSubtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                CreatedTime = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            await controller.EditSubtask(inputModel);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.TodoItemId == TestSubtaskId &&
                t.Title == "Updated Subtask Title" &&
                t.Description == "Updated Description" &&
                t.Status == (int)KinaUnaTypes.TodoStatusType.Completed)), Times.Once);
        }

        #endregion

        #region DeleteSubtask (GET) Tests

        [Fact]
        public async Task DeleteSubtask_Get_Should_Return_NotFoundPartial_When_Subtask_Is_Null()
        {
            // Arrange
            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Return_NotFoundPartial_When_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_NotFoundPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Return_AccessDeniedPartial_When_Permission_Level_Too_Low()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AccessDeniedPartial", partialViewResult.ViewName);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Return_PartialView_With_Model()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DeleteSubtaskPartial", partialViewResult.ViewName);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.Equal(TestSubtaskId, model.TodoItem.TodoItemId);
            Assert.NotNull(model.TodoItem);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Set_Progeny_With_Picture_Link_When_ProgenyId_Greater_Than_Zero()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ProgenyId = TestProgenyId;
            subtask.FamilyId = 0;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.TodoItem.Progeny);
            Assert.Equal(TestProgenyId, model.TodoItem.Progeny.Id);
            Assert.NotNull(model.TodoItem.Progeny.PictureLink);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Set_Family_With_Picture_Link_When_FamilyId_Greater_Than_Zero()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForFamily();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ProgenyId = 0;
            subtask.FamilyId = TestFamilyId;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForFamily();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, 0, TestFamilyId, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.TodoItem.Family);
            Assert.Equal(TestFamilyId, model.TodoItem.Family.FamilyId);
            Assert.NotNull(model.TodoItem.Family.PictureLink);
        }

        [Fact]
        public async Task DeleteSubtask_Get_Should_Set_Status_List()
        {
            // Arrange
            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            // Act
            IActionResult result = await _controller.DeleteSubtask(TestSubtaskId);

            // Assert
            PartialViewResult partialViewResult = Assert.IsType<PartialViewResult>(result);
            TodoViewModel model = Assert.IsType<TodoViewModel>(partialViewResult.Model);
            Assert.NotNull(model.StatusList);
            Assert.NotEmpty(model.StatusList);
        }

        #endregion

        #region DeleteSubtask (POST) Tests

        [Fact]
        public async Task DeleteSubtask_Post_Should_Return_NotFound_When_Subtask_Is_Null()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem)null!);

            // Act
            IActionResult result = await _controller.DeleteSubtask(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Return_NotFound_When_TodoItemId_Is_Zero()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.TodoItemId = 0;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.DeleteSubtask(inputModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
        {
            // Arrange
            TodoViewModel inputModel = new(CreateMockBaseItemsViewModelForProgeny())
            {
                TodoItem = new TodoItem { TodoItemId = TestSubtaskId }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Edit;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.DeleteSubtask(inputModel);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Delete_Subtask_And_Return_Parent_TodoItem()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                CreatedTime = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.DeleteSubtask(inputModel);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal(TestParentTodoItemId, resultTodoItem.TodoItemId);

            _mockSubtasksHttpClient.Verify(x => x.DeleteSubtask(TestSubtaskId), Times.Once);
            mockTodoItemsHttpClient.Verify(x => x.GetTodoItem(TestParentTodoItemId), Times.Once);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Convert_Parent_TodoItem_Dates_To_User_Timezone()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                CreatedTime = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(7),
                CompletedDate = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.DeleteSubtask(inputModel);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);

            // All dates should be converted from UTC to user timezone
            Assert.NotEqual(default, resultTodoItem.CreatedTime);
            Assert.NotNull(resultTodoItem.StartDate);
            Assert.NotNull(resultTodoItem.DueDate);
            Assert.NotNull(resultTodoItem.CompletedDate);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Handle_Null_Optional_Dates_For_Parent_TodoItem()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                Title = "Parent Todo",
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                CreatedTime = DateTime.UtcNow,
                StartDate = null,
                DueDate = null,
                CompletedDate = null
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            IActionResult result = await controller.DeleteSubtask(inputModel);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);

            Assert.Null(resultTodoItem.StartDate);
            Assert.Null(resultTodoItem.DueDate);
            Assert.Null(resultTodoItem.CompletedDate);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Use_Subtask_ProgenyId_And_FamilyId()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ProgenyId = 999, // Should use subtask's values instead
                    FamilyId = 888
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ProgenyId = TestProgenyId;
            subtask.FamilyId = TestFamilyId;
            subtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                CreatedTime = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            await controller.DeleteSubtask(inputModel);

            // Assert
            _mockViewModelSetupService.Verify(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, TestFamilyId, false), Times.Once);
        }

        [Fact]
        public async Task DeleteSubtask_Post_Should_Call_DeleteSubtask_With_Correct_Id()
        {
            // Arrange
            BaseItemsViewModel baseModel = CreateMockBaseItemsViewModelForProgeny();
            TodoViewModel inputModel = new(baseModel)
            {
                TodoItem = new TodoItem
                {
                    TodoItemId = TestSubtaskId,
                    ParentTodoItemId = TestParentTodoItemId,
                    ProgenyId = TestProgenyId,
                    FamilyId = 0
                }
            };

            TodoItem subtask = CreateMockSubtaskForProgeny();
            subtask.ItemPerMission.PermissionLevel = PermissionLevel.Admin;
            subtask.ParentTodoItemId = TestParentTodoItemId;

            TodoItem parentTodoItem = new()
            {
                TodoItemId = TestParentTodoItemId,
                CreatedTime = DateTime.UtcNow
            };

            Mock<ITodoItemsHttpClient> mockTodoItemsHttpClient = new();
            mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.DeleteSubtask(TestSubtaskId))
                .ReturnsAsync(true);
            _mockViewModelSetupService.Setup(x => x.SetupViewModel(1, TestUserEmail, TestProgenyId, 0, false))
                .ReturnsAsync(baseModel);

            KinaUnaWeb.Controllers.SubtasksController controller = new(
                _mockSubtasksHttpClient.Object,
                mockTodoItemsHttpClient.Object,
                _mockViewModelSetupService.Object,
                _mockUserInfosHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object,
                Mock.Of<IKanbanItemsHttpClient>(),
                Mock.Of<IKanbanBoardsHttpClient>());
            SetupControllerContextForController(controller);

            // Act
            await controller.DeleteSubtask(inputModel);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.DeleteSubtask(TestSubtaskId), Times.Once);
        }

        #endregion

        

        
        

        #region SetSubtaskAsNotStarted Tests

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Return_NotFound_When_Subtask_Is_Null()
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
            IActionResult result = await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Return_NotFound_When_TodoItemId_Is_Zero()
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
            IActionResult result = await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
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
            IActionResult result = await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Update_Status_And_Return_Json()
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
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            subtask.CompletedDate = DateTime.UtcNow;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.NotStarted, resultTodoItem.Status);
            Assert.Null(resultTodoItem.CompletedDate);

            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.NotStarted &&
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Clear_CompletedDate()
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
            subtask.CompletedDate = DateTime.UtcNow;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            await controller.SetSubtaskAsNotStarted(TestSubtaskId);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsNotStarted_Should_Call_UpdateKanbanItemsStatus()
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
            mockKanbanItemsHttpClient.Verify(x => x.GetKanbanItemsForTodoItem(TestSubtaskId), Times.Once);
        }

        #endregion

        #region SetSubtaskAsInProgress Tests

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Return_NotFound_When_Subtask_Is_Null()
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
            IActionResult result = await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Return_NotFound_When_TodoItemId_Is_Zero()
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
            IActionResult result = await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
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
            IActionResult result = await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Update_Status_And_Return_Json()
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
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, resultTodoItem.Status);

            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.InProgress &&
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Clear_CompletedDate()
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
            subtask.CompletedDate = DateTime.UtcNow;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsInProgress_Should_Call_UpdateKanbanItemsStatus()
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
            await controller.SetSubtaskAsInProgress(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.GetKanbanItemsForTodoItem(TestSubtaskId), Times.Once);
        }

        #endregion

        #region SetSubtaskAsCompleted Tests

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Return_NotFound_When_Subtask_Is_Null()
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
            IActionResult result = await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Return_NotFound_When_TodoItemId_Is_Zero()
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
            IActionResult result = await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
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
            IActionResult result = await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Update_Status_And_Return_Json()
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
            subtask.CompletedDate = null;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            updatedSubtask.CompletedDate = DateTime.UtcNow;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Completed, resultTodoItem.Status);
            Assert.NotNull(resultTodoItem.CompletedDate);

            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.Completed &&
                t.CompletedDate != null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Set_CompletedDate_To_UTC()
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
            subtask.CompletedDate = null;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.CompletedDate = DateTime.UtcNow;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.CompletedDate.HasValue &&
                t.CompletedDate.Value.Kind == DateTimeKind.Utc)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsCompleted_Should_Call_UpdateKanbanItemsStatus()
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
            await controller.SetSubtaskAsCompleted(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.GetKanbanItemsForTodoItem(TestSubtaskId), Times.Once);
        }

        #endregion

        #region SetSubtaskAsCancelled Tests

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Return_NotFound_When_Subtask_Is_Null()
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
            IActionResult result = await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Return_NotFound_When_TodoItemId_Is_Zero()
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
            IActionResult result = await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Return_Unauthorized_When_Permission_Level_Too_Low()
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
            IActionResult result = await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Update_Status_And_Return_Json()
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

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>(jsonResult.Value);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Cancelled, resultTodoItem.Status);

            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.Status == (int)KinaUnaTypes.TodoStatusType.Cancelled &&
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Clear_CompletedDate()
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
            subtask.CompletedDate = DateTime.UtcNow;

            TodoItem updatedSubtask = CreateMockSubtaskForProgeny();
            updatedSubtask.CompletedDate = null;

            _mockSubtasksHttpClient.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockSubtasksHttpClient.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);
            mockKanbanItemsHttpClient.Setup(x => x.GetKanbanItemsForTodoItem(TestSubtaskId))
                .ReturnsAsync([]);

            // Act
            await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            _mockSubtasksHttpClient.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(t =>
                t.CompletedDate == null)), Times.Once);
        }

        [Fact]
        public async Task SetSubtaskAsCancelled_Should_Call_UpdateKanbanItemsStatus()
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
            await controller.SetSubtaskAsCancelled(TestSubtaskId);

            // Assert
            mockKanbanItemsHttpClient.Verify(x => x.GetKanbanItemsForTodoItem(TestSubtaskId), Times.Once);
        }

        #endregion
    }
}