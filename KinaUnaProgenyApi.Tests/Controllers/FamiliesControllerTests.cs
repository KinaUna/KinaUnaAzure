using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class FamiliesControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IFamilyMembersService> _mockFamilyMembersService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly FamiliesController _controller;

        private readonly UserInfo _testUser;
        private readonly Family _testFamily;
        private readonly FamilyMember _testFamilyMember;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestFamilyId = 5;
        private const int TestProgenyId = 10;
        private const int TestFamilyMemberId = 100;

        public FamiliesControllerTests()
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
                ViewChild = 1,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User"
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family",
                Admins = TestUserEmail,
                PictureLink = "family.jpg"
            };

            _testFamilyMember = new FamilyMember
            {
                FamilyMemberId = TestFamilyMemberId,
                FamilyId = TestFamilyId,
                UserId = TestUserId,
                Email = TestUserEmail,
                MemberType = FamilyMemberType.Parent
            };

            // Setup mocks
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockFamilyMembersService = new Mock<IFamilyMembersService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize controller
            _controller = new FamiliesController(
                _mockFamiliesService.Object,
                _mockFamilyMembersService.Object,
                _mockUserInfoService.Object);

            // Setup controller context with claims
            SetupControllerContext(TestUserEmail);
        }

        private void SetupControllerContext(string email)
        {
            List<Claim> claims = [new(ClaimTypes.Email, email)];
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
            GC.SuppressFinalize(this);
        }

        #region GetFamily Tests

        [Fact]
        public async Task GetFamily_Should_Return_Ok_With_Family_When_User_Has_Access()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.GetFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Family returnedFamily = Assert.IsType<Family>(okResult.Value);
            Assert.Equal(TestFamilyId, returnedFamily.FamilyId);
            Assert.Equal(_testFamily.Name, returnedFamily.Name);

            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(TestUserEmail), Times.Once);
            _mockFamiliesService.Verify(x => x.GetFamilyById(TestFamilyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetFamily_Should_Return_Unauthorized_When_Family_Not_Found()
        {
            // Arrange
            Family unauthorizedFamily = new() { FamilyId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(999, _testUser))
                .ReturnsAsync(unauthorizedFamily);

            // Act
            IActionResult result = await _controller.GetFamily(999);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetFamily_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.GetFamily(TestFamilyId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region GetCurrentUsersFamilies Tests

        [Fact]
        public async Task GetCurrentUsersFamilies_Should_Return_Ok_With_Families_List()
        {
            // Arrange
            List<Family> families =
            [
                _testFamily,
                new() { FamilyId = TestFamilyId + 1, Name = "Another Family" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetUsersFamiliesByUserId(_testUser.UserId, _testUser))
                .ReturnsAsync(families);

            // Act
            IActionResult result = await _controller.GetCurrentUsersFamilies();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> returnedFamilies = Assert.IsType<List<Family>>(okResult.Value);
            Assert.Equal(2, returnedFamilies.Count);
            Assert.Contains(returnedFamilies, f => f.FamilyId == TestFamilyId);

            _mockFamiliesService.Verify(x => x.GetUsersFamiliesByUserId(_testUser.UserId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUsersFamilies_Should_Return_Empty_List_When_User_Has_No_Families()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetUsersFamiliesByUserId(_testUser.UserId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetCurrentUsersFamilies();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> returnedFamilies = Assert.IsType<List<Family>>(okResult.Value);
            Assert.Empty(returnedFamilies);
        }

        [Fact]
        public async Task GetCurrentUsersFamilies_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetUsersFamiliesByUserId(_testUser.UserId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetCurrentUsersFamilies();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region AddFamily Tests

        [Fact]
        public async Task AddFamily_Should_Return_Ok_With_New_Family()
        {
            // Arrange
            Family newFamily = new()
            {
                Name = "New Family",
                PictureLink = "newfamily.jpg"
            };

            Family createdFamily = new()
            {
                FamilyId = TestFamilyId,
                Name = "New Family",
                PictureLink = "newfamily.jpg",
                Admins = TestUserEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.AddFamily(newFamily, _testUser))
                .ReturnsAsync(createdFamily);

            // Act
            IActionResult result = await _controller.AddFamily(newFamily);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Family returnedFamily = Assert.IsType<Family>(okResult.Value);
            Assert.Equal(TestFamilyId, returnedFamily.FamilyId);
            Assert.Equal("New Family", returnedFamily.Name);

            _mockFamiliesService.Verify(x => x.AddFamily(newFamily, _testUser), Times.Once);
        }

        [Fact]
        public async Task AddFamily_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            Family newFamily = new() { Name = "New Family" };
            Family createdFamily = new() { FamilyId = TestFamilyId, Name = "New Family" };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.AddFamily(newFamily, _testUser))
                .ReturnsAsync(createdFamily);

            // Act
            IActionResult result = await _controller.AddFamily(newFamily);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region UpdateFamily Tests

        [Fact]
        public async Task UpdateFamily_Should_Return_Ok_With_Updated_Family_When_Authorized()
        {
            // Arrange
            Family updateFamily = new()
            {
                FamilyId = TestFamilyId,
                Name = "Updated Family",
                PictureLink = "updated.jpg"
            };

            Family updatedFamily = new()
            {
                FamilyId = TestFamilyId,
                Name = "Updated Family",
                PictureLink = "updated.jpg",
                Admins = TestUserEmail
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.UpdateFamily(updateFamily, _testUser))
                .ReturnsAsync(updatedFamily);

            // Act
            IActionResult result = await _controller.UpdateFamily(updateFamily);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Family returnedFamily = Assert.IsType<Family>(okResult.Value);
            Assert.Equal(TestFamilyId, returnedFamily.FamilyId);
            Assert.Equal("Updated Family", returnedFamily.Name);

            _mockFamiliesService.Verify(x => x.UpdateFamily(updateFamily, _testUser), Times.Once);
        }

        [Fact]
        public async Task UpdateFamily_Should_Return_Unauthorized_When_Update_Fails()
        {
            // Arrange
            Family updateFamily = new() { FamilyId = TestFamilyId, Name = "Updated Family" };
            Family unauthorizedFamily = new() { FamilyId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.UpdateFamily(updateFamily, _testUser))
                .ReturnsAsync(unauthorizedFamily);

            // Act
            IActionResult result = await _controller.UpdateFamily(updateFamily);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateFamily_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            Family updateFamily = new() { FamilyId = TestFamilyId, Name = "Updated" };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.UpdateFamily(updateFamily, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            IActionResult result = await _controller.UpdateFamily(updateFamily);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region DeleteFamily Tests

        [Fact]
        public async Task DeleteFamily_Should_Return_Ok_When_Successfully_Deleted()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockFamiliesService.Setup(x => x.DeleteFamily(_testFamily.FamilyId, _testUser))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);

            _mockFamiliesService.Verify(x => x.GetFamilyById(TestFamilyId, _testUser), Times.Once);
            _mockFamiliesService.Verify(x => x.DeleteFamily(TestFamilyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task DeleteFamily_Should_Return_Unauthorized_When_Family_Not_Found()
        {
            // Arrange
            Family unauthorizedFamily = new() { FamilyId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(999, _testUser))
                .ReturnsAsync(unauthorizedFamily);

            // Act
            IActionResult result = await _controller.DeleteFamily(999);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockFamiliesService.Verify(x => x.DeleteFamily(It.IsAny<int>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task DeleteFamily_Should_Return_Unauthorized_When_Delete_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockFamiliesService.Setup(x => x.DeleteFamily(_testFamily.FamilyId, _testUser))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteFamily(TestFamilyId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteFamily_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockFamiliesService.Setup(x => x.DeleteFamily(_testFamily.FamilyId, _testUser))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteFamily(TestFamilyId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region GetFamilyMembersForFamily Tests

        [Fact]
        public async Task GetFamilyMembersForFamily_Should_Return_Ok_With_Members_List()
        {
            // Arrange
            List<FamilyMember> members =
            [
                _testFamilyMember,
                new()
                {
                    FamilyMemberId = TestFamilyMemberId + 1,
                    FamilyId = TestFamilyId,
                    MemberType = FamilyMemberType.Child
                }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMembersForFamily(TestFamilyId, _testUser))
                .ReturnsAsync(members);

            // Act
            IActionResult result = await _controller.GetFamilyMembersForFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<FamilyMember> returnedMembers = Assert.IsType<List<FamilyMember>>(okResult.Value);
            Assert.Equal(2, returnedMembers.Count);
            Assert.Contains(returnedMembers, m => m.FamilyMemberId == TestFamilyMemberId);

            _mockFamilyMembersService.Verify(x => x.GetFamilyMembersForFamily(TestFamilyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_Should_Return_Empty_List_When_No_Members()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMembersForFamily(TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetFamilyMembersForFamily(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<FamilyMember> returnedMembers = Assert.IsType<List<FamilyMember>>(okResult.Value);
            Assert.Empty(returnedMembers);
        }

        [Fact]
        public async Task GetFamilyMembersForFamily_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMembersForFamily(TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetFamilyMembersForFamily(TestFamilyId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region GetFamilyMember Tests

        [Fact]
        public async Task GetFamilyMember_Should_Return_Ok_With_FamilyMember_When_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMember(TestFamilyMemberId, _testUser))
                .ReturnsAsync(_testFamilyMember);

            // Act
            IActionResult result = await _controller.GetFamilyMember(TestFamilyMemberId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyMember returnedMember = Assert.IsType<FamilyMember>(okResult.Value);
            Assert.Equal(TestFamilyMemberId, returnedMember.FamilyMemberId);
            
            _mockFamilyMembersService.Verify(x => x.GetFamilyMember(TestFamilyMemberId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetFamilyMember_Should_Return_Unauthorized_When_Member_Not_Found()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMember(999, _testUser))
                .ReturnsAsync((FamilyMember?)null);

            // Act
            IActionResult result = await _controller.GetFamilyMember(999);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetFamilyMember_Should_Return_Unauthorized_When_FamilyMemberId_Is_Zero()
        {
            // Arrange
            FamilyMember memberWithZeroId = new() { FamilyMemberId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMember(TestFamilyMemberId, _testUser))
                .ReturnsAsync(memberWithZeroId);

            // Act
            IActionResult result = await _controller.GetFamilyMember(TestFamilyMemberId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetFamilyMember_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.GetFamilyMember(TestFamilyMemberId, _testUser))
                .ReturnsAsync(_testFamilyMember);

            // Act
            IActionResult result = await _controller.GetFamilyMember(TestFamilyMemberId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region AddFamilyMember Tests

        [Fact]
        public async Task AddFamilyMember_Should_Return_Ok_With_New_Member_When_Authorized()
        {
            // Arrange
            FamilyMember newMember = new()
            {
                FamilyId = TestFamilyId,
                MemberType = FamilyMemberType.Child
            };

            FamilyMember createdMember = new()
            {
                FamilyMemberId = TestFamilyMemberId,
                FamilyId = TestFamilyId,
                MemberType = FamilyMemberType.Child
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.AddFamilyMember(newMember, _testUser))
                .ReturnsAsync(createdMember);

            // Act
            IActionResult result = await _controller.AddFamilyMember(newMember);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyMember returnedMember = Assert.IsType<FamilyMember>(okResult.Value);
            Assert.Equal(TestFamilyMemberId, returnedMember.FamilyMemberId);
            
            _mockFamilyMembersService.Verify(x => x.AddFamilyMember(newMember, _testUser), Times.Once);
        }

        [Fact]
        public async Task AddFamilyMember_Should_Return_Unauthorized_When_Add_Fails()
        {
            // Arrange
            FamilyMember newMember = new() { FamilyId = TestFamilyId, MemberType = FamilyMemberType.Child};
            FamilyMember failedMember = new() { FamilyMemberId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.AddFamilyMember(newMember, _testUser))
                .ReturnsAsync(failedMember);

            // Act
            IActionResult result = await _controller.AddFamilyMember(newMember);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AddFamilyMember_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            FamilyMember newMember = new() { FamilyId = TestFamilyId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.AddFamilyMember(newMember, _testUser))
                .ReturnsAsync(_testFamilyMember);

            // Act
            IActionResult result = await _controller.AddFamilyMember(newMember);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region UpdateFamilyMember Tests

        [Fact]
        public async Task UpdateFamilyMember_Should_Return_Ok_With_Updated_Member_When_Authorized()
        {
            // Arrange
            FamilyMember updateMember = new()
            {
                FamilyMemberId = TestFamilyMemberId,
                FamilyId = TestFamilyId,
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 10
            };

            FamilyMember updatedMember = new()
            {
                FamilyMemberId = TestFamilyMemberId,
                FamilyId = TestFamilyId,
                MemberType = FamilyMemberType.Parent,
                ProgenyId = 10,
                UserId = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.UpdateFamilyMember(updateMember, _testUser))
                .ReturnsAsync(updatedMember);

            // Act
            IActionResult result = await _controller.UpdateFamilyMember(updateMember);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            FamilyMember returnedMember = Assert.IsType<FamilyMember>(okResult.Value);
            Assert.Equal(TestFamilyMemberId, returnedMember.FamilyMemberId);
            Assert.Equal(TestUserId, returnedMember.UserId);

            _mockFamilyMembersService.Verify(x => x.UpdateFamilyMember(updateMember, _testUser), Times.Once);
        }

        [Fact]
        public async Task UpdateFamilyMember_Should_Return_Unauthorized_When_Update_Fails()
        {
            // Arrange
            FamilyMember updateMember = new() { FamilyMemberId = TestFamilyMemberId };
            FamilyMember failedMember = new() { FamilyMemberId = 0 };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.UpdateFamilyMember(updateMember, _testUser))
                .ReturnsAsync(failedMember);

            // Act
            IActionResult result = await _controller.UpdateFamilyMember(updateMember);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateFamilyMember_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");
            FamilyMember updateMember = new() { FamilyMemberId = TestFamilyMemberId };

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.UpdateFamilyMember(updateMember, _testUser))
                .ReturnsAsync(_testFamilyMember);

            // Act
            IActionResult result = await _controller.UpdateFamilyMember(updateMember);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region DeleteFamilyMember Tests

        [Fact]
        public async Task DeleteFamilyMember_Should_Return_Ok_When_Successfully_Deleted()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.DeleteFamilyMember(TestFamilyMemberId, _testUser))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteFamilyMember(TestFamilyMemberId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);

            _mockFamilyMembersService.Verify(x => x.DeleteFamilyMember(TestFamilyMemberId, _testUser), Times.Once);
        }

        [Fact]
        public async Task DeleteFamilyMember_Should_Return_Unauthorized_When_Delete_Fails()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.DeleteFamilyMember(999, _testUser))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteFamilyMember(999);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteFamilyMember_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamilyMembersService.Setup(x => x.DeleteFamilyMember(TestFamilyMemberId, _testUser))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteFamilyMember(TestFamilyMemberId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion

        #region GetFamiliesForProgeny Tests

        [Fact]
        public async Task GetFamiliesForProgeny_Should_Return_Ok_With_Families_List()
        {
            // Arrange
            List<Family> families =
            [
                _testFamily,
                new() { FamilyId = TestFamilyId + 1, Name = "Second Family" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamiliesForProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(families);

            // Act
            IActionResult result = await _controller.GetFamiliesForProgeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> returnedFamilies = Assert.IsType<List<Family>>(okResult.Value);
            Assert.Equal(2, returnedFamilies.Count);
            Assert.Contains(returnedFamilies, f => f.FamilyId == TestFamilyId);

            _mockFamiliesService.Verify(x => x.GetFamiliesForProgeny(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetFamiliesForProgeny_Should_Return_Empty_List_When_No_Families()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(TestUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamiliesForProgeny(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetFamiliesForProgeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Family> returnedFamilies = Assert.IsType<List<Family>>(okResult.Value);
            Assert.Empty(returnedFamilies);
        }

        [Fact]
        public async Task GetFamiliesForProgeny_Should_Use_DefaultUserEmail_When_User_Email_Is_Null()
        {
            // Arrange
            SetupControllerContext("");

            _mockUserInfoService.Setup(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail))
                .ReturnsAsync(_testUser);
            _mockFamiliesService.Setup(x => x.GetFamiliesForProgeny(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetFamiliesForProgeny(TestProgenyId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
        }

        #endregion
    }
}