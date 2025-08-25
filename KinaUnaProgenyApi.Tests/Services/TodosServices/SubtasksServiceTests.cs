using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services.TodosServices
{
    public class SubtasksServiceTests
    {
        private const int TestProgenyId = 1;
        private const int TestParentTodoItemId = 100;
        private const string TestUserId = "test-user-id";

        #region AddSubtask Tests

        [Fact]
        public async Task AddSubtask_Should_Add_Subtask_With_Initialized_Properties()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("AddSubtask_InitializedProperties")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            TodoItem subtaskToAdd = new()
            {
                Title = "Test Subtask",
                Description = "Test Description",
                ProgenyId = TestProgenyId,
                ParentTodoItemId = TestParentTodoItemId,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                CreatedBy = TestUserId
            };

            // Act
            TodoItem result = await service.AddSubtask(subtaskToAdd);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TodoItemId > 0);
            Assert.Equal(subtaskToAdd.Title, result.Title);
            Assert.Equal(subtaskToAdd.Description, result.Description);
            Assert.Equal(subtaskToAdd.ProgenyId, result.ProgenyId);
            Assert.Equal(subtaskToAdd.ParentTodoItemId, result.ParentTodoItemId);
            Assert.Equal(TestUserId, result.CreatedBy);
            Assert.Equal(TestUserId, result.ModifiedBy);
            Assert.False(result.IsDeleted);
            Assert.True(result.CreatedTime > DateTime.MinValue);
            Assert.True(result.ModifiedTime > DateTime.MinValue);
            Assert.Equal(result.CreatedTime.Date, result.ModifiedTime.Date);
            Assert.Equal(result.CreatedTime.Hour, result.ModifiedTime.Hour);
            Assert.Equal(result.CreatedTime.Minute, result.ModifiedTime.Minute);
            Assert.Equal(result.CreatedTime.Second, result.ModifiedTime.Second);

        }

        [Fact]
        public async Task AddSubtask_Should_Persist_To_Database()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("AddSubtask_PersistToDatabase")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            TodoItem subtaskToAdd = new()
            {
                Title = "Persistent Subtask",
                CreatedBy = TestUserId,
                ProgenyId = TestProgenyId,
                ParentTodoItemId = TestParentTodoItemId
            };

            // Act
            TodoItem result = await service.AddSubtask(subtaskToAdd);

            // Assert
            TodoItem? retrievedSubtask = await context.TodoItemsDb
                .FirstOrDefaultAsync(t => t.TodoItemId == result.TodoItemId);
            
            Assert.NotNull(retrievedSubtask);
            Assert.Equal(result.Title, retrievedSubtask.Title);
            Assert.Equal(result.CreatedBy, retrievedSubtask.CreatedBy);
        }

        #endregion

        #region GetSubtask Tests

        [Fact]
        public async Task GetSubtask_Should_Return_Subtask_When_Exists()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetSubtask_ReturnWhenExists")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            
            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Title = "Existing Subtask",
                Description = "Test Description",
                ProgenyId = TestProgenyId,
                ParentTodoItemId = TestParentTodoItemId,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                IsDeleted = false
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            // Act
            TodoItem? result = await service.GetSubtask(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingSubtask.TodoItemId, result.TodoItemId);
            Assert.Equal(existingSubtask.Title, result.Title);
            Assert.Equal(existingSubtask.Description, result.Description);
        }

        [Fact]
        public async Task GetSubtask_Should_Return_Null_When_Not_Exists()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetSubtask_ReturnNullWhenNotExists")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            // Act
            TodoItem? result = await service.GetSubtask(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetSubtasksForTodoItem Tests

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_All_Non_Deleted_Subtasks()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetSubtasksForTodoItem_NonDeleted")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            List<TodoItem> subtasks = new()
            {
                new()
                {
                    TodoItemId = 1,
                    Title = "Active Subtask 1",
                    ParentTodoItemId = TestParentTodoItemId,
                    IsDeleted = false
                },
                new()
                {
                    TodoItemId = 2,
                    Title = "Active Subtask 2",
                    ParentTodoItemId = TestParentTodoItemId,
                    IsDeleted = false
                },
                new()
                {
                    TodoItemId = 3,
                    Title = "Deleted Subtask",
                    ParentTodoItemId = TestParentTodoItemId,
                    IsDeleted = true
                },
                new()
                {
                    TodoItemId = 4,
                    Title = "Different Parent",
                    ParentTodoItemId = 999,
                    IsDeleted = false
                }
            };

            context.TodoItemsDb.AddRange(subtasks);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            // Act
            List<TodoItem> result = await service.GetSubtasksForTodoItem(TestParentTodoItemId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(TestParentTodoItemId, item.ParentTodoItemId));
            Assert.All(result, item => Assert.False(item.IsDeleted));
            Assert.Contains(result, item => item.Title == "Active Subtask 1");
            Assert.Contains(result, item => item.Title == "Active Subtask 2");
        }

        [Fact]
        public async Task GetSubtasksForTodoItem_Should_Return_Empty_List_When_No_Subtasks()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("GetSubtasksForTodoItem_EmptyList")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            // Act
            List<TodoItem> result = await service.GetSubtasksForTodoItem(TestParentTodoItemId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region UpdateSubtask Tests

        [Fact]
        public async Task UpdateSubtask_Should_Update_Existing_Subtask()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_UpdateExisting")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Title = "Original Title",
                Description = "Original Description",
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                ProgenyId = TestProgenyId,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                IsDeleted = false
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                ModifiedBy = TestUserId
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateValues.Title, result.Title);
            Assert.Equal(updateValues.Description, result.Description);
            Assert.Equal(updateValues.Status, result.Status);
            Assert.Equal(updateValues.ModifiedBy, result.ModifiedBy);
        }

        [Fact]
        public async Task UpdateSubtask_Should_Return_Null_When_Subtask_Not_Exists()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_NotExists")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 999,
                Title = "Updated Title"
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSubtask_Should_Set_CompletedDate_When_Status_Changes_To_Completed()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_SetCompletedDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress,
                CompletedDate = null
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.CompletedDate);
            Assert.True(result.CompletedDate.Value > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task UpdateSubtask_Should_Reset_CompletedDate_When_Status_Changes_To_NotStarted()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_ResetCompletedDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CompletedDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateSubtask_Should_Set_StartDate_And_Reset_CompletedDate_When_Status_Changes_To_InProgress()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_SetStartDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.NotStarted,
                StartDate = null,
                CompletedDate = null
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.InProgress
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.StartDate);
            Assert.Null(result.CompletedDate);
            Assert.True(result.StartDate.Value > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task UpdateSubtask_Should_Reset_CompletedDate_When_Status_Changes_To_Cancelled()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_CancelledResetCompletedDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CompletedDate = DateTime.UtcNow
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Cancelled
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateSubtask_Should_Preserve_CompletedDate_When_Status_Unchanged()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("UpdateSubtask_PreserveCompletedDate")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            DateTime originalCompletedDate = DateTime.UtcNow.AddDays(-1);
            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed,
                CompletedDate = originalCompletedDate,
                Title = "Original Title"
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem updateValues = new()
            {
                TodoItemId = 1,
                Status = (int)KinaUnaTypes.TodoStatusType.Completed, // Same status
                Title = "Updated Title",
                CompletedDate = originalCompletedDate
            };

            // Act
            TodoItem? result = await service.UpdateSubtask(updateValues);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalCompletedDate, result.CompletedDate);
            Assert.Equal("Updated Title", result.Title);
        }

        #endregion

        #region DeleteSubtask Tests

        [Fact]
        public async Task DeleteSubtask_Should_Soft_Delete_By_Default()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteSubtask_SoftDelete")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Title = "Subtask to Delete",
                IsDeleted = false,
                ModifiedTime = DateTime.UtcNow.AddDays(-1)
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 1,
                ModifiedBy = TestUserId
            };

            // Act
            bool result = await service.DeleteSubtask(subtaskToDelete);

            // Assert
            Assert.True(result);

            TodoItem? deletedSubtask = await context.TodoItemsDb
                .FirstOrDefaultAsync(t => t.TodoItemId == 1);
            
            Assert.NotNull(deletedSubtask);
            Assert.True(deletedSubtask.IsDeleted);
            Assert.Equal(TestUserId, deletedSubtask.ModifiedBy);
            Assert.True(deletedSubtask.ModifiedTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task DeleteSubtask_Should_Hard_Delete_When_Specified()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteSubtask_HardDelete")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);

            TodoItem existingSubtask = new()
            {
                TodoItemId = 1,
                Title = "Subtask to Delete",
                IsDeleted = false
            };

            context.TodoItemsDb.Add(existingSubtask);
            await context.SaveChangesAsync();

            SubtasksService service = new(context);

            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 1
            };

            // Act
            bool result = await service.DeleteSubtask(subtaskToDelete, hardDelete: true);

            // Assert
            Assert.True(result);

            TodoItem? deletedSubtask = await context.TodoItemsDb
                .FirstOrDefaultAsync(t => t.TodoItemId == 1);
            
            Assert.Null(deletedSubtask);
        }

        [Fact]
        public async Task DeleteSubtask_Should_Return_False_When_Subtask_Not_Exists()
        {
            // Arrange
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase("DeleteSubtask_NotExists")
                .Options;

            await using ProgenyDbContext context = new(dbOptions);
            SubtasksService service = new(context);

            TodoItem subtaskToDelete = new()
            {
                TodoItemId = 999
            };

            // Act
            bool result = await service.DeleteSubtask(subtaskToDelete);

            // Assert
            Assert.False(result);
        }

        #endregion

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

            List<TodoItem> subtasks = new();

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 15) },
                new() { TodoItemId = 3, StartDate = new DateTime(2023, 2, 1) }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, DueDate = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, DueDate = new DateTime(2023, 1, 15) },
                new() { TodoItemId = 3, DueDate = new DateTime(2023, 2, 1) }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Tags = "urgent,important" },
                new() { TodoItemId = 2, Tags = "important,work" },
                new() { TodoItemId = 3, Tags = "personal" },
                new() { TodoItemId = 4, Tags = null }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Context = "work" },
                new() { TodoItemId = 2, Context = "home" },
                new() { TodoItemId = 3, Context = "work-project" },
                new() { TodoItemId = 4, Context = null }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Location = "office" },
                new() { TodoItemId = 2, Location = "home" },
                new() { TodoItemId = 3, Location = "office-building" },
                new() { TodoItemId = 4, Location = null }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Status = (int)KinaUnaTypes.TodoStatusType.NotStarted },
                new() { TodoItemId = 2, Status = (int)KinaUnaTypes.TodoStatusType.InProgress },
                new() { TodoItemId = 3, Status = (int)KinaUnaTypes.TodoStatusType.Completed },
                new() { TodoItemId = 4, Status = (int)KinaUnaTypes.TodoStatusType.Cancelled }
            };

            SubtasksRequest request = new()
            {
                ParentTodoItemId = TestParentTodoItemId,
                StatusFilter = new List<KinaUnaTypes.TodoStatusType>
                {
                    KinaUnaTypes.TodoStatusType.NotStarted,
                    KinaUnaTypes.TodoStatusType.InProgress
                }
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

            List<TodoItem> subtasks = new();
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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Status = (int)KinaUnaTypes.TodoStatusType.Completed, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, Status = (int)KinaUnaTypes.TodoStatusType.NotStarted, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, Status = (int)KinaUnaTypes.TodoStatusType.InProgress, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, ProgenyId = 3, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, ProgenyId = 1, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, ProgenyId = 2, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, Location = "home", StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) },
                new() { TodoItemId = 2, Location = "office", StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, Location = "store", StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) }
            };

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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 3), CreatedTime = new DateTime(2023, 1, 3) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 3, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) }
            };

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

            List<TodoItem> subtasks = new();
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

            List<TodoItem> subtasks = new()
            {
                new() { TodoItemId = 1, StartDate = new DateTime(2023, 1, 1), CreatedTime = new DateTime(2023, 1, 1) },
                new() { TodoItemId = 2, StartDate = new DateTime(2023, 1, 2), CreatedTime = new DateTime(2023, 1, 2) }
            };

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