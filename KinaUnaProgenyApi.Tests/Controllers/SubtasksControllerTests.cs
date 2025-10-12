using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using KinaUna.Data;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class SubtasksControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<ISubtasksService> _mockSubtasksService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SubtasksController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;
        private readonly TodoItem _parentTodoItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestParentTodoItemId = 100;
        private const int TestSubtaskId = 200;
        private const int TestAccessLevel = 0;

        public SubtasksControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _otherUser = new UserInfo
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                Admins = TestUserEmail
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family",
                Admins = TestUserEmail
            };

            _parentTodoItem = CreateTestTodoItem(
                TestParentTodoItemId,
                TestProgenyId,
                TestFamilyId,
                parentTodoItemId: 0,
                accessLevel: TestAccessLevel);

            // Seed database
            SeedTestData();

            // Setup mocks
            _mockSubtasksService = new Mock<ISubtasksService>();
            _mockTodosService = new Mock<ITodosService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            Mock<IUserInfoService> mockUserInfoService = new();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Default mock setups
            mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            mockUserInfoService.Setup(x => x.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);

            // Initialize controller
            _controller = new SubtasksController(
                _mockSubtasksService.Object,
                _mockTodosService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                mockUserInfoService.Object,
                _mockAccessManagementService.Object);

            SetupControllerContext();
        }

        private void SeedTestData()
        {
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);
            _progenyDbContext.ProgenyDb.Add(_testProgeny);
            _progenyDbContext.FamiliesDb.Add(_testFamily);
            _progenyDbContext.TodoItemsDb.Add(_parentTodoItem);
            _progenyDbContext.SaveChanges();
        }

        private void SetupControllerContext(string userId = TestUserId, string userEmail = TestUserEmail)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Email, userEmail),
                new Claim(ClaimTypes.NameIdentifier, userId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        private static TodoItem CreateTestTodoItem(
            int id,
            int progenyId,
            int familyId,
            int parentTodoItemId = 0,
            int accessLevel = 0)
        {
            return new TodoItem
            {
                TodoItemId = id,
                ProgenyId = progenyId,
                FamilyId = familyId,
                ParentTodoItemId = parentTodoItemId,
                AccessLevel = accessLevel,
                Title = $"Test Todo {id}",
                Description = $"Test Description {id}",
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                UId = Guid.NewGuid().ToString(),
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow
            };
        }

        private static SubtasksRequest CreateTestSubtasksRequest(int parentTodoItemId, int progenyId = TestProgenyId)
        {
            return new SubtasksRequest
            {
                ParentTodoItemId = parentTodoItemId,
                ProgenyId = progenyId,
                FamilyId = TestFamilyId,
                Skip = 0,
                NumberOfItems = 10
            };
        }

        #region GetSubtasksForTodoItem Tests

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_Ok_When_Valid_Request()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            List<TodoItem> subtasks =
            [
                CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestFamilyId, TestParentTodoItemId),
                CreateTestTodoItem(TestSubtaskId + 1, TestProgenyId, TestFamilyId, TestParentTodoItemId)
            ];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(_parentTodoItem);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(subtasks);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SubtasksResponse response = Assert.IsType<SubtasksResponse>(okResult.Value);
            Assert.Equal(TestParentTodoItemId, response.ParentTodoItemId);
            Assert.Equal(2, response.Subtasks.Count);
            Assert.All(response.Subtasks, subtask =>
            {
                Assert.Equal(_testProgeny, subtask.Progeny);
                Assert.Equal(_testFamily, subtask.Family);
            });
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_NotFound_When_TodoItem_Not_Exists()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("TodoItem not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Set_ProgenyId_And_FamilyId_From_TodoItem()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            request.ProgenyId = 999; // Different progeny ID in request
            request.FamilyId = 888; // Different family ID in request
            List<TodoItem> subtasks = [CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestFamilyId, TestParentTodoItemId)];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(_parentTodoItem);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(subtasks);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, It.IsAny<SubtasksRequest>()))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(TestProgenyId, request.ProgenyId); // Should be updated from parent TodoItem
            Assert.Equal(TestFamilyId, request.FamilyId); // Should be updated from parent TodoItem
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Handle_Subtasks_Without_Progeny()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            TodoItem subtaskWithoutProgeny = CreateTestTodoItem(TestSubtaskId, 0, TestFamilyId, TestParentTodoItemId);
            List<TodoItem> subtasks = [subtaskWithoutProgeny];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(_parentTodoItem);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(subtasks);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SubtasksResponse response = Assert.IsType<SubtasksResponse>(okResult.Value);
            Assert.Equal(1, response.Subtasks.Count);
            _mockProgenyService.Verify(x => x.GetProgeny(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Handle_Subtasks_Without_Family()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            TodoItem subtaskWithoutFamily = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            List<TodoItem> subtasks = [subtaskWithoutFamily];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(_parentTodoItem);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(subtasks);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SubtasksResponse response = Assert.IsType<SubtasksResponse>(okResult.Value);
            Assert.Equal(1, response.Subtasks.Count);
            _mockFamiliesService.Verify(x => x.GetFamilyById(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region GetSubtask Tests

        [Fact]
        public async Task GetSubtask_Should_Return_Ok_When_Subtask_Exists()
        {
            // Arrange
            TodoItem subtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestFamilyId, TestParentTodoItemId, TestAccessLevel);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.GetSubtask(TestSubtaskId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedSubtask = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestSubtaskId, returnedSubtask.TodoItemId);
        }

        [Fact]
        public async Task GetSubtask_Should_Return_NotFound_When_Subtask_Not_Exists()
        {
            // Arrange
            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.GetSubtask(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetSubtask_Should_Return_NotFound_When_Subtask_Has_Zero_Id()
        {
            // Arrange
            TodoItem subtask = CreateTestTodoItem(0, TestProgenyId, TestFamilyId, TestParentTodoItemId, TestAccessLevel);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(subtask);

            // Act
            IActionResult result = await _controller.GetSubtask(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Subtask_With_Progeny_Permission()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId, TestAccessLevel);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0, 0, TestAccessLevel);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId, TestAccessLevel);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedSubtask = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestSubtaskId, returnedSubtask.TodoItemId);

            // Verify that the subtask inherits properties from parent
            _mockSubtasksService.Verify(x => x.AddSubtask(It.Is<TodoItem>(s =>
                s.AccessLevel == parentTodoItem.AccessLevel &&
                s.ProgenyId == parentTodoItem.ProgenyId &&
                s.CreatedBy == TestUserId &&
                s.ModifiedBy == TestUserId &&
                !string.IsNullOrWhiteSpace(s.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Subtask_With_Family_Permission()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, 0, TestFamilyId, TestParentTodoItemId, TestAccessLevel);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, 0, TestFamilyId, 0, TestAccessLevel);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, 0, TestFamilyId, TestParentTodoItemId, TestAccessLevel);

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedSubtask = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(TestSubtaskId, returnedSubtask.TodoItemId);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_No_Progeny_Or_Family_Permission()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(It.IsAny<int>(), _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Parent_TodoItem_Not_Exists()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Parent TodoItem not found", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddSubtask_Returns_Null()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId);
            subtaskToAdd.UId = ""; // Empty UId
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.AddSubtask(It.Is<TodoItem>(s =>
                !string.IsNullOrWhiteSpace(s.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Preserve_Existing_UId_When_Provided()
        {
            // Arrange
            string existingUId = "existing-uid-123";
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, 0, TestParentTodoItemId);
            subtaskToAdd.UId = existingUId;
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.AddSubtask(It.Is<TodoItem>(s =>
                s.UId == existingUId), _testUser), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Return_Ok_When_Valid_Update_And_User_Has_Permission()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            updateValues.Title = "Updated Title";
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0, 0, TestAccessLevel);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            updatedSubtask.Title = "Updated Title";

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedSubtask = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal("Updated Title", returnedSubtask.Title);

            // Verify that the subtask inherits properties from parent
            _mockSubtasksService.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(s =>
                s.AccessLevel == parentTodoItem.AccessLevel &&
                s.ProgenyId == parentTodoItem.ProgenyId &&
                s.ModifiedBy == TestUserId &&
                !string.IsNullOrWhiteSpace(s.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Subtask_Not_Exists()
        {
            // Arrange
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Has_No_Permission()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Parent_TodoItem_Not_Exists()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Parent TodoItem not found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateSubtask_Returns_Null()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            updateValues.UId = ""; // Empty UId
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(s =>
                !string.IsNullOrWhiteSpace(s.UId)), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Preserve_Existing_UId_When_Provided()
        {
            // Arrange
            string existingUId = "existing-uid-456";
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);
            updateValues.UId = existingUId;
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, 0);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.TodoItem, TestSubtaskId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId, _testUser))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>(), _testUser))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(s =>
                s.UId == existingUId), _testUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_Successfully_Deleted()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksService.Setup(x => x.DeleteSubtask(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSubtasksService.Verify(x => x.DeleteSubtask(It.Is<TodoItem>(s =>
                s.TodoItemId == TestSubtaskId &&
                s.ModifiedBy == TestUserId), _testUser, false), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Subtask_Not_Exists()
        {
            // Arrange
            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_Delete_Returns_False()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, 0, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId, _testUser))
                .ReturnsAsync(existingSubtask);
            _mockSubtasksService.Setup(x => x.DeleteSubtask(It.IsAny<TodoItem>(), _testUser, false))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}