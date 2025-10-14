using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class AccessManagementControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly AccessManagementController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;
        private readonly Progeny _testProgeny;
        private readonly Progeny _otherProgeny;
        private readonly Family _testFamily;
        private readonly Family _otherFamily;
        private readonly ProgenyPermission _testProgenyPermission;
        private readonly FamilyPermission _testFamilyPermission;
        private readonly TimelineItemPermission _testTimelineItemPermission;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const int TestProgenyId = 1;
        private const int OtherProgenyId = 2;
        private const int TestFamilyId = 1;
        private const int OtherFamilyId = 2;
        private const int TestPermissionId = 100;
        private const int TestItemId = 500;

        public AccessManagementControllerTests()
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

            _otherUser = new UserInfo
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                ViewChild = OtherProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Other",
                MiddleName = "O",
                LastName = "User"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _otherProgeny = new Progeny
            {
                Id = OtherProgenyId,
                Name = "Other Progeny",
                NickName = "OtherNick",
                BirthDay = DateTime.UtcNow.AddYears(-3)
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family"
            };

            _otherFamily = new Family
            {
                FamilyId = OtherFamilyId,
                Name = "Other Family"
            };

            _testProgenyPermission = new ProgenyPermission
            {
                ProgenyPermissionId = TestPermissionId,
                ProgenyId = TestProgenyId,
                UserId = TestUserId,
                Email = TestUserEmail,
                PermissionLevel = PermissionLevel.Edit
            };

            _testFamilyPermission = new FamilyPermission
            {
                FamilyPermissionId = TestPermissionId,
                FamilyId = TestFamilyId,
                UserId = TestUserId,
                Email = TestUserEmail,
                PermissionLevel = PermissionLevel.Edit
            };

            _testTimelineItemPermission = new TimelineItemPermission
            {
                TimelineType = KinaUnaTypes.TimeLineType.Note,
                ItemId = TestItemId,
                ProgenyId = TestProgenyId,
                UserId = TestUserId,
                PermissionLevel = PermissionLevel.View
            };

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();

            // Initialize controller
            _controller = new AccessManagementController(
                _mockAccessManagementService.Object,
                _mockUserInfoService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object
            );

            // Setup controller context with claims
            SetupControllerContext(TestUserEmail, TestUserId);
        }

        private void SetupControllerContext(string email, string userId)
        {
            List<Claim> claims = new()
            {
                new(ClaimTypes.Email, email),
                new("sub", userId)
            };
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

        #region UserCanAddItemsForProgeny Tests

        [Fact]
        public async Task UserCanAddItemsForProgeny_Should_Return_Ok_With_True_When_User_Has_Add_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            IActionResult? result = await _controller.UserCanAddItemsForProgeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);
        }

        [Fact]
        public async Task UserCanAddItemsForProgeny_Should_Return_Ok_With_False_When_User_Lacks_Add_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult? result = await _controller.UserCanAddItemsForProgeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.False((bool)okResult.Value!);
        }

        [Fact]
        public async Task UserCanAddItemsForProgeny_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("", "");
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            IActionResult? result = await _controller.UserCanAddItemsForProgeny(TestProgenyId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region UserCanAddItemsForFamily Tests

        [Fact]
        public async Task UserCanAddItemsForFamily_Should_Return_Ok_With_True_When_User_Has_Add_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            IActionResult? result = await _controller.UserCanAddItemsForFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);
        }

        [Fact]
        public async Task UserCanAddItemsForFamily_Should_Return_Ok_With_False_When_User_Lacks_Add_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult? result = await _controller.UserCanAddItemsForFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.False((bool)okResult.Value!);
        }

        #endregion

        #region ProgeniesUserCanAccessList Tests

        [Fact]
        public async Task ProgeniesUserCanAccessList_Should_Return_Ok_With_Progenies_List()
        {
            // Arrange
            List<int> progenyIds = new() { TestProgenyId, OtherProgenyId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(progenyIds);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockProgenyService.Setup(x => x.GetProgeny(OtherProgenyId, _testUser))
                .ReturnsAsync(_otherProgeny);

            // Act
            IActionResult? result = await _controller.ProgeniesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Progeny> progenies = Assert.IsAssignableFrom<List<Progeny>>(okResult.Value);
            Assert.Equal(2, progenies.Count);
            Assert.Contains(progenies, p => p.Id == TestProgenyId);
            Assert.Contains(progenies, p => p.Id == OtherProgenyId);
        }

        [Fact]
        public async Task ProgeniesUserCanAccessList_Should_Return_Empty_List_When_User_Has_No_Access()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(new List<int>());

            // Act
            IActionResult? result = await _controller.ProgeniesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Progeny> progenies = Assert.IsAssignableFrom<List<Progeny>>(okResult.Value);
            Assert.Empty(progenies);
        }

        [Fact]
        public async Task ProgeniesUserCanAccessList_Should_Skip_Null_Progenies()
        {
            // Arrange
            List<int> progenyIds = new() { TestProgenyId, OtherProgenyId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.ProgeniesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(progenyIds);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockProgenyService.Setup(x => x.GetProgeny(OtherProgenyId, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            IActionResult? result = await _controller.ProgeniesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Progeny> progenies = Assert.IsAssignableFrom<List<Progeny>>(okResult.Value);
            Assert.Single(progenies);
            Assert.Equal(TestProgenyId, progenies[0].Id);
        }

        #endregion

        #region FamiliesUserCanAccessList Tests

        [Fact]
        public async Task FamiliesUserCanAccessList_Should_Return_Ok_With_Families_List()
        {
            // Arrange
            List<int> familyIds = new() { TestFamilyId, OtherFamilyId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(familyIds);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockFamiliesService.Setup(x => x.GetFamilyById(OtherFamilyId, _testUser))
                .ReturnsAsync(_otherFamily);

            // Act
            IActionResult? result = await _controller.FamiliesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> families = Assert.IsAssignableFrom<List<Family>>(okResult.Value);
            Assert.Equal(2, families.Count);
            Assert.Contains(families, f => f.FamilyId == TestFamilyId);
            Assert.Contains(families, f => f.FamilyId == OtherFamilyId);
        }

        [Fact]
        public async Task FamiliesUserCanAccessList_Should_Return_Empty_List_When_User_Has_No_Access()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(new List<int>());

            // Act
            IActionResult? result = await _controller.FamiliesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> families = Assert.IsAssignableFrom<List<Family>>(okResult.Value);
            Assert.Empty(families);
        }

        [Fact]
        public async Task FamiliesUserCanAccessList_Should_Skip_Null_Families()
        {
            // Arrange
            List<int> familyIds = new() { TestFamilyId, OtherFamilyId };
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.FamiliesUserCanAccess(_testUser, PermissionLevel.View))
                .ReturnsAsync(familyIds);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockFamiliesService.Setup(x => x.GetFamilyById(OtherFamilyId, _testUser))
                .ReturnsAsync((Family)null!);

            // Act
            IActionResult? result = await _controller.FamiliesUserCanAccessList((int)PermissionLevel.View);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> families = Assert.IsAssignableFrom<List<Family>>(okResult.Value);
            Assert.Single(families);
            Assert.Equal(TestFamilyId, families[0].FamilyId);
        }

        #endregion

        #region GetItemPermissionForUser Tests

        [Fact]
        public async Task GetItemPermissionForUser_Should_Return_Ok_With_Permission_Level()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Note, TestItemId, TestProgenyId, 0, _testUser, null))
                .ReturnsAsync(_testTimelineItemPermission);

            // Act
            IActionResult? result = await _controller.GetItemPermissionForUser(
                (int)KinaUnaTypes.TimeLineType.Note, TestItemId, TestProgenyId, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(PermissionLevel.View, (PermissionLevel)okResult.Value!);
        }

        [Fact]
        public async Task GetItemPermissionForUser_Should_Return_Permission_For_Family_Item()
        {
            // Arrange
            TimelineItemPermission familyItemPermission = new()
            {
                TimelineType = KinaUnaTypes.TimeLineType.Note,
                ItemId = TestItemId,
                FamilyId = TestFamilyId,
                UserId = TestUserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetItemPermissionForUser(
                KinaUnaTypes.TimeLineType.Note, TestItemId, 0, TestFamilyId, _testUser, null))
                .ReturnsAsync(familyItemPermission);

            // Act
            IActionResult? result = await _controller.GetItemPermissionForUser(
                (int)KinaUnaTypes.TimeLineType.Note, TestItemId, 0, TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(PermissionLevel.Edit, (PermissionLevel)okResult.Value!);
        }

        #endregion

        #region GetTimelineItemPermissionsList Tests

        [Fact]
        public async Task GetTimelineItemPermissionsList_Should_Return_Ok_With_Permissions_List()
        {
            // Arrange
            List<TimelineItemPermission> permissions = new()
            {
                _testTimelineItemPermission,
                new TimelineItemPermission
                {
                    TimelineType = KinaUnaTypes.TimeLineType.Note,
                    ItemId = TestItemId,
                    ProgenyId = TestProgenyId,
                    UserId = OtherUserId,
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetTimelineItemPermissionsList(
                KinaUnaTypes.TimeLineType.Note, TestItemId, _testUser))
                .ReturnsAsync(permissions);

            // Act
            IActionResult? result = await _controller.GetTimelineItemPermissionsList((int)KinaUnaTypes.TimeLineType.Note, TestItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TimelineItemPermission> returnedPermissions = Assert.IsAssignableFrom<List<TimelineItemPermission>>(okResult.Value);
            Assert.Equal(2, returnedPermissions.Count);
        }

        [Fact]
        public async Task GetTimelineItemPermissionsList_Should_Return_Empty_List_When_No_Permissions()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetTimelineItemPermissionsList(
                KinaUnaTypes.TimeLineType.Note, TestItemId, _testUser))
                .ReturnsAsync(new List<TimelineItemPermission>());

            // Act
            IActionResult? result = await _controller.GetTimelineItemPermissionsList((int)KinaUnaTypes.TimeLineType.Note, TestItemId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TimelineItemPermission> returnedPermissions = Assert.IsAssignableFrom<List<TimelineItemPermission>>(okResult.Value);
            Assert.Empty(returnedPermissions);
        }

        #endregion

        #region GetProgenyPermissionsList Tests

        [Fact]
        public async Task GetProgenyPermissionsList_Should_Return_Permissions_With_UserInfo()
        {
            // Arrange
            List<ProgenyPermission> permissions = new()
            {
                new ProgenyPermission
                {
                    ProgenyPermissionId = TestPermissionId,
                    ProgenyId = TestProgenyId,
                    UserId = TestUserId,
                    Email = TestUserEmail,
                    PermissionLevel = PermissionLevel.Edit
                },
                new ProgenyPermission
                {
                    ProgenyPermissionId = TestPermissionId + 1,
                    ProgenyId = TestProgenyId,
                    UserId = OtherUserId,
                    Email = OtherUserEmail,
                    PermissionLevel = PermissionLevel.View
                }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermissionsList(TestProgenyId, _testUser))
                .ReturnsAsync(permissions);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);

            // Act
            List<ProgenyPermission>? result = await _controller.GetProgenyPermissionsList(TestProgenyId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.NotNull(result[0].UserInfo);
            Assert.NotNull(result[1].UserInfo);
            Assert.Equal(TestUserEmail, result[0].UserInfo.UserEmail);
            Assert.Equal(OtherUserEmail, result[1].UserInfo.UserEmail);
        }

        [Fact]
        public async Task GetProgenyPermissionsList_Should_Skip_UserInfo_When_UserId_Is_Empty()
        {
            // Arrange
            List<ProgenyPermission> permissions = new()
            {
                new ProgenyPermission
                {
                    ProgenyPermissionId = TestPermissionId,
                    ProgenyId = TestProgenyId,
                    UserId = "",
                    Email = TestUserEmail,
                    GroupId = 1,
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermissionsList(TestProgenyId, _testUser))
                .ReturnsAsync(permissions);

            // Act
            List<ProgenyPermission>? result = await _controller.GetProgenyPermissionsList(TestProgenyId);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].UserInfo);
            Assert.Equal(0, result[0].UserInfo.Id);
            Assert.Empty(result[0].UserInfo.UserId);
        }

        [Fact]
        public async Task GetProgenyPermissionsList_Should_Return_Empty_List_When_No_Permissions()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermissionsList(TestProgenyId, _testUser))
                .ReturnsAsync(new List<ProgenyPermission>());

            // Act
            List<ProgenyPermission>? result = await _controller.GetProgenyPermissionsList(TestProgenyId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetFamilyPermissionsList Tests

        [Fact]
        public async Task GetFamilyPermissionsList_Should_Return_Permissions_With_UserInfo()
        {
            // Arrange
            List<FamilyPermission> permissions = new()
            {
                new FamilyPermission
                {
                    FamilyPermissionId = TestPermissionId,
                    FamilyId = TestFamilyId,
                    UserId = TestUserId,
                    Email = TestUserEmail,
                    PermissionLevel = PermissionLevel.Edit
                },
                new FamilyPermission
                {
                    FamilyPermissionId = TestPermissionId + 1,
                    FamilyId = TestFamilyId,
                    UserId = OtherUserId,
                    Email = OtherUserEmail,
                    PermissionLevel = PermissionLevel.View
                }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetFamilyPermissionsList(TestFamilyId, _testUser))
                .ReturnsAsync(permissions);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);

            // Act
            List<FamilyPermission>? result = await _controller.GetFamilyPermissionsList(TestFamilyId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.NotNull(result[0].UserInfo);
            Assert.NotNull(result[1].UserInfo);
            Assert.Equal(TestUserEmail, result[0].UserInfo.UserEmail);
            Assert.Equal(OtherUserEmail, result[1].UserInfo.UserEmail);
        }

        [Fact]
        public async Task GetFamilyPermissionsList_Should_Skip_UserInfo_When_UserId_Is_Empty()
        {
            // Arrange
            List<FamilyPermission> permissions = new()
            {
                new FamilyPermission
                {
                    FamilyPermissionId = TestPermissionId,
                    FamilyId = TestFamilyId,
                    UserId = "",
                    Email = TestUserEmail,
                    GroupId = 1,
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetFamilyPermissionsList(TestFamilyId, _testUser))
                .ReturnsAsync(permissions);

            // Act
            List<FamilyPermission>? result = await _controller.GetFamilyPermissionsList(TestFamilyId);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].UserInfo);
            Assert.Equal(0, result[0].UserInfo.Id);
            Assert.Empty(result[0].UserInfo.UserId);
        }

        #endregion

        #region GetFamilyPermission Tests

        [Fact]
        public async Task GetFamilyPermission_Should_Return_Ok_With_Permission_And_UserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetFamilyPermission(TestPermissionId, _testUser))
                .ReturnsAsync(_testFamilyPermission);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult? result = await _controller.GetFamilyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyPermission permission = Assert.IsType<FamilyPermission>(okResult.Value);
            Assert.NotNull(permission.UserInfo);
            Assert.Equal(TestUserEmail, permission.UserInfo.UserEmail);
        }

        [Fact]
        public async Task GetFamilyPermission_Should_Return_Ok_Without_UserInfo_When_Permission_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetFamilyPermission(TestPermissionId, _testUser))
                .ReturnsAsync((FamilyPermission)null!);

            // Act
            IActionResult? result = await _controller.GetFamilyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task GetFamilyPermission_Should_Not_Load_UserInfo_When_UserId_Is_Empty()
        {
            // Arrange
            FamilyPermission permission = new()
            {
                FamilyPermissionId = TestPermissionId,
                FamilyId = TestFamilyId,
                UserId = "",
                Email = TestUserEmail,
                GroupId = 1,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetFamilyPermission(TestPermissionId, _testUser))
                .ReturnsAsync(permission);

            // Act
            IActionResult? result = await _controller.GetFamilyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyPermission returnedPermission = Assert.IsType<FamilyPermission>(okResult.Value);
            Assert.NotNull(returnedPermission.UserInfo);
            Assert.Equal(0, returnedPermission.UserInfo.Id);
            Assert.Empty(returnedPermission.UserInfo.UserId);
        }

        #endregion

        #region AddFamilyPermission Tests

        [Fact]
        public async Task AddFamilyPermission_Should_Return_Ok_With_Added_Permission()
        {
            // Arrange
            FamilyPermission newPermission = new()
            {
                FamilyId = TestFamilyId,
                UserId = OtherUserId,
                Email = OtherUserEmail,
                PermissionLevel = PermissionLevel.View
            };

            FamilyPermission addedPermission = new()
            {
                FamilyPermissionId = TestPermissionId + 1,
                FamilyId = TestFamilyId,
                UserId = OtherUserId,
                Email = OtherUserEmail,
                PermissionLevel = PermissionLevel.View
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GrantFamilyPermission(newPermission, _testUser))
                .ReturnsAsync(addedPermission);

            // Act
            IActionResult? result = await _controller.AddFamilyPermission(newPermission);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyPermission returnedPermission = Assert.IsType<FamilyPermission>(okResult.Value);
            Assert.Equal(TestPermissionId + 1, returnedPermission.FamilyPermissionId);
            Assert.Equal(OtherUserId, returnedPermission.UserId);
        }

        #endregion

        #region GetProgenyPermission Tests

        [Fact]
        public async Task GetProgenyPermission_Should_Return_Ok_With_Permission_And_UserInfo()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermission(TestPermissionId, _testUser))
                .ReturnsAsync(_testProgenyPermission);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Act
            IActionResult? result = await _controller.GetProgenyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            ProgenyPermission permission = Assert.IsType<ProgenyPermission>(okResult.Value);
            Assert.NotNull(permission.UserInfo);
            Assert.Equal(TestUserEmail, permission.UserInfo.UserEmail);
        }

        [Fact]
        public async Task GetProgenyPermission_Should_Return_Ok_Without_UserInfo_When_Permission_Is_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermission(TestPermissionId, _testUser))
                .ReturnsAsync((ProgenyPermission)null!);

            // Act
            IActionResult? result = await _controller.GetProgenyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task GetProgenyPermission_Should_Not_Load_UserInfo_When_UserId_Is_Empty()
        {
            // Arrange
            ProgenyPermission permission = new()
            {
                ProgenyPermissionId = TestPermissionId,
                ProgenyId = TestProgenyId,
                UserId = "",
                Email = TestUserEmail,
                GroupId = 1,
                PermissionLevel = PermissionLevel.Edit
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GetProgenyPermission(TestPermissionId, _testUser))
                .ReturnsAsync(permission);

            // Act
            IActionResult? result = await _controller.GetProgenyPermission(TestPermissionId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            ProgenyPermission returnedPermission = Assert.IsType<ProgenyPermission>(okResult.Value);
            Assert.NotNull(returnedPermission.UserInfo);
            Assert.Equal(0, returnedPermission.UserInfo.Id);
            Assert.Empty(returnedPermission.UserInfo.UserId);
        }

        #endregion

        #region AddProgenyPermission Tests

        [Fact]
        public async Task AddProgenyPermission_Should_Return_Ok_With_Added_Permission()
        {
            // Arrange
            ProgenyPermission newPermission = new()
            {
                ProgenyId = TestProgenyId,
                UserId = OtherUserId,
                Email = OtherUserEmail,
                PermissionLevel = PermissionLevel.View
            };

            ProgenyPermission addedPermission = new()
            {
                ProgenyPermissionId = TestPermissionId + 1,
                ProgenyId = TestProgenyId,
                UserId = OtherUserId,
                Email = OtherUserEmail,
                PermissionLevel = PermissionLevel.View
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.GrantProgenyPermission(newPermission, _testUser))
                .ReturnsAsync(addedPermission);

            // Act
            IActionResult? result = await _controller.AddProgenyPermission(newPermission);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            ProgenyPermission returnedPermission = Assert.IsType<ProgenyPermission>(okResult.Value);
            Assert.Equal(TestPermissionId + 1, returnedPermission.ProgenyPermissionId);
            Assert.Equal(OtherUserId, returnedPermission.UserId);
        }

        #endregion
    }
}