using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.TodosServices
{
    public class GetTodosForProgenyTests
    {
        private readonly DateTime _sampleDateTime = new(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetTodosForProgeny_Should_Return_Filtered_List_When_Valid_Parameters_Are_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Return_Filtered_List_When_Valid_Parameters_Are_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            // Create test data - NOTE: AccessLevel filtering now uses >= instead of <=
            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false, Status = 0, Tags = "tag1", Context = "context1" },
                new() { ProgenyId = 1, AccessLevel = 1, DueDate = _sampleDateTime.AddDays(2), CreatedTime = _sampleDateTime.AddMinutes(1), IsDeleted = false, Status = 1, Tags = "tag2", Context = "context2" },
                new()
                {
                    ProgenyId = 1, AccessLevel = 2, DueDate = _sampleDateTime.AddDays(3), CreatedTime = _sampleDateTime.AddMinutes(2), IsDeleted = false, Status = 2, Tags = "tag1,tag3", Context = "context1,context3"
                },
                new() { ProgenyId = 1, AccessLevel = 3, DueDate = _sampleDateTime.AddDays(4), CreatedTime = _sampleDateTime.AddMinutes(3), IsDeleted = false, Status = 0, Tags = "tag4", Context = "context4" },
                new()
                {
                    ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(5), CreatedTime = _sampleDateTime.AddMinutes(4), IsDeleted = true, Status = 0, Tags = "tag1", Context = "context1"
                }, // Deleted item
                new() { ProgenyId = 2, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false, Status = 0, Tags = "tag1", Context = "context1" } // Different progeny
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new();

            // Act - access level 1 should include items with AccessLevel >= 1 (items 2, 3, 4)
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 1, request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<TodoItem>>(result);
            Assert.Equal(3, result.Count); // Only items with access level >= 1, not deleted, and for progeny 1
            Assert.All(result, item => Assert.Equal(1, item.ProgenyId));
            Assert.All(result, item => Assert.True(item.AccessLevel >= 1));
            Assert.All(result, item => Assert.False(item.IsDeleted));
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_DateRange_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_DateRange_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(5), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(10), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = null, CreatedTime = _sampleDateTime, IsDeleted = false } // Null DueDate should be included
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                StartDate = _sampleDateTime.AddDays(2),
                EndDate = _sampleDateTime.AddDays(8)
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Item with DueDate = _sampleDateTime.AddDays(5) and item with null DueDate
            Assert.Contains(result, item => item.DueDate == _sampleDateTime.AddDays(5));
            Assert.Contains(result, item => item.DueDate == null);
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_StartDate_Only_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_StartDate_Only_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(5), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = null, CreatedTime = _sampleDateTime, IsDeleted = false }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                StartDate = _sampleDateTime.AddDays(3)
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Item with DueDate >= StartDate and item with null DueDate
            Assert.Contains(result, item => item.DueDate == _sampleDateTime.AddDays(5));
            Assert.Contains(result, item => item.DueDate == null);
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_EndDate_Only_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_EndDate_Only_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(5), CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = null, CreatedTime = _sampleDateTime, IsDeleted = false }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                EndDate = _sampleDateTime.AddDays(3)
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Item with DueDate <= EndDate and item with null DueDate
            Assert.Contains(result, item => item.DueDate == _sampleDateTime.AddDays(1));
            Assert.Contains(result, item => item.DueDate == null);
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_Location_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_Location_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Location = "home,office" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Location = "school,library" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Location = "home,park" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Location = null } // Should be excluded
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                LocationFilter = "home,school"
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Items with locations containing "home" or "school"
            Assert.All(result, item => Assert.NotNull(item.Location));
            Assert.Contains(result, item => item.Location!.Contains("home"));
            Assert.Contains(result, item => item.Location!.Contains("school"));
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_Tags_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_Tags_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Tags = "tag1,tag2" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Tags = "tag3,tag4" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Tags = "tag1,tag5" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Tags = null } // Should be excluded
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                TagFilter = "tag1,tag3"
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Items with tags containing "tag1" or "tag3"
            Assert.All(result, item => Assert.NotNull(item.Tags));
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_Context_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_Context_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Context = "work,project" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Context = "personal,home" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Context = "work,meeting" },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Context = null } // Should be excluded
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                ContextFilter = "work,personal"
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Items with context containing "work" or "personal"
            Assert.All(result, item => Assert.NotNull(item.Context));
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Filter_By_Status_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Filter_By_Status_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Status = 0 },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Status = 1 },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Status = 2 }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                StatusFilter = [KinaUnaTypes.TodoStatusType.NotStarted, KinaUnaTypes.TodoStatusType.Completed]
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Items with status 0 (NotStarted) and 2 (Completed)
            Assert.Contains(result, item => item.Status == 0);
            Assert.Contains(result, item => item.Status == 2);
            Assert.DoesNotContain(result, item => item.Status == 1);
        }
    }
}
