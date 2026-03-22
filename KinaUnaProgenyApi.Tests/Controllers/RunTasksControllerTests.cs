using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class RunTasksControllerTests
    {
        private readonly Mock<IBackgroundTasksService> _mockBackgroundTasksService;
        private readonly Mock<ITaskRunnerService> _mockTaskRunnerService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly RunTasksController _controller;

        private readonly UserInfo _testAdminUser;
        private readonly UserInfo _testNonAdminUser;
        private readonly KinaUnaBackgroundTask _testTask;

        private const string TestAdminEmail = "admin@kinauna.com";
        private const string TestAdminUserId = "admin-user-id";
        private const string TestNonAdminEmail = "user@kinauna.com";
        private const string TestNonAdminUserId = "user-id";
        private const int TestTaskId = 123;

        public RunTasksControllerTests()
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
                TaskName = "TestTask",
                IsRunning = false,
                LastRun = DateTime.UtcNow.AddHours(-1),
            };

            // Setup mocks
            _mockBackgroundTasksService = new Mock<IBackgroundTasksService>();
            _mockTaskRunnerService = new Mock<ITaskRunnerService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize controller
            _controller = new RunTasksController(
                _mockBackgroundTasksService.Object,
                _mockTaskRunnerService.Object,
                _mockUserInfoService.Object);

            SetupControllerContext(TestAdminEmail);
        }

        private void SetupControllerContext(string userEmail)
        {
            List<Claim> claims = [new(ClaimTypes.Email, userEmail)];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region CheckPictureExtensions Tests

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_Ok_When_Admin_And_Task_Valid()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockTaskRunnerService.Setup(x => x.CheckPictureExtensions(_testTask))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(_testTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask returnedTask = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(TestTaskId, returnedTask.TaskId);

            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(_testTask), Times.Once);
        }

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_BadRequest_When_Task_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(null!);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task not found.", badRequestResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_ActionResult_When_GetTask_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(new CustomError("", "Task not found")));

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(_testTask);

            // Assert
            Assert.IsNotType<OkObjectResult>(result);
            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureExtensions_Should_Return_Ok_Without_Running_When_Task_Is_Running()
        {
            // Arrange
            KinaUnaBackgroundTask runningTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "RunningTask",
                IsRunning = true
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(runningTask));

            // Act
            IActionResult result = await _controller.CheckPictureExtensions(runningTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureExtensions(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        #endregion

        #region CheckPictureLinks Tests

        [Fact]
        public async Task CheckPictureLinks_Should_Return_Ok_When_Admin_And_Task_Valid()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockTaskRunnerService.Setup(x => x.CheckPictureLinks(_testTask))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult result = await _controller.CheckPictureLinks(_testTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask returnedTask = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(TestTaskId, returnedTask.TaskId);

            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(_testTask), Times.Once);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult result = await _controller.CheckPictureLinks(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.CheckPictureLinks(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Return_BadRequest_When_Task_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.CheckPictureLinks(null!);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task not found.", badRequestResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Return_ActionResult_When_GetTask_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(new CustomError("", "Task not found")));

            // Act
            IActionResult result = await _controller.CheckPictureLinks(_testTask);

            // Assert
            Assert.IsNotType<OkObjectResult>(result);
            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task CheckPictureLinks_Should_Return_Ok_Without_Running_When_Task_Is_Running()
        {
            // Arrange
            KinaUnaBackgroundTask runningTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "RunningTask",
                IsRunning = true
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(runningTask));

            // Act
            IActionResult result = await _controller.CheckPictureLinks(runningTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockTaskRunnerService.Verify(x => x.CheckPictureLinks(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        #endregion

        #region SendCalendarReminders Tests

        [Fact]
        public async Task SendCalendarReminders_Should_Return_Ok_When_Admin_And_Task_Valid()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));
            _mockTaskRunnerService.Setup(x => x.SendCalendarReminders(_testTask))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act
            IActionResult result = await _controller.SendCalendarReminders(_testTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaBackgroundTask returnedTask = Assert.IsType<KinaUnaBackgroundTask>(okResult.Value);
            Assert.Equal(TestTaskId, returnedTask.TaskId);

            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(_testTask), Times.Once);
        }

        [Fact]
        public async Task SendCalendarReminders_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult result = await _controller.SendCalendarReminders(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task SendCalendarReminders_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.SendCalendarReminders(_testTask);

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);

            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task SendCalendarReminders_Should_Return_BadRequest_When_Task_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.SendCalendarReminders(null!);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Task not found.", badRequestResult.Value);

            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task SendCalendarReminders_Should_Return_ActionResult_When_GetTask_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Failure(new CustomError("", "Task not found")));

            // Act
            IActionResult result = await _controller.SendCalendarReminders(_testTask);

            // Assert
            Assert.IsNotType<OkObjectResult>(result);
            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        [Fact]
        public async Task SendCalendarReminders_Should_Return_Ok_Without_Running_When_Task_Is_Running()
        {
            // Arrange
            KinaUnaBackgroundTask runningTask = new()
            {
                TaskId = TestTaskId,
                TaskName = "RunningTask",
                IsRunning = true
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(runningTask));

            // Act
            IActionResult result = await _controller.SendCalendarReminders(runningTask);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockTaskRunnerService.Verify(x => x.SendCalendarReminders(It.IsAny<KinaUnaBackgroundTask>()), Times.Never);
        }

        #endregion

        #region GetTaskList Tests

        [Fact]
        public async Task GetTaskList_Should_Return_Ok_With_List_Of_Task_Names_When_User_Is_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.GetTaskList();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> taskList = Assert.IsType<List<string>>(okResult.Value);
            
            // Verify that the list contains the expected task method names
            Assert.Contains("CheckPictureExtensions", taskList);
            Assert.Contains("CheckPictureLinks", taskList);
            Assert.Contains("SendCalendarReminders", taskList);
            
            // Verify that GetTaskList itself is not in the list (it's a GET method, not POST)
            Assert.DoesNotContain("GetTaskList", taskList);
        }

        [Fact]
        public async Task GetTaskList_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            SetupControllerContext(TestNonAdminEmail);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestNonAdminEmail))
                .ReturnsAsync(_testNonAdminUser);

            // Act
            IActionResult result = await _controller.GetTaskList();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetTaskList_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestAdminEmail))
                .ReturnsAsync((UserInfo)null!);

            // Act
            IActionResult result = await _controller.GetTaskList();

            // Assert
            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not admin.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetTaskList_Should_Only_Include_HttpPost_Methods()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.GetTaskList();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> taskList = Assert.IsType<List<string>>(okResult.Value);

            // Get all methods from the controller
            MethodInfo[] allMethods = typeof(RunTasksController).GetMethods();
            MethodInfo[] postMethods = allMethods
                .Where(m => m.GetCustomAttributes(typeof(HttpPostAttribute), false).Length > 0)
                .ToArray();
            MethodInfo[] getMethods = allMethods
                .Where(m => m.GetCustomAttributes(typeof(HttpGetAttribute), false).Length > 0)
                .ToArray();

            // Verify that all POST methods are in the list
            foreach (MethodInfo method in postMethods)
            {
                Assert.Contains(method.Name, taskList);
            }

            // Verify that no GET methods are in the list
            foreach (MethodInfo method in getMethods)
            {
                Assert.DoesNotContain(method.Name, taskList);
            }
        }

        [Fact]
        public async Task GetTaskList_Should_Return_Non_Empty_List()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);

            // Act
            IActionResult result = await _controller.GetTaskList();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> taskList = Assert.IsType<List<string>>(okResult.Value);
            
            Assert.NotEmpty(taskList);
        }

        #endregion

        #region User.GetEmail() Extension Tests

        [Fact]
        public async Task All_Methods_Should_Handle_Null_Email_From_Claims()
        {
            // Arrange
            SetupControllerContext(""); // Empty email
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
                .ReturnsAsync(_testAdminUser);
            _mockBackgroundTasksService.Setup(x => x.GetTask(TestTaskId))
                .ReturnsAsync(CustomResult<KinaUnaBackgroundTask>.Success(_testTask));

            // Act & Assert - Each method should handle the empty email gracefully
            IActionResult result1 = await _controller.CheckPictureExtensions(_testTask);
            IActionResult result2 = await _controller.CheckPictureLinks(_testTask);
            IActionResult result3 = await _controller.SendCalendarReminders(_testTask);
            IActionResult result4 = await _controller.GetTaskList();

            // All should return Ok since the mock returns an admin user
            Assert.IsType<OkObjectResult>(result1);
            Assert.IsType<OkObjectResult>(result2);
            Assert.IsType<OkObjectResult>(result3);
            Assert.IsType<OkObjectResult>(result4);
        }

        #endregion
    }
}