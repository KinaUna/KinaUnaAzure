using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.Subtasks
{
    public class CreateSubtasksResponseTests
    {
        private const int TestParentTodoItemId = 100;

        #region CreateSubtaskResponseForTodoItem Tests

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Throw_ArgumentException_When_Request_Is_Null()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_NullRequest")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks = [];

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => Task.FromResult(service.CreateSubtaskResponseForTodoItem(subtasks, null!)));

            Assert.Equal("Request is invalid.", exception.Message);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_StartDate()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterStartDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 15) },
                new() { TodoItemId = 3, StartDate = new DateTime(2023, 2, 1) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                StartYear = 2023,
                StartMonth = 1,
                StartDay = 10
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 2);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 3);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_EndDate()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterEndDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, DueDate = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, DueDate = new DateTime(2023, 1, 15) },
                new() { TodoItemId = 3, DueDate = new DateTime(2023, 2, 1) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                EndYear = 2023,
                EndMonth = 1,
                EndDay = 20
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 1);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 2);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_TagFilter()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterTag")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Tags = "urgent,important" },
                new() { TodoItemId = 2, Tags = "important,work" },
                new() { TodoItemId = 3, Tags = "personal" },
                new() { TodoItemId = 4, Tags = null }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                TagFilter = "important"
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 1);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 2);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_ContextFilter()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterContext")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Context = "work" },
                new() { TodoItemId = 2, Context = "home" },
                new() { TodoItemId = 3, Context = "work-project" },
                new() { TodoItemId = 4, Context = null }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                ContextFilter = "work"
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 1);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 3);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_LocationFilter()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterLocation")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Location = "office" },
                new() { TodoItemId = 2, Location = "home" },
                new() { TodoItemId = 3, Location = "office-building" },
                new() { TodoItemId = 4, Location = null }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                LocationFilter = "office"
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 1);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 3);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Filter_By_StatusFilter()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_FilterStatus")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Status = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                new() { TodoItemId = 2, Status = (int)KinaUnaTypes.TodoStatusType.InProgress },
                new() { TodoItemId = 3, Status = (int)KinaUnaTypes.TodoStatusType.Completed },
                new() { TodoItemId = 4, Status = (int)KinaUnaTypes.TodoStatusType.Cancelled }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                StatusFilter =
                [
                    KinaUnaTypes.TodoStatusType.NotStarted,
                    KinaUnaTypes.TodoStatusType.InProgress
                ]
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.Subtasks.Count);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 1);
            Assert.Contains(result.Subtasks, s => s.TodoItemId == 2);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Apply_Pagination()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_Pagination")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks = [];
            for (int i = 1; i <= 10; i++)
            {
                subtasks.Add(new TodoItem
                {
                    TodoItemId = i,
                    Title = $"Subtask {i}",
                    StartDate = new DateTime(2023, 1, i),
                    CreatedTime = new DateTime(2023, 1, i)
                });
            }

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Skip = 3,
                NumberOfItems = 5
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(5, result.Subtasks.Count);
            Assert.Equal(10, result.TotalItems);
            Assert.Equal(2, result.TotalPages); // Math.Ceiling(10/5)
            Assert.Equal(1, result.PageNumber); // (3/5) + 1

            // Verify correct items are returned (items 4-8 based on skip=3, take=5)
            Assert.Equal(4, result.Subtasks[0].TodoItemId);
            Assert.Equal(8, result.Subtasks[4].TodoItemId);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Group_By_Status()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_GroupByStatus")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Status = (int)KinaUnaTypes.TodoStatusType.Completed, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, Status = (int)KinaUnaTypes.TodoStatusType.NotStarted, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, Status = (int)KinaUnaTypes.TodoStatusType.InProgress, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                GroupBy = 1 // Group by Status
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(3, result.Subtasks.Count);

            // Verify sorting: NotStarted (0), InProgress (1), Completed (2)
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.NotStarted, result.Subtasks[0].Status);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.InProgress, result.Subtasks[1].Status);
            Assert.Equal((int)KinaUnaTypes.TodoStatusType.Completed, result.Subtasks[2].Status);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Group_By_Progeny()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_GroupByProgeny")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, ProgenyId = 3, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, ProgenyId = 1, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, ProgenyId = 2, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                GroupBy = 2 // Group by Progeny
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(3, result.Subtasks.Count);

            // Verify sorting by ProgenyId: 1, 2, 3
            Assert.Equal(1, result.Subtasks[0].ProgenyId);
            Assert.Equal(2, result.Subtasks[1].ProgenyId);
            Assert.Equal(3, result.Subtasks[2].ProgenyId);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Group_By_Location()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_GroupByLocation")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, Location = "home", StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, Location = "office", StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, Location = "store", StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                GroupBy = 3 // Group by Location
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(3, result.Subtasks.Count);

            // Verify sorting by Location: alphabetical
            Assert.Equal("home", result.Subtasks[0].Location);
            Assert.Equal("office", result.Subtasks[1].Location);
            Assert.Equal("store", result.Subtasks[2].Location);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Sort_By_StartDate_When_No_Grouping()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_SortByStartDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                GroupBy = 0 // No grouping
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(3, result.Subtasks.Count);

            // Verify sorting by StartDate
            Assert.Equal(new DateTime(2023, 1, 1), result.Subtasks[0].StartDate);
            Assert.Equal(new DateTime(2023, 1, 2), result.Subtasks[1].StartDate);
            Assert.Equal(new DateTime(2023, 1, 3), result.Subtasks[2].StartDate);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Calculate_Correct_Page_Numbers()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_PageNumbers")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks = [];
            for (int i = 1; i <= 25; i++)
            {
                subtasks.Add(new TodoItem
                {
                    TodoItemId = i,
                    StartDate = new DateTime(2023, 1, 1),
                    CreatedTime = new DateTime(2023, 1, 1)
                });
            }

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Skip = 10,
                NumberOfItems = 5
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(25, result.TotalItems);
            Assert.Equal(5, result.TotalPages); // Math.Ceiling(25/5)
            Assert.Equal(3, result.PageNumber); // (10/5) + 1
            Assert.Equal(5, result.Subtasks.Count);
        }

        [Fact]
        public async Task CreateSubtaskResponseForTodoItem_Should_Handle_Zero_NumberOfItems()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("CreateSubtaskResponse_ZeroNumberOfItems")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            List<TodoItem> subtasks =
            [
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) }
            ];

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                Skip = 0,
                NumberOfItems = 0 // No pagination limit
            };

            // Act
            SubtasksResponse result = service.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Assert
            Assert.Equal(2, result.TotalItems);
            Assert.Equal(2, result.TotalPages); // 2/1 when NumberOfItems is 0, defaults to 1
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(2, result.Subtasks.Count); // All items returned
        }

        #endregion
    }
}
