using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.CalendarServices;

public class CalendarRemindersServiceTests
{
    private static ProgenyDbContext GetDatabaseContext(string dbName)
    {
        DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        ProgenyDbContext context = new(options);
        return context;
    }

    private static UserInfo GetTestUserInfo()
    {
        return new UserInfo
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            IsKinaUnaAdmin = false,
            Timezone = "UTC"
        };
    }

    private static UserInfo GetAdminUserInfo()
    {
        return new UserInfo
        {
            UserId = "adminUserId",
            UserEmail = "admin@example.com",
            IsKinaUnaAdmin = true,
            Timezone = "UTC"
        };
    }

    [Fact]
    public async Task GetAllCalendarReminders_ShouldReturnAllReminders()
    {
        // Arrange
        string dbName = $"GetAllCalendarReminders_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 1, UserId = "user1" },
            new() { CalendarReminderId = 2, EventId = 2, UserId = "user2" },
            new() { CalendarReminderId = 3, EventId = 3, UserId = "user1" }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        List<CalendarReminder> result = await service.GetAllCalendarReminders();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.CalendarReminderId == 1);
        Assert.Contains(result, r => r.CalendarReminderId == 2);
        Assert.Contains(result, r => r.CalendarReminderId == 3);
    }

    [Fact]
    public async Task GetAllCalendarReminders_ShouldReturnEmptyListWhenNoReminders()
    {
        // Arrange
        string dbName = $"GetAllCalendarRemindersEmpty_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        List<CalendarReminder> result = await service.GetAllCalendarReminders();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCalendarReminder_ShouldReturnReminderById()
    {
        // Arrange
        string dbName = $"GetCalendarReminder_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.GetCalendarReminder(1, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.CalendarReminderId);
        Assert.Equal(userInfo.UserId, result.Value.UserId);
    }

    [Fact]
    public async Task GetCalendarReminder_ShouldReturnErrorWhenNotFound()
    {
        // Arrange
        string dbName = $"GetCalendarReminderNotFound_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.GetCalendarReminder(999, userInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NotFoundError", result.Error.Code);
    }

    [Fact]
    public async Task GetCalendarReminder_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"GetCalendarReminderUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.GetCalendarReminder(1, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task GetCalendarReminder_ShouldAllowAdminAccess()
    {
        // Arrange
        string dbName = $"GetCalendarReminderAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.GetCalendarReminder(1, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.CalendarReminderId);
    }

    [Fact]
    public async Task AddCalendarReminder_ShouldAddValidReminder()
    {
        // Arrange
        string dbName = $"AddCalendarReminder_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 0, // New reminder
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.AddCalendarReminder(reminder, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value.CalendarReminderId); // Should have an ID assigned
        Assert.Equal(userInfo.UserId, result.Value.UserId);

        // Verify it was added to database
        CalendarReminder? savedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.EventId == 1);
        Assert.NotNull(savedReminder);
    }

    [Fact]
    public async Task AddCalendarReminder_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"AddCalendarReminderUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 0,
            EventId = 1,
            UserId = userInfo.UserId, // Belongs to userInfo but otherUserInfo is adding it
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.AddCalendarReminder(reminder, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task AddCalendarReminder_ShouldAllowAdminToAddAnyReminder()
    {
        // Arrange
        string dbName = $"AddCalendarReminderAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 0,
            EventId = 1,
            UserId = userInfo.UserId, // Belongs to userInfo but admin is adding it
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.AddCalendarReminder(reminder, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value.CalendarReminderId);
    }

    [Fact]
    public async Task AddCalendarReminder_ShouldReturnErrorWhenDuplicate()
    {
        // Arrange
        string dbName = $"AddCalendarReminderDuplicate_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1, // Existing reminder ID
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.AddCalendarReminder(reminder, userInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("ValidationError", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ShouldUpdateValidReminder()
    {
        // Arrange
        string dbName = $"UpdateCalendarReminder_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15,
            Notified = false,
            NotifiedDate = DateTime.MinValue
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Create updated reminder
        DateTime newNotifyTime = DateTime.UtcNow.AddHours(1);
        DateTime newNotifiedDate = DateTime.UtcNow;
        CalendarReminder updatedReminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = newNotifyTime,
            NotifyTimeOffsetType = 30,
            Notified = true,
            NotifiedDate = newNotifiedDate
        };

        // Act
        CustomResult<CalendarReminder> result = await service.UpdateCalendarReminder(updatedReminder, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.NotifyTimeOffsetType);
        Assert.True(result.Value.Notified);
        Assert.Equal(newNotifyTime, result.Value.NotifyTime);
        Assert.Equal(newNotifiedDate, result.Value.NotifiedDate);

        // Verify it was updated in database
        CalendarReminder? savedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.CalendarReminderId == 1);
        Assert.NotNull(savedReminder);
        Assert.Equal(30, savedReminder.NotifyTimeOffsetType);
        Assert.True(savedReminder.Notified);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ShouldReturnErrorWhenNotFound()
    {
        // Arrange
        string dbName = $"UpdateCalendarReminderNotFound_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 999, // Non-existent ID
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.UpdateCalendarReminder(reminder, userInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NotFoundError", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"UpdateCalendarReminderUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act - try to update with a different user
        CustomResult<CalendarReminder> result = await service.UpdateCalendarReminder(reminder, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCalendarReminder_ShouldAllowAdminToUpdateAnyReminder()
    {
        // Arrange
        string dbName = $"UpdateCalendarReminderAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15,
            Notified = false
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        CalendarReminder updatedReminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow.AddHours(1),
            NotifyTimeOffsetType = 30,
            Notified = true
        };

        // Act
        CustomResult<CalendarReminder> result = await service.UpdateCalendarReminder(updatedReminder, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.NotifyTimeOffsetType);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ShouldDeleteValidReminder()
    {
        // Arrange
        string dbName = $"DeleteCalendarReminder_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.DeleteCalendarReminder(reminder, userInfo);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify it was removed from database
        CalendarReminder? savedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.CalendarReminderId == 1);
        Assert.Null(savedReminder);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ShouldReturnErrorWhenNotFound()
    {
        // Arrange
        string dbName = $"DeleteCalendarReminderNotFound_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 999, // Non-existent ID
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.DeleteCalendarReminder(reminder, userInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NotFoundError", result.Error.Code);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"DeleteCalendarReminderUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act - try to delete with a different user
        CustomResult<CalendarReminder> result = await service.DeleteCalendarReminder(reminder, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task DeleteCalendarReminder_ShouldAllowAdminToDeleteAnyReminder()
    {
        // Arrange
        string dbName = $"DeleteCalendarReminderAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = DateTime.UtcNow,
            NotifyTimeOffsetType = 15
        };

        context.CalendarRemindersDb.Add(reminder);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<CalendarReminder> result = await service.DeleteCalendarReminder(reminder, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify it was removed from database
        CalendarReminder? savedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.CalendarReminderId == 1);
        Assert.Null(savedReminder);
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ShouldReturnUserReminders()
    {
        // Arrange
        string dbName = $"GetCalendarRemindersForUser_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 1, UserId = userInfo.UserId, Notified = false },
            new() { CalendarReminderId = 2, EventId = 2, UserId = userInfo.UserId, Notified = true },
            new() { CalendarReminderId = 3, EventId = 3, UserId = userInfo.UserId, Notified = false },
            new() { CalendarReminderId = 4, EventId = 4, UserId = "otherUserId", Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = userInfo.UserId,
            FilterNotified = false
        };

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetCalendarRemindersForUser(request, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count); // 3 reminders for the user
        Assert.DoesNotContain(result.Value, r => r.UserId == "otherUserId");
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ShouldFilterNotified()
    {
        // Arrange
        string dbName = $"GetCalendarRemindersForUserFilter_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 1, UserId = userInfo.UserId, Notified = false },
            new() { CalendarReminderId = 2, EventId = 2, UserId = userInfo.UserId, Notified = true },
            new() { CalendarReminderId = 3, EventId = 3, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = userInfo.UserId,
            FilterNotified = true // Filter out notified reminders
        };

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetCalendarRemindersForUser(request, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count); // Only non-notified reminders
        Assert.All(result.Value, r => Assert.False(r.Notified));
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"GetCalendarRemindersForUserUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 1, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = userInfo.UserId // Requesting userInfo's reminders
        };

        // Act - try to get with a different user
        CustomResult<List<CalendarReminder>> result = await service.GetCalendarRemindersForUser(request, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task GetCalendarRemindersForUser_ShouldAllowAdminAccess()
    {
        // Arrange
        string dbName = $"GetCalendarRemindersForUserAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = 1, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        CalendarRemindersForUserRequest request = new()
        {
            UserId = userInfo.UserId
        };

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetCalendarRemindersForUser(request, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ShouldReturnEventReminders()
    {
        // Arrange
        string dbName = $"GetUsersCalendarRemindersForEvent_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        int eventId = 1;
        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = eventId, UserId = userInfo.UserId, Notified = false },
            new() { CalendarReminderId = 2, EventId = eventId, UserId = userInfo.UserId, Notified = true },
            new() { CalendarReminderId = 3, EventId = 2, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetUsersCalendarRemindersForEvent(eventId, userInfo.UserId, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count); // 2 reminders for event 1
        Assert.All(result.Value, r => Assert.Equal(eventId, r.EventId));
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ShouldReturnEmptyListWhenNoReminders()
    {
        // Arrange
        string dbName = $"GetUsersCalendarRemindersForEventEmpty_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetUsersCalendarRemindersForEvent(999, userInfo.UserId, userInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ShouldReturnErrorWhenUnauthorized()
    {
        // Arrange
        string dbName = $"GetUsersCalendarRemindersForEventUnauthorized_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo otherUserInfo = new() { UserId = "otherUserId", IsKinaUnaAdmin = false };

        int eventId = 1;
        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = eventId, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act - try to get with a different user
        CustomResult<List<CalendarReminder>> result = await service.GetUsersCalendarRemindersForEvent(eventId, userInfo.UserId, otherUserInfo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("UnauthorizedError", result.Error.Code);
    }

    [Fact]
    public async Task GetUsersCalendarRemindersForEvent_ShouldAllowAdminAccess()
    {
        // Arrange
        string dbName = $"GetUsersCalendarRemindersForEventAdmin_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);
        UserInfo userInfo = GetTestUserInfo();
        UserInfo adminUserInfo = GetAdminUserInfo();

        int eventId = 1;
        List<CalendarReminder> reminders =
        [
            new() { CalendarReminderId = 1, EventId = eventId, UserId = userInfo.UserId, Notified = false }
        ];

        context.CalendarRemindersDb.AddRange(reminders);
        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        CustomResult<List<CalendarReminder>> result = await service.GetUsersCalendarRemindersForEvent(eventId, userInfo.UserId, adminUserInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task SendExpiredCalendarReminders_ShouldSendRemindersForNonRecurringEvents()
    {
        // Arrange
        string dbName = $"SendExpiredCalendarReminders_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        // Create progeny
        Progeny progeny = new() { Id = 1, NickName = "TestChild" };
        context.ProgenyDb.Add(progeny);

        // Create user
        UserInfo userInfo = new()
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            Timezone = "UTC"
        };
        context.UserInfoDb.Add(userInfo);

        // Create event
        CalendarItem calendarItem = new()
        {
            EventId = 1,
            ProgenyId = 1,
            Title = "Test Event",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Location = "Test Location",
            Context = "",
            Notes = ""
        };
        context.CalendarDb.Add(calendarItem);

        // Create expired non-recurring reminder (RecurrenceRuleId should be 0 for non-recurring)
        
        List<CalendarReminder> reminders =
        [
            new() {
                CalendarReminderId = 1,
                EventId = 1,
                UserId = userInfo.UserId,
                NotifyTime = DateTime.UtcNow.AddMinutes(-10), // In the past
                Notified = false,
                RecurrenceRuleId = 0
            }
        ];
        context.CalendarRemindersDb.AddRange(reminders);

        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        emailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        Mock<IPushMessageSender> pushMessageSender = new();
        pushMessageSender.Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        await service.SendExpiredCalendarReminders();

        // Assert
        // Verify reminder was marked as notified
        CalendarReminder? updatedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.CalendarReminderId == 1);
        Assert.NotNull(updatedReminder);
        Assert.True(updatedReminder.Notified);

        // Verify email was sent
        emailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        // Verify push notification was sent
        pushMessageSender.Verify(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendExpiredCalendarReminders_ShouldNotSendWhenNoExpiredReminders()
    {
        // Arrange
        string dbName = $"SendExpiredCalendarRemindersNone_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        UserInfo userInfo = new()
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            Timezone = "UTC"
        };
        context.UserInfoDb.Add(userInfo);

        // Create future reminder
        List<CalendarReminder> reminders =
        [
            new() {
                CalendarReminderId = 1,
                EventId = 1,
                UserId = userInfo.UserId,
                NotifyTime = DateTime.UtcNow.AddHours(1), // In the future
                Notified = false,
                RecurrenceRuleId = 1
            }
        ];
        context.CalendarRemindersDb.AddRange(reminders);

        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        await service.SendExpiredCalendarReminders();

        // Assert
        // Verify email was not sent
        emailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        // Verify push notification was not sent
        pushMessageSender.Verify(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendExpiredRecurringReminders_ShouldSendRemindersForRecurringEvents()
    {
        // Arrange
        string dbName = $"SendExpiredRecurringReminders_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        // Create progeny
        Progeny progeny = new() { Id = 1, NickName = "TestChild" };
        context.ProgenyDb.Add(progeny);

        // Create user
        UserInfo userInfo = new()
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            Timezone = "UTC"
        };
        context.UserInfoDb.Add(userInfo);

        // Create recurrence rule
        RecurrenceRule recurrenceRule = new()
        {
            RecurrenceRuleId = 1,
            ProgenyId = 1,
            Frequency = 1, // Daily
            Interval = 1
        };
        context.RecurrenceRulesDb.Add(recurrenceRule);

        // Create event
        DateTime eventStartTime = DateTime.UtcNow.AddMinutes(15);
        CalendarItem calendarItem = new()
        {
            EventId = 1,
            ProgenyId = 1,
            Title = "Test Recurring Event",
            StartTime = eventStartTime,
            EndTime = eventStartTime.AddHours(1),
            RecurrenceRuleId = 1,
            Location = "Test Location",
            Context = "",
            Notes = ""
        };
        context.CalendarDb.Add(calendarItem);

        // Create reminders - notify 30 minutes before
        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = eventStartTime.AddMinutes(-30), // Would be in the past now
            Notified = false,
            RecurrenceRuleId = 1,
            NotifiedDate = DateTime.UtcNow.AddDays(-2) // Last notified more than 24 hours ago
        };
        context.CalendarRemindersDb.Add(reminder);

        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        emailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        Mock<IPushMessageSender> pushMessageSender = new();
        pushMessageSender.Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();
        // Setup to return a recurring event instance that should trigger a reminder
        calendarRecurrencesService.Setup(x => x.GetCalendarItemsForRecurrenceRule(
                It.IsAny<RecurrenceRule>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<UserInfo>()))
            .ReturnsAsync(
            [
                new()
                {
                    EventId = 101, // New event ID for the instance
                    ProgenyId = 1,
                    Title = "Test Recurring Event Instance",
                    StartTime = eventStartTime, // This instance's start time
                    EndTime = eventStartTime.AddHours(1),
                    RecurrenceRuleId = 1,
                    Location = "Test Location",
                    Context = "",
                    Notes = ""
                }
            ]);

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        await service.SendExpiredRecurringReminders();

        // Assert
        // Verify that the reminder was updated as notified
        CalendarReminder? updatedReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(r => r.CalendarReminderId == 1);
        Assert.NotNull(updatedReminder);
        Assert.True(updatedReminder.Notified);
        Assert.True(updatedReminder.NotifiedDate > DateTime.UtcNow.AddMinutes(-1)); // Updated recently

        // Verify email was sent
        emailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        // Verify push notification was sent
        pushMessageSender.Verify(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendExpiredRecurringReminders_ShouldNotSendWhenNotifiedRecently()
    {
        // Arrange
        string dbName = $"SendExpiredRecurringRemindersRecent_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        Progeny progeny = new() { Id = 1, NickName = "TestChild" };
        context.ProgenyDb.Add(progeny);

        UserInfo userInfo = new()
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            Timezone = "UTC"
        };
        context.UserInfoDb.Add(userInfo);

        RecurrenceRule recurrenceRule = new()
        {
            RecurrenceRuleId = 1,
            ProgenyId = 1,
            Frequency = 1,
            Interval = 1
        };
        context.RecurrenceRulesDb.Add(recurrenceRule);

        DateTime eventStartTime = DateTime.UtcNow.AddMinutes(15);
        CalendarItem calendarItem = new()
        {
            EventId = 1,
            ProgenyId = 1,
            Title = "Test Recurring Event",
            StartTime = eventStartTime,
            EndTime = eventStartTime.AddHours(1),
            RecurrenceRuleId = 1
        };
        context.CalendarDb.Add(calendarItem);

        // Create reminder that was notified recently (less than 24 hours ago)
        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = eventStartTime.AddMinutes(-30),
            Notified = true,
            RecurrenceRuleId = 1,
            NotifiedDate = DateTime.UtcNow.AddHours(-1) // Notified 1 hour ago
        };
        context.CalendarRemindersDb.Add(reminder);

        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        calendarRecurrencesService.Setup(x => x.GetCalendarItemsForRecurrenceRule(
                It.IsAny<RecurrenceRule>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<UserInfo>()))
            .ReturnsAsync(
            [
                new()
                {
                    EventId = 101,
                    ProgenyId = 1,
                    Title = "Test Recurring Event Instance",
                    StartTime = eventStartTime,
                    EndTime = eventStartTime.AddHours(1),
                    RecurrenceRuleId = 1
                }
            ]);

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        await service.SendExpiredRecurringReminders();

        // Assert
        // Verify email was not sent
        emailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        // Verify push notification was not sent
        pushMessageSender.Verify(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendExpiredRecurringReminders_ShouldNotSendWhenNoRecurrenceInstances()
    {
        // Arrange
        string dbName = $"SendExpiredRecurringRemindersNoInstances_{Guid.NewGuid()}";
        ProgenyDbContext context = GetDatabaseContext(dbName);

        Progeny progeny = new() { Id = 1, NickName = "TestChild" };
        context.ProgenyDb.Add(progeny);

        UserInfo userInfo = new()
        {
            UserId = "testUserId",
            UserEmail = "test@example.com",
            Timezone = "UTC"
        };
        context.UserInfoDb.Add(userInfo);

        RecurrenceRule recurrenceRule = new()
        {
            RecurrenceRuleId = 1,
            ProgenyId = 1,
            Frequency = 1,
            Interval = 1
        };
        context.RecurrenceRulesDb.Add(recurrenceRule);

        DateTime eventStartTime = DateTime.UtcNow.AddMinutes(15);
        CalendarItem calendarItem = new()
        {
            EventId = 1,
            ProgenyId = 1,
            Title = "Test Recurring Event",
            StartTime = eventStartTime,
            EndTime = eventStartTime.AddHours(1),
            RecurrenceRuleId = 1
        };
        context.CalendarDb.Add(calendarItem);

        CalendarReminder reminder = new()
        {
            CalendarReminderId = 1,
            EventId = 1,
            UserId = userInfo.UserId,
            NotifyTime = eventStartTime.AddMinutes(-30),
            Notified = false,
            RecurrenceRuleId = 1,
            NotifiedDate = DateTime.UtcNow.AddDays(-2)
        };
        context.CalendarRemindersDb.Add(reminder);

        await context.SaveChangesAsync();

        Mock<IEmailSender> emailSender = new();
        Mock<IPushMessageSender> pushMessageSender = new();
        Mock<ICalendarRecurrencesService> calendarRecurrencesService = new();

        // Return empty list - no instances in the time window
        calendarRecurrencesService.Setup(x => x.GetCalendarItemsForRecurrenceRule(
                It.IsAny<RecurrenceRule>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<UserInfo>()))
            .ReturnsAsync(new List<CalendarItem>());

        CalendarRemindersService service = new(context, emailSender.Object, pushMessageSender.Object, calendarRecurrencesService.Object);

        // Act
        await service.SendExpiredRecurringReminders();

        // Assert
        // Verify email was not sent
        emailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        // Verify push notification was not sent
        pushMessageSender.Verify(x => x.SendMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}