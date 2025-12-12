using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class AutoSuggestsControllerTests
    {
        private readonly Mock<ICalendarService> _mockCalendarService;
        private readonly Mock<IContactService> _mockContactService;
        private readonly Mock<IFriendService> _mockFriendService;
        private readonly Mock<INoteService> _mockNoteService;
        private readonly Mock<ISkillService> _mockSkillService;
        private readonly Mock<IPicturesService> _mockPicturesService;
        private readonly Mock<IVideosService> _mockVideosService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<IVocabularyService> _mockVocabularyService;
        private readonly Mock<ITodosService> _mockTodosService;
        private readonly Mock<IKanbanBoardsService> _mockKanbanBoardsService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly AutoSuggestsController _controller;

        private readonly UserInfo _testUser;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;

        public AutoSuggestsControllerTests()
        {
            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false
            };

            // Setup mocks
            _mockCalendarService = new Mock<ICalendarService>();
            _mockContactService = new Mock<IContactService>();
            _mockFriendService = new Mock<IFriendService>();
            _mockNoteService = new Mock<INoteService>();
            _mockSkillService = new Mock<ISkillService>();
            _mockPicturesService = new Mock<IPicturesService>();
            _mockVideosService = new Mock<IVideosService>();
            _mockLocationService = new Mock<ILocationService>();
            _mockVocabularyService = new Mock<IVocabularyService>();
            _mockTodosService = new Mock<ITodosService>();
            _mockKanbanBoardsService = new Mock<IKanbanBoardsService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize controller
            _controller = new AutoSuggestsController(
                _mockCalendarService.Object,
                _mockContactService.Object,
                _mockFriendService.Object,
                _mockNoteService.Object,
                _mockSkillService.Object,
                _mockPicturesService.Object,
                _mockVideosService.Object,
                _mockLocationService.Object,
                _mockVocabularyService.Object,
                _mockTodosService.Object,
                _mockKanbanBoardsService.Object,
                _mockUserInfoService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, TestUserEmail),
                new(ClaimTypes.NameIdentifier, TestUserId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        #region GetCategoryAutoSuggestList Tests

        [Fact]
        public async Task GetCategoryAutoSuggestList_Should_Return_Ok_With_Empty_List_When_No_Items()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetCategoryAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> categories = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(categories);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockNoteService.Verify(x => x.GetNotesList(TestProgenyId, _testUser), Times.Once);
            _mockSkillService.Verify(x => x.GetSkillsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetCategoryAutoSuggestList_Should_Return_Ok_With_Categories_From_Notes()
        {
            // Arrange
            List<Note> notes =
            [
                new() { NoteId = 1, Category = "Health" },
                new() { NoteId = 2, Category = "Development" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(notes);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetCategoryAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> categories = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, categories.Count);
            Assert.Contains("Health", categories);
            Assert.Contains("Development", categories);
        }

        [Fact]
        public async Task GetCategoryAutoSuggestList_Should_Return_Ok_With_Categories_From_Skills()
        {
            // Arrange
            List<Skill> skills =
            [
                new() { SkillId = 1, Category = "Motor Skills" },
                new() { SkillId = 2, Category = "Language" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(skills);

            // Act
            IActionResult result = await _controller.GetCategoryAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> categories = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, categories.Count);
            Assert.Contains("Motor Skills", categories);
            Assert.Contains("Language", categories);
        }

        [Fact]
        public async Task GetCategoryAutoSuggestList_Should_Return_Sorted_Unique_Categories()
        {
            // Arrange
            List<Note> notes =
            [
                new() { NoteId = 1, Category = "Health,Development" },
                new() { NoteId = 2, Category = "Health" }
            ];
            List<Skill> skills =
            [
                new() { SkillId = 1, Category = "Development" },
                new() { SkillId = 2, Category = "Academic" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(notes);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(skills);

            // Act
            IActionResult result = await _controller.GetCategoryAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> categories = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, categories.Count);
            Assert.Equal("Academic", categories[0]); // Sorted alphabetically
            Assert.Equal("Development", categories[1]);
            Assert.Equal("Health", categories[2]);
        }

        [Fact]
        public async Task GetCategoryAutoSuggestList_Should_Handle_Comma_Separated_Categories()
        {
            // Arrange
            List<Note> notes =
            [
                new() { NoteId = 1, Category = "Health,Behavior,Sleep" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(notes);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetCategoryAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> categories = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, categories.Count);
            Assert.Contains("Health", categories);
            Assert.Contains("Behavior", categories);
            Assert.Contains("Sleep", categories);
        }

        #endregion

        #region GetContextAutoSuggestList Tests

        [Fact]
        public async Task GetContextAutoSuggestList_Should_Return_Ok_With_Empty_List_When_No_Items()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetContextAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> contexts = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(contexts);
        }

        [Fact]
        public async Task GetContextAutoSuggestList_Should_Return_Contexts_From_All_Sources()
        {
            // Arrange
            List<Friend> friends = [new() { FriendId = 1, Context = "School" }];
            List<CalendarItem> calendarItems = [new() { EventId = 1, Context = "Home" }];
            List<Contact> contacts = [new() { ContactId = 1, Context = "Family" }];
            List<TodoItem> todoItems = [new() { TodoItemId = 1, Context = "Work" }];
            List<KanbanBoard> kanbanBoards = [new() { KanbanBoardId = 1, Context = "Project" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync(friends);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(calendarItems);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(contacts);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(todoItems);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(kanbanBoards);

            // Act
            IActionResult result = await _controller.GetContextAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> contexts = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(5, contexts.Count);
            Assert.Contains("School", contexts);
            Assert.Contains("Home", contexts);
            Assert.Contains("Family", contexts);
            Assert.Contains("Work", contexts);
            Assert.Contains("Project", contexts);
        }

        [Fact]
        public async Task GetContextAutoSuggestList_Should_Return_Sorted_Unique_Contexts()
        {
            // Arrange
            List<Friend> friends = [new() { FriendId = 1, Context = "School,Home" }];
            List<CalendarItem> calendarItems = [new() { EventId = 1, Context = "School" }];
            List<Contact> contacts = [new() { ContactId = 1, Context = "Work" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync(friends);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(calendarItems);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(contacts);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetContextAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> contexts = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, contexts.Count);
            Assert.Equal("Home", contexts[0]); // Sorted alphabetically
            Assert.Equal("School", contexts[1]);
            Assert.Equal("Work", contexts[2]);
        }

        #endregion

        #region GetLocationAutoSuggestList Tests

        [Fact]
        public async Task GetLocationAutoSuggestList_Should_Return_Ok_With_Empty_List_When_No_Items()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetLocationAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> locations = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(locations);
        }

        [Fact]
        public async Task GetLocationAutoSuggestList_Should_Return_Locations_From_All_Sources()
        {
            // Arrange
            List<CalendarItem> calendarItems = [new() { EventId = 1, Location = "Library" }];
            List<Location> locations = [new() { LocationId = 1, Name = "Park" }];
            List<TodoItem> todoItems = [new() { TodoItemId = 1, Location = "Home" }];
            List<Picture> pictures = [new() { PictureId = 1, Location = "Beach" }];
            List<Video> videos = [new() { VideoId = 1, Location = "Zoo" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(calendarItems);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(locations);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(todoItems);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videos);

            // Act
            IActionResult result = await _controller.GetLocationAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> locationsList = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(5, locationsList.Count);
            Assert.Contains("Library", locationsList);
            Assert.Contains("Park", locationsList);
            Assert.Contains("Home", locationsList);
            Assert.Contains("Beach", locationsList);
            Assert.Contains("Zoo", locationsList);
        }

        [Fact]
        public async Task GetLocationAutoSuggestList_Should_Return_Sorted_Unique_Locations()
        {
            // Arrange
            List<CalendarItem> calendarItems = [new() { EventId = 1, Location = "Home,School" }];
            List<Location> locations = [new() { LocationId = 1, Name = "School" }];
            List<Picture> pictures = [new() { PictureId = 1, Location = "Park" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(calendarItems);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(locations);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetLocationAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> locationsList = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, locationsList.Count);
            Assert.Equal("Home", locationsList[0]); // Sorted alphabetically
            Assert.Equal("Park", locationsList[1]);
            Assert.Equal("School", locationsList[2]);
        }

        #endregion

        #region GetTagsAutoSuggestList Tests

        [Fact]
        public async Task GetTagsAutoSuggestList_Should_Return_Ok_With_Empty_List_When_No_Items()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetTagsAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> tags = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(tags);
        }

        [Fact]
        public async Task GetTagsAutoSuggestList_Should_Return_Tags_From_All_Sources()
        {
            // Arrange
            List<Location> locations = [new() { LocationId = 1, Tags = "outdoor" }];
            List<Friend> friends = [new() { FriendId = 1, Tags = "childhood" }];
            List<Contact> contacts = [new() { ContactId = 1, Tags = "family" }];
            List<TodoItem> todoItems = [new() { TodoItemId = 1, Tags = "important" }];
            List<KanbanBoard> kanbanBoards = [new() { KanbanBoardId = 1, Tags = "project" }];
            List<Picture> pictures = [new() { PictureId = 1, Tags = "vacation" }];
            List<Video> videos = [new() { VideoId = 1, Tags = "birthday" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(locations);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync(friends);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(contacts);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(todoItems);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(kanbanBoards);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videos);

            // Act
            IActionResult result = await _controller.GetTagsAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> tags = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(7, tags.Count);
            Assert.Contains("outdoor", tags);
            Assert.Contains("childhood", tags);
            Assert.Contains("family", tags);
            Assert.Contains("important", tags);
            Assert.Contains("project", tags);
            Assert.Contains("vacation", tags);
            Assert.Contains("birthday", tags);
        }

        [Fact]
        public async Task GetTagsAutoSuggestList_Should_Return_Sorted_Unique_Tags()
        {
            // Arrange
            List<Location> locations = [new() { LocationId = 1, Tags = "outdoor,fun" }];
            List<Friend> friends = [new() { FriendId = 1, Tags = "outdoor" }];
            List<Picture> pictures = [new() { PictureId = 1, Tags = "vacation" }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockLocationService.Setup(x => x.GetLocationsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync(locations);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync(friends);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockTodosService.Setup(x => x.GetTodosList(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsListForProgenyOrFamily(TestProgenyId, TestFamilyId, _testUser))
                .ReturnsAsync([]);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetTagsAutoSuggestList(TestProgenyId, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> tags = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, tags.Count);
            Assert.Equal("fun", tags[0]); // Sorted alphabetically
            Assert.Equal("outdoor", tags[1]);
            Assert.Equal("vacation", tags[2]);
        }

        #endregion

        #region GetVocabularyLanguagesSuggestList Tests

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Return_Ok_With_Empty_List_When_No_Items()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(languages);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVocabularyService.Verify(x => x.GetVocabularyList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Return_Languages_From_VocabularyItems()
        {
            // Arrange
            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, Language = "English" },
                new() { WordId = 2, Language = "Spanish" },
                new() { WordId = 3, Language = "French" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(vocabularyItems);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, languages.Count);
            Assert.Contains("English", languages);
            Assert.Contains("Spanish", languages);
            Assert.Contains("French", languages);
        }

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Handle_Comma_Separated_Languages()
        {
            // Arrange
            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, Language = "English,Spanish" },
                new() { WordId = 2, Language = "French" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(vocabularyItems);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, languages.Count);
            Assert.Contains("English", languages);
            Assert.Contains("Spanish", languages);
            Assert.Contains("French", languages);
        }

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Return_Sorted_Unique_Languages()
        {
            // Arrange
            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, Language = "Spanish,English" },
                new() { WordId = 2, Language = "English" },
                new() { WordId = 3, Language = "French,Spanish" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(vocabularyItems);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, languages.Count);
            Assert.Equal("English", languages[0]); // Sorted alphabetically
            Assert.Equal("French", languages[1]);
            Assert.Equal("Spanish", languages[2]);
        }

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Trim_Whitespace_From_Languages()
        {
            // Arrange
            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, Language = " English , Spanish " },
                new() { WordId = 2, Language = "  French  " }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(vocabularyItems);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(3, languages.Count);
            Assert.Contains("English", languages);
            Assert.Contains("Spanish", languages);
            Assert.Contains("French", languages);
            Assert.DoesNotContain(" English ", languages);
        }

        [Fact]
        public async Task GetVocabularyLanguagesSuggestList_Should_Skip_Null_Or_Empty_Languages()
        {
            // Arrange
            List<VocabularyItem> vocabularyItems =
            [
                new() { WordId = 1, Language = "English" },
                new() { WordId = 2, Language = null },
                new() { WordId = 3, Language = "" },
                new() { WordId = 4, Language = "Spanish" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(vocabularyItems);

            // Act
            IActionResult result = await _controller.GetVocabularyLanguagesSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> languages = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, languages.Count);
            Assert.Contains("English", languages);
            Assert.Contains("Spanish", languages);
        }

        #endregion
    }
}