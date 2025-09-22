using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.TodosServices
{
    public class CreateTodoItemsResponseForTodoPageTests
    {
        private readonly DateTime _sampleDateTime = new(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_Should_Return_Correct_Response_With_Default_Sorting()
        {
            // Arrange
            List<TodoItem> todoItems =
            [
                new() { TodoItemId = 1, DueDate = _sampleDateTime.AddDays(3), CreatedTime = _sampleDateTime.AddHours(2), Status = 0, ProgenyId = 1, Tags = "tag1,tag2", Context = "context1" },
                new() { TodoItemId = 2, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime.AddHours(1), Status = 1, ProgenyId = 1, Tags = "tag2,tag3", Context = "context2" },
                new() { TodoItemId = 3, DueDate = _sampleDateTime.AddDays(2), CreatedTime = _sampleDateTime.AddHours(3), Status = 0, ProgenyId = 2, Tags = "tag1", Context = "context1,context3" }
            ];

            TodoItemsRequest request = new()
            {
                SortBy = 0, // DueDate
                Sort = 0, // Ascending
                GroupBy = 0, // No grouping
                NumberOfItems = 10,
                Skip = 0
            };

            TodosService todosService = new(null!); // DbContext not needed for this method

            // Act
            TodoItemsResponse result = todosService.CreateTodoItemsResponseForTodoPage(todoItems, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalItems);
            Assert.Equal(1, result.TotalPages);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(3, result.TodoItems.Count);

            // Check sorting: DueDate ascending
            Assert.Equal(_sampleDateTime.AddDays(1), result.TodoItems[0].DueDate);
            Assert.Equal(_sampleDateTime.AddDays(2), result.TodoItems[1].DueDate);
            Assert.Equal(_sampleDateTime.AddDays(3), result.TodoItems[2].DueDate);

            // Check tags and contexts lists
            Assert.Contains("tag1", result.TagsList);
            Assert.Contains("tag2", result.TagsList);
            Assert.Contains("tag3", result.TagsList);
            Assert.Contains("context1", result.ContextsList);
            Assert.Contains("context2", result.ContextsList);
            Assert.Contains("context3", result.ContextsList);
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_Should_Apply_Pagination_When_NumberOfItems_Greater_Than_Zero()
        {
            // Arrange
            List<TodoItem> todoItems = [];
            for (int i = 1; i <= 10; i++)
            {
                todoItems.Add(new TodoItem
                {
                    TodoItemId = i,
                    DueDate = _sampleDateTime.AddDays(i),
                    CreatedTime = _sampleDateTime.AddHours(i),
                    Tags = $"tag{i}",
                    Context = $"context{i}"
                });
            }

            TodoItemsRequest request = new()
            {
                NumberOfItems = 3,
                Skip = 6, // Start from 7th item
                SortBy = 0,
                Sort = 0
            };

            TodosService todosService = new(null!);

            // Act
            TodoItemsResponse result = todosService.CreateTodoItemsResponseForTodoPage(todoItems, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalItems);
            Assert.Equal(4, result.TotalPages); // 10 items / 3 per page = 4 pages
            Assert.Equal(3, result.PageNumber); // (Skip 6 / NumberOfItems 3) + 1 = 3
            Assert.Equal(3, result.TodoItems.Count); // Should return 3 items
            Assert.Equal(7, result.TodoItems[0].TodoItemId); // 7th item should be first
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_Should_Sort_By_CreatedTime_When_SortBy_Is_1()
        {
            // Arrange
            List<TodoItem> todoItems =
            [
                new() { TodoItemId = 1, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime.AddHours(3), Tags = "tag1", Context = "context1" },
                new() { TodoItemId = 2, DueDate = _sampleDateTime.AddDays(2), CreatedTime = _sampleDateTime.AddHours(1), Tags = "tag2", Context = "context2" },
                new() { TodoItemId = 3, DueDate = _sampleDateTime.AddDays(3), CreatedTime = _sampleDateTime.AddHours(2), Tags = "tag3", Context = "context3" }
            ];

            TodoItemsRequest request = new()
            {
                SortBy = 1, // CreatedTime
                Sort = 0, // Ascending
                NumberOfItems = 10
            };

            TodosService todosService = new(null!);

            // Act
            TodoItemsResponse result = todosService.CreateTodoItemsResponseForTodoPage(todoItems, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TodoItems.Count);

            // Check sorting: CreatedTime ascending
            Assert.Equal(_sampleDateTime.AddHours(1), result.TodoItems[0].CreatedTime);
            Assert.Equal(_sampleDateTime.AddHours(2), result.TodoItems[1].CreatedTime);
            Assert.Equal(_sampleDateTime.AddHours(3), result.TodoItems[2].CreatedTime);
            
        }

        [Fact]
        public void CreateTodoItemsResponseForTodoPage_Should_Group_By_Status_When_GroupBy_Is_1()
        {
            // Arrange
            List<TodoItem> todoItems =
            [
                new() { TodoItemId = 1, Status = 2, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, Tags = "tag1", Context = "context1" },
                new() { TodoItemId = 2, Status = 0, DueDate = _sampleDateTime.AddDays(2), CreatedTime = _sampleDateTime, Tags = "tag2", Context = "context2" },
                new() { TodoItemId = 3, Status = 1, DueDate = _sampleDateTime.AddDays(3), CreatedTime = _sampleDateTime, Tags = "tag3", Context = "context3" }
            ];

            TodoItemsRequest request = new()
            {
                SortBy = 0,
                Sort = 0,
                GroupBy = 1, // Group by Status
                NumberOfItems = 10
            };

            TodosService todosService = new(null!);

            // Act
            TodoItemsResponse result = todosService.CreateTodoItemsResponseForTodoPage(todoItems, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TodoItems.Count);

            // Check grouping by status (should be ordered by Status first)
            Assert.Equal(0, result.TodoItems[0].Status); // NotStarted
            Assert.Equal(1, result.TodoItems[1].Status); // InProgress
            Assert.Equal(2, result.TodoItems[2].Status); // Completed
        }

        [Fact]
        public async Task GetTodosList_Should_Return_List_Of_TodoItems_When_Valid_Parameters_Are_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosList_Should_Return_List_Of_TodoItems_When_Valid_Parameters_Are_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems =
            [
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 1, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 2, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 3, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false },
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = true }, // Deleted item
                new() { ProgenyId = 2, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false } // Different progeny
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            // Act
            List<TodoItem> result = await todosService.GetTodosList(1, 2);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<TodoItem>>(result);
            Assert.Equal(3, result.Count); // Only items with access level <= 2, not deleted, and for progeny 1
            Assert.All(result, item => Assert.Equal(1, item.ProgenyId));
            Assert.All(result, item => Assert.True(item.AccessLevel <= 2));
            Assert.All(result, item => Assert.False(item.IsDeleted));
        }
    }
}
