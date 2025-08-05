using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.TodosServices
{
    public class TodosServiceTests
    {
        private readonly DateTime _sampleDateTime = new(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task AddTodoItem_Should_Return_TodoItem_Object_When_Valid_TodoItem_Is_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("AddTodoItem_Should_Return_TodoItem_Object_When_Valid_TodoItem_Is_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem todoItem = new()
            {
                ProgenyId = 1,
                Title = "Test Todo",
                Description = "Test Description",
                Status = 0,
                DueDate = _sampleDateTime.AddDays(7),
                AccessLevel = 0,
                Tags = "test,tag",
                Context = "Testing",
                CreatedBy = "TestUser",
                ModifiedBy = "TestUser",
                IsDeleted = false
            };

            TodosService todosService = new(context);

            // Act
            TodoItem result = await todosService.AddTodoItem(todoItem);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TodoItem>(result);
            Assert.Equal(todoItem.ProgenyId, result.ProgenyId);
            Assert.Equal(todoItem.Title, result.Title);
            Assert.Equal(todoItem.Description, result.Description);
            Assert.Equal(todoItem.Status, result.Status);
            Assert.Equal(todoItem.DueDate, result.DueDate);
            Assert.Equal(todoItem.AccessLevel, result.AccessLevel);
            Assert.Equal(todoItem.Tags, result.Tags);
            Assert.Equal(todoItem.Context, result.Context);
            Assert.Equal(todoItem.CreatedBy, result.CreatedBy);
            Assert.Equal(todoItem.CreatedBy, result.ModifiedBy); // ModifiedBy should be set to CreatedBy
            Assert.False(result.IsDeleted);
            Assert.True(result.CreatedTime > DateTime.MinValue);
            Assert.True(result.ModifiedTime > DateTime.MinValue);
            Assert.Equal(result.CreatedTime.Date, result.ModifiedTime.Date);
            Assert.Equal(result.CreatedTime.Hour, result.ModifiedTime.Hour);
            Assert.Equal(result.CreatedTime.Minute, result.ModifiedTime.Minute);
            Assert.Equal(result.CreatedTime.Second, result.ModifiedTime.Second);
        }

        [Fact]
        public async Task DeleteTodoItem_Should_Return_True_When_TodoItem_Exists_And_SoftDelete()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteTodoItem_Should_Return_True_When_TodoItem_Exists_And_SoftDelete")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Test Todo",
                Description = "Test Description",
                Status = 0,
                DueDate = _sampleDateTime.AddDays(7),
                AccessLevel = 0,
                Tags = "test,tag",
                Context = "Testing",
                CreatedBy = "TestUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "TestUser",
                IsDeleted = false
            };

            context.Add(todoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItem todoItemToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "DeleteUser"
            };

            // Act
            bool result = await todosService.DeleteTodoItem(todoItemToDelete, false);

            // Assert
            Assert.True(result);

            TodoItem? deletedItem = await context.TodoItemsDb.FindAsync(1);
            Assert.NotNull(deletedItem);
            Assert.True(deletedItem.IsDeleted);
            Assert.Equal("DeleteUser", deletedItem.ModifiedBy);
            Assert.True(deletedItem.ModifiedTime > _sampleDateTime);
        }

        [Fact]
        public async Task DeleteTodoItem_Should_Return_True_When_TodoItem_Exists_And_HardDelete()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteTodoItem_Should_Return_True_When_TodoItem_Exists_And_HardDelete")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Test Todo",
                Description = "Test Description",
                Status = 0,
                DueDate = _sampleDateTime.AddDays(7),
                AccessLevel = 0,
                Tags = "test,tag",
                Context = "Testing",
                CreatedBy = "TestUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "TestUser",
                IsDeleted = false
            };

            context.Add(todoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItem todoItemToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = "DeleteUser"
            };

            // Act
            bool result = await todosService.DeleteTodoItem(todoItemToDelete, true);

            // Assert
            Assert.True(result);

            TodoItem? deletedItem = await context.TodoItemsDb.FindAsync(1);
            Assert.Null(deletedItem); // Should be completely removed from database
        }

        [Fact]
        public async Task DeleteTodoItem_Should_Return_False_When_TodoItem_Does_Not_Exist()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteTodoItem_Should_Return_False_When_TodoItem_Does_Not_Exist")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodosService todosService = new(context);

            TodoItem todoItemToDelete = new()
            {
                TodoItemId = 999, // Non-existent ID
                ModifiedBy = "DeleteUser"
            };

            // Act
            bool result = await todosService.DeleteTodoItem(todoItemToDelete, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetTodoItem_Should_Return_TodoItem_Object_When_Id_Is_Valid()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodoItem_Should_Return_TodoItem_Object_When_Id_Is_Valid")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Test Todo",
                Description = "Test Description",
                Status = 0,
                DueDate = _sampleDateTime.AddDays(7),
                AccessLevel = 0,
                Tags = "test,tag",
                Context = "Testing",
                CreatedBy = "TestUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "TestUser",
                IsDeleted = false
            };

            context.Add(todoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            // Act
            TodoItem result = await todosService.GetTodoItem(1);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TodoItem>(result);
            Assert.Equal(todoItem.TodoItemId, result.TodoItemId);
            Assert.Equal(todoItem.ProgenyId, result.ProgenyId);
            Assert.Equal(todoItem.Title, result.Title);
            Assert.Equal(todoItem.Description, result.Description);
            Assert.Equal(todoItem.Status, result.Status);
            Assert.Equal(todoItem.DueDate, result.DueDate);
            Assert.Equal(todoItem.AccessLevel, result.AccessLevel);
            Assert.Equal(todoItem.Tags, result.Tags);
            Assert.Equal(todoItem.Context, result.Context);
            Assert.Equal(todoItem.CreatedBy, result.CreatedBy);
            Assert.Equal(todoItem.CreatedTime, result.CreatedTime);
            Assert.Equal(todoItem.ModifiedTime, result.ModifiedTime);
            Assert.Equal(todoItem.ModifiedBy, result.ModifiedBy);
            Assert.Equal(todoItem.IsDeleted, result.IsDeleted);
        }

        [Fact]
        public async Task GetTodoItem_Should_Return_Null_When_Id_Is_Invalid()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodoItem_Should_Return_Null_When_Id_Is_Invalid")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodosService todosService = new(context);

            // Act
            TodoItem result = await todosService.GetTodoItem(999); // Non-existent ID

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Return_Filtered_List_When_Valid_Parameters_Are_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Return_Filtered_List_When_Valid_Parameters_Are_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            // Create test data
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
                new() { ProgenyId = 2, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(1), CreatedTime = _sampleDateTime, IsDeleted = false, Status = 0, Tags = "tag1", Context = "context1" }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 2, request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<TodoItem>>(result);
            Assert.Equal(3, result.Count); // Only items with access level <= 2, not deleted, and for progeny 1
            Assert.All(result, item => Assert.Equal(1, item.ProgenyId));
            Assert.All(result, item => Assert.True(item.AccessLevel <= 2));
            Assert.All(result, item => Assert.False(item.IsDeleted));
            
            // Check ordering: by DueDate descending, then by CreatedTime descending
            Assert.True(result[0].DueDate >= result[1].DueDate);
            Assert.True(result[1].DueDate >= result[2].DueDate);
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
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime.AddDays(10), CreatedTime = _sampleDateTime, IsDeleted = false }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                StartDate = _sampleDateTime.AddDays(2),
                EndDate = _sampleDateTime.AddDays(8),
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the item with DueDate = _sampleDateTime.AddDays(5) should match
            Assert.Equal(_sampleDateTime.AddDays(5), result[0].DueDate);
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
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Tags = "tag1,tag5" }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                TagFilter = "tag1,tag3",
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // All items should match (first and third have tag1, second has tag3)
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
                new() { ProgenyId = 1, AccessLevel = 0, DueDate = _sampleDateTime, CreatedTime = _sampleDateTime, IsDeleted = false, Context = "work,meeting" }
            ];

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                ContextFilter = "work,personal",
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // All items should match (first and third have work, second has personal)
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
                StatusFilter = "0,2",
                Skip = 0,
                NumberOfItems = 10
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Items with status 0 and 2
            Assert.Contains(result, item => item.Status == 0);
            Assert.Contains(result, item => item.Status == 2);
            Assert.DoesNotContain(result, item => item.Status == 1);
        }

        [Fact]
        public async Task GetTodosForProgeny_Should_Apply_Pagination_When_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosForProgeny_Should_Apply_Pagination_When_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> todoItems = [];
            for (int i = 1; i <= 10; i++)
            {
                todoItems.Add(new TodoItem
                {
                    ProgenyId = 1,
                    AccessLevel = 0,
                    DueDate = _sampleDateTime.AddDays(i),
                    CreatedTime = _sampleDateTime.AddMinutes(i),
                    IsDeleted = false,
                    Title = $"Todo {i}"
                });
            }

            context.AddRange(todoItems);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItemsRequest request = new()
            {
                Skip = 3,
                NumberOfItems = 4
            };

            // Act
            List<TodoItem> result = await todosService.GetTodosForProgeny(1, 0, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count); // Should return 4 items
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

        [Fact]
        public async Task GetTodosList_Should_Return_Empty_List_When_No_Matching_Items()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetTodosList_Should_Return_Empty_List_When_No_Matching_Items")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodosService todosService = new(context);

            // Act
            List<TodoItem> result = await todosService.GetTodosList(999, 0); // Non-existent progeny

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<TodoItem>>(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateTodoItem_Should_Return_Updated_TodoItem_When_Valid_TodoItem_Is_Provided()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateTodoItem_Should_Return_Updated_TodoItem_When_Valid_TodoItem_Is_Provided")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem originalTodoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Original Title",
                Description = "Original Description",
                Status = 0,
                DueDate = _sampleDateTime.AddDays(7),
                AccessLevel = 0,
                Tags = "original,tags",
                Context = "Original Context",
                CreatedBy = "OriginalUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "OriginalUser",
                IsDeleted = false
            };

            context.Add(originalTodoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItem updatedTodoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = 1,
                DueDate = _sampleDateTime.AddDays(14),
                AccessLevel = 1,
                Tags = "updated,tags",
                Context = "Updated Context",
                ModifiedBy = "UpdatedUser",
                IsDeleted = false
            };

            // Act
            TodoItem result = await todosService.UpdateTodoItem(updatedTodoItem);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TodoItem>(result);
            Assert.Equal(updatedTodoItem.TodoItemId, result.TodoItemId);
            Assert.Equal(updatedTodoItem.ProgenyId, result.ProgenyId);
            Assert.Equal(updatedTodoItem.Title, result.Title);
            Assert.Equal(updatedTodoItem.Description, result.Description);
            Assert.Equal(updatedTodoItem.Status, result.Status);
            Assert.Equal(updatedTodoItem.DueDate, result.DueDate);
            Assert.Equal(updatedTodoItem.AccessLevel, result.AccessLevel);
            Assert.Equal(updatedTodoItem.Tags, result.Tags);
            Assert.Equal(updatedTodoItem.Context, result.Context);
            Assert.Equal(updatedTodoItem.ModifiedBy, result.ModifiedBy);
            Assert.Equal(updatedTodoItem.IsDeleted, result.IsDeleted);
            
            // ModifiedTime should be updated to current UTC time
            Assert.True(result.ModifiedTime > _sampleDateTime);
            
            // CreatedBy and CreatedTime should remain unchanged
            Assert.Equal(originalTodoItem.CreatedBy, result.CreatedBy);
            Assert.Equal(originalTodoItem.CreatedTime, result.CreatedTime);
        }

        [Fact]
        public async Task UpdateTodoItem_Should_Return_Null_When_TodoItem_Does_Not_Exist()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateTodoItem_Should_Return_Null_When_TodoItem_Does_Not_Exist")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodosService todosService = new(context);

            TodoItem nonExistentTodoItem = new()
            {
                TodoItemId = 999, // Non-existent ID
                ProgenyId = 1,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = 1,
                DueDate = _sampleDateTime.AddDays(14),
                AccessLevel = 1,
                Tags = "updated,tags",
                Context = "Updated Context",
                ModifiedBy = "UpdatedUser",
                IsDeleted = false
            };

            // Act
            TodoItem result = await todosService.UpdateTodoItem(nonExistentTodoItem);

            // Assert
            Assert.Null(result);
        }
    }
}