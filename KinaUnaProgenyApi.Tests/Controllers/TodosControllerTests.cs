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
    public class TodosControllerTests
    {
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserAccessService> _mockUserAccessService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IAzureNotifications> _mockAzureNotifications;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly TodosController _controller;
        private const string TestUserEmail = "test@kinauna.com";
        private const string TestUserId = "test-user-id";

        public TodosControllerTests()
        {
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserAccessService = new Mock<IUserAccessService>();
            _mockTodosService = new Mock<ITodosService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockAzureNotifications = new Mock<IAzureNotifications>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new TodosController(
                _mockProgenyService.Object,
                _mockUserAccessService.Object,
                _mockTodosService.Object,
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockAzureNotifications.Object,
                _mockWebNotificationsService.Object);

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

        [Fact]
        public async Task GetProgeniesTodoItemsList_Should_Return_Ok_When_Valid_Request_With_Accessible_Progenies()
        {
            // Arrange
            TodoItemsRequest request = new()
            {
                ProgenyIds = [1, 2],
                Skip = 0,
                NumberOfItems = 10
            };

            Progeny progeny1 = new() { Id = 1, Name = "Test Child 1", Admins = TestUserEmail };
            Progeny progeny2 = new() { Id = 2, Name = "Test Child 2", Admins = TestUserEmail };
            UserAccess userAccess1 = new() { ProgenyId = 1, UserId = TestUserEmail, AccessLevel = 0 };
            UserAccess userAccess2 = new() { ProgenyId = 2, UserId = TestUserEmail, AccessLevel = 1 };

            List<TodoItem> todoItems =
            [
                new() { TodoItemId = 1, ProgenyId = 1, Title = "Todo 1" },
                new() { TodoItemId = 2, ProgenyId = 2, Title = "Todo 2" }
            ];

            TodoItemsResponse expectedResponse = new()
            {
                TodoItems = todoItems,
                TotalItems = 2,
                TotalPages = 1,
                PageNumber = 1
            };

            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny1);
            _mockProgenyService.Setup(x => x.GetProgeny(2)).ReturnsAsync(progeny2);
            _mockUserAccessService.Setup(x => x.GetProgenyUserAccessForUser(1, TestUserEmail)).ReturnsAsync(userAccess1);
            _mockUserAccessService.Setup(x => x.GetProgenyUserAccessForUser(2, TestUserEmail)).ReturnsAsync(userAccess2);
            _mockTodosService.Setup(x => x.GetTodosForProgeny(1, 0, request)).ReturnsAsync([todoItems[0]]);
            _mockTodosService.Setup(x => x.GetTodosForProgeny(2, 1, request)).ReturnsAsync([todoItems[1]]);
            _mockTodosService.Setup(x => x.CreateTodoItemsResponseForTodoPage(It.IsAny<List<TodoItem>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult? result = await _controller.GetProgeniesTodoItemsList(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItemsResponse response = Assert.IsType<TodoItemsResponse>(okResult.Value);
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(todoItems, response.TodoItems);

            // Verify service integration
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgeny(2), Times.Once);
            _mockUserAccessService.Verify(x => x.GetProgenyUserAccessForUser(1, TestUserEmail), Times.Exactly(2));
            _mockUserAccessService.Verify(x => x.GetProgenyUserAccessForUser(2, TestUserEmail), Times.Exactly(2));
        }

        [Fact]
        public async Task GetTodoItem_Should_Return_Ok_When_User_Has_Access()
        {
            // Arrange
            TodoItem todoItem = new()
            { 
                TodoItemId = 1, 
                ProgenyId = 1, 
                Title = "Test Todo",
                AccessLevel = 1
            };
            CustomResult<int> accessResult = 1;

            _mockTodosService.Setup(x => x.GetTodoItem(1)).ReturnsAsync(todoItem);
            _mockUserAccessService.Setup(x => x.GetValidatedAccessLevel(1, TestUserEmail, 1))
                .ReturnsAsync(accessResult);

            // Act
            IActionResult? result = await _controller.GetTodoItem(1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(1, returnedTodo.TodoItemId);

            // Verify service integration
            _mockTodosService.Verify(x => x.GetTodoItem(1), Times.Once);
            _mockUserAccessService.Verify(x => x.GetValidatedAccessLevel(1, TestUserEmail, 1), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Create_TodoItem_And_Timeline_And_Send_Notifications()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                ProgenyId = 1,
                Title = "New Todo",
                Description = "Test Description"
            };

            Progeny progeny = new()
            { 
                Id = 1, 
                Name = "Test Child", 
                NickName = "TestNick",
                Admins = TestUserEmail 
            };

            TodoItem createdTodoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "New Todo",
                Description = "Test Description",
                CreatedBy = TestUserId,
                UId = "test-uid"
            };

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = 1,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = "1"
            };

            UserInfo userInfo = new()
            {
                UserEmail = TestUserEmail,
                FirstName = "Test",
                LastName = "User"
            };

            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>())).ReturnsAsync(createdTodoItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>())).ReturnsAsync(timelineItem);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail)).ReturnsAsync(userInfo);

            // Act
            IActionResult? result = await _controller.Post(todoItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(1, returnedTodo.TodoItemId);
            Assert.Equal(TestUserId, returnedTodo.CreatedBy);

            // Verify service integration - all services should be called in correct order
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockTodosService.Verify(x => x.AddTodoItem(It.Is<TodoItem>(t => 
                t.CreatedBy == TestUserId && !string.IsNullOrWhiteSpace(t.UId))), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>()), Times.Once);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(TestUserEmail), Times.Once);
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeLineItem>(), It.IsAny<string>()), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendTodoItemNotification(
                It.IsAny<TodoItem>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Update_TodoItem_And_Timeline()
        {
            // Arrange
            TodoItem existingTodoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Existing Todo"
            };

            TodoItem updatedTodoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Updated Todo",
                ModifiedBy = TestUserId
            };

            Progeny progeny = new()
            { 
                Id = 1, 
                Admins = TestUserEmail 
            };

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = 1,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = "1"
            };

            _mockTodosService.Setup(x => x.GetTodoItem(1)).ReturnsAsync(existingTodoItem);
            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.UpdateTodoItem(It.IsAny<TodoItem>())).ReturnsAsync(updatedTodoItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.TodoItem))
                .ReturnsAsync(timelineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>())).ReturnsAsync(timelineItem);

            // Act
            IActionResult? result = await _controller.Put(1, updatedTodoItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TodoItem returnedTodo = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal("Updated Todo", returnedTodo.Title);

            // Verify service integration
            _mockTodosService.Verify(x => x.GetTodoItem(1), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockTodosService.Verify(x => x.UpdateTodoItem(It.Is<TodoItem>(t => 
                t.ModifiedBy == TestUserId)), Times.Once);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.TodoItem), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Remove_TodoItem_Timeline_And_Send_Notifications()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Todo to Delete"
            };

            Progeny progeny = new()
            { 
                Id = 1, 
                NickName = "TestNick",
                Admins = TestUserEmail 
            };

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = 1,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = "1",
                AccessLevel = 1
            };

            UserInfo userInfo = new()
            {
                UserEmail = TestUserEmail,
                FirstName = "Test",
                LastName = "User"
            };

            _mockTodosService.Setup(x => x.GetTodoItem(1)).ReturnsAsync(todoItem);
            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.TodoItem))
                .ReturnsAsync(timelineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(timelineItem)).ReturnsAsync(timelineItem);
            _mockTodosService.Setup(x => x.DeleteTodoItem(It.IsAny<TodoItem>(), false)).ReturnsAsync(true);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail)).ReturnsAsync(userInfo);

            // Act
            IActionResult? result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify service integration - all services called in correct order
            _mockTodosService.Verify(x => x.GetTodoItem(1), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.TodoItem), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(timelineItem), Times.Once);
            _mockTodosService.Verify(x => x.DeleteTodoItem(It.Is<TodoItem>(t => 
                t.ModifiedBy == TestUserId), false), Times.Once);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(TestUserEmail), Times.Once);
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeLineItem>(), It.IsAny<string>()), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendTodoItemNotification(
                It.IsAny<TodoItem>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetProgeniesTodoItemsList_Should_Return_NotFound_When_No_Accessible_Progenies()
        {
            // Arrange
            TodoItemsRequest request = new()
            {
                ProgenyIds = [1, 2],
                Skip = 0
            };

            _mockProgenyService.Setup(x => x.GetProgeny(It.IsAny<int>())).ReturnsAsync(null as Progeny);

            // Act
            IActionResult? result = await _controller.GetProgeniesTodoItemsList(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockProgenyService.Verify(x => x.GetProgeny(2), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            TodoItem todoItem = new() { ProgenyId = 1 };
            Progeny progeny = new() { Id = 1, Admins = "other@example.com" };

            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny);

            // Act
            IActionResult? result = await _controller.Post(todoItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockProgenyService.Verify(x => x.GetProgeny(1), Times.Once);
            _mockTodosService.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_TodoService_Fails()
        {
            // Arrange
            TodoItem todoItem = new() { ProgenyId = 1 };
            Progeny progeny = new() { Id = 1, Admins = TestUserEmail };

            _mockProgenyService.Setup(x => x.GetProgeny(1)).ReturnsAsync(progeny);
            _mockTodosService.Setup(x => x.AddTodoItem(It.IsAny<TodoItem>())).ReturnsAsync(null as TodoItem);

            // Act
            IActionResult? result = await _controller.Post(todoItem);

            // Assert
            Assert.IsType<BadRequestResult>(result);
            _mockTodosService.Verify(x => x.AddTodoItem(It.IsAny<TodoItem>()), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>()), Times.Never);
        }
    }
}