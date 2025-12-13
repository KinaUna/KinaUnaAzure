using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class TimelineFilteringServiceTests
    {
        private readonly Mock<IPicturesService> _mockPicturesService;
        private readonly Mock<IVideosService> _mockVideosService;
        private readonly Mock<IFriendService> _mockFriendService;
        private readonly Mock<IContactService> _mockContactService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<ISkillService> _mockSkillService;
        private readonly Mock<INoteService> _mockNoteService;
        private readonly Mock<ICalendarService> _mockCalendarService;
        private readonly Mock<IVocabularyService> _mockVocabularyService;
        private readonly Mock<IVaccinationService> _mockVaccinationService;
        private readonly TimelineFilteringService _service;
        private readonly UserInfo _testUser;

        public TimelineFilteringServiceTests()
        {
            _mockPicturesService = new Mock<IPicturesService>();
            _mockVideosService = new Mock<IVideosService>();
            _mockFriendService = new Mock<IFriendService>();
            _mockContactService = new Mock<IContactService>();
            _mockLocationService = new Mock<ILocationService>();
            _mockSkillService = new Mock<ISkillService>();
            _mockNoteService = new Mock<INoteService>();
            _mockCalendarService = new Mock<ICalendarService>();
            _mockVocabularyService = new Mock<IVocabularyService>();
            _mockVaccinationService = new Mock<IVaccinationService>();

            _service = new TimelineFilteringService(
                _mockPicturesService.Object,
                _mockVideosService.Object,
                _mockFriendService.Object,
                _mockContactService.Object,
                _mockLocationService.Object,
                _mockSkillService.Object,
                _mockNoteService.Object,
                _mockCalendarService.Object,
                _mockVocabularyService.Object,
                _mockVaccinationService.Object
            );

            _testUser = new UserInfo
            {
                UserId = "test-user@example.com",
                UserEmail = "test-user@example.com"
            };
        }

        #region GetTimeLineItemsWithTags Tests

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Return_Original_List_When_Tags_Is_Null()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, null, _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Return_Original_List_When_Tags_Is_Empty()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "", _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Filter_Photos_By_Tag()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "2" }
            ];

            List<Picture> picturesWithTag = [new() { PictureId = 1, ProgenyId = 1, Tags = "birthday,party" }];

            _mockPicturesService
                .Setup(x => x.GetPicturesWithTag(1, "birthday", _testUser))
                .ReturnsAsync(picturesWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "birthday", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Photo, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Filter_Videos_By_Tag()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Video, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Video, ItemId = "2" }
            ];

            List<Video> videosWithTag = [new() { VideoId = 1, ProgenyId = 1, Tags = "vacation,beach" }];

            _mockVideosService
                .Setup(x => x.GetVideosWithTag(1, "beach", _testUser))
                .ReturnsAsync(videosWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "beach", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Video, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Filter_Friends_By_Tag()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "2" }
            ];

            List<Friend> friendsWithTag = [new() { FriendId = 1, ProgenyId = 1, Tags = "school,classmate" }];

            _mockFriendService
                .Setup(x => x.GetFriendsWithTag(1, "school", _testUser))
                .ReturnsAsync(friendsWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "school", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Friend, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Filter_Contacts_By_Tag()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "2" }
            ];

            List<Contact> contactsWithTag = [new() { ContactId = 1, FamilyId = 1, Tags = "doctor,pediatrician" }];

            _mockContactService
                .Setup(x => x.GetContactsWithTag(1, 1, "doctor", _testUser))
                .ReturnsAsync(contactsWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 1, timeLineItems, "doctor", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Contact, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Filter_Locations_By_Tag()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Location, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Location, ItemId = "2" }
            ];

            List<Location> locationsWithTag = [new() { LocationId = 1, FamilyId = 1, Tags = "playground,park" }];

            _mockLocationService
                .Setup(x => x.GetLocationsWithTag(1, 1, "park", _testUser))
                .ReturnsAsync(locationsWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 1, timeLineItems, "park", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Location, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Handle_Multiple_Tags()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "2" },
                new() { TimeLineId = 3, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "3" }
            ];

            List<Picture> picturesWithTag1 = [new() { PictureId = 1, ProgenyId = 1, Tags = "birthday" }];

            List<Picture> picturesWithTag2 = [new() { PictureId = 2, ProgenyId = 1, Tags = "christmas" }];

            _mockPicturesService
                .Setup(x => x.GetPicturesWithTag(1, "birthday", _testUser))
                .ReturnsAsync(picturesWithTag1);

            _mockPicturesService
                .Setup(x => x.GetPicturesWithTag(1, "christmas", _testUser))
                .ReturnsAsync(picturesWithTag2);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "birthday,christmas", _testUser);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, item => item.ItemId == "1");
            Assert.Contains(result, item => item.ItemId == "2");
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Trim_Tag_Whitespace()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            List<Picture> picturesWithTag = [new() { PictureId = 1, ProgenyId = 1, Tags = "birthday" }];

            _mockPicturesService
                .Setup(x => x.GetPicturesWithTag(1, "birthday", _testUser))
                .ReturnsAsync(picturesWithTag);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, " birthday , party ", _testUser);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Skip_Friends_When_ProgenyId_Is_Zero()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(0, 1, timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
            _mockFriendService.Verify(x => x.GetFriendsWithTag(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeLineItemsWithTags_Should_Return_Empty_List_When_No_Matches()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            _mockPicturesService
                .Setup(x => x.GetPicturesWithTag(1, "nonexistent", _testUser))
                .ReturnsAsync([]);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithTags(1, 0, timeLineItems, "nonexistent", _testUser);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetTimeLineItemsWithCategories Tests

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Return_Original_List_When_Categories_Is_Null()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, null, _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Return_Original_List_When_Categories_Is_Empty()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, "", _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Filter_Skills_By_Category()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "2" }
            ];

            List<Skill> skillsWithCategory = [new() { SkillId = 1, ProgenyId = 1, Category = "Physical" }];

            _mockSkillService
                .Setup(x => x.GetSkillsWithCategory(1, "Physical", _testUser))
                .ReturnsAsync(skillsWithCategory);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, "Physical", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Skill, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Filter_Notes_By_Category()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "2" }
            ];

            List<Note> notesWithCategory = [new() { NoteId = 1, ProgenyId = 1, Category = "School" }];

            _mockNoteService
                .Setup(x => x.GetNotesWithCategory(1, "School", _testUser))
                .ReturnsAsync(notesWithCategory);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, "School", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Note, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Handle_Multiple_Categories()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "2" },
                new() { TimeLineId = 3, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "1" }
            ];

            List<Skill> skillsWithCategory1 = [new() { SkillId = 1, ProgenyId = 1, Category = "Physical" }];

            List<Note> notesWithCategory2 = [new() { NoteId = 1, ProgenyId = 1, Category = "School" }];

            _mockSkillService
                .Setup(x => x.GetSkillsWithCategory(1, "Physical", _testUser))
                .ReturnsAsync(skillsWithCategory1);

            _mockNoteService
                .Setup(x => x.GetNotesWithCategory(1, "School", _testUser))
                .ReturnsAsync(notesWithCategory2);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, "Physical,School", _testUser);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, item => item.ItemType == (int)KinaUnaTypes.TimeLineType.Skill && item.ItemId == "1");
            Assert.Contains(result, item => item.ItemType == (int)KinaUnaTypes.TimeLineType.Note && item.ItemId == "1");
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Trim_Category_Whitespace()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            List<Skill> skillsWithCategory = [new() { SkillId = 1, ProgenyId = 1, Category = "Physical" }];

            _mockSkillService
                .Setup(x => x.GetSkillsWithCategory(1, "Physical", _testUser))
                .ReturnsAsync(skillsWithCategory);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, " Physical , Cognitive ", _testUser);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Skip_Items_When_ProgenyId_Is_Zero()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(0, 1, timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
            _mockSkillService.Verify(x => x.GetSkillsWithCategory(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeLineItemsWithCategories_Should_Return_Empty_List_When_No_Matches()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            _mockSkillService
                .Setup(x => x.GetSkillsWithCategory(1, "nonexistent", _testUser))
                .ReturnsAsync([]);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithCategories(1, 0, timeLineItems, "nonexistent", _testUser);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetTimeLineItemsWithContexts Tests

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Return_Original_List_When_Contexts_Is_Null()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, null, _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Return_Original_List_When_Contexts_Is_Empty()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, "", _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Filter_Calendar_By_Context()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "2" }
            ];

            List<CalendarItem> calendarItemsWithContext = [new() { EventId = 1, ProgenyId = 1, Context = "Birthday Party" }];

            _mockCalendarService
                .Setup(x => x.GetCalendarItemsWithContext(1, 0, "Birthday", _testUser))
                .ReturnsAsync(calendarItemsWithContext);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, "Birthday", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Calendar, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Filter_Friends_By_Context()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "2" }
            ];

            List<Friend> friendsWithContext = [new() { FriendId = 1, ProgenyId = 1, Context = "School" }];

            _mockFriendService
                .Setup(x => x.GetFriendsWithContext(1, "School", _testUser))
                .ReturnsAsync(friendsWithContext);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, "School", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Friend, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Filter_Contacts_By_Context()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "2" }
            ];

            List<Contact> contactsWithContext = [new() { ContactId = 1, FamilyId = 1, Context = "Medical" }];

            _mockContactService
                .Setup(x => x.GetContactsWithContext(1, 1, "Medical", _testUser))
                .ReturnsAsync(contactsWithContext);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 1, timeLineItems, "Medical", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Contact, result[0].ItemType);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Handle_Multiple_Contexts()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" }
            ];

            List<CalendarItem> calendarItems = [new() { EventId = 1, ProgenyId = 1, Context = "Birthday" }];

            List<Friend> friends = [new() { FriendId = 1, ProgenyId = 1, Context = "School" }];

            _mockCalendarService
                .Setup(x => x.GetCalendarItemsWithContext(1, 0, "Birthday", _testUser))
                .ReturnsAsync(calendarItems);

            _mockFriendService
                .Setup(x => x.GetFriendsWithContext(1, "School", _testUser))
                .ReturnsAsync(friends);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, "Birthday,School", _testUser);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Trim_Context_Whitespace()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            List<CalendarItem> calendarItems = [new() { EventId = 1, ProgenyId = 1, Context = "Birthday" }];

            _mockCalendarService
                .Setup(x => x.GetCalendarItemsWithContext(1, 0, "Birthday", _testUser))
                .ReturnsAsync(calendarItems);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, " Birthday , School ", _testUser);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Skip_Friends_When_ProgenyId_Is_Zero()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" }];

            _mockCalendarService
                .Setup(x => x.GetCalendarItemsWithContext(0, 1, "test", _testUser))
                .ReturnsAsync([]);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(0, 1, timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
            _mockFriendService.Verify(x => x.GetFriendsWithContext(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeLineItemsWithContexts_Should_Return_Empty_List_When_No_Matches()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            _mockCalendarService
                .Setup(x => x.GetCalendarItemsWithContext(1, 0, "nonexistent", _testUser))
                .ReturnsAsync([]);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithContexts(1, 0, timeLineItems, "nonexistent", _testUser);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetTimeLineItemsWithKeyword Tests

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Return_Original_List_When_Keywords_Is_Null()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, null, _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Return_Original_List_When_Keywords_Is_Empty()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "", _testUser);

            // Assert
            Assert.Equal(timeLineItems, result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Photos_By_Tags_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "birthday,party,celebration"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "birthday", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Photos_By_Location_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Location = "Central Park"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Park", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Videos_By_Tags_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Video, ItemId = "1" }];

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Tags = "vacation,beach"
            };

            _mockVideosService
                .Setup(x => x.GetVideo(1, _testUser))
                .ReturnsAsync(video);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "vacation", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Videos_By_Location_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Video, ItemId = "1" }];

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Location = "Beach Resort"
            };

            _mockVideosService
                .Setup(x => x.GetVideo(1, _testUser))
                .ReturnsAsync(video);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Resort", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Calendar_By_Context_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                Context = "Birthday Party"
            };

            _mockCalendarService
                .Setup(x => x.GetCalendarItem(1, _testUser))
                .ReturnsAsync(calendarItem);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Birthday", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Calendar_By_Location_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Calendar, ItemId = "1" }];

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                Location = "Community Center"
            };

            _mockCalendarService
                .Setup(x => x.GetCalendarItem(1, _testUser))
                .ReturnsAsync(calendarItem);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Center", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Vocabulary_By_Word_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary, ItemId = "1" }];

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello"
            };

            _mockVocabularyService
                .Setup(x => x.GetVocabularyItem(1, _testUser))
                .ReturnsAsync(vocabularyItem);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Hello", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Vocabulary_By_Language_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary, ItemId = "1" }];

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Language = "English"
            };

            _mockVocabularyService
                .Setup(x => x.GetVocabularyItem(1, _testUser))
                .ReturnsAsync(vocabularyItem);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "English", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Skills_By_Name_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            Skill skill = new()
            {
                SkillId = 1,
                ProgenyId = 1,
                Name = "Walking"
            };

            _mockSkillService
                .Setup(x => x.GetSkill(1, _testUser))
                .ReturnsAsync(skill);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Walking", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Skills_By_Category_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Skill, ItemId = "1" }];

            Skill skill = new()
            {
                SkillId = 1,
                ProgenyId = 1,
                Category = "Physical Development"
            };

            _mockSkillService
                .Setup(x => x.GetSkill(1, _testUser))
                .ReturnsAsync(skill);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Physical", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Friends_By_Tags_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" }];

            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                Tags = "school,classmate"
            };

            _mockFriendService
                .Setup(x => x.GetFriend(1, _testUser))
                .ReturnsAsync(friend);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "school", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Friends_By_Context_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Friend, ItemId = "1" }];

            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                Context = "Kindergarten"
            };

            _mockFriendService
                .Setup(x => x.GetFriend(1, _testUser))
                .ReturnsAsync(friend);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Kindergarten", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Skip_Measurement_Items()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Measurement, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Skip_Sleep_Items()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Sleep, ItemId = "1" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Notes_By_Title_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "1" }];

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Important Note"
            };

            _mockNoteService
                .Setup(x => x.GetNote(1, _testUser))
                .ReturnsAsync(note);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Important", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Notes_By_Content_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "1" }];

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Content = "This is a detailed note about school activities"
            };

            _mockNoteService
                .Setup(x => x.GetNote(1, _testUser))
                .ReturnsAsync(note);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "detailed", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Notes_By_Category_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Note, ItemId = "1" }];

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Category = "School Activities"
            };

            _mockNoteService
                .Setup(x => x.GetNote(1, _testUser))
                .ReturnsAsync(note);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "School", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Contacts_By_Tags_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "1" }];

            Contact contact = new()
            {
                ContactId = 1,
                FamilyId = 1,
                Tags = "doctor,pediatrician"
            };

            _mockContactService
                .Setup(x => x.GetContact(1, _testUser))
                .ReturnsAsync(contact);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "doctor", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Contacts_By_Context_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Contact, ItemId = "1" }];

            Contact contact = new()
            {
                ContactId = 1,
                FamilyId = 1,
                Context = "Medical Professional"
            };

            _mockContactService
                .Setup(x => x.GetContact(1, _testUser))
                .ReturnsAsync(contact);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Medical", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Vaccinations_By_Name_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination, ItemId = "1" }];

            Vaccination vaccination = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                VaccinationName = "MMR Vaccine"
            };

            _mockVaccinationService
                .Setup(x => x.GetVaccination(1, _testUser))
                .ReturnsAsync(vaccination);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "MMR", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Locations_By_Tags_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Location, ItemId = "1" }];

            Location location = new()
            {
                LocationId = 1,
                FamilyId = 1,
                Tags = "playground,park"
            };

            _mockLocationService
                .Setup(x => x.GetLocation(1, _testUser))
                .ReturnsAsync(location);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "playground", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Filter_Locations_By_Name_Keyword()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Location, ItemId = "1" }];

            Location location = new()
            {
                LocationId = 1,
                FamilyId = 1,
                Name = "Central Park"
            };

            _mockLocationService
                .Setup(x => x.GetLocation(1, _testUser))
                .ReturnsAsync(location);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "Central", _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].ItemId);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Be_Case_Insensitive()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "Birthday"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "birthday", _testUser);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Handle_Multiple_Keywords()
        {
            // Arrange
            List<TimeLineItem> timeLineItems =
            [
                new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" },
                new() { TimeLineId = 2, ItemType = (int)KinaUnaTypes.TimeLineType.Video, ItemId = "1" }
            ];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "birthday"
            };

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Tags = "vacation"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            _mockVideosService
                .Setup(x => x.GetVideo(1, _testUser))
                .ReturnsAsync(video);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "birthday,vacation", _testUser);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Trim_Keyword_Whitespace()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "birthday"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, " birthday , party ", _testUser);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Skip_Items_With_Invalid_ItemId()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "invalid" }];

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
            _mockPicturesService.Verify(x => x.GetPicture(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Skip_Items_When_Entity_Is_Null()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Not_Add_Duplicate_Items()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "birthday,party",
                Location = "Home"
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act - searching for multiple keywords that all match the same item
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "birthday,party,Home", _testUser);

            // Assert - should only add the item once even though multiple keywords match
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTimeLineItemsWithKeyword_Should_Handle_Null_Properties_In_Entities()
        {
            // Arrange
            List<TimeLineItem> timeLineItems = [new() { TimeLineId = 1, ItemType = (int)KinaUnaTypes.TimeLineType.Photo, ItemId = "1" }];

            Picture picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = null,
                Location = null
            };

            _mockPicturesService
                .Setup(x => x.GetPicture(1, _testUser))
                .ReturnsAsync(picture);

            // Act
            List<TimeLineItem> result = await _service.GetTimeLineItemsWithKeyword(timeLineItems, "test", _testUser);

            // Assert
            Assert.Empty(result);
        }

        #endregion
    }
}