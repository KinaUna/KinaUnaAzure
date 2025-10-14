using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Moq;
using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Tests.Services
{
    public class TimeLineItemsServiceTests
    {
        private readonly Mock<IMediaHttpClient> _mockMediaHttpClient;
        private readonly Mock<IWordsHttpClient> _mockWordsHttpClient;
        private readonly Mock<IVaccinationsHttpClient> _mockVaccinationsHttpClient;
        private readonly Mock<ISkillsHttpClient> _mockSkillsHttpClient;
        private readonly Mock<INotesHttpClient> _mockNotesHttpClient;
        private readonly Mock<IMeasurementsHttpClient> _mockMeasurementsHttpClient;
        private readonly Mock<ILocationsHttpClient> _mockLocationsHttpClient;
        private readonly Mock<IFriendsHttpClient> _mockFriendsHttpClient;
        private readonly Mock<IContactsHttpClient> _mockContactsHttpClient;
        private readonly Mock<ICalendarsHttpClient> _mockCalendarsHttpClient;
        private readonly Mock<ISleepHttpClient> _mockSleepHttpClient;
        private readonly Mock<ITodoItemsHttpClient> _mockTodoItemsHttpClient;
        private readonly Mock<IProgenyHttpClient> _mockProgenyHttpClient;
        private readonly Mock<IFamiliesHttpClient> _mockFamiliesHttpClient;
        private readonly TimeLineItemsService _service;
        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;

        public TimeLineItemsServiceTests()
        {
            _mockMediaHttpClient = new Mock<IMediaHttpClient>();
            _mockWordsHttpClient = new Mock<IWordsHttpClient>();
            _mockVaccinationsHttpClient = new Mock<IVaccinationsHttpClient>();
            _mockSkillsHttpClient = new Mock<ISkillsHttpClient>();
            _mockNotesHttpClient = new Mock<INotesHttpClient>();
            _mockMeasurementsHttpClient = new Mock<IMeasurementsHttpClient>();
            _mockLocationsHttpClient = new Mock<ILocationsHttpClient>();
            _mockFriendsHttpClient = new Mock<IFriendsHttpClient>();
            _mockContactsHttpClient = new Mock<IContactsHttpClient>();
            _mockCalendarsHttpClient = new Mock<ICalendarsHttpClient>();
            _mockSleepHttpClient = new Mock<ISleepHttpClient>();
            _mockTodoItemsHttpClient = new Mock<ITodoItemsHttpClient>();
            _mockProgenyHttpClient = new Mock<IProgenyHttpClient>();
            _mockFamiliesHttpClient = new Mock<IFamiliesHttpClient>();

            _service = new TimeLineItemsService(
                _mockMediaHttpClient.Object,
                _mockWordsHttpClient.Object,
                _mockVaccinationsHttpClient.Object,
                _mockSkillsHttpClient.Object,
                _mockNotesHttpClient.Object,
                _mockMeasurementsHttpClient.Object,
                _mockLocationsHttpClient.Object,
                _mockFriendsHttpClient.Object,
                _mockContactsHttpClient.Object,
                _mockCalendarsHttpClient.Object,
                _mockSleepHttpClient.Object,
                _mockTodoItemsHttpClient.Object,
                _mockProgenyHttpClient.Object,
                _mockFamiliesHttpClient.Object
            );

            _testUser = new UserInfo
            {
                UserId = "test-user-id",
                UserEmail = "test@example.com",
                Timezone = "Pacific Standard Time"
            };

            _testProgeny = new Progeny
            {
                Id = 1,
                Name = "Test Progeny",
                NickName = "Test"
            };

            _testFamily = new Family
            {
                FamilyId = 1,
                Name = "Test Family"
            };
        }

        #region Photo Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Photo_ViewModel_For_Valid_Photo()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = 1,
                CurrentUser = _testUser,
                TagFilter = string.Empty
            };

            PictureViewModel picture = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                CommentsList = [new Comment(), new Comment()]
            };

            _mockMediaHttpClient.Setup(x => x.GetTimelinePictureViewModel(It.IsAny<PictureViewModelRequest>()))
                .ReturnsAsync(picture);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            PictureViewModel resultPicture = Assert.IsType<PictureViewModel>((dynamic)result.TimeLineItem);
            Assert.Equal("/Pictures/File?id=1&size=600", resultPicture.PictureLink);
            Assert.Equal(2, resultPicture.CommentsCount);
            Assert.Equal(_testProgeny, resultPicture.Progeny);
        }
        
        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Error_Note_For_Invalid_Photo()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = 1,
                CurrentUser = _testUser
            };

            _mockMediaHttpClient.Setup(x => x.GetPictureViewModel(It.IsAny<PictureViewModelRequest>()))
                .ReturnsAsync((PictureViewModel)null!);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Note errorNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Error, content not found.", errorNote.Title);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Error_Note_For_Invalid_ItemId_Parse()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = 0, // Will result in invalid parse
                CurrentUser = _testUser
            };

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Note errorNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Error, content not found.", errorNote.Title);
        }

        #endregion

        #region Video Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Video_ViewModel_For_Valid_Video()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Video,
                ItemId = 1,
                CurrentUser = _testUser
            };

            VideoViewModel video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                CommentsList = [new Comment(), new Comment(), new Comment()]
            };

            _mockMediaHttpClient.Setup(x => x.GetVideoViewModel(It.IsAny<VideoViewModelRequest>()))
                .ReturnsAsync(video);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            VideoViewModel resultVideo = Assert.IsType<VideoViewModel>((dynamic)result.TimeLineItem);
            Assert.Equal(3, resultVideo.CommentsCount);
            Assert.Equal(_testProgeny, resultVideo.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Use_Correct_Timezone_For_Video()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Video,
                ItemId = 1,
                CurrentUser = _testUser
            };

            VideoViewModel video = new() { VideoId = 1, ProgenyId = 1 };

            _mockMediaHttpClient.Setup(x => x.GetVideoViewModel(It.Is<VideoViewModelRequest>(
                req => req.TimeZone == _testUser.Timezone)))
                .ReturnsAsync(video);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            _mockMediaHttpClient.Verify(x => x.GetVideoViewModel(
                It.Is<VideoViewModelRequest>(req => req.TimeZone == _testUser.Timezone)), Times.Once);
        }

        #endregion

        #region Calendar Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Calendar_ViewModel_For_Valid_Event()
        {
            // Arrange
            DateTime startTime = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 1,
                CurrentUser = _testUser
            };

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                StartTime = startTime,
                EndTime = endTime
            };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(1))
                .ReturnsAsync(calendarItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            CalendarItem resultEvent = Assert.IsType<CalendarItem>((dynamic)result.TimeLineItem);
            Assert.NotNull(resultEvent.StartTime);
            Assert.NotNull(resultEvent.EndTime);
            Assert.Equal(_testProgeny, resultEvent.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Convert_Calendar_Times_To_User_Timezone()
        {
            // Arrange
            DateTime utcStartTime = new(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc); // 6 PM UTC
            DateTime utcEndTime = new(2024, 1, 15, 20, 0, 0, DateTimeKind.Utc);   // 8 PM UTC

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 1,
                CurrentUser = _testUser
            };

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                StartTime = utcStartTime,
                EndTime = utcEndTime
            };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(1))
                .ReturnsAsync(calendarItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            CalendarItem resultEvent = Assert.IsType<CalendarItem>((dynamic)result.TimeLineItem);
            Assert.NotEqual(utcStartTime, resultEvent.StartTime);
            Assert.NotEqual(utcEndTime, resultEvent.EndTime);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Handle_Recurring_Calendar_Event()
        {
            // Arrange
            DateTime startTime = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 1,
                CurrentUser = _testUser,
                ItemYear = 2024,
                ItemMonth = 2,
                ItemDay = 15
            };

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                StartTime = startTime,
                EndTime = endTime,
                RecurrenceRuleId = 1
            };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(1))
                .ReturnsAsync(calendarItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            CalendarItem resultEvent = Assert.IsType<CalendarItem>((dynamic)result.TimeLineItem);
            Assert.Equal(2024, resultEvent.StartTime!.Value.Year);
            Assert.Equal(2, resultEvent.StartTime.Value.Month);
            Assert.Equal(15, resultEvent.StartTime.Value.Day);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Load_Family_For_Calendar_Event()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 1,
                CurrentUser = _testUser
            };

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                FamilyId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(1))
                .ReturnsAsync(calendarItem);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(1))
                .ReturnsAsync(_testFamily);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            CalendarItem resultEvent = Assert.IsType<CalendarItem>((dynamic)result.TimeLineItem);
            Assert.Equal(_testFamily, resultEvent.Family);
        }

        #endregion

        #region Vocabulary Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Vocabulary_ViewModel_For_Valid_Word()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Vocabulary,
                ItemId = 1,
                CurrentUser = _testUser
            };

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Date = DateTime.UtcNow
            };

            _mockWordsHttpClient.Setup(x => x.GetWord(1))
                .ReturnsAsync(vocabularyItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            VocabularyItem resultWord = Assert.IsType<VocabularyItem>((dynamic)result.TimeLineItem);
            Assert.Equal("Hello", resultWord.Word);
            Assert.Equal(_testProgeny, resultWord.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Convert_Vocabulary_Date_To_User_Timezone()
        {
            // Arrange
            DateTime utcDate = new(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Vocabulary,
                ItemId = 1,
                CurrentUser = _testUser
            };

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Date = utcDate
            };

            _mockWordsHttpClient.Setup(x => x.GetWord(1))
                .ReturnsAsync(vocabularyItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            VocabularyItem resultWord = Assert.IsType<VocabularyItem>((dynamic)result.TimeLineItem);
            Assert.NotEqual(utcDate, resultWord.Date);
        }

        #endregion

        #region Skill Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Skill_ViewModel_For_Valid_Skill()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Skill,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Skill skill = new()
            {
                SkillId = 1,
                ProgenyId = 1,
                Name = "Walking"
            };

            _mockSkillsHttpClient.Setup(x => x.GetSkill(1))
                .ReturnsAsync(skill);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Skill resultSkill = Assert.IsType<Skill>((dynamic)result.TimeLineItem);
            Assert.Equal("Walking", resultSkill.Name);
            Assert.Equal(_testProgeny, resultSkill.Progeny);
        }

        #endregion

        #region Friend Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Friend_ViewModel_For_Valid_Friend()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Friend,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Best Friend"
            };

            _mockFriendsHttpClient.Setup(x => x.GetFriend(1))
                .ReturnsAsync(friend);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Friend resultFriend = Assert.IsType<Friend>((dynamic)result.TimeLineItem);
            Assert.Equal("Best Friend", resultFriend.Name);
            Assert.Equal(_testProgeny, resultFriend.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Set_PictureLink_For_Friend()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Friend,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Friend friend = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                PictureLink = ""
            };

            _mockFriendsHttpClient.Setup(x => x.GetFriend(1))
                .ReturnsAsync(friend);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Friend resultFriend = Assert.IsType<Friend>((dynamic)result.TimeLineItem);
            Assert.NotNull(resultFriend.PictureLink);
        }

        #endregion

        #region Measurement Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Measurement_ViewModel_For_Valid_Measurement()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Measurement,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Measurement measurement = new()
            {
                MeasurementId = 1,
                ProgenyId = 1,
                Height = 100,
                Weight = 15
            };

            _mockMeasurementsHttpClient.Setup(x => x.GetMeasurement(1))
                .ReturnsAsync(measurement);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Measurement resultMeasurement = Assert.IsType<Measurement>((dynamic)result.TimeLineItem);
            Assert.Equal(100, resultMeasurement.Height);
            Assert.Equal(_testProgeny, resultMeasurement.Progeny);
        }

        #endregion

        #region Sleep Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Sleep_ViewModel_For_Valid_Sleep()
        {
            // Arrange
            DateTime sleepStart = new(2024, 1, 15, 20, 0, 0, DateTimeKind.Utc);
            DateTime sleepEnd = new(2024, 1, 16, 6, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Sleep,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Sleep sleep = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                SleepStart = sleepStart,
                SleepEnd = sleepEnd
            };

            _mockSleepHttpClient.Setup(x => x.GetSleepItem(1))
                .ReturnsAsync(sleep);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Sleep resultSleep = Assert.IsType<Sleep>((dynamic)result.TimeLineItem);
            Assert.NotEqual(sleepStart, resultSleep.SleepStart);
            Assert.NotEqual(sleepEnd, resultSleep.SleepEnd);
            Assert.Equal(_testProgeny, resultSleep.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Calculate_Sleep_Duration()
        {
            // Arrange
            DateTime sleepStart = new(2024, 1, 15, 20, 0, 0, DateTimeKind.Utc);
            DateTime sleepEnd = new(2024, 1, 16, 6, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Sleep,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Sleep sleep = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                SleepStart = sleepStart,
                SleepEnd = sleepEnd
            };

            _mockSleepHttpClient.Setup(x => x.GetSleepItem(1))
                .ReturnsAsync(sleep);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Sleep resultSleep = Assert.IsType<Sleep>((dynamic)result.TimeLineItem);
            Assert.True(resultSleep.SleepDuration.TotalHours > 0);
        }

        #endregion

        #region Note Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Note_ViewModel_For_Valid_Note()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Note,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                Title = "Important Note",
                CreatedDate = DateTime.UtcNow
            };

            _mockNotesHttpClient.Setup(x => x.GetNote(1))
                .ReturnsAsync(note);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Note resultNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Important Note", resultNote.Title);
            Assert.Equal(_testProgeny, resultNote.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Convert_Note_CreatedDate_To_User_Timezone()
        {
            // Arrange
            DateTime utcDate = new(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Note,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Note note = new()
            {
                NoteId = 1,
                ProgenyId = 1,
                CreatedDate = utcDate
            };

            _mockNotesHttpClient.Setup(x => x.GetNote(1))
                .ReturnsAsync(note);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Note resultNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.NotEqual(utcDate, resultNote.CreatedDate);
        }

        #endregion

        #region Contact Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Contact_ViewModel_For_Valid_Contact()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Contact,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                LastName = "Doe",
                DateAdded = DateTime.UtcNow
            };

            _mockContactsHttpClient.Setup(x => x.GetContact(1))
                .ReturnsAsync(contact);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Contact resultContact = Assert.IsType<Contact>((dynamic)result.TimeLineItem);
            Assert.Equal("John", resultContact.FirstName);
            Assert.Equal(_testProgeny, resultContact.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Set_Default_DateAdded_For_Contact_When_Null()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Contact,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                DateAdded = null
            };

            _mockContactsHttpClient.Setup(x => x.GetContact(1))
                .ReturnsAsync(contact);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Contact resultContact = Assert.IsType<Contact>((dynamic)result.TimeLineItem);
            Assert.NotNull(resultContact.DateAdded);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Load_Family_For_Contact()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Contact,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Contact contact = new()
            {
                ContactId = 1,
                FamilyId = 1
            };

            _mockContactsHttpClient.Setup(x => x.GetContact(1))
                .ReturnsAsync(contact);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(1))
                .ReturnsAsync(_testFamily);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Contact resultContact = Assert.IsType<Contact>((dynamic)result.TimeLineItem);
            Assert.Equal(_testFamily, resultContact.Family);
        }

        #endregion

        #region Vaccination Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Vaccination_ViewModel_For_Valid_Vaccination()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Vaccination,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Vaccination vaccination = new()
            {
                VaccinationId = 1,
                ProgenyId = 1,
                VaccinationName = "MMR"
            };

            _mockVaccinationsHttpClient.Setup(x => x.GetVaccination(1))
                .ReturnsAsync(vaccination);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Vaccination resultVaccination = Assert.IsType<Vaccination>((dynamic)result.TimeLineItem);
            Assert.Equal("MMR", resultVaccination.VaccinationName);
            Assert.Equal(_testProgeny, resultVaccination.Progeny);
        }

        #endregion

        #region Location Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Location_ViewModel_For_Valid_Location()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Location,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Location location = new()
            {
                LocationId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Name = "Park"
            };

            _mockLocationsHttpClient.Setup(x => x.GetLocation(1))
                .ReturnsAsync(location);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            Location resultLocation = Assert.IsType<Location>((dynamic)result.TimeLineItem);
            Assert.Equal("Park", resultLocation.Name);
            Assert.Equal(_testProgeny, resultLocation.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Load_Family_For_Location()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Location,
                ItemId = 1,
                CurrentUser = _testUser
            };

            Location location = new()
            {
                LocationId = 1,
                FamilyId = 1
            };

            _mockLocationsHttpClient.Setup(x => x.GetLocation(1))
                .ReturnsAsync(location);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(1))
                .ReturnsAsync(_testFamily);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Location resultLocation = Assert.IsType<Location>((dynamic)result.TimeLineItem);
            Assert.Equal(_testFamily, resultLocation.Family);
        }

        #endregion

        #region TodoItem Timeline Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_TodoItem_ViewModel_For_Valid_TodoItem()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = 1,
                CurrentUser = _testUser
            };

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Task",
                DueDate = DateTime.UtcNow.AddDays(1),
                CompletedDate = null,
                CreatedTime = DateTime.UtcNow
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(1))
                .ReturnsAsync(todoItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            TodoItem resultTodoItem = Assert.IsType<TodoItem>((dynamic)result.TimeLineItem);
            Assert.Equal("Task", resultTodoItem.Title);
            Assert.Equal(_testProgeny, resultTodoItem.Progeny);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Convert_TodoItem_Dates_To_User_Timezone()
        {
            // Arrange
            DateTime utcDueDate = new(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc);
            DateTime utcCreatedTime = new(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = 1,
                CurrentUser = _testUser
            };

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                ProgenyId = 1,
                DueDate = utcDueDate,
                CreatedTime = utcCreatedTime
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(1))
                .ReturnsAsync(todoItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            TodoItem resultTodoItem = Assert.IsType<TodoItem>((dynamic)result.TimeLineItem);
            Assert.NotEqual(utcDueDate, resultTodoItem.DueDate);
            Assert.NotEqual(utcCreatedTime, resultTodoItem.CreatedTime);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Load_Family_For_TodoItem()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.TodoItem,
                ItemId = 1,
                CurrentUser = _testUser
            };

            TodoItem todoItem = new()
            {
                TodoItemId = 1,
                FamilyId = 1,
                CreatedTime = DateTime.UtcNow
            };

            _mockTodoItemsHttpClient.Setup(x => x.GetTodoItem(1))
                .ReturnsAsync(todoItem);
            _mockFamiliesHttpClient.Setup(x => x.GetFamily(1))
                .ReturnsAsync(_testFamily);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            TodoItem resultTodoItem = Assert.IsType<TodoItem>((dynamic)result.TimeLineItem);
            Assert.Equal(_testFamily, resultTodoItem.Family);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Error_Note_When_Photo_Not_Found()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Photo,
                ItemId = 999,
                CurrentUser = _testUser
            };

            PictureViewModel picture = new() { PictureId = 0 };

            _mockMediaHttpClient.Setup(x => x.GetPictureViewModel(It.IsAny<PictureViewModelRequest>()))
                .ReturnsAsync(picture);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Note errorNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Error, content not found.", errorNote.Title);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Error_Note_For_Unknown_Type()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = 999, // Unknown type
                ItemId = 1,
                CurrentUser = _testUser
            };

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Note errorNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Error, content not found.", errorNote.Title);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Return_Error_Note_When_Calendar_Event_Not_Found()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 999,
                CurrentUser = _testUser
            };

            CalendarItem calendarItem = new() { EventId = 0 };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(999))
                .ReturnsAsync(calendarItem);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Note errorNote = Assert.IsType<Note>((dynamic)result.TimeLineItem);
            Assert.Equal("Error, content not found.", errorNote.Title);
        }

        [Fact]
        public async Task GetTimeLineItemPartialViewModel_Should_Handle_Calendar_Event_Without_StartTime()
        {
            // Arrange
            TimeLineItemViewModel model = new()
            {
                TypeId = (int)KinaUnaTypes.TimeLineType.Calendar,
                ItemId = 1,
                CurrentUser = _testUser
            };

            CalendarItem calendarItem = new()
            {
                EventId = 1,
                ProgenyId = 1,
                StartTime = null,
                EndTime = null
            };

            _mockCalendarsHttpClient.Setup(x => x.GetCalendarItem(1))
                .ReturnsAsync(calendarItem);
            _mockProgenyHttpClient.Setup(x => x.GetProgeny(1))
                .ReturnsAsync(_testProgeny);

            // Act
            TimeLineItemPartialViewModel result = await _service.GetTimeLineItemPartialViewModel(model);

            // Assert
            Assert.NotNull(result);
            CalendarItem resultEvent = Assert.IsType<CalendarItem>((dynamic)result.TimeLineItem);
            Assert.Null(resultEvent.StartTime);
        }

        #endregion
    }
}