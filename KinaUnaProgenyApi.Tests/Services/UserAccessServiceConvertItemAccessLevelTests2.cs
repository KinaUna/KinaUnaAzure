using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public sealed class UserAccessServiceConvertItemAccessLevelTests
    {
        private readonly ProgenyDbContext _progenyContext;
        private readonly MediaDbContext _mediaContext;
        private readonly KinaUnaProgenyApi.Services.UserAccessService.UserAccessService _service;
        
        public UserAccessServiceConvertItemAccessLevelTests()
        {
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _progenyContext = new ProgenyDbContext(progenyOptions);

            DbContextOptions<MediaDbContext> mediaOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mediaContext = new MediaDbContext(mediaOptions);

            IOptions<MemoryDistributedCacheOptions> cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache cache = new MemoryDistributedCache(cacheOptions);

            _service = new KinaUnaProgenyApi.Services.UserAccessService.UserAccessService(
                _progenyContext,
                _mediaContext,
                cache
            );
        }

        #region Helper Methods

        private async Task CreateTestProgenyWithGroups(int progenyId)
        {
            Progeny progeny = new()
            {
                Id = progenyId,
                Name = $"Test Progeny {progenyId}",
                NickName = $"Progeny{progenyId}",
                Admins = "admin@test.com"
            };
            _progenyContext.ProgenyDb.Add(progeny);
            await _progenyContext.SaveChangesAsync();

            // Create access level groups
            AccessLevelList accessLevels = new();
            foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
            {
                if (int.TryParse(accessLevel.Value, out int accessLevelValue) && accessLevelValue <= 3)
                {
                    UserGroup userGroup = new()
                    {
                        ProgenyId = progenyId,
                        Name = accessLevel.Text,
                        Description = $"Auto-generated group for {accessLevel.Text}"
                    };
                    _progenyContext.UserGroupsDb.Add(userGroup);
                }
            }
            await _progenyContext.SaveChangesAsync();
        }

        private async Task CreateTestUserInfo(string email)
        {
            UserInfo userInfo = new()
            {
                UserEmail = email,
                UserId = email
            };
            _progenyContext.UserInfoDb.Add(userInfo);
            await _progenyContext.SaveChangesAsync();
        }

        #endregion

        #region Photo Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Photo_WithAccessLevel0_CreatesAdminPermissions()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);
            await CreateTestUserInfo("admin@test.com");

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                PictureLink = "test.jpg"
            };
            _mediaContext.PicturesDb.Add(picture);
            await _mediaContext.SaveChangesAsync();

            // Act
            bool moreItemsRemaining = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            Assert.False(moreItemsRemaining);
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Photo && p.ItemId == 1)
                .ToListAsync();

            Assert.NotEmpty(permissions);
            Assert.Contains(permissions, p => p.PermissionLevel == PermissionLevel.Admin);

            Picture? updatedPicture = await _mediaContext.PicturesDb.FindAsync(1);
            Assert.Equal(99, updatedPicture!.AccessLevel);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Photo_WithAccessLevel3_CreatesMultiplePermissions()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Picture picture = new()
            {
                PictureId = 2,
                ProgenyId = 1,
                AccessLevel = 3, // Friends level
                PictureLink = "test2.jpg"
            };
            _mediaContext.PicturesDb.Add(picture);
            await _mediaContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            Assert.False(result);
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Photo && p.ItemId == 2)
                .ToListAsync();

            // Should create permissions for Admin (0), Family (1), Caretakers (2), and Friends (3)
            Assert.True(permissions.Count >= 4);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Photo_WithMultipleItems_ReturnsTrue()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            for (int i = 1; i <= 15; i++)
            {
                Picture picture = new()
                {
                    PictureId = i,
                    ProgenyId = 1,
                    AccessLevel = 0,
                    PictureLink = $"test{i}.jpg"
                };
                _mediaContext.PicturesDb.Add(picture);
            }
            await _mediaContext.SaveChangesAsync();

            // Act - Request only 10 items
            bool moreItemsRemaining = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            Assert.True(moreItemsRemaining);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Photo_SkipsAlreadyConvertedItems()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                AccessLevel = 99, // Already converted
                PictureLink = "test.jpg"
            };
            _mediaContext.PicturesDb.Add(picture);
            await _mediaContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            Assert.False(result);
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Photo)
                .ToListAsync();

            Assert.Empty(permissions);
        }

        #endregion

        #region Video Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Video_WithAccessLevel1_CreatesCorrectPermissions()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                AccessLevel = 1, // Family level
                VideoLink = "test.mp4"
            };
            _mediaContext.VideoDb.Add(video);
            await _mediaContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Video, 10);

            // Assert
            Assert.False(result);
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Video && p.ItemId == 1)
                .ToListAsync();

            Assert.NotEmpty(permissions);
            Video? updatedVideo = await _mediaContext.VideoDb.FindAsync(1);
            Assert.Equal(99, updatedVideo!.AccessLevel);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Video_CreatesAdminUserPermissions()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);
            await CreateTestUserInfo("admin@test.com");

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                VideoLink = "test.mp4"
            };
            _mediaContext.VideoDb.Add(video);
            await _mediaContext.SaveChangesAsync();

            // Act
            await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Video, 10);

            // Assert
            List<TimelineItemPermission> adminPermissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Video
                    && p.ItemId == 1
                    && p.Email == "admin@test.com")
                .ToListAsync();

            Assert.NotEmpty(adminPermissions);
        }

        #endregion

        #region Calendar Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Calendar_WithAccessLevel2_CreatesCorrectPermissions()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                AccessLevel = 2, // Caretakers level
                Title = "Test Event",
                StartTime = DateTime.UtcNow,
                UId = Guid.NewGuid().ToString()
            };
            _progenyContext.CalendarDb.Add(calendarItem);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Calendar, 10);

            // Assert
            Assert.False(result);
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Calendar && p.ItemId == 1)
                .ToListAsync();

            Assert.NotEmpty(permissions);
            // Should create permissions for Admin (0), Family (1), and Caretakers (2)
            Assert.True(permissions.Count >= 3);

            CalendarItem? updatedItem = await _progenyContext.CalendarDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Vocabulary Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Vocabulary_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                AccessLevel = 1,
                Word = "Test Word",
                Date = DateTime.UtcNow
            };
            _progenyContext.VocabularyDb.Add(vocabularyItem);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Vocabulary, 10);

            // Assert
            Assert.False(result);
            VocabularyItem? updatedItem = await _progenyContext.VocabularyDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Skill Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Skill_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Skill skill = new()
            {
                SkillId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                Name = "Test Skill",
                SkillFirstObservation = DateTime.UtcNow
            };
            _progenyContext.SkillsDb.Add(skill);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Skill, 10);

            // Assert
            Assert.False(result);
            Skill? updatedItem = await _progenyContext.SkillsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Friend Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Friend_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                AccessLevel = 2,
                Name = "Test Friend"
            };
            _progenyContext.FriendsDb.Add(friend);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Friend, 10);

            // Assert
            Assert.False(result);
            Friend? updatedItem = await _progenyContext.FriendsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Measurement Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Measurement_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Measurement measurement = new()
            {
                MeasurementId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                Date = DateTime.UtcNow
            };
            _progenyContext.MeasurementsDb.Add(measurement);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Measurement, 10);

            // Assert
            Assert.False(result);
            Measurement? updatedItem = await _progenyContext.MeasurementsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Sleep Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Sleep_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Sleep sleep = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                AccessLevel = 1,
                SleepStart = DateTime.UtcNow,
                SleepEnd = DateTime.UtcNow.AddHours(8)
            };
            _progenyContext.SleepDb.Add(sleep);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Sleep, 10);

            // Assert
            Assert.False(result);
            Sleep? updatedItem = await _progenyContext.SleepDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Note Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Note_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                AccessLevel = 3,
                Title = "Test Note",
                Content = "Test Content",
                CreatedDate = DateTime.UtcNow
            };
            _progenyContext.NotesDb.Add(note);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Note, 10);

            // Assert
            Assert.False(result);
            Note? updatedItem = await _progenyContext.NotesDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Contact Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Contact_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                DisplayName = "Test Contact"
            };
            _progenyContext.ContactsDb.Add(contact);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Contact, 10);

            // Assert
            Assert.False(result);
            Contact? updatedItem = await _progenyContext.ContactsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Vaccination Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Vaccination_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Vaccination vaccination = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                AccessLevel = 2,
                VaccinationName = "Test Vaccine",
                VaccinationDate = DateTime.UtcNow
            };
            _progenyContext.VaccinationsDb.Add(vaccination);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Vaccination, 10);

            // Assert
            Assert.False(result);
            Vaccination? updatedItem = await _progenyContext.VaccinationsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Location Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Location_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Location location = new()
            {
                LocationId = 1,
                ProgenyId = 1,
                AccessLevel = 1,
                Name = "Test Location"
            };
            _progenyContext.LocationsDb.Add(location);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Location, 10);

            // Assert
            Assert.False(result);
            Location? updatedItem = await _progenyContext.LocationsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region TodoItem Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_TodoItem_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                Title = "Test Todo"
            };
            _progenyContext.TodoItemsDb.Add(todoItem);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.TodoItem, 10);

            // Assert
            Assert.False(result);
            TodoItem? updatedItem = await _progenyContext.TodoItemsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region KanbanBoard Conversion Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_KanbanBoard_ConvertsSuccessfully()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = 1,
                ProgenyId = 1,
                AccessLevel = 3,
                Title = "Test Board"
            };
            _progenyContext.KanbanBoardsDb.Add(kanbanBoard);
            await _progenyContext.SaveChangesAsync();

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.KanbanBoard, 10);

            // Assert
            Assert.False(result);
            KanbanBoard? updatedItem = await _progenyContext.KanbanBoardsDb.FindAsync(1);
            Assert.Equal(99, updatedItem!.AccessLevel);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Theory]
        [InlineData(KinaUnaTypes.TimeLineType.Photo)]
        [InlineData(KinaUnaTypes.TimeLineType.Video)]
        [InlineData(KinaUnaTypes.TimeLineType.Calendar)]
        [InlineData(KinaUnaTypes.TimeLineType.Vocabulary)]
        [InlineData(KinaUnaTypes.TimeLineType.Skill)]
        [InlineData(KinaUnaTypes.TimeLineType.Friend)]
        [InlineData(KinaUnaTypes.TimeLineType.Measurement)]
        [InlineData(KinaUnaTypes.TimeLineType.Sleep)]
        [InlineData(KinaUnaTypes.TimeLineType.Note)]
        [InlineData(KinaUnaTypes.TimeLineType.Contact)]
        [InlineData(KinaUnaTypes.TimeLineType.Vaccination)]
        [InlineData(KinaUnaTypes.TimeLineType.Location)]
        [InlineData(KinaUnaTypes.TimeLineType.TodoItem)]
        [InlineData(KinaUnaTypes.TimeLineType.KanbanBoard)]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_AllTimelineTypes_ReturnsFalseWhenNoItems(
            KinaUnaTypes.TimeLineType timelineType)
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            // Act
            bool result = await _service.ConvertItemAccessLevelToItemPermissionsForGroups(timelineType, 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_WithExistingPermission_DoesNotDuplicate()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                PictureLink = "test.jpg"
            };
            _mediaContext.PicturesDb.Add(picture);
            await _mediaContext.SaveChangesAsync();

            UserGroup userGroup = await _progenyContext.UserGroupsDb.FirstAsync(ug => ug.ProgenyId == 1);
            TimelineItemPermission existingPermission = new()
            {
                TimelineType = KinaUnaTypes.TimeLineType.Photo,
                ItemId = 1,
                ProgenyId = 1,
                GroupId = userGroup.UserGroupId,
                PermissionLevel = PermissionLevel.Admin
            };
            _progenyContext.TimelineItemPermissionsDb.Add(existingPermission);
            await _progenyContext.SaveChangesAsync();

            // Act
            await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            List<TimelineItemPermission> permissions = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.TimelineType == KinaUnaTypes.TimeLineType.Photo
                    && p.ItemId == 1
                    && p.GroupId == userGroup.UserGroupId)
                .ToListAsync();

            // Should still only have one permission (the original)
            Assert.Single(permissions);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_MultipleProgenies_ProcessesCorrectly()
        {
            // Arrange
            await CreateTestProgenyWithGroups(1);
            await CreateTestProgenyWithGroups(2);

            Picture picture1 = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                PictureLink = "test1.jpg"
            };
            Picture picture2 = new()
            {
                PictureId = 2,
                ProgenyId = 2,
                AccessLevel = 1,
                PictureLink = "test2.jpg"
            };
            _mediaContext.PicturesDb.AddRange(picture1, picture2);
            await _mediaContext.SaveChangesAsync();

            // Act
            await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert
            List<TimelineItemPermission> permissions1 = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.ProgenyId == 1 && p.ItemId == 1)
                .ToListAsync();
            List<TimelineItemPermission> permissions2 = await _progenyContext.TimelineItemPermissionsDb
                .Where(p => p.ProgenyId == 2 && p.ItemId == 2)
                .ToListAsync();

            Assert.NotEmpty(permissions1);
            Assert.NotEmpty(permissions2);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_WithoutUserGroups_HandlesGracefully()
        {
            // Arrange
            Progeny progeny = new()
            {
                Id = 1,
                Name = "Test Progeny",
                NickName = "Progeny1",
                Admins = "admin@test.com"
            };
            _progenyContext.ProgenyDb.Add(progeny);
            await _progenyContext.SaveChangesAsync();

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                AccessLevel = 0,
                PictureLink = "test.jpg"
            };
            _mediaContext.PicturesDb.Add(picture);
            await _mediaContext.SaveChangesAsync();

            // Act
            await _service.ConvertItemAccessLevelToItemPermissionsForGroups(
                KinaUnaTypes.TimeLineType.Photo, 10);

            // Assert - Should not throw and should still update AccessLevel
            Picture? updatedPicture = await _mediaContext.PicturesDb.FindAsync(1);
            Assert.Equal(99, updatedPicture!.AccessLevel);
        }

        #endregion
    }
}