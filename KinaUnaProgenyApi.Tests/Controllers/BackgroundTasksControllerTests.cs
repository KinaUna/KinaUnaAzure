using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class BackgroundTasksControllerTests
    {
        private readonly Mock<IBackgroundTasksService> _mockBackgroundTasksService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly BackgroundTasksController _controller;

        private readonly UserInfo _testAdminUser;
        private readonly UserInfo _testNonAdminUser;
        private readonly KinaUnaBackgroundTask _testTask;
        private readonly List<KinaUnaBackgroundTask> _testTasks;

        private const string TestAdminEmail = "admin@test.com";
        private const string TestNonAdminEmail = "user@test.com";
        private const string TestAdminUserId = "admin-user-id";
        private const string TestNonAdminUserId = "non-admin-user-id";
        private const int TestTaskId = 1;

        public BackgroundTasksControllerTests()
        {
            // Setup test data
            _testAdminUser = new UserInfo
            {
                UserId = TestAdminUserId,
                UserEmail = TestAdminEmail,
                IsKinaUnaAdmin = true,
                FirstName = "Admin",
                LastName = "User"
            };

            _testNonAdminUser = new UserInfo
            {
                UserId = TestNonAdminUserId,
                UserEmail = TestNonAdminEmail,
                IsKinaUnaAdmin = false,
                FirstName = "Regular",
                LastName = "User"
            };

            _testTask = new KinaUnaBackgroundTask
            {
                TaskId = TestTaskId,
                TaskName = "Test Task",
                TaskDescription = "Test Description",
                ApiEndpoint = "CheckPictureExtensions",
                LastRun = DateTime.UtcNow.AddHours(-1),
                Interval = TimeSpan.FromHours(6),
                IsRunning = false,
                IsEnabled = true
            };

            _testTasks = new List<KinaUnaBackgroundTask>
            {
                _testTask,
                new KinaUnaBackgroundTask
                {
                    TaskId = 2,
                    TaskName = "Second Task",
                    TaskDescription = "Second Description",
                    ApiEndpoint = "AnotherEndpoint",
                    LastRun = DateTime.UtcNow.AddHours(-2),
                    Interval = TimeSpan.FromHours(12),
                    IsRunning = false,
                    IsEnabled = true
                }
            };

            // Setup mocks
            _mockBackgroundTasksService = new Mock<IBackgroundTasksService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize controller
            _controller = new BackgroundTasksController(
                _mockBackgroundTasksService.Object,
                _mockUserInfoService.Object
            );

            // Setup controller context with admin claims by default
            SetupControllerContext(TestAdminEmail, TestAdminUserId);
        }

        private void SetupControllerContext(string email, string userId)
        {
            List<Claim> claims = new()
            {
                new(ClaimTypes.Email, email),
                new("sub", userId)
            };
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
        
        #region GetTasks Tests

        [Fact]
        public async Task GetTasks_Should_Return_Ok_With_Tasks_When_User_Is_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTasks())
                .ReturnsAsync(CustomResult<List<KinaUnaBackgroundTask>>.Success(_testTasks));

            // Act
            IActionResult? result = await _controller.GetTasks();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaBackgroundTask> tasks = Assert.IsType<List<KinaUnaBackgroundTask>>(okResult.Value);
            Assert.Equal(2, tasks.Count);
        }

        [Fact]
        public async Task GetTasks_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.GetTasks();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetTasks_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult? result = await _controller.GetTasks();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetTasks_Should_Return_NotFound_When_Service_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTasks())
                .ReturnsAsync(CustomResult<List<KinaUnaBackgroundTask>>.Failure(
                    CustomError.NotFoundError("No tasks found.")));

            // Act
            IActionResult? result = await _controller.GetTasks();

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No tasks found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetTasks_Should_Use_Default_Email_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("", "");
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTasks())
                .ReturnsAsync(CustomResult<List<KinaUnaBackgroundTask>>.Success(_testTasks));

            // Act
            IActionResult? result = await _controller.GetTasks();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region ResetAllTasks Tests

        [Fact]
        public async Task ResetAllTasks_Should_Return_Ok_With_Tasks_When_User_Is_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.ResetTasks())
                .ReturnsAsync(CustomResult<List<KinaUnaBackgroundTask>>.Success(_testTasks));

            // Act
            IActionResult? result = await _controller.ResetAllTasks();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaBackgroundTask> tasks = Assert.IsType<List<KinaUnaBackgroundTask>>(okResult.Value);
            Assert.Equal(2, tasks.Count);
        }

        [Fact]
        public async Task ResetAllTasks_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.ResetAllTasks();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task ResetAllTasks_Should_Return_NotFound_When_Service_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.ResetTasks())
                .ReturnsAsync(CustomResult<List<KinaUnaBackgroundTask>>.Failure(
                    CustomError.NotFoundError("Failed to reset tasks.")));

            // Act
            IActionResult? result = await _controller.ResetAllTasks();

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Failed to reset tasks.", notFoundResult.Value);
        }

        #endregion

        #region AddTask Tests

        [Fact]
        public async Task AddTask_Should_Return_Ok_With_Task_When_User_Is_Admin_And_Task_Valid()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.AddTask(_testTask))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult? result = await _controller.AddTask(_testTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask task = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(_testTask.TaskId, task.TaskId);
        }

        [Fact]
        public async Task AddTask_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.AddTask(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task AddTask_Should_Return_BadRequest_When_Task_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult? result = await _controller.AddTask(null!);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task invalid.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddTask_Should_Return_BadRequest_When_Task_Validation_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            KinaUnaBackgroundTask invalidTask = new()
            {
                TaskName = "Invalid Task",
                ApiEndpoint = "NonExistentEndpoint" // Invalid endpoint
            };

            // Act
            IActionResult? result = await _controller.AddTask(invalidTask);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task invalid.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddTask_Should_Return_BadRequest_When_Service_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.AddTask(_testTask))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(
                    CustomError.ValidationError("Task already exists.")));

            // Act
            IActionResult? result = await _controller.AddTask(_testTask);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task already exists.", badRequestResult.Value);
        }

        #endregion

        #region UpdateTask Tests

        [Fact]
        public async Task UpdateTask_Should_Return_Ok_With_Updated_Task_When_User_Is_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockBackgroundTasksService.Setup(x => x.UpdateTask(It.IsAny<KinaUnaBackgroundTask>()))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, _testTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask task = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(_testTask.TaskId, task.TaskId);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, _testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_BadRequest_When_Task_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, null!);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task not found.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_BadRequest_When_Task_Id_Mismatch()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            KinaUnaBackgroundTask mismatchedTask = new()
            {
                TaskId = 999,
                TaskName = "Mismatched Task"
            };

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, mismatchedTask);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task ID mismatch.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_NotFound_When_Existing_Task_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(
                    CustomError.NotFoundError($"Task with id {TestTaskId} not found.")));

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, _testTask);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateTask_Should_Preserve_LastRun_When_Updated_LastRun_Is_Too_Old()
        {
            // Arrange
            DateTime oldLastRun = DateTime.UtcNow.AddDays(-31);
            DateTime currentLastRun = DateTime.UtcNow.AddHours(-1);

            KinaUnaBackgroundTask existingTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "Existing Task",
                ApiEndpoint = "CheckPictureExtensions",
                LastRun = currentLastRun,
                IsRunning = false
            };

            KinaUnaBackgroundTask updatedTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "Updated Task",
                ApiEndpoint = "CheckPictureExtensions",
                LastRun = oldLastRun,
                IsRunning = true
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(existingTask));
            _mockBackgroundTasksService.Setup(x => x.UpdateTask(It.IsAny<KinaUnaBackgroundTask>()))
                .ReturnsAsync((KinaUnaBackgroundTask task) => CustomResult<KinaUnaBackgroundTask>.Success(task));

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, updatedTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask resultTask = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(currentLastRun, resultTask.LastRun);
        }

        [Fact]
        public async Task UpdateTask_Should_Preserve_IsRunning_From_Existing_Task()
        {
            // Arrange
            KinaUnaBackgroundTask existingTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "Existing Task",
                ApiEndpoint = "CheckPictureExtensions",
                IsRunning = true
            };

            KinaUnaBackgroundTask updatedTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "Updated Task",
                ApiEndpoint = "CheckPictureExtensions",
                IsRunning = false
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(existingTask));
            _mockBackgroundTasksService.Setup(x => x.UpdateTask(It.IsAny<KinaUnaBackgroundTask>()))
                .ReturnsAsync((KinaUnaBackgroundTask task) => CustomResult<KinaUnaBackgroundTask>.Success(task));

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, updatedTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask resultTask = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.True(resultTask.IsRunning);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_BadRequest_When_Update_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockBackgroundTasksService.Setup(x => x.UpdateTask(It.IsAny<KinaUnaBackgroundTask>()))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(
                    CustomError.ValidationError("Update failed.")));

            // Act
            IActionResult? result = await _controller.UpdateTask(TestTaskId, _testTask);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Update failed.", badRequestResult.Value);
        }

        #endregion

        #region DeleteTask Tests

        [Fact]
        public async Task DeleteTask_Should_Return_Ok_When_User_Is_Admin_And_Task_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockBackgroundTasksService.Setup(x => x.DeleteTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult? result = await _controller.DeleteTask(TestTaskId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);
        }

        [Fact]
        public async Task DeleteTask_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.DeleteTask(TestTaskId);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task DeleteTask_Should_Return_NotFound_When_Task_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(
                    CustomError.NotFoundError($"Task with id {TestTaskId} not found.")));

            // Act
            IActionResult? result = await _controller.DeleteTask(TestTaskId);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteTask_Should_Return_BadRequest_When_Delete_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockBackgroundTasksService.Setup(x => x.DeleteTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(
                    CustomError.ValidationError("Delete failed.")));

            // Act
            IActionResult? result = await _controller.DeleteTask(TestTaskId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Delete failed.", badRequestResult.Value);
        }

        #endregion

        #region GetCommands Tests

        [Fact]
        public async Task GetCommands_Should_Return_Ok_With_Commands_List_When_User_Is_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult? result = await _controller.GetCommands();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> commands = Assert.IsType<List<string>>(okResult.Value);
            Assert.NotEmpty(commands);
        }

        [Fact]
        public async Task GetCommands_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail, TestNonAdminUserId);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult? result = await _controller.GetCommands();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetCommands_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult? result = await _controller.GetCommands();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        #endregion
    }
}