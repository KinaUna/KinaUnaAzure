using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
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
                Location = "Test Location",
                CreatedBy = "TestUser"
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
            Assert.Equal(todoItem.DueDate.Value.Date, result.DueDate?.Date ?? new DateTime()); // The time part can be off by a few milliseconds, only compare the date
            Assert.Equal(todoItem.AccessLevel, result.AccessLevel);
            Assert.Equal(todoItem.Tags, result.Tags);
            Assert.Equal(todoItem.Context, result.Context);
            Assert.Equal(todoItem.Location, result.Location);
            Assert.Equal(todoItem.CreatedBy, result.CreatedBy);
            Assert.Equal(todoItem.CreatedBy, result.ModifiedBy); // ModifiedBy should be set to CreatedBy
            Assert.False(result.IsDeleted);
            Assert.True(result.CreatedTime > DateTime.MinValue);
            Assert.True(result.ModifiedTime > DateTime.MinValue);
            Assert.Equal(result.CreatedTime.Date, result.ModifiedTime.Date); // The time part can be off by a few milliseconds, only compare the date, hour, minute, and second
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
                Location = "Test Location",
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
            Assert.Equal(todoItem.Location, result.Location);
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
                Status = 1, // Changed to InProgress
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

            // StartDate should be set when status changes to InProgress
            Assert.NotNull(result.StartDate);
            Assert.True(result.StartDate > _sampleDateTime);
        }

        [Fact]
        public async Task UpdateTodoItem_Should_Set_CompletedDate_When_Status_Changes_To_Completed()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateTodoItem_Should_Set_CompletedDate_When_Status_Changes_To_Completed")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem originalTodoItem = new()
            {
                TodoItemId = 1,
                Status = 1, // InProgress
                CreatedBy = "TestUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "TestUser"
            };

            context.Add(originalTodoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItem updatedTodoItem = new()
            {
                TodoItemId = 1,
                Status = 2, // Completed
                ModifiedBy = "UpdatedUser"
            };

            // Act
            TodoItem result = await todosService.UpdateTodoItem(updatedTodoItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Status);
            Assert.NotNull(result.CompletedDate);
            Assert.True(result.CompletedDate > _sampleDateTime);
        }

        [Fact]
        public async Task UpdateTodoItem_Should_Reset_Dates_When_Status_Changes_To_NotStarted()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateTodoItem_Should_Reset_Dates_When_Status_Changes_To_NotStarted")
                .Options;
            await using ProgenyDbContext context = new(dbOptions);

            TodoItem originalTodoItem = new()
            {
                TodoItemId = 1,
                Status = 2, // Completed
                StartDate = _sampleDateTime,
                CompletedDate = _sampleDateTime.AddDays(1),
                CreatedBy = "TestUser",
                CreatedTime = _sampleDateTime,
                ModifiedTime = _sampleDateTime,
                ModifiedBy = "TestUser"
            };

            context.Add(originalTodoItem);
            await context.SaveChangesAsync();

            TodosService todosService = new(context);

            TodoItem updatedTodoItem = new()
            {
                TodoItemId = 1,
                Status = 0, // NotStarted
                ModifiedBy = "UpdatedUser"
            };

            // Act
            TodoItem result = await todosService.UpdateTodoItem(updatedTodoItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Status);
            Assert.Null(result.StartDate);
            Assert.Null(result.CompletedDate);
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
                Status = 1,
                ModifiedBy = "UpdatedUser"
            };

            // Act
            TodoItem result = await todosService.UpdateTodoItem(nonExistentTodoItem);

            // Assert
            Assert.Null(result);
        }
    }
}