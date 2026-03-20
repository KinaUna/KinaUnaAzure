using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class NoteServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService = new();

        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
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

        private void SetupDefaultMocks()
        {
            _mockKinaUnaCacheService
                .Setup(x => x.GetNotesListCache(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((NotesListCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.SetNotesListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Note[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);
        }

        #region GetNote Tests

        [Fact]
        public async Task GetNote_Should_Return_Note_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNote_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.GetNote(1, userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNote_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.GetNote(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNote_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.GetNote(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNote_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNote_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result1 = await service.GetNote(1, userInfo);
            Note? result2 = await service.GetNote(1, userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("AddNote_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.AddNote(note, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.NoteId);
            Assert.Equal("New Note", result.Title);
            Assert.Equal("New Content", result.Content);
            Assert.NotEqual(default(DateTime), result.CreatedTime);

            Note? dbNote = await context.NotesDb.FindAsync(result.NoteId);
            Assert.NotNull(dbNote);
            Assert.Equal("New Note", dbNote.Title);

            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Note), Times.Once);
        }

        [Fact]
        public async Task AddNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddNote_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
            {
                ProgenyId = 1,
                Title = "New Note",
                Content = "New Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.AddNote(note, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.NotesDb);
        }

        [Fact]
        public async Task AddNote_Should_Set_CreatedDate()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddNote_CreatedDate");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            DateTime beforeAdd = DateTime.UtcNow;
            SetupDefaultMocks();

            Note note = new()
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

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.AddNote(note, userInfo);
            DateTime afterAdd = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedTime >= beforeAdd);
            Assert.True(result.CreatedTime <= afterAdd);
        }

        #endregion

        #region UpdateNote Tests

        [Fact]
        public async Task UpdateNote_Should_Update_Note_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateNote_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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

            Note updatedNote = new()
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

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Content", result.Content);
            Assert.Equal("Updated Category", result.Category);

            Note? dbNote = await context.NotesDb.FindAsync(1);
            Assert.NotNull(dbNote);
            Assert.Equal("Updated Title", dbNote.Title);
            Assert.Equal("Updated Content", dbNote.Content);

            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Note), Times.Once);
        }

        [Fact]
        public async Task UpdateNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateNote_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Original Title",
                Content = "Original Content"
            };

            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            Note updatedNote = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.Null(result);

            // Verify original note unchanged
            Note? dbNote = await context.NotesDb.FindAsync(1);
            Assert.Equal("Original Title", dbNote!.Title);
        }

        [Fact]
        public async Task UpdateNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateNote_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note updatedNote = new()
            {
                NoteId = 999,
                ProgenyId = 1,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.UpdateNote(updatedNote, userInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteNote Tests

        [Fact]
        public async Task DeleteNote_Should_Delete_Note_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteNote_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NoteId);

            Note? dbNote = await context.NotesDb.FindAsync(1);
            Assert.Null(dbNote);

            _mockKinaUnaCacheService.Verify(x => x.SetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Note), Times.Once);
        }

        [Fact]
        public async Task DeleteNote_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteNote_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
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

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.Null(result);

            // Verify note still exists
            Note? dbNote = await context.NotesDb.FindAsync(1);
            Assert.NotNull(dbNote);
        }

        [Fact]
        public async Task DeleteNote_Should_Return_Null_When_Note_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteNote_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new()
            {
                NoteId = 999,
                ProgenyId = 1,
                Title = "Test Note"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Note? result = await service.DeleteNote(note, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteNote_Should_Remove_From_Cache()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteNote_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note1 = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Note 1"
            };

            Note note2 = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Prime the cache
            await service.GetNotesList(1, userInfo);

            // Act
            await service.DeleteNote(note1, userInfo);

            // Get list again - should be updated
            List<Note>? result = await service.GetNotesList(1, userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesList_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            _mockKinaUnaCacheService.Verify(x => x.SetNotesListCache(userInfo.UserId, 1, It.IsAny<Note[]>()), Times.Once);
        }

        [Fact]
        public async Task GetNotesList_Should_Return_Only_Accessible_Notes()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesList_PartialAccess");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesList(1, userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesList_Empty");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesList_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Note note = new() { NoteId = 1, ProgenyId = 1, Title = "Note 1" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NotesListCacheEntry cacheEntry = new()
            {
                NotesList = new[] { note },
                UpdateTime = DateTime.UtcNow
            };

            TimelineUpdatedCacheEntry timelineUpdatedEntry = new()
            {
                UpdateTime = DateTime.UtcNow.AddMinutes(-5)
            };

            _mockKinaUnaCacheService
                .Setup(x => x.GetNotesListCache(userInfo.UserId, 1))
                .ReturnsAsync(cacheEntry);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(1, 0, KinaUnaTypes.TimeLineType.Note))
                .ReturnsAsync(timelineUpdatedEntry);

            _mockKinaUnaCacheService
                .Setup(x => x.SetNotesListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Note[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result1 = await service.GetNotesList(1, userInfo);
            List<Note>? result2 = await service.GetNotesList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);

            // Verify cache was checked
            _mockKinaUnaCacheService.Verify(x => x.GetNotesListCache(userInfo.UserId, 1), Times.AtLeast(2));
        }

        [Fact]
        public async Task GetNotesList_Should_Not_Include_Notes_From_Other_Progenies()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesList_FilterByProgeny");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesList(1, userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "School", userInfo);

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
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_NoFilter");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_All_Notes_When_Category_Is_Empty()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_EmptyFilter");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_CaseInsensitive");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new() { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "School Activities" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Match_Substring()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_Substring");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            Note note = new() { NoteId = 1, ProgenyId = 1, Title = "Note 1", Category = "After School Activities" };
            context.NotesDb.Add(note);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Handle_Null_Category_In_Note()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_NullCategory");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].NoteId);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Return_Empty_List_When_No_Matching_Categories()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_NoMatch");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesWithCategory_Should_Only_Return_Accessible_Notes()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetNotesWithCategory_AccessFiltered");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            SetupDefaultMocks();

            List<Note> notes = new()
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
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            NoteService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Note>? result = await service.GetNotesWithCategory(1, "School", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.NoteId == 1);
            Assert.Contains(result, n => n.NoteId == 3);
        }

        #endregion
    }
}