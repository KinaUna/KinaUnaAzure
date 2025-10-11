using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class NoteServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;

        public NoteServiceTests()
        {
            _mockAccessManagementService = new Mock<IAccessManagementService>();
        }

        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            var options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        #region GetNote Tests

        [Fact]
        public async Task GetNote_Should_Return_Note_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNote_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Test Note",
                Content = "Test Content",
                Category = "Test Category",
                Owner = "testuser@test.com",
                CreatedDate = DateTime.UtcNow
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNote(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NoteId);
            Assert.Equal("Test Note", result.Title);
            Assert.Equal("Test Content", result.Content);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNote_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Test Note"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNote(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNote_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNote(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNote_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNote_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Test Note",
                Content = "Test Content"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetNote(1, userInfo);
            var result2 = await service.GetNote(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Title, result2.Title);
            Assert.Equal(result1.Content, result2.Content);
        }

        #endregion

        #region AddNote Tests

        [Fact]
        public async Task AddNote_Should_Add_Note_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddNote_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                ProgenyId = 1,
                Title = "New Note",
                Content = "New Content",
                Category = "New Category",
                Owner = "testuser@test.com",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddNote(note, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.NoteId);
            Assert.Equal("New Note", result.Title);
            Assert.Equal("New Content", result.Content);
            Assert.NotEqual(default(DateTime), result.CreatedDate);

            var dbNote = await context.NotesDb.FindAsync(result.NoteId);
            Assert.NotNull(dbNote);
            Assert.Equal("New Note", dbNote.Title);
        }

        [Fact]
        public async Task AddNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddNote_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                ProgenyId = 1,
                Title = "New Note",
                Content = "New Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddNote(note, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.NotesDb);
        }

        [Fact]
        public async Task AddNote_Should_Set_CreatedDate()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddNote_CreatedDate");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();
            var beforeAdd = DateTime.UtcNow;

            var note = new Note
            {
                ProgenyId = 1,
                Title = "New Note",
                Content = "New Content",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddNote(note, userInfo);
            var afterAdd = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedDate >= beforeAdd);
            Assert.True(result.CreatedDate <= afterAdd);
        }

        #endregion

        #region UpdateNote Tests

        [Fact]
        public async Task UpdateNote_Should_Update_Note_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateNote_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Original Title",
                Content = "Original Content",
                Category = "Original Category"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();
            context.Entry(note).State = EntityState.Detached;

            var updatedNote = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Updated Title",
                Content = "Updated Content",
                Category = "Updated Category",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Content", result.Content);
            Assert.Equal("Updated Category", result.Category);

            var dbNote = await context.NotesDb.FindAsync(1);
            Assert.NotNull(dbNote);
            Assert.Equal("Updated Title", dbNote.Title);
            Assert.Equal("Updated Content", dbNote.Content);
        }

        [Fact]
        public async Task UpdateNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateNote_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Original Title",
                Content = "Original Content"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            var updatedNote = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.Null(result);

            // Verify original note unchanged
            var dbNote = await context.NotesDb.FindAsync(1);
            Assert.Equal("Original Title", dbNote!.Title);
        }

        [Fact]
        public async Task UpdateNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateNote_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var updatedNote = new Note
            {
                NoteId = 999,
                ProgenyId = 1,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteNote Tests

        [Fact]
        public async Task DeleteNote_Should_Delete_Note_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteNote_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Test Note",
                Content = "Test Content"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NoteId);

            var dbNote = await context.NotesDb.FindAsync(1);
            Assert.Null(dbNote);
        }

        [Fact]
        public async Task DeleteNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteNote_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Test Note",
                Content = "Test Content"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.Null(result);

            // Verify note still exists
            var dbNote = await context.NotesDb.FindAsync(1);
            Assert.NotNull(dbNote);
        }

        [Fact]
        public async Task DeleteNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteNote_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note
            {
                NoteId = 999,
                ProgenyId = 1,
                Title = "Test Note"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteNote_Should_Remove_From_Cache()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteNote_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note1 = new Note
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Note 1"
            };

            var note2 = new Note
            {
                NoteId = 2,
                ProgenyId = 1,
                Title = "Note 2"
            };

            context.NotesDb.AddRange(note1, note2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Prime the cache
            await service.GetNotesList(1, userInfo);

            // Act
            await service.DeleteNote(note1, userInfo);

            // Get list again - should be updated
            var result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].NoteId);
        }

        #endregion

        #region GetNotesList Tests

        [Fact]
        public async Task GetNotesList_Should_Return_List_Of_Accessible_Notes()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesList_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Content = "Content 1" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Content = "Content 2" },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3", Content = "Content 3" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetNotesList_Should_Return_Only_Accessible_Notes()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesList_PartialAccess");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2" },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.NoteId == 1);
            Assert.Contains(result, n => n.NoteId == 3);
            Assert.DoesNotContain(result, n => n.NoteId == 2);
        }

        [Fact]
        public async Task GetNotesList_Should_Return_Empty_List_When_No_Notes_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesList_Empty");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesList_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetNotesList(1, userInfo);
            var result2 = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);
        }

        [Fact]
        public async Task GetNotesList_Should_Not_Include_Notes_From_Other_Progenies()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesList_FilterByProgeny");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1" },
                new Note { NoteId = 2, ProgenyId = 2, Title = "Note 2" },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, n => Assert.Equal(1, n.ProgenyId));
        }

        #endregion

        #region GetNotesWithCategory Tests

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_Notes_With_Matching_Category()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School Notes" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = "Personal" },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3", Category = "School Activities" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.NoteId == 1);
            Assert.Contains(result, n => n.NoteId == 3);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_All_Notes_When_Category_Is_Null()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_NoFilter");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = "Personal" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_All_Notes_When_Category_Is_Empty()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_EmptyFilter");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = "Personal" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_CaseInsensitive");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School Activities" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Match_Substring()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_Substring");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var note = new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "After School Activities" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Handle_Null_Category_In_Note()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_NullCategory");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = null },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3", Category = "Personal" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].NoteId);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_Empty_List_When_No_Matching_Categories()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_NoMatch");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "Work" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = "Personal" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Only_Return_Accessible_Notes()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetNotesWithCategory_AccessFiltered");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var notes = new List<Note>
            {
                new Note { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School" },
                new Note { NoteId = 2, ProgenyId = 1, Title = "Note 2", Category = "School" },
                new Note { NoteId = 3, ProgenyId = 1, Title = "Note 3", Category = "School" }
            };

            context.NotesDb.AddRange(notes);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new NoteService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.NoteId == 1);
            Assert.Contains(result, n => n.NoteId == 3);
        }

        #endregion
    }
}