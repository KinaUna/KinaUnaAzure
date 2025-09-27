using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.TodosServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class SubtasksControllerTests
    {
        private readonly Mock<IUserAccessService> _mockUserAccessService;
        private readonly Mock<ISubtasksService> _mockSubtasksService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly SubtasksController _controller;
        
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";
        private const int TestProgenyId = 1;
        private const int TestParentTodoItemId = 100;
        private const int TestSubtaskId = 200;
        private const int TestAccessLevel = 0;

        public SubtasksControllerTests()
        {
            _mockUserAccessService = new Mock<IUserAccessService>();
            _mockSubtasksService = new Mock<ISubtasksService>();
            _mockTodosService = new Mock<ITodosService>();
            _mockProgenyService = new Mock<IProgenyService>();

            _controller = new SubtasksController(
                _mockUserAccessService.Object,
                _mockSubtasksService.Object,
                _mockTodosService.Object,
                _mockProgenyService.Object);

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

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        private static TodoItem CreateTestTodoItem(int id, int progenyId, int parentTodoItemId = 0, int accessLevel = 0)
        {
            return new TodoItem
            {
                TodoItemId = id,
                ProgenyId = progenyId,
                ParentTodoItemId = parentTodoItemId,
                AccessLevel = accessLevel,
                Title = $"Test Todo {id}",
                Description = $"Test Description {id}",
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = TestUserId,
                UId = Guid.NewGuid().ToString()
            };
        }

        private static Progeny CreateTestProgeny(int id, bool userIsAdmin = true)
        {
            Progeny progeny = new()
            {
                Id = id,
                Name = $"Test Progeny {id}",
                Admins = userIsAdmin ? TestUserEmail : "other@example.com"
            };
            return progeny;
        }

        private static SubtasksRequest CreateTestSubtasksRequest(int parentTodoItemId, int progenyId = TestProgenyId)
        {
            return new SubtasksRequest
            {
                ParentTodoItemId = parentTodoItemId,
                ProgenyId = progenyId,
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
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            Progeny progeny = CreateTestProgeny(TestProgenyId);
            List<TodoItem> subtasks = 
            [
                CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId),
                CreateTestTodoItem(TestSubtaskId + 1, TestProgenyId, TestParentTodoItemId)
            ];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Success(TestAccessLevel));
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId))
                .ReturnsAsync(subtasks);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SubtasksResponse response = Assert.IsType<SubtasksResponse>(okResult.Value);
            Assert.Equal(TestParentTodoItemId, response.ParentTodoItemId);
            Assert.Equal(2, response.Subtasks.Count);
            Assert.All(response.Subtasks, subtask => Assert.Equal(progeny, subtask.Progeny));
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_NotFound_When_TodoItem_Not_Exists()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("TodoItem not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_Access_Error_When_User_Has_No_Access()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            CustomError accessError = CustomError.UnauthorizedError("User does not have access");

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Failure(accessError));

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            // The ToActionResult() method will convert the CustomResult to an appropriate ActionResult
            Assert.IsNotType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_NotFound_When_Progeny_Not_Exists()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Success(TestAccessLevel));
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync((Progeny?)null);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Progeny not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Set_ProgenyId_From_TodoItem()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            request.ProgenyId = 999; // Different progeny ID in request
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            Progeny progeny = CreateTestProgeny(TestProgenyId);
            List<TodoItem> subtasks = [CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId)];
            SubtasksResponse expectedResponse = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Success(TestAccessLevel));
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockSubtasksService.Setup(x => x.GetSubtasksForTodoItem(TestParentTodoItemId))
                .ReturnsAsync(subtasks);
            _mockSubtasksService.Setup(x => x.CreateSubtaskResponseForTodoItem(subtasks, It.IsAny<SubtasksRequest>()))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetSubtasksForTodoItem(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(TestProgenyId, request.ProgenyId); // Should be updated from parent TodoItem
        }

        #endregion

        #region GetSubtask Tests

        [Fact]
        public async Task GetSubtask_Should_Return_Ok_When_Subtask_Exists_And_User_Has_Access()
        {
            // Arrange
            TodoItem subtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId, TestAccessLevel);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Success(TestAccessLevel));

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
            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.GetSubtask(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetSubtask_Should_Return_Access_Error_When_User_Has_No_Access()
        {
            // Arrange
            TodoItem subtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId, TestAccessLevel);
            CustomError accessError = CustomError.UnauthorizedError("User does not have access");

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(subtask);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, TestUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Failure(accessError));

            // Act
            IActionResult result = await _controller.GetSubtask(TestSubtaskId);

            // Assert
            Assert.IsNotType<OkObjectResult>(result);
            Assert.IsNotType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Subtask_And_User_Is_Admin()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId, TestAccessLevel);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId, TestAccessLevel);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
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
                !string.IsNullOrWhiteSpace(s.UId))), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Progeny_Not_Exists()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync((Progeny?)null);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: false);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Parent_TodoItem_Not_Exists()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Parent TodoItem not found", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_AddSubtask_Fails()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);
            subtaskToAdd.UId = ""; // Empty UId
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.AddSubtask(It.Is<TodoItem>(s => 
                !string.IsNullOrWhiteSpace(s.UId))), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Return_Ok_When_Valid_Update_And_User_Is_Admin()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            updateValues.Title = "Updated Title";
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            updatedSubtask.Title = "Updated Title";

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
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
                !string.IsNullOrWhiteSpace(s.UId))), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Subtask_Not_Exists()
        {
            // Arrange
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Progeny_Not_Exists()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync((Progeny?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: false);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Parent_TodoItem_Not_Exists()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Parent TodoItem not found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Put_Should_Generate_UId_When_Not_Provided()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            updateValues.UId = ""; // Empty UId
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(s => 
                !string.IsNullOrWhiteSpace(s.UId))), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_Successfully_Deleted()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockSubtasksService.Setup(x => x.DeleteSubtask(It.IsAny<TodoItem>(), false))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSubtasksService.Verify(x => x.DeleteSubtask(It.Is<TodoItem>(s => 
                s.TodoItemId == TestSubtaskId &&
                s.ModifiedBy == TestUserId), false), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Subtask_Not_Exists()
        {
            // Arrange
            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync((TodoItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_BadRequest_When_Progeny_Not_Exists()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync((Progeny?)null);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: false);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_BadRequest_When_Delete_Fails()
        {
            // Arrange
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockSubtasksService.Setup(x => x.DeleteSubtask(It.IsAny<TodoItem>(), false))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestSubtaskId);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        #endregion

        #region Edge Cases and User Context Tests

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SubtasksRequest request = CreateTestSubtasksRequest(TestParentTodoItemId);
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId, accessLevel: TestAccessLevel);
            
            // Setup controller context with no email claim
            List<Claim> claims = [new(OpenIddictConstants.Claims.Subject, TestUserId)];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(TestProgenyId, Constants.DefaultUserEmail, TestAccessLevel))
                .ReturnsAsync(CustomResult<int>.Success(TestAccessLevel));

            // Act
            await _controller.GetSubtasksForTodoItem(request);

            // Assert
            _mockUserAccessService.Verify(x => x.GetValidatedAccessLevel(TestProgenyId, Constants.DefaultUserEmail, TestAccessLevel), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Preserve_Existing_UId_When_Provided()
        {
            // Arrange
            string existingUId = "existing-uid-123";
            TodoItem subtaskToAdd = CreateTestTodoItem(0, TestProgenyId, TestParentTodoItemId);
            subtaskToAdd.UId = existingUId;
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem addedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.AddSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(addedSubtask);

            // Act
            IActionResult result = await _controller.Post(subtaskToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.AddSubtask(It.Is<TodoItem>(s => 
                s.UId == existingUId)), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Preserve_Existing_UId_When_Provided()
        {
            // Arrange
            string existingUId = "existing-uid-456";
            TodoItem existingSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            TodoItem updateValues = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);
            updateValues.UId = existingUId;
            TodoItem parentTodoItem = CreateTestTodoItem(TestParentTodoItemId, TestProgenyId);
            Progeny progeny = CreateTestProgeny(TestProgenyId, userIsAdmin: true);
            TodoItem updatedSubtask = CreateTestTodoItem(TestSubtaskId, TestProgenyId, TestParentTodoItemId);

            _mockSubtasksService.Setup(x => x.GetSubtask(TestSubtaskId))
                .ReturnsAsync(existingSubtask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, null))
                .ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.GetTodoItem(TestParentTodoItemId))
                .ReturnsAsync(parentTodoItem);
            _mockSubtasksService.Setup(x => x.UpdateSubtask(It.IsAny<TodoItem>()))
                .ReturnsAsync(updatedSubtask);

            // Act
            IActionResult result = await _controller.Put(TestSubtaskId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSubtasksService.Verify(x => x.UpdateSubtask(It.Is<TodoItem>(s => 
                s.UId == existingUId)), Times.Once);
        }

        #endregion
    }
}