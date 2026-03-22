using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class VideosControllerTests : IDisposable
    {
        private readonly MediaDbContext _mediaDbContext;
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IVideosService> _mockVideosService;
        private readonly Mock<ICommentsService> _mockCommentsService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly VideosController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Video _testVideo;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestVideoId = 100;

        public VideosControllerTests()
        {
            // Setup in-memory DbContext for media
            DbContextOptions<MediaDbContext> mediaOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _mediaDbContext = new MediaDbContext(mediaOptions);

            // Setup in-memory DbContext for progeny
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
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testVideo = new Video
            {
                VideoId = TestVideoId,
                ProgenyId = TestProgenyId,
                VideoLink = "https://example.com/video.mp4",
                ThumbLink = "https://example.com/thumb.jpg",
                VideoTime = DateTime.UtcNow.AddMonths(-1),
                Duration = TimeSpan.FromMinutes(5),
                Author = TestUserId,
                CommentThreadNumber = 1,
                Tags = "family,fun",
                Location = "Home",
                ItemPermission = new TimelineItemPermission() {PermissionLevel = PermissionLevel.View}
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Video,
                ItemId = TestVideoId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockVideosService = new Mock<IVideosService>();
            _mockCommentsService = new Mock<ICommentsService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            _controller = new VideosController(
                _mockVideosService.Object,
                _mockCommentsService.Object,
                _mockProgenyService.Object,
                _mockUserInfoService.Object,
                _mockWebNotificationsService.Object,
                _mockTimelineService.Object,
                _mockAccessManagementService.Object);

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
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Page Tests

        [Fact]
        public async Task Page_Should_Return_Ok_With_VideoPageViewModel()
        {
            // Arrange
            List<Video> videosList = [_testVideo];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Single(model.VideosList);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(1, model.TotalPages);
        }

        [Fact]
        public async Task Page_Should_Set_PageIndex_To_1_When_Less_Than_1()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(8, 0, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task Page_Should_Filter_Videos_By_Tag()
        {
            // Arrange
            List<Video> videosList =
            [
                _testVideo,
                new() { VideoId = 2, ProgenyId = TestProgenyId, Tags = "vacation", VideoTime = DateTime.UtcNow }
            ];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(8, 1, TestProgenyId, "family");

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Single(model.VideosList);
            Assert.Contains("family", model.VideosList[0].Tags);
        }

        [Fact]
        public async Task Page_Should_Sort_Videos_Newest_First_When_SortBy_Is_1()
        {
            // Arrange
            Video olderVideo = new() { VideoId = 1, ProgenyId = TestProgenyId, VideoTime = DateTime.UtcNow.AddDays(-2) };
            Video newerVideo = new() { VideoId = 2, ProgenyId = TestProgenyId, VideoTime = DateTime.UtcNow.AddDays(-1) };
            List<Video> videosList = [olderVideo, newerVideo];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Equal(2, model.VideosList[0].VideoId); // Newer video first
        }

        [Fact]
        public async Task Page_Should_Set_Video_Duration_Components()
        {
            // Arrange
            Video videoWithDuration = new()
            {
                VideoId = 1,
                ProgenyId = TestProgenyId,
                VideoTime = DateTime.UtcNow,
                Duration = new TimeSpan(1, 23, 45) // 1 hour, 23 minutes, 45 seconds
            };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([videoWithDuration]);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Equal("1", model.VideosList[0].DurationHours);
            Assert.Equal("23", model.VideosList[0].DurationMinutes);
            Assert.Equal("45", model.VideosList[0].DurationSeconds);
        }

        [Fact]
        public async Task Page_Should_Load_Comments_For_Videos()
        {
            // Arrange
            List<Comment> comments =
            [
                new() { CommentId = 1, CommentText = "Great video!" }
            ];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([_testVideo]);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync(comments);

            // Act
            IActionResult result = await _controller.Page(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoPageViewModel model = Assert.IsType<VideoPageViewModel>(okResult.Value);
            Assert.Single(model.VideosList[0].Comments);
        }

        #endregion

        #region VideoViewModel Tests

        [Fact]
        public async Task VideoViewModel_Should_Return_Ok_With_ViewModel()
        {
            // Arrange
            VideoViewModelRequest request = new()
            {
                VideoId = TestVideoId,
                Progenies = [TestProgenyId]
            };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([_testVideo]);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.VideoViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoViewModel model = Assert.IsType<VideoViewModel>(okResult.Value);
            Assert.Equal(TestVideoId, model.VideoId);
        }

        [Fact]
        public async Task VideoViewModel_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            VideoViewModelRequest request = new() { VideoId = 999 };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.VideoViewModel(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task VideoViewModel_Should_Set_Next_And_Previous_Videos()
        {
            // Arrange
            Video video1 = new() { VideoId = 1, ProgenyId = TestProgenyId, VideoTime = DateTime.UtcNow.AddDays(-3) };
            Video video2 = new() { VideoId = 2, ProgenyId = TestProgenyId, VideoTime = DateTime.UtcNow.AddDays(-2) };
            Video video3 = new() { VideoId = 3, ProgenyId = TestProgenyId, VideoTime = DateTime.UtcNow.AddDays(-1) };
            List<Video> videosList = [video1, video2, video3];

            VideoViewModelRequest request = new()
            {
                VideoId = 2,
                Progenies = [TestProgenyId]
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(2, _testUser))
                .ReturnsAsync(video2);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.VideoViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoViewModel model = Assert.IsType<VideoViewModel>(okResult.Value);
            Assert.Equal(1, model.PrevVideo);
            Assert.Equal(3, model.NextVideo);
        }

        [Fact]
        public async Task VideoViewModel_Should_Use_Default_Progenies_When_Null()
        {
            // Arrange
            VideoViewModelRequest request = new() { VideoId = TestVideoId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([_testVideo]);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.VideoViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoViewModel model = Assert.IsType<VideoViewModel>(okResult.Value);
            Assert.NotNull(model);
        }

        #endregion

        #region TimelineVideoViewModel Tests

        [Fact]
        public async Task TimelineVideoViewModel_Should_Return_Ok_With_ViewModel()
        {
            // Arrange
            VideoViewModelRequest request = new() { VideoId = TestVideoId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.TimelineVideoViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoViewModel model = Assert.IsType<VideoViewModel>(okResult.Value);
            Assert.Equal(TestVideoId, model.VideoId);
        }

        [Fact]
        public async Task TimelineVideoViewModel_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            VideoViewModelRequest request = new() { VideoId = 999 };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.TimelineVideoViewModel(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region VideoElement Tests

        [Fact]
        public async Task VideoElement_Should_Return_Ok_With_VideoViewModel()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.VideoElement(TestVideoId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VideoViewModel model = Assert.IsType<VideoViewModel>(okResult.Value);
            Assert.Equal(TestVideoId, model.VideoId);
            Assert.Equal(0, model.VideoNumber);
            Assert.Equal(0, model.VideoCount);
        }

        [Fact]
        public async Task VideoElement_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.VideoElement(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Videos_List()
        {
            // Arrange
            List<Video> videosList = [_testVideo];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Video> videos = Assert.IsType<List<Video>>(okResult.Value);
            Assert.Single(videos);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Videos()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Video> videos = Assert.IsType<List<Video>>(okResult.Value);
            Assert.Empty(videos);
        }

        #endregion

        #region ProgenyVideosList Tests

        [Fact]
        public async Task ProgenyVideosList_Should_Return_Ok_With_Videos_List()
        {
            // Arrange
            List<Video> videosList = [_testVideo];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync(videosList);

            // Act
            IActionResult result = await _controller.ProgenyVideosList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Video> videos = Assert.IsType<List<Video>>(okResult.Value);
            Assert.Single(videos);
        }

        [Fact]
        public async Task ProgenyVideosList_Should_Return_List_With_Temp_Video_When_Empty()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.ProgenyVideosList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Video> videos = Assert.IsType<List<Video>>(okResult.Value);
            Assert.Single(videos);
        }

        #endregion

        #region GetVideo Tests

        [Fact]
        public async Task GetVideo_Should_Return_Ok_When_Video_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);

            // Act
            IActionResult result = await _controller.GetVideo(TestVideoId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Video video = Assert.IsType<Video>(okResult.Value);
            Assert.Equal(TestVideoId, video.VideoId);
        }

        [Fact]
        public async Task GetVideo_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.GetVideo(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region ByLink Tests

        [Fact]
        public async Task ByLink_Should_Return_Ok_When_Video_Exists()
        {
            // Arrange
            string videoLink = "test-video.mp4";
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideoByLink(videoLink, TestProgenyId, _testUser))
                .ReturnsAsync(_testVideo);

            // Act
            IActionResult result = await _controller.ByLink(videoLink, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Video video = Assert.IsType<Video>(okResult.Value);
            Assert.Equal(TestVideoId, video.VideoId);
        }

        [Fact]
        public async Task ByLink_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            string videoLink = "nonexistent.mp4";
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideoByLink(videoLink, TestProgenyId, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.ByLink(videoLink, TestProgenyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Video_And_Return_Ok()
        {
            // Arrange
            Video newVideo = new()
            {
                ProgenyId = TestProgenyId,
                VideoLink = "new-video.mp4",
                VideoTime = DateTime.UtcNow
            };

            Video createdVideo = new()
            {
                VideoId = TestVideoId,
                ProgenyId = TestProgenyId,
                VideoLink = "new-video.mp4",
                CommentThreadNumber = 1,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockCommentsService.Setup(x => x.AddCommentThread())
                .ReturnsAsync(new CommentThread { Id = 1 });
            _mockVideosService.Setup(x => x.AddVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(createdVideo);
            _mockVideosService.Setup(x => x.SetVideoInCache(It.IsAny<int>()))
                .ReturnsAsync(createdVideo);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(createdVideo);
            _mockCommentsService.Setup(x => x.SetCommentsList(1))
                .ReturnsAsync([]);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockWebNotificationsService.Setup(x => x.SendVideoNotification(It.IsAny<Video>(), It.IsAny<UserInfo>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Post(newVideo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Video returnedVideo = Assert.IsType<Video>(okResult.Value);
            Assert.Equal(TestVideoId, returnedVideo.VideoId);
            Assert.Equal(TestUserId, returnedVideo.CreatedBy);

            _mockVideosService.Verify(x => x.AddVideo(It.Is<Video>(v =>
                v.CreatedBy == TestUserId && v.CommentThreadNumber == 1), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Permission()
        {
            // Arrange
            Video newVideo = new() { ProgenyId = TestProgenyId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newVideo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Video_And_Return_Ok()
        {
            // Arrange
            Video updatedVideo = new()
            {
                VideoId = TestVideoId,
                ProgenyId = TestProgenyId,
                Tags = "updated,tags",
                VideoTime = DateTime.UtcNow
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.UpdateVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(updatedVideo);
            _mockVideosService.Setup(x => x.SetVideoInCache(TestVideoId))
                .ReturnsAsync(updatedVideo);
            _mockCommentsService.Setup(x => x.SetCommentsList(1))
                .ReturnsAsync([]);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestVideoId, updatedVideo);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Video returnedVideo = Assert.IsType<Video>(okResult.Value);
            Assert.Equal(TestVideoId, returnedVideo.VideoId);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            Video updatedVideo = new() { VideoId = 999 };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.Put(999, updatedVideo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateVideo_Returns_Null()
        {
            // Arrange
            Video updatedVideo = new() { VideoId = TestVideoId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.UpdateVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.Put(TestVideoId, updatedVideo);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Video_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.DeleteVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.RemoveVideoFromCache(TestVideoId, TestProgenyId))
                .Returns(Task.CompletedTask);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync([]);
            _mockCommentsService.Setup(x => x.GetCommentThread(1))
                .ReturnsAsync(new CommentThread { Id = 1 });
            _mockCommentsService.Setup(x => x.DeleteCommentThread(It.IsAny<CommentThread>()))
                .ReturnsAsync(new CommentThread { Id = 1 });
            _mockCommentsService.Setup(x => x.RemoveCommentsList(1))
                .Returns(Task.CompletedTask);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestVideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendVideoNotification(It.IsAny<Video>(), It.IsAny<UserInfo>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestVideoId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockVideosService.Verify(x => x.DeleteVideo(It.IsAny<Video>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Video_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(999, _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteVideo_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.DeleteVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(null as Video);

            // Act
            IActionResult result = await _controller.Delete(TestVideoId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Delete_Comments_And_CommentThread()
        {
            // Arrange
            List<Comment> comments =
            [
                new() { CommentId = 1, CommentThreadNumber = 1 },
                new() { CommentId = 2, CommentThreadNumber = 1 }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVideosService.Setup(x => x.GetVideo(TestVideoId, _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.DeleteVideo(It.IsAny<Video>(), _testUser))
                .ReturnsAsync(_testVideo);
            _mockVideosService.Setup(x => x.RemoveVideoFromCache(TestVideoId, TestProgenyId))
                .Returns(Task.CompletedTask);
            _mockCommentsService.Setup(x => x.GetCommentsList(1))
                .ReturnsAsync(comments);
            _mockCommentsService.Setup(x => x.DeleteComment(It.IsAny<Comment>()))
                .ReturnsAsync(new Comment());
            _mockCommentsService.Setup(x => x.GetCommentThread(1))
                .ReturnsAsync(new CommentThread { Id = 1 });
            _mockCommentsService.Setup(x => x.DeleteCommentThread(It.IsAny<CommentThread>()))
                .ReturnsAsync(new CommentThread());
            _mockCommentsService.Setup(x => x.RemoveCommentsList(1))
                .Returns(Task.CompletedTask);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestVideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendVideoNotification(It.IsAny<Video>(), It.IsAny<UserInfo>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestVideoId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockCommentsService.Verify(x => x.DeleteComment(It.IsAny<Comment>()), Times.Exactly(2));
            _mockCommentsService.Verify(x => x.DeleteCommentThread(It.IsAny<CommentThread>()), Times.Once);
        }

        #endregion
    }
}