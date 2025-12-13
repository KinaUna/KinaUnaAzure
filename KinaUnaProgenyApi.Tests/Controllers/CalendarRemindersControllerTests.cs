using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers;

public class CalendarRemindersControllerTests
{
    private readonly Mock<ICalendarRemindersService> _mockCalendarRemindersService;
    private readonly Mock<IUserInfoService> _mockUserInfoService;
    private readonly CalendarRemindersController _controller;
    private readonly UserInfo _testUser;
    private readonly UserInfo _adminUser;
    private readonly CalendarReminder _testCalendarReminder;

    private const string TestUserEmail = Constants.DefaultUserEmail;
    private const string TestUserId = Constants.DefaultUserId;
    private const string AdminUserEmail = "admin@example.com";
    private const string AdminUserId = "adminUserId";
    private const int TestReminderId = 1;
    private const int TestEventId = 100;

    public CalendarRemindersControllerTests()
    {
        // Setup test data
        _testUser = new UserInfo
        {
            UserId = TestUserId,
            UserEmail = TestUserEmail,
            IsKinaUnaAdmin = false,
            FirstName = "Test",
            LastName = "User",
            Timezone = "UTC"
        };

        _adminUser = new UserInfo
        {
            UserId = AdminUserId,
            UserEmail = AdminUserEmail,
            IsKinaUnaAdmin = true,
            FirstName = "Admin",
            LastName = "User",
            Timezone = "UTC"
        };

        _testCalendarReminder = new CalendarReminder
        {
            CalendarReminderId = TestReminderId,
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 15,
            NotifyTime = DateTime.UtcNow.AddDays(1),
            RecurrenceRuleId = 0,
            Notified = false,
            NotifiedDate = DateTime.MinValue
        };

        // Setup mocks
        _mockCalendarRemindersService = new Mock<ICalendarRemindersService>();
        _mockUserInfoService = new Mock<IUserInfoService>();

        // Initialize controller
        _controller = new CalendarRemindersController(
            _mockCalendarRemindersService.Object,
            _mockUserInfoService.Object
        );
    }

