using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class FriendsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IFriendService> _mockFriendService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly FriendsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Friend _testFriend;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFriendId = 100;

        private readonly HttpClient _httpClient = new();

        public FriendsControllerTests()
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

            _testFriend = new Friend
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Test Friend",
                Description = "Test Description",
                PictureLink = Constants.ProfilePictureUrl,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                Tags = "friend, school",
                Context = "School activities"
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Friend,
                ItemId = TestFriendId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            Mock<IImageStore> mockImageStore = new();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockFriendService = new Mock<IFriendService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Initialize controller
            _controller = new FriendsController(
                mockImageStore.Object,
                _mockUserInfoService.Object,
                _mockFriendService.Object,
                _mockTimelineService.Object,
                _mockProgenyService.Object,
                _mockWebNotificationsService.Object,
                _mockAccessManagementService.Object,
                _httpClient
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
            _progenyDbContext.Dispose();
        }

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Friends_List()
        {
            // Arrange
            List<Friend> friendsList =
            [
                _testFriend,
                new() { FriendId = TestFriendId + 1, ProgenyId = TestProgenyId, Name = "Another Friend" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync(friendsList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Friend> returnedFriends = Assert.IsType<List<Friend>>(okResult.Value);
            Assert.Equal(2, returnedFriends.Count);
            Assert.Contains(returnedFriends, f => f.FriendId == TestFriendId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockFriendService.Verify(x => x.GetFriendsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Friends()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriendsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Friend> returnedFriends = Assert.IsAssignableFrom<List<Friend>>(okResult.Value);
            Assert.Empty(returnedFriends);
        }

        #endregion

        #region GetFriendItem Tests

        [Fact]
        public async Task GetFriendItem_Should_Return_Ok_When_Friend_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(_testFriend);

            // Act
            IActionResult result = await _controller.GetFriendItem(TestFriendId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Friend returnedFriend = Assert.IsType<Friend>(okResult.Value);
            Assert.Equal(TestFriendId, returnedFriend.FriendId);
            Assert.Equal("Test Friend", returnedFriend.Name);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockFriendService.Verify(x => x.GetFriend(TestFriendId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetFriendItem_Should_Return_NotFound_When_Friend_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(999, _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.GetFriendItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockFriendService.Verify(x => x.GetFriend(999, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Friend_And_Timeline_And_Send_Notification()
        {
            // Arrange
            Friend newFriend = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                Description = "New Description",
                PictureLink = Constants.DefaultPictureLink
            };

            Friend createdFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                Description = "New Description",
                PictureLink = Constants.ProfilePictureUrl,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.AddFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(createdFriend);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(createdFriend);

            // Act
            IActionResult result = await _controller.Post(newFriend);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Friend returnedFriend = Assert.IsType<Friend>(okResult.Value);
            Assert.Equal(TestFriendId, returnedFriend.FriendId);
            Assert.Equal(TestUserId, returnedFriend.CreatedBy);

            _mockAccessManagementService.Verify(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add), Times.Once);
            _mockFriendService.Verify(x => x.AddFriend(It.Is<Friend>(f =>
                f.CreatedBy == TestUserId &&
                f.ModifiedBy == TestUserId &&
                f.Author == TestUserId &&
                f.PictureLink == Constants.ProfilePictureUrl), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendFriendNotification(
                It.IsAny<Friend>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Permission()
        {
            // Arrange
            Friend newFriend = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newFriend);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockFriendService.Verify(x => x.AddFriend(It.IsAny<Friend>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_FriendService_Returns_Null()
        {
            // Arrange
            Friend newFriend = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.AddFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.Post(newFriend);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Replace_DefaultPictureLink_With_ProfilePictureUrl()
        {
            // Arrange
            Friend newFriend = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                PictureLink = Constants.DefaultPictureLink
            };

            Friend createdFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                PictureLink = Constants.ProfilePictureUrl,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.AddFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(createdFriend);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(createdFriend);

            // Act
            IActionResult result = await _controller.Post(newFriend);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockFriendService.Verify(x => x.AddFriend(It.Is<Friend>(f =>
                f.PictureLink == Constants.ProfilePictureUrl), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Set_CreatedBy_And_ModifiedBy_To_Current_User()
        {
            // Arrange
            Friend newFriend = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                PictureLink = Constants.ProfilePictureUrl
            };

            Friend createdFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "New Friend",
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.AddFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(createdFriend);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(createdFriend);

            // Act
            IActionResult result = await _controller.Post(newFriend);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockFriendService.Verify(x => x.AddFriend(It.Is<Friend>(f =>
                f.Author == TestUserId &&
                f.CreatedBy == TestUserId &&
                f.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Friend_And_Timeline()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend",
                Description = "Updated Description",
                PictureLink = "updated.jpg"
            };

            Friend existingFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Original Friend",
                FriendSince = new DateTime(2020, 1, 1)
            };

            Friend updatedFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend",
                ModifiedBy = TestUserId,
                Author = TestUserId,
                FriendSince = new DateTime(2020, 1, 1)
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(existingFriend);
            _mockFriendService.Setup(x => x.UpdateFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(updatedFriend);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestFriendId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Friend returnedFriend = Assert.IsType<Friend>(okResult.Value);
            Assert.Equal("Updated Friend", returnedFriend.Name);
            Assert.Equal(TestUserId, returnedFriend.ModifiedBy);

            _mockFriendService.Verify(x => x.GetFriend(TestFriendId, _testUser), Times.Exactly(2));
            _mockFriendService.Verify(x => x.UpdateFriend(It.Is<Friend>(f =>
                f.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Lacks_Permission()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestFriendId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockFriendService.Verify(x => x.UpdateFriend(It.IsAny<Friend>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Friend_Does_Not_Exist()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.Put(TestFriendId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockFriendService.Verify(x => x.UpdateFriend(It.IsAny<Friend>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateFriend_Returns_Null()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend"
            };

            Friend existingFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Original Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(existingFriend);
            _mockFriendService.Setup(x => x.UpdateFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.Put(TestFriendId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Replace_DefaultPictureLink_With_ProfilePictureUrl()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend",
                PictureLink = Constants.DefaultPictureLink
            };

            Friend existingFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Original Friend"
            };

            Friend updatedFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend",
                PictureLink = Constants.ProfilePictureUrl,
                ModifiedBy = TestUserId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(existingFriend);
            _mockFriendService.Setup(x => x.UpdateFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(updatedFriend);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Put(TestFriendId, updateValues);

            // Assert
            Assert.Equal(Constants.ProfilePictureUrl, updateValues.PictureLink);
        }

        [Fact]
        public async Task Put_Should_Not_Update_Timeline_When_It_Does_Not_Exist()
        {
            // Arrange
            Friend updateValues = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend"
            };

            Friend existingFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Original Friend"
            };

            Friend updatedFriend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Updated Friend",
                ModifiedBy = TestUserId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(existingFriend);
            _mockFriendService.Setup(x => x.UpdateFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(updatedFriend);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            IActionResult result = await _controller.Put(TestFriendId, updateValues);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Friend_And_Timeline_And_Send_Notification()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(_testFriend);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFriendService.Setup(x => x.DeleteFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(_testFriend);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockFriendService.Verify(x => x.GetFriend(TestFriendId, _testUser), Times.Once);
            _mockFriendService.Verify(x => x.DeleteFriend(It.Is<Friend>(f =>
                f.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendFriendNotification(
                It.IsAny<Friend>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Lacks_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockFriendService.Verify(x => x.DeleteFriend(It.IsAny<Friend>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Friend_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockFriendService.Verify(x => x.DeleteFriend(It.IsAny<Friend>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteFriend_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(_testFriend);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFriendService.Setup(x => x.DeleteFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Not_Delete_Timeline_When_It_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(_testFriend);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync((TimeLineItem)null!);
            _mockFriendService.Setup(x => x.DeleteFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(_testFriend);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
            _mockWebNotificationsService.Verify(x => x.SendFriendNotification(
                It.IsAny<Friend>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Send_Notification_Only_When_Timeline_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Friend, TestFriendId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(_testFriend);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestFriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFriendService.Setup(x => x.DeleteFriend(It.IsAny<Friend>(), _testUser))
                .ReturnsAsync(_testFriend);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestFriendId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockWebNotificationsService.Verify(x => x.SendFriendNotification(
                It.Is<Friend>(f => f.Author == TestUserId), _testUser, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region DownloadPicture Tests

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_Friend_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync((Friend)null!);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestFriendId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_FriendId_Is_Zero()
        {
            // Arrange
            Friend friend = new()
            {
                FriendId = 0,
                ProgenyId = TestProgenyId,
                Name = "Test Friend"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(friend);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestFriendId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_User_Lacks_Edit_Permission()
        {
            // Arrange
            Friend friend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Test Friend",
                PictureLink = "https://example.com/picture.jpg",
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.View }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(friend);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestFriendId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_PictureLink_Does_Not_Start_With_Http()
        {
            // Arrange
            Friend friend = new()
            {
                FriendId = TestFriendId,
                ProgenyId = TestProgenyId,
                Name = "Test Friend",
                PictureLink = "local-picture.jpg",
                ItemPerMission = new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockFriendService.Setup(x => x.GetFriend(TestFriendId, _testUser))
                .ReturnsAsync(friend);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestFriendId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        #endregion
    }
}