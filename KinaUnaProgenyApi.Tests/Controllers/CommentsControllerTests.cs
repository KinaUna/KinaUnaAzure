using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class CommentsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<ICommentsService> _mockCommentsService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly CommentsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Comment _testComment;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestCommentId = 100;
        private const int TestCommentThreadNumber = 1;

        public CommentsControllerTests()
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
                LastName = "User",
                ProfilePicture = "profile.jpg"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2),
                Admins = TestUserEmail
            };

            _testComment = new Comment
            {
                CommentId = TestCommentId,
                CommentThreadNumber = TestCommentThreadNumber,
                Author = TestUserId,
                DisplayName = "Test User",
                CommentText = "Test Comment",
                Created = DateTime.UtcNow,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            // Seed database
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.ProgenyDb.Add(_testProgeny);
            _progenyDbContext.SaveChanges();

            // Setup mocks
            _mockCommentsService = new Mock<ICommentsService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new CommentsController(
                _mockCommentsService.Object,
                _mockProgenyService.Object,
                _mockUserInfoService.Object,
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

        #region GetComment Tests

        [Fact]
        public async Task GetComment_Should_Return_Ok_When_Comment_Exists()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(_testComment);

            // Act
            IActionResult result = await _controller.GetComment(TestCommentId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Comment returnedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Equal(TestCommentId, returnedComment.CommentId);
            Assert.Equal(_testComment.CommentText, returnedComment.CommentText);

            _mockCommentsService.Verify(x => x.GetComment(TestCommentId), Times.Once);
        }

        [Fact]
        public async Task GetComment_Should_Return_NotFound_When_Comment_Does_Not_Exist()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(999))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.GetComment(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.GetComment(999), Times.Once);
        }

        [Fact]
        public async Task GetComment_Should_Handle_Zero_CommentId()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(0))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.GetComment(0);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.GetComment(0), Times.Once);
        }

        [Fact]
        public async Task GetComment_Should_Handle_Negative_CommentId()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(-1))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.GetComment(-1);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.GetComment(-1), Times.Once);
        }

        #endregion

        #region GetCommentsByThread Tests

        [Fact]
        public async Task GetCommentsByThread_Should_Return_Ok_With_Comments_List()
        {
            // Arrange
            List<Comment> comments =
            [
                new() { CommentId = TestCommentId, Author = TestUserId, CommentText = "Comment 1", CommentThreadNumber = TestCommentThreadNumber },
                new() { CommentId = TestCommentId + 1, Author = TestUserId, CommentText = "Comment 2", CommentThreadNumber = TestCommentThreadNumber }
            ];

            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync(comments);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.GetCommentsByThread(TestCommentThreadNumber);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Comment> returnedComments = Assert.IsType<List<Comment>>(okResult.Value);
            Assert.Equal(2,
                returnedComments.Count);
            
            _mockCommentsService.Verify(x => x.GetCommentsList(TestCommentThreadNumber),
                Times.Once);
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetCommentsByThread_Should_Return_NotFound_When_Comments_List_Is_Null()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetCommentsList(999))
                .ReturnsAsync(null as List<Comment>);

            // Act
            IActionResult result = await _controller.GetCommentsByThread(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.GetCommentsList(999), Times.Once);
        }

        [Fact]
        public async Task GetCommentsByThread_Should_Return_Empty_List_When_No_Comments()
        {
            // Arrange
            List<Comment> emptyComments = [];

            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync(emptyComments);

            // Act
            IActionResult result = await _controller.GetCommentsByThread(TestCommentThreadNumber);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Comment> returnedComments = Assert.IsType<List<Comment>>(okResult.Value);
            Assert.Empty(returnedComments);
        }

        [Fact]
        public async Task GetCommentsByThread_Should_Skip_Author_Info_When_UserInfo_Is_Null()
        {
            // Arrange
            List<Comment> comments =
            [
                new() { CommentId = TestCommentId, Author = TestUserId, CommentText = "Comment 1", CommentThreadNumber = TestCommentThreadNumber }
            ];

            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync(comments);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(null as UserInfo);

            // Act
            IActionResult result = await _controller.GetCommentsByThread(TestCommentThreadNumber);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Comment> returnedComments = Assert.IsType<List<Comment>>(okResult.Value);
            Assert.Single(returnedComments);
            Assert.Null(returnedComments[0].AuthorImage);
            Assert.Null(returnedComments[0].DisplayName);
        }

        [Fact]
        public async Task GetCommentsByThread_Should_Set_AuthorImage_And_DisplayName()
        {
            // Arrange
            List<Comment> comments =
            [
                new() { CommentId = TestCommentId, Author = TestUserId, CommentText = "Comment 1", CommentThreadNumber = TestCommentThreadNumber }
            ];

            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync(comments);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult result = await _controller.GetCommentsByThread(TestCommentThreadNumber);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Comment> returnedComments = Assert.IsType<List<Comment>>(okResult.Value);
            Assert.Single(returnedComments);
            Assert.NotNull(returnedComments[0].AuthorImage);
            Assert.Equal("Test M User", returnedComments[0].DisplayName);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Add_Comment_And_Return_Ok()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                DisplayName = "Test User",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment addedComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                DisplayName = "Test User",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockCommentsService.Setup(x => x.AddComment(It.IsAny<Comment>()))
                .ReturnsAsync(addedComment);

            // Act
            IActionResult result = await _controller.Post(newComment);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Comment returnedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Equal(TestCommentId, returnedComment.CommentId);
            Assert.Equal("New Comment", returnedComment.CommentText);

            _mockCommentsService.Verify(x => x.AddComment(newComment), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendCommentNotification(
                It.IsAny<Comment>(), _testUser, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_Author_Does_Not_Match_User()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = "different-user-id",
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Post(newComment);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            _mockCommentsService.Verify(x => x.AddComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_NotFound_When_Progeny_Is_Null()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(null as Progeny);

            // Act
            IActionResult result = await _controller.Post(newComment);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.AddComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_NotFound_When_Progeny_Id_Is_Zero()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Progeny zeroIdProgeny = new()
            {
                Id = 0,
                Name = "Invalid Progeny"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(zeroIdProgeny);

            // Act
            IActionResult result = await _controller.Post(newComment);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.AddComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_NotFound_When_CommentThreadNumber_Is_Zero()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = 0,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Post(newComment);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.AddComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Correct_Title_And_Message()
        {
            // Arrange
            Comment newComment = new()
            {
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                DisplayName = "Test User",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment addedComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "New Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                DisplayName = "Test User",
                Progeny = _testProgeny
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockCommentsService.Setup(x => x.AddComment(It.IsAny<Comment>()))
                .ReturnsAsync(addedComment);

            // Act
            await _controller.Post(newComment);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendCommentNotification(
                It.IsAny<Comment>(),
                _testUser,
                "New comment for TestNick",
                "Test User added a new comment for TestNick"), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Comment_And_Return_Ok()
        {
            // Arrange
            Comment existingComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "Old Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment updateValues = new()
            {
                CommentId = TestCommentId,
                CommentText = "Updated Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment updatedComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "Updated Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(existingComment);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockCommentsService.Setup(x => x.UpdateComment(updateValues))
                .ReturnsAsync(updatedComment);

            // Act
            IActionResult result = await _controller.Put(TestCommentId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Comment returnedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Equal("Updated Comment", returnedComment.CommentText);

            _mockCommentsService.Verify(x => x.GetComment(TestCommentId), Times.Once);
            _mockCommentsService.Verify(x => x.UpdateComment(updateValues), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Comment_Does_Not_Exist()
        {
            // Arrange
            Comment updateValues = new()
            {
                CommentId = 999,
                CommentText = "Updated Comment",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCommentsService.Setup(x => x.GetComment(999))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.Put(999, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.UpdateComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Is_Not_Author()
        {
            // Arrange
            Comment existingComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "Old Comment",
                Author = "different-user-id",
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment updateValues = new()
            {
                CommentId = TestCommentId,
                CommentText = "Updated Comment",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(existingComment);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Put(TestCommentId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            _mockCommentsService.Verify(x => x.UpdateComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Progeny_Is_Null()
        {
            // Arrange
            Comment existingComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "Old Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment updateValues = new()
            {
                CommentId = TestCommentId,
                CommentText = "Updated Comment",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(existingComment);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(null as Progeny);

            // Act
            IActionResult result = await _controller.Put(TestCommentId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.UpdateComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Progeny_Id_Is_Zero()
        {
            // Arrange
            Comment existingComment = new()
            {
                CommentId = TestCommentId,
                CommentText = "Old Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber,
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Comment updateValues = new()
            {
                CommentId = TestCommentId,
                CommentText = "Updated Comment",
                Progeny = new Progeny { Id = TestProgenyId }
            };

            Progeny zeroIdProgeny = new()
            {
                Id = 0,
                Name = "Invalid Progeny"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(existingComment);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(zeroIdProgeny);

            // Act
            IActionResult result = await _controller.Put(TestCommentId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.UpdateComment(It.IsAny<Comment>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Comment_And_Return_NoContent()
        {
            // Arrange
            Comment commentToDelete = new()
            {
                CommentId = TestCommentId,
                CommentText = "Comment to Delete",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber
            };

            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(commentToDelete);
            _mockCommentsService.Setup(x => x.DeleteComment(commentToDelete))
                .ReturnsAsync(commentToDelete);

            // Act
            IActionResult result = await _controller.Delete(TestCommentId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockCommentsService.Verify(x => x.GetComment(TestCommentId), Times.Once);
            _mockCommentsService.Verify(x => x.DeleteComment(commentToDelete), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Comment_Does_Not_Exist()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(999))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.DeleteComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Is_Not_Author()
        {
            // Arrange
            Comment commentToDelete = new()
            {
                CommentId = TestCommentId,
                CommentText = "Comment to Delete",
                Author = "different-user-id",
                CommentThreadNumber = TestCommentThreadNumber
            };

            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(commentToDelete);

            // Act
            IActionResult result = await _controller.Delete(TestCommentId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            _mockCommentsService.Verify(x => x.DeleteComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Call_DeleteComment_With_Correct_Comment()
        {
            // Arrange
            Comment commentToDelete = new()
            {
                CommentId = TestCommentId,
                CommentText = "Comment to Delete",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber
            };

            _mockCommentsService.Setup(x => x.GetComment(TestCommentId))
                .ReturnsAsync(commentToDelete);
            _mockCommentsService.Setup(x => x.DeleteComment(commentToDelete))
                .ReturnsAsync(commentToDelete);

            // Act
            await _controller.Delete(TestCommentId);

            // Assert
            _mockCommentsService.Verify(x => x.DeleteComment(It.Is<Comment>(c =>
                c.CommentId == TestCommentId &&
                c.Author == TestUserId)), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Handle_Zero_CommentId()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(0))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.Delete(0);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.DeleteComment(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Handle_Negative_CommentId()
        {
            // Arrange
            _mockCommentsService.Setup(x => x.GetComment(-1))
                .ReturnsAsync(null as Comment);

            // Act
            IActionResult result = await _controller.Delete(-1);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockCommentsService.Verify(x => x.DeleteComment(It.IsAny<Comment>()), Times.Never);
        }

        #endregion
    }
}