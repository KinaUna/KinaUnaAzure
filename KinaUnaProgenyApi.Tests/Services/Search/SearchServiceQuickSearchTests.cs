using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.Search;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.Search
{
    public class SearchServiceQuickSearchTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;

        public SearchServiceQuickSearchTests()
        {
            _progenyDbContext = GetInMemoryProgenyDbContext(Guid.NewGuid().ToString());
            _mediaDbContext = GetInMemoryMediaDbContext(Guid.NewGuid().ToString());
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockTimelineService = new Mock<ITimelineService>();

            _testUser = CreateTestUserInfo();

            _service = new SearchService(
                _progenyDbContext,
                _mediaDbContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SetupDefaultMocks();
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        private static ProgenyDbContext GetInMemoryProgenyDbContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static MediaDbContext GetInMemoryMediaDbContext(string dbName)
        {
            DbContextOptions<MediaDbContext> options = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new MediaDbContext(options);
        }

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                Id = 1,
                UserId = userId,
                UserEmail = userId,
                FirstName = "Test",
                LastName = "User"
            };
        }

        private void SetupDefaultMocks()
        {
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), It.IsAny<UserInfo>(), It.IsAny<PermissionLevel>()))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(It.IsAny<int>(), It.IsAny<UserInfo>(), It.IsAny<PermissionLevel>()))
                .ReturnsAsync(true);

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserInfo>()))
                .ReturnsAsync([]);
        }

        #region Null and Empty Input Tests

        [Fact]
        public async Task QuickSearch_Should_Return_Empty_Response_When_UserInfo_Is_Null()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, null!);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Results);
            Assert.Equal(request, response.SearchRequest);
        }

        [Fact]
        public async Task QuickSearch_Should_Return_Empty_Response_When_Query_Is_Null()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = null!,
                ProgenyIds = [TestProgenyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Return_Empty_Response_When_Query_Is_Empty()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "",
                ProgenyIds = [TestProgenyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Return_Empty_Response_When_Query_Is_Whitespace()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [TestProgenyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Results);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task QuickSearch_Should_Skip_Progeny_When_User_Has_No_Permission()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Empty(response.Results);
            _mockTimelineService.Verify(x => x.GetTimeLineList(TestProgenyId, 0, _testUser), Times.Never);
        }

        [Fact]
        public async Task QuickSearch_Should_Skip_Family_When_User_Has_No_Permission()
        {
            // Arrange
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchRequest request = new()
            {
                Query = "test",
                FamilyIds = [TestFamilyId]
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Empty(response.Results);
            _mockTimelineService.Verify(x => x.GetTimeLineList(0, TestFamilyId, _testUser), Times.Never);
        }

        [Fact]
        public async Task QuickSearch_Should_Get_Timeline_For_Progeny_When_User_Has_Permission()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId]
            };

            // Act
            await _service.QuickSearch(request, _testUser);

            // Assert
            _mockTimelineService.Verify(x => x.GetTimeLineList(TestProgenyId, 0, _testUser), Times.Once);
        }

        [Fact]
        public async Task QuickSearch_Should_Get_Timeline_For_Family_When_User_Has_Permission()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "test",
                FamilyIds = [TestFamilyId]
            };

            // Act
            await _service.QuickSearch(request, _testUser);

            // Assert
            _mockTimelineService.Verify(x => x.GetTimeLineList(0, TestFamilyId, _testUser), Times.Once);
        }

        #endregion

        #region Calendar Item Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Calendar_Item_By_Title()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "Birthday Party",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
            Assert.Equal("1", response.Results[0].ItemId);
        }

        [Fact]
        public async Task QuickSearch_Should_Find_Calendar_Item_By_Notes()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "",
                Notes = "Remember to bring cake",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "cake",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Find_Calendar_Item_By_Location()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "",
                Notes = "",
                Location = "Central Park",
                Context = ""
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "park",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Contact Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Contact_By_FirstName()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = TestProgenyId,
                FirstName = "John",
                MiddleName = "",
                LastName = "",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = ""
            };
            _progenyDbContext.ContactsDb.Add(contact);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Contact,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Find_Contact_By_Email()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = TestProgenyId,
                FirstName = "",
                MiddleName = "",
                LastName = "",
                DisplayName = "",
                Email1 = "john@example.com",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = ""
            };
            _progenyDbContext.ContactsDb.Add(contact);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Contact,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "example.com",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Friend Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Friend_By_Name()
        {
            // Arrange
            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = TestProgenyId,
                Name = "Alice Smith",
                Description = "",
                Context = "",
                Notes = "",
                Tags = ""
            };
            _progenyDbContext.FriendsDb.Add(friend);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Friend,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "alice",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Note Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Note_By_Title()
        {
            // Arrange
            Note note = new()
            {
                NoteId = 1,
                ProgenyId = TestProgenyId,
                Title = "Important Reminder",
                Content = "",
                Category = ""
            };
            _progenyDbContext.NotesDb.Add(note);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "reminder",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Picture Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Picture_By_Tags()
        {
            // Arrange
            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = TestProgenyId,
                Tags = "vacation, beach, summer",
                Location = ""
            };
            _mediaDbContext.PicturesDb.Add(picture);
            await _mediaDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "beach",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Find_Picture_By_Location()
        {
            // Arrange
            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = TestProgenyId,
                Tags = "",
                Location = "Miami Beach"
            };
            _mediaDbContext.PicturesDb.Add(picture);
            await _mediaDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "miami",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Video Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Video_By_Tags()
        {
            // Arrange
            Video video = new()
            {
                VideoId = 1,
                ProgenyId = TestProgenyId,
                Tags = "first steps, walking",
                Location = ""
            };
            _mediaDbContext.VideoDb.Add(video);
            await _mediaDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Video,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "walking",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Skill Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Skill_By_Name()
        {
            // Arrange
            Skill skill = new()
            {
                SkillId = 1,
                ProgenyId = TestProgenyId,
                Name = "Walking",
                Description = "",
                Category = ""
            };
            _progenyDbContext.SkillsDb.Add(skill);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Skill,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "walking",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Vaccination Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Vaccination_By_Name()
        {
            // Arrange
            Vaccination vaccination = new()
            {
                VaccinationId = 1,
                ProgenyId = TestProgenyId,
                VaccinationName = "MMR Vaccine",
                VaccinationDescription = "",
                Notes = ""
            };
            _progenyDbContext.VaccinationsDb.Add(vaccination);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "mmr",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Vocabulary Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Vocabulary_By_Word()
        {
            // Arrange
            VocabularyItem vocabulary = new()
            {
                WordId = 1,
                ProgenyId = TestProgenyId,
                Word = "Mama",
                Description = "",
                SoundsLike = ""
            };
            _progenyDbContext.VocabularyDb.Add(vocabulary);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "mama",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Location Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_Location_By_City()
        {
            // Arrange
            Location location = new()
            {
                LocationId = 1,
                ProgenyId = TestProgenyId,
                Name = "",
                StreetName = "",
                City = "New York",
                District = "",
                County = "",
                State = "",
                Country = "",
                PostalCode = "",
                Notes = "",
                Tags = ""
            };
            _progenyDbContext.LocationsDb.Add(location);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Location,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "york",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Excluded Item Type Tests

        [Fact]
        public async Task QuickSearch_Should_Not_Include_Measurement_Items()
        {
            // Arrange
            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Measurement,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Empty(response.Results);
        }

        [Fact]
        public async Task QuickSearch_Should_Not_Include_Sleep_Items()
        {
            // Arrange
            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Sleep,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Empty(response.Results);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task QuickSearch_Should_Sort_By_Descending_ProgenyTime_When_Sort_Is_0()
        {
            // Arrange
            CalendarItem calendarItem1 = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "Test Event 1",
                Notes = "",
                Location = "",
                Context = ""
            };
            CalendarItem calendarItem2 = new()
            {
                EventId = 2,
                ProgenyId = TestProgenyId,
                Title = "Test Event 2",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.AddRange(calendarItem1, calendarItem2);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<TimeLineItem> timelineItems =
            [
                new()
                {
                    TimeLineId = 1,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "1",
                    ProgenyTime = new DateTime(2023, 1, 1)
                },
                new()
                {
                    TimeLineId = 2,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "2",
                    ProgenyTime = new DateTime(2024, 1, 1)
                }
            ];

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timelineItems);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10,
                Sort = 0
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
            Assert.Equal("2", response.Results[0].ItemId); // Newer first
            Assert.Equal("1", response.Results[1].ItemId);
        }

        [Fact]
        public async Task QuickSearch_Should_Sort_By_Ascending_ProgenyTime_When_Sort_Is_1()
        {
            // Arrange
            CalendarItem calendarItem1 = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "Test Event 1",
                Notes = "",
                Location = "",
                Context = ""
            };
            CalendarItem calendarItem2 = new()
            {
                EventId = 2,
                ProgenyId = TestProgenyId,
                Title = "Test Event 2",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.AddRange(calendarItem1, calendarItem2);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<TimeLineItem> timelineItems =
            [
                new()
                {
                    TimeLineId = 1,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "1",
                    ProgenyTime = new DateTime(2023, 1, 1)
                },
                new()
                {
                    TimeLineId = 2,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "2",
                    ProgenyTime = new DateTime(2024, 1, 1)
                }
            ];

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timelineItems);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10,
                Sort = 1
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
            Assert.Equal("1", response.Results[0].ItemId); // Older first
            Assert.Equal("2", response.Results[1].ItemId);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task QuickSearch_Should_Return_Correct_Number_Of_Items()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _progenyDbContext.CalendarDb.Add(new CalendarItem
                {
                    EventId = i,
                    ProgenyId = TestProgenyId,
                    Title = $"Test Event {i}",
                    Notes = "",
                    Location = "",
                    Context = ""
                });
            }
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<TimeLineItem> timelineItems = [];
            for (int i = 1; i <= 5; i++)
            {
                timelineItems.Add(new TimeLineItem
                {
                    TimeLineId = i,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = i.ToString(),
                    ProgenyTime = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timelineItems);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 2
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
        }

        [Fact]
        public async Task QuickSearch_Should_Skip_Items_Correctly()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _progenyDbContext.CalendarDb.Add(new CalendarItem
                {
                    EventId = i,
                    ProgenyId = TestProgenyId,
                    Title = $"Test Event {i}",
                    Notes = "",
                    Location = "",
                    Context = ""
                });
            }
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<TimeLineItem> timelineItems = [];
            for (int i = 1; i <= 5; i++)
            {
                timelineItems.Add(new TimeLineItem
                {
                    TimeLineId = i,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = i.ToString(),
                    ProgenyTime = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timelineItems);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 2,
                Skip = 2
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
        }

        [Fact]
        public async Task QuickSearch_Should_Set_RemainingItems_When_More_Results_Available()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _progenyDbContext.CalendarDb.Add(new CalendarItem
                {
                    EventId = i,
                    ProgenyId = TestProgenyId,
                    Title = $"Test Event {i}",
                    Notes = "",
                    Location = "",
                    Context = ""
                });
            }
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<TimeLineItem> timelineItems = [];
            for (int i = 1; i <= 5; i++)
            {
                timelineItems.Add(new TimeLineItem
                {
                    TimeLineId = i,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = i.ToString(),
                    ProgenyTime = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timelineItems);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 2,
                Skip = 0
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(1, response.RemainingItems);
        }

        #endregion

        #region Case Insensitive Search Tests

        [Fact]
        public async Task QuickSearch_Should_Be_Case_Insensitive()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "BIRTHDAY PARTY",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Multiple Progenies and Families Tests

        [Fact]
        public async Task QuickSearch_Should_Search_Multiple_Progenies()
        {
            // Arrange
            int progenyId1 = 1;
            int progenyId2 = 2;

            CalendarItem calendarItem1 = new()
            {
                EventId = 1,
                ProgenyId = progenyId1,
                Title = "Test Event Progeny 1",
                Notes = "",
                Location = "",
                Context = ""
            };
            CalendarItem calendarItem2 = new()
            {
                EventId = 2,
                ProgenyId = progenyId2,
                Title = "Test Event Progeny 2",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.AddRange(calendarItem1, calendarItem2);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(progenyId1, 0, _testUser))
                .ReturnsAsync([new TimeLineItem
                {
                    TimeLineId = 1,
                    ProgenyId = progenyId1,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "1",
                    ProgenyTime = DateTime.UtcNow
                }]);

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(progenyId2, 0, _testUser))
                .ReturnsAsync([new TimeLineItem
                {
                    TimeLineId = 2,
                    ProgenyId = progenyId2,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "2",
                    ProgenyTime = DateTime.UtcNow
                }]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [progenyId1, progenyId2],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
        }

        [Fact]
        public async Task QuickSearch_Should_Search_Both_Progenies_And_Families()
        {
            // Arrange
            CalendarItem calendarItem1 = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Event Progeny",
                Notes = "",
                Location = "",
                Context = ""
            };
            CalendarItem calendarItem2 = new()
            {
                EventId = 2,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Test Event Family",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.AddRange(calendarItem1, calendarItem2);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([new TimeLineItem
                {
                    TimeLineId = 1,
                    ProgenyId = TestProgenyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "1",
                    ProgenyTime = DateTime.UtcNow
                }]);

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(0, TestFamilyId, _testUser))
                .ReturnsAsync([new TimeLineItem
                {
                    TimeLineId = 2,
                    FamilyId = TestFamilyId,
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemId = "2",
                    ProgenyTime = DateTime.UtcNow
                }]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                FamilyIds = [TestFamilyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Equal(2, response.Results.Count);
        }

        #endregion

        #region Null Field Handling Tests

        [Fact]
        public async Task QuickSearch_Should_Handle_Null_Fields_In_Calendar_Item()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = null,
                Notes = null,
                Location = null,
                Context = null
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert - Should not throw and should return empty results since no match
            Assert.Empty(response.Results);
        }

        #endregion

        #region Item Not Found Tests

        [Fact]
        public async Task QuickSearch_Should_Skip_Item_When_Not_Found_In_Database()
        {
            // Arrange - Timeline item exists but corresponding Calendar item doesn't
            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "999", // Non-existent item
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Empty(response.Results);
        }

        #endregion

        #region TodoItem Search Tests

        [Fact]
        public async Task QuickSearch_Should_Find_TodoItem_By_Title()
        {
            // Arrange
            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = TestProgenyId,
                Title = "Buy groceries",
                Description = "",
                Context = ""
            };
            _progenyDbContext.TodoItemsDb.Add(todoItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "groceries",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion

        #region Query Trimming Tests

        [Fact]
        public async Task QuickSearch_Should_Trim_Query_Whitespace()
        {
            // Arrange
            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = TestProgenyId,
                Title = "Birthday Party",
                Notes = "",
                Location = "",
                Context = ""
            };
            _progenyDbContext.CalendarDb.Add(calendarItem);
            await _progenyDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            TimeLineItem timelineItem = new()
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = "1",
                ProgenyTime = DateTime.UtcNow
            };

            _mockTimelineService
                .Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([timelineItem]);

            SearchRequest request = new()
            {
                Query = "   birthday   ",
                ProgenyIds = [TestProgenyId],
                NumberOfItems = 10
            };

            // Act
            SearchResponse<TimeLineItem> response = await _service.QuickSearch(request, _testUser);

            // Assert
            Assert.Single(response.Results);
        }

        #endregion
    }
}