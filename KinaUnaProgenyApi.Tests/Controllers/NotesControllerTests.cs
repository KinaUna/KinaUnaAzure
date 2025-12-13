using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class NotesControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IImageStore> _mockImageStore;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<INoteService> _mockNoteService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly NotesController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Note _testNote;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestNoteId = 100;

        public NotesControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testNote = new Note
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Test Note",
                Content = "Test Content with <img src=\"image.jpg\">",
                Category = "Test Category",
                Owner = TestUserId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Note,
                ItemId = TestNoteId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockImageStore = new Mock<IImageStore>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockNoteService = new Mock<INoteService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            // Default mock setup
            _mockImageStore.Setup(x => x.UpdateBlobLinks(It.IsAny<string>(), It.IsAny<int>()))
                .Returns((string content, int _) => content);

            _controller = new NotesController(
                _mockImageStore.Object,
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockNoteService.Object,
                _mockProgenyService.Object,
                _mockWebNotificationsService.Object);

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

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Notes_List_When_Valid_Request()
        {
            // Arrange
            List<Note> notesList = [_testNote];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(notesList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Note> returnedNotes = Assert.IsType<List<Note>>(okResult.Value);
            Assert.Single(returnedNotes);
            Assert.Equal(TestNoteId, returnedNotes[0].NoteId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockNoteService.Verify(x => x.GetNotesList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Update_Blob_Links_For_All_Notes()
        {
            // Arrange
            List<Note> notesList =
            [
                new Note { NoteId = 1, Content = "Content1", ProgenyId = TestProgenyId },
                new Note { NoteId = 2, Content = "Content2", ProgenyId = TestProgenyId }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(notesList);
            _mockImageStore.Setup(x => x.UpdateBlobLinks("Content1", 1))
                .Returns("UpdatedContent1");
            _mockImageStore.Setup(x => x.UpdateBlobLinks("Content2", 2))
                .Returns("UpdatedContent2");

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Note> returnedNotes = Assert.IsType<List<Note>>(okResult.Value);
            Assert.Equal("UpdatedContent1", returnedNotes[0].Content);
            Assert.Equal("UpdatedContent2", returnedNotes[1].Content);

            _mockImageStore.Verify(x => x.UpdateBlobLinks("Content1", 1), Times.Once);
            _mockImageStore.Verify(x => x.UpdateBlobLinks("Content2", 2), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Notes()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Note> returnedNotes = Assert.IsType<List<Note>>(okResult.Value);
            Assert.Empty(returnedNotes);
        }

        #endregion

        #region GetNoteItem Tests

        [Fact]
        public async Task GetNoteItem_Should_Return_Ok_When_Note_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);

            // Act
            IActionResult result = await _controller.GetNoteItem(TestNoteId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Note returnedNote = Assert.IsType<Note>(okResult.Value);
            Assert.Equal(TestNoteId, returnedNote.NoteId);
            Assert.Equal("Test Note", returnedNote.Title);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockNoteService.Verify(x => x.GetNote(TestNoteId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetNoteItem_Should_Return_NotFound_When_Note_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(999, _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.GetNoteItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockNoteService.Verify(x => x.GetNote(999, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetNoteItem_Should_Update_Blob_Links()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockImageStore.Setup(x => x.UpdateBlobLinks(_testNote.Content, TestNoteId))
                .Returns("Updated Content");

            // Act
            IActionResult result = await _controller.GetNoteItem(TestNoteId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Note returnedNote = Assert.IsType<Note>(okResult.Value);
            Assert.Equal("Updated Content", returnedNote.Content);

            _mockImageStore.Verify(x => x.UpdateBlobLinks(It.IsAny<string>(), TestNoteId), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Note_And_Return_Ok()
        {
            // Arrange
            Note newNote = new()
            {
                ProgenyId = TestProgenyId,
                Title = "New Note",
                Content = "New Content",
                Category = "New Category"
            };

            Note createdNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "New Note",
                Content = "New Content",
                Category = "New Category",
                Owner = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.AddNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(createdNote);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(createdNote);

            // Act
            IActionResult result = await _controller.Post(newNote);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Note returnedNote = Assert.IsType<Note>(okResult.Value);
            Assert.Equal(TestNoteId, returnedNote.NoteId);
            Assert.Equal(TestUserId, returnedNote.Owner);
            Assert.Equal(TestUserId, returnedNote.CreatedBy);
            Assert.Equal(TestUserId, returnedNote.ModifiedBy);

            _mockNoteService.Verify(x => x.AddNote(It.Is<Note>(n =>
                n.Owner == TestUserId &&
                n.CreatedBy == TestUserId &&
                n.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendNoteNotification(
                It.IsAny<Note>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddNote_Returns_Null()
        {
            // Arrange
            Note newNote = new()
            {
                ProgenyId = TestProgenyId,
                Title = "Failed Note"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.AddNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.Post(newNote);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Create_TimelineItem_With_Note_Properties()
        {
            // Arrange
            Note newNote = new()
            {
                ProgenyId = TestProgenyId,
                Title = "New Note",
                Content = "New Content",
                CreatedDate = DateTime.UtcNow
            };

            Note createdNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "New Note",
                Content = "New Content",
                CreatedDate = newNote.CreatedDate,
                Owner = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            TimeLineItem capturedTimelineItem = null!;

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.AddNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(createdNote);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .Callback<TimeLineItem, UserInfo>((tli, _) => capturedTimelineItem = tli)
                .ReturnsAsync(_testTimeLineItem);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(createdNote);

            // Act
            await _controller.Post(newNote);

            // Assert
            Assert.NotNull(capturedTimelineItem);
            Assert.Equal(TestNoteId.ToString(), capturedTimelineItem.ItemId);
            Assert.Equal((int)KinaUnaTypes.TimeLineType.Note, capturedTimelineItem.ItemType);
            Assert.Equal(TestProgenyId, capturedTimelineItem.ProgenyId);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Progeny_NickName()
        {
            // Arrange
            Note newNote = new()
            {
                ProgenyId = TestProgenyId,
                Title = "New Note"
            };

            Note createdNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "New Note",
                Owner = TestUserId
            };

            string capturedNotificationTitle = null!;

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.AddNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(createdNote);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(createdNote);
            _mockWebNotificationsService.Setup(x => x.SendNoteNotification(
                It.IsAny<Note>(), It.IsAny<UserInfo>(), It.IsAny<string>()))
                .Callback<Note, UserInfo, string>((_, _, t) => capturedNotificationTitle = t)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Post(newNote);

            // Assert
            Assert.NotNull(capturedNotificationTitle);
            Assert.Contains(_testProgeny.NickName, capturedNotificationTitle);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Note_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Note updatedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            Note returnedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                Content = "Updated Content",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockNoteService.Setup(x => x.UpdateNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(returnedNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestNoteId, updatedNote);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Note responseNote = Assert.IsType<Note>(okResult.Value);
            Assert.Equal(TestUserId, responseNote.ModifiedBy);

            _mockNoteService.Verify(x => x.GetNote(TestNoteId, _testUser), Times.Exactly(2));
            _mockNoteService.Verify(x => x.UpdateNote(It.Is<Note>(n =>
                n.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Note_Does_Not_Exist()
        {
            // Arrange
            Note updatedNote = new()
            {
                NoteId = 999,
                ProgenyId = TestProgenyId,
                Title = "Non-existent Note"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(999, _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.Put(999, updatedNote);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockNoteService.Verify(x => x.UpdateNote(It.IsAny<Note>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateNote_Returns_Null()
        {
            // Arrange
            Note updatedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Failed Update"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockNoteService.Setup(x => x.UpdateNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.Put(TestNoteId, updatedNote);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Note updatedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title"
            };

            Note returnedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockNoteService.Setup(x => x.UpdateNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(returnedNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestNoteId, updatedNote);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Update_Timeline_Properties_From_Note()
        {
            // Arrange
            Note updatedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            Note returnedNote = new()
            {
                NoteId = TestNoteId,
                ProgenyId = TestProgenyId,
                Title = "Updated Title",
                CreatedDate = updatedNote.CreatedDate,
                ModifiedBy = TestUserId
            };

            TimeLineItem capturedTimelineItem = null!;

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockNoteService.Setup(x => x.UpdateNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(returnedNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .Callback<TimeLineItem, UserInfo>((tli, _) => capturedTimelineItem = tli)
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Put(TestNoteId, updatedNote);

            // Assert
            Assert.NotNull(capturedTimelineItem);
            // Verify that CopyNotePropertiesForUpdate extension method was called
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Note_And_Timeline_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.DeleteNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(_testNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestNoteId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockNoteService.Verify(x => x.GetNote(TestNoteId, _testUser), Times.Once);
            _mockNoteService.Verify(x => x.DeleteNote(It.Is<Note>(n =>
                n.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendNoteNotification(
                It.IsAny<Note>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Note_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(999, _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockNoteService.Verify(x => x.DeleteNote(It.IsAny<Note>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteNote_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.DeleteNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(null as Note);

            // Act
            IActionResult result = await _controller.Delete(TestNoteId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.GetTimeLineItemByItemId(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.DeleteNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(_testNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestNoteId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Send_Notification_With_Deleted_Message()
        {
            // Arrange
            string capturedNotificationTitle = null!;

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNote(TestNoteId, _testUser))
                .ReturnsAsync(_testNote);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockNoteService.Setup(x => x.DeleteNote(It.IsAny<Note>(), _testUser))
                .ReturnsAsync(_testNote);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestNoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockWebNotificationsService.Setup(x => x.SendNoteNotification(
                It.IsAny<Note>(), It.IsAny<UserInfo>(), It.IsAny<string>()))
                .Callback<Note, UserInfo, string>((_, _, t) => capturedNotificationTitle = t)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(TestNoteId);

            // Assert
            Assert.NotNull(capturedNotificationTitle);
            Assert.Contains("deleted", capturedNotificationTitle, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(_testProgeny.NickName, capturedNotificationTitle);
        }

        #endregion

        #region GetNotesListPage Tests

        [Fact]
        public async Task GetNotesListPage_Should_Return_Ok_With_Paged_Notes()
        {
            // Arrange
            List<Note> allNotes =
            [
                new Note { NoteId = 1, ProgenyId = TestProgenyId, Title = "Note 1", CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new Note { NoteId = 2, ProgenyId = TestProgenyId, Title = "Note 2", CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new Note { NoteId = 3, ProgenyId = TestProgenyId, Title = "Note 3", CreatedDate = DateTime.UtcNow.AddDays(-1) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 2, pageIndex: 1, progenyId: TestProgenyId, sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(2, model.NotesList.Count);
            Assert.Equal(2, model.TotalPages);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(1, model.SortBy);
        }

        [Fact]
        public async Task GetNotesListPage_Should_Sort_Newest_First_When_SortBy_Is_1()
        {
            // Arrange
            List<Note> allNotes =
            [
                new Note { NoteId = 1, ProgenyId = TestProgenyId, Title = "Note 1", CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new Note { NoteId = 2, ProgenyId = TestProgenyId, Title = "Note 2", CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new Note { NoteId = 3, ProgenyId = TestProgenyId, Title = "Note 3", CreatedDate = DateTime.UtcNow.AddDays(-1) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 10, pageIndex: 1, progenyId: TestProgenyId, sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(3, model.NotesList[0].NoteId); // Newest first
            Assert.Equal(3, model.NotesList[0].NoteNumber);
            Assert.Equal(1, model.NotesList[2].NoteNumber);
        }

        [Fact]
        public async Task GetNotesListPage_Should_Sort_Oldest_First_When_SortBy_Is_0()
        {
            // Arrange
            List<Note> allNotes =
            [
                new Note { NoteId = 1, ProgenyId = TestProgenyId, Title = "Note 1", CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new Note { NoteId = 2, ProgenyId = TestProgenyId, Title = "Note 2", CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new Note { NoteId = 3, ProgenyId = TestProgenyId, Title = "Note 3", CreatedDate = DateTime.UtcNow.AddDays(-1) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 10, pageIndex: 1, progenyId: TestProgenyId, sortBy: 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(1, model.NotesList[0].NoteId); // Oldest first
            Assert.Equal(1, model.NotesList[0].NoteNumber);
            Assert.Equal(3, model.NotesList[2].NoteNumber);
        }

        [Fact]
        public async Task GetNotesListPage_Should_Set_PageIndex_To_1_When_Less_Than_1()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 10, pageIndex: 0, progenyId: TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task GetNotesListPage_Should_Calculate_Total_Pages_Correctly()
        {
            // Arrange
            List<Note> allNotes = Enumerable.Range(1, 25)
                .Select(i => new Note { NoteId = i, ProgenyId = TestProgenyId, Title = $"Note {i}", CreatedDate = DateTime.UtcNow.AddDays(-i) })
                .ToList();

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 8, pageIndex: 1, progenyId: TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(4, model.TotalPages); // Ceiling(25 / 8) = 4
        }

        [Fact]
        public async Task GetNotesListPage_Should_Update_Blob_Links_For_Displayed_Notes()
        {
            // Arrange
            List<Note> allNotes =
            [
                new Note { NoteId = 1, ProgenyId = TestProgenyId, Content = "Content1", CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new Note { NoteId = 2, ProgenyId = TestProgenyId, Content = "Content2", CreatedDate = DateTime.UtcNow.AddDays(-1) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);
            _mockImageStore.Setup(x => x.UpdateBlobLinks("Content1", 1))
                .Returns("UpdatedContent1");
            _mockImageStore.Setup(x => x.UpdateBlobLinks("Content2", 2))
                .Returns("UpdatedContent2");

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 10, pageIndex: 1, progenyId: TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Contains(model.NotesList, n => n.Content == "UpdatedContent1" || n.Content == "UpdatedContent2");

            _mockImageStore.Verify(x => x.UpdateBlobLinks(It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetNotesListPage_Should_Return_Second_Page_Correctly()
        {
            // Arrange
            List<Note> allNotes = Enumerable.Range(1, 10)
                .Select(i => new Note { NoteId = i, ProgenyId = TestProgenyId, Title = $"Note {i}", CreatedDate = DateTime.UtcNow.AddDays(-100 +i) })
                .ToList();

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(TestProgenyId, _testUser))
                .ReturnsAsync(allNotes);

            // Act
            IActionResult result = await _controller.GetNotesListPage(pageSize: 3, pageIndex: 2, progenyId: TestProgenyId, sortBy: 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(3, model.NotesList.Count);
            Assert.Equal(2, model.PageNumber);
            Assert.Equal(4, model.NotesList[0].NoteId); // 4th, 5th, 6th notes (skipping first 3)
        }

        [Fact]
        public async Task GetNotesListPage_Should_Use_Default_Parameters()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockNoteService.Setup(x => x.GetNotesList(Constants.DefaultChildId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetNotesListPage();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            NotesListPage model = Assert.IsType<NotesListPage>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(1, model.SortBy);

            _mockNoteService.Verify(x => x.GetNotesList(Constants.DefaultChildId, _testUser), Times.Once);
        }

        #endregion
    }
}