using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class PicturesControllerTests : IDisposable
    {
        private readonly Mock<IImageStore> _mockImageStore;
        private readonly Mock<IPicturesService> _mockPicturesService;
        private readonly Mock<IVideosService> _mockVideosService;
        private readonly Mock<ICommentsService> _mockCommentsService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly PicturesController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Picture _testPicture;
        private readonly Comment _testComment;
        private readonly CommentThread _testCommentThread;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 5;
        private const int TestPictureId = 100;
        private const int TestCommentThreadNumber = 50;

        public PicturesControllerTests()
        {
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

            _testPicture = new Picture
            {
                PictureId = TestPictureId,
                ProgenyId = TestProgenyId,
                PictureLink = "test-picture.jpg",
                PictureLink600 = "test-picture-600.jpg",
                PictureLink1200 = "test-picture-1200.jpg",
                PictureTime = DateTime.UtcNow,
                Tags = "tag1, tag2, tag3",
                Location = "Test Location",
                CommentThreadNumber = TestCommentThreadNumber,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _testComment = new Comment
            {
                CommentId = 1,
                CommentText = "Test Comment",
                Author = TestUserId,
                CommentThreadNumber = TestCommentThreadNumber
            };

            _testCommentThread = new CommentThread
            {
                Id = TestCommentThreadNumber
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ItemId = TestPictureId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyId = TestProgenyId
            };

            // Setup mocks
            _mockImageStore = new Mock<IImageStore>();
            _mockPicturesService = new Mock<IPicturesService>();
            _mockVideosService = new Mock<IVideosService>();
            _mockCommentsService = new Mock<ICommentsService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Initialize controller
            _controller = new PicturesController(
                _mockImageStore.Object,
                _mockPicturesService.Object,
                _mockVideosService.Object,
                _mockCommentsService.Object,
                _mockProgenyService.Object,
                _mockUserInfoService.Object,
                _mockWebNotificationsService.Object,
                _mockTimelineService.Object,
                _mockAccessManagementService.Object
            );

            // Setup controller context with claims
            SetupControllerContext(TestUserEmail, TestUserId);
        }

        private void SetupControllerContext(string email, string userId)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, email),
                new("sub", userId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        #region Page Tests

        [Fact]
        public async Task Page_Should_Return_Ok_With_PicturePageViewModel()
        {
            // Arrange
            List<Picture> pictures =
            [
                _testPicture,
                new Picture { PictureId = 101, ProgenyId = TestProgenyId, PictureTime = DateTime.UtcNow.AddDays(-1), Tags = "tag1" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesWithTag(TestProgenyId, "", _testUser))
                .ReturnsAsync(pictures);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.Page(pageSize: 16, pageIndex: 1, progenyId: TestProgenyId, tagFilter: "", sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PicturePageViewModel model = Assert.IsType<PicturePageViewModel>(okResult.Value);
            Assert.Equal(2, model.PicturesList.Count);
            Assert.Equal(1, model.TotalPages);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task Page_Should_Handle_PageIndex_Less_Than_One()
        {
            // Arrange
            List<Picture> pictures = [_testPicture];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesWithTag(TestProgenyId, "", _testUser))
                .ReturnsAsync(pictures);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(pageSize: 16, pageIndex: 0, progenyId: TestProgenyId, tagFilter: "", sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PicturePageViewModel model = Assert.IsType<PicturePageViewModel>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task Page_Should_Apply_Tag_Filter()
        {
            // Arrange
            string tagFilter = "tag1";
            List<Picture> pictures = [_testPicture];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesWithTag(TestProgenyId, tagFilter, _testUser))
                .ReturnsAsync(pictures);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(pageSize: 16, pageIndex: 1, progenyId: TestProgenyId, tagFilter: tagFilter, sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PicturePageViewModel model = Assert.IsType<PicturePageViewModel>(okResult.Value);
            Assert.Equal(tagFilter, model.TagFilter);
        }

        [Fact]
        public async Task Page_Should_Sort_By_Oldest_First_When_SortBy_Is_Zero()
        {
            // Arrange
            Picture olderPicture = new() { PictureId = 101, ProgenyId = TestProgenyId, PictureTime = DateTime.UtcNow.AddDays(-10), Tags = "" };
            Picture newerPicture = new() { PictureId = 102, ProgenyId = TestProgenyId, PictureTime = DateTime.UtcNow, Tags = "" };
            List<Picture> pictures = [newerPicture, olderPicture];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesWithTag(TestProgenyId, "", _testUser))
                .ReturnsAsync(pictures);
            _mockCommentsService.Setup(x => x.GetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Page(pageSize: 16, pageIndex: 1, progenyId: TestProgenyId, tagFilter: "", sortBy: 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PicturePageViewModel model = Assert.IsType<PicturePageViewModel>(okResult.Value);
            Assert.Equal(101, model.PicturesList[0].PictureId); // Older picture first
        }

        #endregion

        #region PictureViewModel Tests

        [Fact]
        public async Task PictureViewModel_Should_Return_Ok_With_Model()
        {
            // Arrange
            PictureViewModelRequest request = new()
            {
                PictureId = TestPictureId,
                Progenies = [TestProgenyId],
                SortOrder = 1,
                TagFilter = ""
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([_testPicture]);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.PictureViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PictureViewModel model = Assert.IsType<PictureViewModel>(okResult.Value);
            Assert.Equal(TestPictureId, model.PictureId);
        }

        [Fact]
        public async Task PictureViewModel_Should_Return_NotFound_When_Picture_Is_Null()
        {
            // Arrange
            PictureViewModelRequest request = new() { PictureId = 999 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.PictureViewModel(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PictureViewModel_Should_Use_Picture_ProgenyId_When_Progenies_List_Is_Empty()
        {
            // Arrange
            PictureViewModelRequest request = new()
            {
                PictureId = TestPictureId,
                Progenies = []
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([_testPicture]);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.PictureViewModel(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockPicturesService.Verify(x => x.GetPicturesList(TestProgenyId, _testUser), Times.Once);
        }

        #endregion

        #region TimelinePictureViewModel Tests

        [Fact]
        public async Task TimelinePictureViewModel_Should_Return_Ok_With_Model()
        {
            // Arrange
            PictureViewModelRequest request = new()
            {
                PictureId = TestPictureId,
                Progenies = [TestProgenyId]
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.TimelinePictureViewModel(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PictureViewModel model = Assert.IsType<PictureViewModel>(okResult.Value);
            Assert.Equal(TestPictureId, model.PictureId);
        }

        [Fact]
        public async Task TimelinePictureViewModel_Should_Return_NotFound_When_Picture_Is_Null()
        {
            // Arrange
            PictureViewModelRequest request = new() { PictureId = 999 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.TimelinePictureViewModel(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region PictureElement Tests

        [Fact]
        public async Task PictureElement_Should_Return_Ok_With_Model()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.PictureElement(TestPictureId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PictureViewModel model = Assert.IsType<PictureViewModel>(okResult.Value);
            Assert.Equal(0, model.PictureNumber);
            Assert.Equal(0, model.PictureCount);
        }

        [Fact]
        public async Task PictureElement_Should_Return_NotFound_When_Picture_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.PictureElement(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Pictures_List()
        {
            // Arrange
            List<Picture> pictures = [_testPicture];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Picture> resultPictures = Assert.IsAssignableFrom<List<Picture>>(okResult.Value);
            Assert.Single(resultPictures);
            Assert.NotNull(resultPictures[0].Comments);
        }

        [Fact]
        public async Task Progeny_Should_Return_Placeholder_When_No_Pictures_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Picture> resultPictures = Assert.IsAssignableFrom<List<Picture>>(okResult.Value);
            Assert.Single(resultPictures);
            Assert.Equal(0, resultPictures[0].PictureId);
        }

        #endregion

        #region ProgenyPicturesList Tests

        [Fact]
        public async Task ProgenyPicturesList_Should_Return_Ok_With_Pictures_List()
        {
            // Arrange
            List<Picture> pictures = [_testPicture];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync(pictures);

            // Act
            IActionResult result = await _controller.ProgenyPicturesList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Picture> resultPictures = Assert.IsAssignableFrom<List<Picture>>(okResult.Value);
            Assert.Single(resultPictures);
        }

        [Fact]
        public async Task ProgenyPicturesList_Should_Return_Placeholder_When_No_Pictures_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.ProgenyPicturesList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Picture> resultPictures = Assert.IsAssignableFrom<List<Picture>>(okResult.Value);
            Assert.Single(resultPictures);
            Assert.Equal(0, resultPictures[0].PictureId);
        }

        #endregion

        #region ByLink Tests

        [Fact]
        public async Task ByLink_Should_Return_Ok_With_Picture()
        {
            // Arrange
            string pictureLink = "test-picture.jpg";

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPictureByLink(pictureLink, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);

            // Act
            IActionResult result = await _controller.ByLink(pictureLink);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(TestPictureId, resultPicture.PictureId);
            Assert.NotNull(resultPicture.Comments);
        }

        [Fact]
        public async Task ByLink_Should_Return_Placeholder_When_Picture_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPictureByLink("nonexistent.jpg", _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.ByLink("nonexistent.jpg");

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(0, resultPicture.PictureId);
        }

        #endregion

        #region GetPicture Tests

        [Fact]
        public async Task GetPicture_Should_Return_Ok_With_Picture()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);

            // Act
            IActionResult result = await _controller.GetPicture(TestPictureId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(TestPictureId, resultPicture.PictureId);
        }

        [Fact]
        public async Task GetPicture_Should_Return_Placeholder_When_Picture_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.GetPicture(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(0, resultPicture.PictureId);
        }

        #endregion

        #region File Tests

        [Fact]
        public async Task File_Should_Return_FileContentResult_With_Image()
        {
            // Arrange
            byte[] imageBytes = Encoding.UTF8.GetBytes("fake image content");
            MemoryStream imageStream = new(imageBytes);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockImageStore.Setup(x => x.GetStream(_testPicture.PictureLink))
                .ReturnsAsync(imageStream);

            // Act
            FileContentResult result = await _controller.File(id: TestPictureId, size: 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("image/jpeg", result.ContentType);
        }

        [Fact]
        public async Task File_Should_Return_600_Size_Image()
        {
            // Arrange
            byte[] imageBytes = Encoding.UTF8.GetBytes("fake image content");
            MemoryStream imageStream = new(imageBytes);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockImageStore.Setup(x => x.GetStream(_testPicture.PictureLink600))
                .ReturnsAsync(imageStream);

            // Act
            FileContentResult result = await _controller.File(id: TestPictureId, size: 600);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.GetStream(_testPicture.PictureLink600), Times.Once);
        }

        [Fact]
        public async Task File_Should_Return_1200_Size_Image()
        {
            // Arrange
            byte[] imageBytes = Encoding.UTF8.GetBytes("fake image content");
            MemoryStream imageStream = new(imageBytes);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockImageStore.Setup(x => x.GetStream(_testPicture.PictureLink1200))
                .ReturnsAsync(imageStream);

            // Act
            FileContentResult result = await _controller.File(id: TestPictureId, size: 1200);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.GetStream(_testPicture.PictureLink1200), Times.Once);
        }

        [Fact]
        public async Task File_Should_Return_Placeholder_When_Picture_Is_Null()
        {
            // Arrange
            byte[] placeholderBytes = Encoding.UTF8.GetBytes("placeholder");
            MemoryStream placeholderStream = new(placeholderBytes);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);
            _mockImageStore.Setup(x => x.GetStream(Constants.PlaceholderImageLink))
                .ReturnsAsync(placeholderStream);

            // Act
            FileContentResult result = await _controller.File(id: 999, size: 0);

            // Assert
            Assert.Equal("image/png", result.ContentType);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_With_Added_Picture()
        {
            // Arrange
            Picture newPicture = new()
            {
                ProgenyId = TestProgenyId,
                PictureLink = "new-picture.jpg"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.ProcessPicture(It.IsAny<Picture>()))
                .ReturnsAsync(newPicture);
            _mockCommentsService.Setup(x => x.AddCommentThread())
                .ReturnsAsync(_testCommentThread);
            _mockPicturesService.Setup(x => x.AddPicture(It.IsAny<Picture>(), _testUser))
                .ReturnsAsync(_testPicture);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.SetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Post(newPicture);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(TestPictureId, resultPicture.PictureId);
        }

        [Fact]
        public async Task Post_Should_Return_NotFound_When_Progeny_Is_Null()
        {
            // Arrange
            Picture newPicture = new() { ProgenyId = 999 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult result = await _controller.Post(newPicture);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Add_Permission()
        {
            // Arrange
            Picture newPicture = new() { ProgenyId = TestProgenyId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newPicture);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Skip_Processing_When_PictureLinks_Are_Not_Empty()
        {
            // Arrange
            Picture copyPicture = new()
            {
                ProgenyId = TestProgenyId,
                PictureLink = "copy-picture.jpg",
                PictureLink600 = "copy-picture-600.jpg",
                PictureLink1200 = "copy-picture-1200.jpg"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockCommentsService.Setup(x => x.AddCommentThread())
                .ReturnsAsync(_testCommentThread);
            _mockPicturesService.Setup(x => x.AddPicture(It.IsAny<Picture>(), _testUser))
                .ReturnsAsync(_testPicture);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.SetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([]);

            // Act
            await _controller.Post(copyPicture);

            // Assert
            _mockPicturesService.Verify(x => x.ProcessPicture(It.IsAny<Picture>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddPicture_Returns_Null()
        {
            // Arrange
            Picture newPicture = new() { ProgenyId = TestProgenyId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.ProcessPicture(It.IsAny<Picture>()))
                .ReturnsAsync(newPicture);
            _mockCommentsService.Setup(x => x.AddCommentThread())
                .ReturnsAsync(_testCommentThread);
            _mockPicturesService.Setup(x => x.AddPicture(It.IsAny<Picture>(), _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.Post(newPicture);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Return_Ok_With_Updated_Picture()
        {
            // Arrange
            Picture updatedPicture = new()
            {
                PictureId = TestPictureId,
                ProgenyId = TestProgenyId,
                Location = "Updated Location"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.UpdatePicture(updatedPicture, _testUser))
                .ReturnsAsync(updatedPicture);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestPictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockCommentsService.Setup(x => x.SetCommentsList(It.IsAny<int>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Put(TestPictureId, updatedPicture);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<Picture>(okResult.Value);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Picture_Is_Null()
        {
            // Arrange
            Picture updatedPicture = new() { PictureId = 999 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.Put(999, updatedPicture);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Lacks_Edit_Permission()
        {
            // Arrange
            Picture updatedPicture = new() { PictureId = TestPictureId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestPictureId, updatedPicture);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdatePicture_Returns_Null()
        {
            // Arrange
            Picture updatedPicture = new() { PictureId = TestPictureId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.UpdatePicture(updatedPicture, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.Put(TestPictureId, updatedPicture);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_NoContent_On_Success()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.DeletePicture(_testPicture, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync([_testComment]);
            _mockCommentsService.Setup(x => x.GetCommentThread(TestCommentThreadNumber))
                .ReturnsAsync(_testCommentThread);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestPictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestPictureId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Picture_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(999, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Lacks_Admin_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestPictureId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeletePicture_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.DeletePicture(_testPicture, _testUser))
                .ReturnsAsync((Picture)null!);

            // Act
            IActionResult result = await _controller.Delete(TestPictureId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Delete_Comments_When_They_Exist()
        {
            // Arrange
            List<Comment> comments = [_testComment];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicture(TestPictureId, _testUser))
                .ReturnsAsync(_testPicture);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                    KinaUnaTypes.TimeLineType.Photo, TestPictureId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockPicturesService.Setup(x => x.DeletePicture(_testPicture, _testUser))
                .ReturnsAsync(_testPicture);
            _mockCommentsService.Setup(x => x.GetCommentsList(TestCommentThreadNumber))
                .ReturnsAsync(comments);
            _mockCommentsService.Setup(x => x.GetCommentThread(TestCommentThreadNumber))
                .ReturnsAsync(_testCommentThread);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestPictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestPictureId);

            // Assert
            _mockCommentsService.Verify(x => x.DeleteComment(_testComment), Times.Once);
        }

        #endregion

        #region Random Tests

        [Fact]
        public async Task Random_Should_Return_Ok_With_Picture()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.RandomPicture(TestProgenyId, _testUser))
                .ReturnsAsync(_testPicture);

            // Act
            IActionResult result = await _controller.Random(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Picture resultPicture = Assert.IsType<Picture>(okResult.Value);
            Assert.Equal(TestPictureId, resultPicture.PictureId);
        }

        #endregion

        #region UploadPicture Tests

        private IFormFile CreateMockFormFile(string v, byte[] fileContent)
        {
            MemoryStream stream = new(fileContent);
            return new FormFile(stream, 0, fileContent.Length, "file", v)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }

        [Fact]
        public async Task UploadPicture_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string expectedLink = "uploaded-picture.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockImageStore.Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Pictures, ".jpg"))
                .ReturnsAsync(expectedLink);

            // Act
            IActionResult result = await _controller.UploadPicture(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedLink, okResult.Value);
        }

        

        [Fact]
        public async Task UploadPicture_Should_Return_NoContent_When_SaveImage_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockImageStore.Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Pictures, ".jpg"))
                .ReturnsAsync("");

            // Act
            IActionResult result = await _controller.UploadPicture(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region UploadProgenyPicture Tests

        [Fact]
        public async Task UploadProgenyPicture_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string expectedLink = "progeny-picture.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessProgenyPicture(mockFile))
                .ReturnsAsync(expectedLink);

            // Act
            IActionResult result = await _controller.UploadProgenyPicture(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedLink, okResult.Value);
        }

        [Fact]
        public async Task UploadProgenyPicture_Should_Return_NoContent_When_ProcessProgenyPicture_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessProgenyPicture(mockFile))
                .ReturnsAsync("");

            // Act
            IActionResult result = await _controller.UploadProgenyPicture(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region UploadProfilePicture Tests

        [Fact]
        public async Task UploadProfilePicture_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string expectedLink = "profile-picture.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessProfilePicture(mockFile))
                .ReturnsAsync(expectedLink);

            // Act
            IActionResult result = await _controller.UploadProfilePicture(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedLink, okResult.Value);
        }

        [Fact]
        public async Task UploadProfilePicture_Should_Return_NoContent_When_ProcessProfilePicture_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessProfilePicture(mockFile))
                .ReturnsAsync("");

            // Act
            IActionResult result = await _controller.UploadProfilePicture(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region UploadFriendPicture Tests

        [Fact]
        public async Task UploadFriendPicture_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string expectedLink = "friend-picture.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessFriendPicture(mockFile))
                .ReturnsAsync(expectedLink);

            // Act
            IActionResult result = await _controller.UploadFriendPicture(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedLink, okResult.Value);
        }

        [Fact]
        public async Task UploadFriendPicture_Should_Return_NoContent_When_ProcessFriendPicture_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessFriendPicture(mockFile))
                .ReturnsAsync("");

            // Act
            IActionResult result = await _controller.UploadFriendPicture(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region UploadContactPicture Tests

        [Fact]
        public async Task UploadContactPicture_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string expectedLink = "contact-picture.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessContactPicture(mockFile))
                .ReturnsAsync(expectedLink);

            // Act
            IActionResult result = await _controller.UploadContactPicture(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedLink, okResult.Value);
        }

        [Fact]
        public async Task UploadContactPicture_Should_Return_NoContent_When_ProcessContactPicture_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockPicturesService.Setup(x => x.ProcessContactPicture(mockFile))
                .ReturnsAsync("");

            // Act
            IActionResult result = await _controller.UploadContactPicture(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region UploadNoteImage Tests

        [Fact]
        public async Task UploadNoteImage_Should_Return_Ok_With_PictureLink()
        {
            // Arrange
            string savedFileName = "note-image-guid.jpg";
            string expectedUri = "https://storage.blob.core.windows.net/notes/note-image-guid.jpg";
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockImageStore.Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Notes, ".jpg"))
                .ReturnsAsync(savedFileName);
            _mockImageStore.Setup(x => x.UriFor(savedFileName, BlobContainers.Notes))
                .Returns(expectedUri);

            // Act
            IActionResult result = await _controller.UploadNoteImage(mockFile);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedUri, okResult.Value);
        }

        [Fact]
        public async Task UploadNoteImage_Should_Return_NoContent_When_SaveImage_Returns_Empty()
        {
            // Arrange
            byte[] fileContent = Encoding.UTF8.GetBytes("fake file content");
            IFormFile mockFile = CreateMockFormFile("test.jpg", fileContent);

            _mockImageStore.Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Notes, ".jpg"))
                .ReturnsAsync("");
            _mockImageStore.Setup(x => x.UriFor("", BlobContainers.Notes))
                .Returns("");

            // Act
            IActionResult result = await _controller.UploadNoteImage(mockFile);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region GetProfilePicture Tests

        [Fact]
        public void GetProfilePicture_Should_Return_Ok_With_Uri()
        {
            // Arrange
            string pictureId = "profile-guid.jpg";
            string expectedUri = "https://storage.blob.core.windows.net/profiles/profile-guid.jpg";

            _mockImageStore.Setup(x => x.UriFor(pictureId, "profiles"))
                .Returns(expectedUri);

            // Act
            IActionResult result = _controller.GetProfilePicture(pictureId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedUri, okResult.Value);
        }

        [Fact]
        public void GetProfilePicture_Should_Return_Default_Uri_When_Result_Is_Empty()
        {
            // Arrange
            string pictureId = "nonexistent.jpg";

            _mockImageStore.Setup(x => x.UriFor(pictureId, "profiles"))
                .Returns("");

            // Act
            IActionResult result = _controller.GetProfilePicture(pictureId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(Constants.ProfilePictureUrl, okResult.Value);
        }

        #endregion

        #region GetPictureLocations Tests

        [Fact]
        public async Task GetPictureLocations_Should_Return_Ok_With_Response()
        {
            // Arrange
            PicturesLocationsRequest request = new() { ProgenyId = TestProgenyId };
            PicturesLocationsResponse expectedResponse = new() { LocationsList = [] };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesLocations(request, _testUser))
                .ReturnsAsync(expectedResponse);

            // Act
            IActionResult result = await _controller.GetPictureLocations(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PicturesLocationsResponse>(okResult.Value);
        }

        #endregion

        #region GetPicturesNearLocation Tests

        [Fact]
        public async Task GetPicturesNearLocation_Should_Return_Ok_With_Response()
        {
            // Arrange
            NearByPhotosRequest request = new() { ProgenyId = TestProgenyId };
            NearByPhotosResponse expectedResponse = new() { PicturesList = [] };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesNearLocation(request, _testUser))
                .ReturnsAsync(expectedResponse);

            // Act
            IActionResult result = await _controller.GetPicturesNearLocation(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<NearByPhotosResponse>(okResult.Value);
        }

        #endregion

        #region GetLocationAutoSuggestList Tests

        [Fact]
        public async Task GetLocationAutoSuggestList_Should_Return_Ok_With_Sorted_List()
        {
            // Arrange
            Picture pictureWithLocation = new() { PictureId = 1, Location = "Location A" };
            Video videoWithLocation = new() { VideoId = 1, Location = "Location B" };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockPicturesService.Setup(x => x.GetPicturesList(TestProgenyId, _testUser))
                .ReturnsAsync([pictureWithLocation]);
            _mockVideosService.Setup(x => x.GetVideosList(TestProgenyId, _testUser))
                .ReturnsAsync([videoWithLocation]);

            // Act
            IActionResult result = await _controller.GetLocationAutoSuggestList(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<string> locations = Assert.IsAssignableFrom<List<string>>(okResult.Value);
            Assert.Contains("Location A", locations);
            Assert.Contains("Location B", locations);
        }

        #endregion
    }
}