    private void SetupControllerContext(string userEmail, string userId)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, userEmail)
        ];
        ClaimsIdentity identity = new(claims, "TestAuthType");
        ClaimsPrincipal claimsPrincipal = new(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetAllCalendarReminders Tests
    
    [Fact]
    public async Task GetAllCalendarReminders_ReturnsUnauthorized_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        // Act
        IActionResult result = await _controller.GetAllCalendarReminders();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _mockCalendarRemindersService.Verify(x => x.GetAllCalendarReminders(), Times.Never);
    }

    [Fact]
    public async Task GetAllCalendarReminders_ReturnsOkWithEmptyList_WhenNoRemindersExist()
    {
        // Arrange
        SetupControllerContext(AdminUserEmail, AdminUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(It.IsAny<string>()))
            .ReturnsAsync(_adminUser);

        _mockCalendarRemindersService.Setup(x => x.GetAllCalendarReminders())
            .ReturnsAsync(new List<CalendarReminder>());

        // Act
        IActionResult result = await _controller.GetAllCalendarReminders();

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.Empty(returnedReminders);
    }

    #endregion

    #region GetCalendarReminder Tests

    [Fact]
    public async Task GetCalendarReminder_ReturnsOk_WhenReminderExists()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> successResult = CustomResult<CalendarReminder>.Success(_testCalendarReminder);
        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetCalendarReminder(TestReminderId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        CalendarReminder returnedReminder = Assert.IsType<CalendarReminder>(okResult.Value);
        Assert.Equal(TestReminderId, returnedReminder.CalendarReminderId);
    }

    [Fact]
    public async Task GetCalendarReminder_ReturnsNotFound_WhenReminderDoesNotExist()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("NotFoundError", "Calendar reminder not found."));

        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.GetCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCalendarReminder_ReturnsUnauthorized_WhenUserLacksPermission()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to access this calendar reminder."));

        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.GetCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region AddCalendarReminder Tests

    [Fact]
    public async Task AddCalendarReminder_ReturnsOk_WhenReminderAddedSuccessfully()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder newReminder = new()
        {
            CalendarReminderId = 0,
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 15,
            NotifyTime = DateTime.UtcNow.AddDays(1)
        };

        CalendarReminder addedReminder = new()
        {
            CalendarReminderId = TestReminderId,
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 15,
            NotifyTime = DateTime.UtcNow.AddDays(1)
        };

        CustomResult<CalendarReminder> successResult = CustomResult<CalendarReminder>.Success(addedReminder);
        _mockCalendarRemindersService.Setup(x => x.AddCalendarReminder(newReminder, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.AddCalendarReminder(newReminder);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        CalendarReminder returnedReminder = Assert.IsType<CalendarReminder>(okResult.Value);
        Assert.Equal(TestReminderId, returnedReminder.CalendarReminderId);
    }

    [Fact]
    public async Task AddCalendarReminder_ReturnsUnauthorized_WhenUserLacksPermission()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder newReminder = new()
        {
            EventId = TestEventId,
            UserId = "otherUserId",
            NotifyTimeOffsetType = 15,
            NotifyTime = DateTime.UtcNow.AddDays(1)
        };

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to add this calendar reminder."));

        _mockCalendarRemindersService.Setup(x => x.AddCalendarReminder(newReminder, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.AddCalendarReminder(newReminder);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task AddCalendarReminder_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder newReminder = new()
        {
            CalendarReminderId = TestReminderId, // Already has an ID
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 15,
            NotifyTime = DateTime.UtcNow.AddDays(1)
        };

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("ValidationError", "Calendar reminder already exists."));

        _mockCalendarRemindersService.Setup(x => x.AddCalendarReminder(newReminder, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.AddCalendarReminder(newReminder);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpdateCalendarReminder Tests

    [Fact]
    public async Task UpdateCalendarReminder_ReturnsOk_WhenReminderUpdatedSuccessfully()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder updatedReminder = new()
        {
            CalendarReminderId = TestReminderId,
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 30, // Changed
            NotifyTime = DateTime.UtcNow.AddDays(2), // Changed
            Notified = true
        };

        CustomResult<CalendarReminder> successResult = CustomResult<CalendarReminder>.Success(updatedReminder);
        _mockCalendarRemindersService.Setup(x => x.UpdateCalendarReminder(updatedReminder, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.UpdateCalendarReminder(updatedReminder);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        CalendarReminder returnedReminder = Assert.IsType<CalendarReminder>(okResult.Value);
        Assert.Equal(30, returnedReminder.NotifyTimeOffsetType);
        Assert.True(returnedReminder.Notified);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ReturnsNotFound_WhenReminderDoesNotExist()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder updatedReminder = new()
        {
            CalendarReminderId = 999, // Non-existent
            EventId = TestEventId,
            UserId = TestUserId,
            NotifyTimeOffsetType = 30,
            NotifyTime = DateTime.UtcNow.AddDays(2)
        };

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("NotFoundError", "Calendar reminder not found."));

        _mockCalendarRemindersService.Setup(x => x.UpdateCalendarReminder(updatedReminder, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.UpdateCalendarReminder(updatedReminder);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ReturnsUnauthorized_WhenUserLacksPermission()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarReminder updatedReminder = new()
        {
            CalendarReminderId = TestReminderId,
            EventId = TestEventId,
            UserId = "otherUserId", // Different user
            NotifyTimeOffsetType = 30,
            NotifyTime = DateTime.UtcNow.AddDays(2)
        };

        CustomResult<CalendarReminder> failureResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to update this calendar reminder."));

        _mockCalendarRemindersService.Setup(x => x.UpdateCalendarReminder(updatedReminder, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.UpdateCalendarReminder(updatedReminder);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region DeleteCalendarReminder Tests

    [Fact]
    public async Task DeleteCalendarReminder_ReturnsNoContent_WhenReminderDeletedSuccessfully()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> getReminderResult = CustomResult<CalendarReminder>.Success(_testCalendarReminder);
        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(getReminderResult);

        CustomResult<CalendarReminder> deleteResult = CustomResult<CalendarReminder>.Success(_testCalendarReminder);
        _mockCalendarRemindersService.Setup(x => x.DeleteCalendarReminder(_testCalendarReminder, _testUser))
            .ReturnsAsync(deleteResult);

        // Act
        IActionResult result = await _controller.DeleteCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ReturnsNotFound_WhenReminderDoesNotExist()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> getReminderResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("NotFoundError", "Calendar reminder not found."));

        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(getReminderResult);

        // Act
        IActionResult result = await _controller.DeleteCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _mockCalendarRemindersService.Verify(x => x.DeleteCalendarReminder(It.IsAny<CalendarReminder>(), It.IsAny<UserInfo>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ReturnsUnauthorized_WhenUserLacksPermissionToGet()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> getReminderResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to access this calendar reminder."));

        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(getReminderResult);

        // Act
        IActionResult result = await _controller.DeleteCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        _mockCalendarRemindersService.Verify(x => x.DeleteCalendarReminder(It.IsAny<CalendarReminder>(), It.IsAny<UserInfo>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ReturnsUnauthorized_WhenUserLacksPermissionToDelete()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CustomResult<CalendarReminder> getReminderResult = CustomResult<CalendarReminder>.Success(_testCalendarReminder);
        _mockCalendarRemindersService.Setup(x => x.GetCalendarReminder(TestReminderId, _testUser))
            .ReturnsAsync(getReminderResult);

        CustomResult<CalendarReminder> deleteResult = CustomResult<CalendarReminder>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to delete this calendar reminder."));

        _mockCalendarRemindersService.Setup(x => x.DeleteCalendarReminder(_testCalendarReminder, _testUser))
            .ReturnsAsync(deleteResult);

        // Act
        IActionResult result = await _controller.DeleteCalendarReminder(TestReminderId);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region GetCalendarRemindersForUser Tests

    [Fact]
    public async Task GetCalendarRemindersForUser_ReturnsOk_WhenRemindersFound()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = TestUserId,
            EventId = 0,
            FilterNotified = false
        };

        List<CalendarReminder> reminders =
        [
            _testCalendarReminder,
            new() { CalendarReminderId = 2, EventId = 200, UserId = TestUserId, Notified = false }
        ];

        CustomResult<List<CalendarReminder>> successResult = CustomResult<List<CalendarReminder>>.Success(reminders);
        _mockCalendarRemindersService.Setup(x => x.GetCalendarRemindersForUser(request, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetCalendarRemindersForUser(request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.Equal(2, returnedReminders.Count);
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ReturnsOkWithEmptyList_WhenNoRemindersFound()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = TestUserId,
            EventId = 0,
            FilterNotified = true
        };

        CustomResult<List<CalendarReminder>> successResult = CustomResult<List<CalendarReminder>>.Success(new List<CalendarReminder>());
        _mockCalendarRemindersService.Setup(x => x.GetCalendarRemindersForUser(request, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetCalendarRemindersForUser(request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.Empty(returnedReminders);
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ReturnsUnauthorized_WhenUserLacksPermission()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = "otherUserId", // Different user
            EventId = 0,
            FilterNotified = false
        };

        CustomResult<List<CalendarReminder>> failureResult = CustomResult<List<CalendarReminder>>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to access calendar reminders for this user."));

        _mockCalendarRemindersService.Setup(x => x.GetCalendarRemindersForUser(request, _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.GetCalendarRemindersForUser(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_FiltersNotifiedReminders_WhenFilterNotifiedIsTrue()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = TestUserId,
            EventId = 0,
            FilterNotified = true
        };

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 100, UserId = TestUserId, Notified = false },
            new() { CalendarReminderId = 2, EventId = 200, UserId = TestUserId, Notified = false }
        ];

        CustomResult<List<CalendarReminder>> successResult = CustomResult<List<CalendarReminder>>.Success(reminders);
        _mockCalendarRemindersService.Setup(x => x.GetCalendarRemindersForUser(request, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetCalendarRemindersForUser(request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.All(returnedReminders, r => Assert.False(r.Notified));
    }

    #endregion

    #region GetUsersCalendarRemindersForEvent Tests

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ReturnsOk_WhenRemindersFound()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = TestUserId,
            EventId = TestEventId,
            FilterNotified = false
        };

        List<CalendarReminder> reminders =
        [
            _testCalendarReminder,
            new() { CalendarReminderId = 2, EventId = TestEventId, UserId = TestUserId, NotifyTimeOffsetType = 30 }
        ];

        CustomResult<List<CalendarReminder>> successResult = CustomResult<List<CalendarReminder>>.Success(reminders);
        _mockCalendarRemindersService.Setup(x => x.GetUsersCalendarRemindersForEvent(TestEventId, TestUserId, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetUsersCalendarRemindersForEvent(request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.Equal(2, returnedReminders.Count);
        Assert.All(returnedReminders, r => Assert.Equal(TestEventId, r.EventId));
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ReturnsOkWithEmptyList_WhenNoRemindersFound()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = TestUserId,
            EventId = 999, // Non-existent event
            FilterNotified = false
        };

        CustomResult<List<CalendarReminder>> successResult = CustomResult<List<CalendarReminder>>.Success(new List<CalendarReminder>());
        _mockCalendarRemindersService.Setup(x => x.GetUsersCalendarRemindersForEvent(999, TestUserId, _testUser))
            .ReturnsAsync(successResult);

        // Act
        IActionResult result = await _controller.GetUsersCalendarRemindersForEvent(request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<CalendarReminder> returnedReminders = Assert.IsAssignableFrom<List<CalendarReminder>>(okResult.Value);
        Assert.Empty(returnedReminders);
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ReturnsUnauthorized_WhenUserLacksPermission()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = "otherUserId",
            EventId = TestEventId,
            FilterNotified = false
        };

        CustomResult<List<CalendarReminder>> failureResult = CustomResult<List<CalendarReminder>>.Failure(
            new CustomError("UnauthorizedError", "User is not authorized to access calendar reminders for this event."));

        _mockCalendarRemindersService.Setup(x => x.GetUsersCalendarRemindersForEvent(TestEventId, "otherUserId", _testUser))
            .ReturnsAsync(failureResult);

        // Act
        IActionResult result = await _controller.GetUsersCalendarRemindersForEvent(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion
}