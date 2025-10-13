using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using KinaUnaProgenyApi.Services.KanbanServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class KanbanBoardsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IKanbanBoardsService> _mockKanbanBoardsService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly KanbanBoardsController _controller;

        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const string OtherUserEmail = "other@example.com";
        private const string OtherUserId = "other-user-id";
        private const int TestProgenyId = 1;
        private const int TestProgenyId2 = 2;
        private const int TestFamilyId = 1;
        private const int TestKanbanBoardId = 100;

        public KanbanBoardsControllerTests()
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
                IsKinaUnaAdmin = false
            };

            _adminUser = new UserInfo
            {
                UserId = AdminUserId,
                UserEmail = AdminUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _otherUser = new UserInfo
            {
                UserId = OtherUserId,
                UserEmail = OtherUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                Admins = AdminUserEmail
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family",
                Admins = AdminUserEmail
            };

            // Seed database
            SeedTestData();

            // Setup mocks
            _mockKanbanBoardsService = new Mock<IKanbanBoardsService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Default mock setups
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(AdminUserId))
                .ReturnsAsync(_adminUser);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(OtherUserId))
                .ReturnsAsync(_otherUser);

            // Initialize controller
            _controller = new KanbanBoardsController(
                _mockKanbanBoardsService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                _mockUserInfoService.Object,
                _mockAccessManagementService.Object);

            SetupControllerContext();
        }

        private void SeedTestData()
        {
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);
            _progenyDbContext.ProgenyDb.Add(_testProgeny);
            _progenyDbContext.FamiliesDb.Add(_testFamily);
            _progenyDbContext.SaveChanges();
        }

        private void SetupControllerContext(string userId = TestUserId, string userEmail = TestUserEmail)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Email, userEmail),
                new Claim(ClaimTypes.NameIdentifier, userId)
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

        private static KanbanBoard CreateTestKanbanBoard(
            int id,
            int progenyId,
            int familyId,
            string title = "Test Board",
            int accessLevel = 5)
        {
            return new KanbanBoard
            {
                KanbanBoardId = id,
                UId = Guid.NewGuid().ToString(),
                ProgenyId = progenyId,
                FamilyId = familyId,
                Title = title,
                Description = $"Test Description for {title}",
                Columns = "[{\"Id\":1,\"Name\":\"To Do\",\"ColumnIndex\":0},{\"Id\":2,\"Name\":\"In Progress\",\"ColumnIndex\":1},{\"Id\":3,\"Name\":\"Done\",\"ColumnIndex\":2}]",
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                AccessLevel = accessLevel,
                Tags = "test,board",
                Context = "test-context",
                IsDeleted = false
            };
        }

        #region GetKanbanBoard Tests

        [Fact]
        public async Task GetKanbanBoard_Should_Return_Ok_When_Board_Exists()
        {
            // Arrange
            KanbanBoard kanbanBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await _controller.GetKanbanBoard(TestKanbanBoardId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoard returnedBoard = Assert.IsType<KanbanBoard>(okResult.Value);
            Assert.Equal(TestKanbanBoardId, returnedBoard.KanbanBoardId);
            Assert.Equal("Test Board", returnedBoard.Title);
        }

        [Fact]
        public async Task GetKanbanBoard_Should_Return_NotFound_When_Board_Not_Exists()
        {
            // Arrange
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.GetKanbanBoard(TestKanbanBoardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetKanbanBoard_Should_Return_NotFound_When_Board_Has_Zero_Id()
        {
            // Arrange
            KanbanBoard kanbanBoard = CreateTestKanbanBoard(0, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await _controller.GetKanbanBoard(TestKanbanBoardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region GetProgeniesKanbanBoardsList Tests

        [Fact]
        public async Task GetProgeniesKanbanBoardsList_Should_Return_Ok_With_Boards_From_Progenies_And_Families()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = [TestProgenyId, TestProgenyId2],
                FamilyIds = [TestFamilyId],
                Skip = 0,
                NumberOfItems = 10
            };

            Progeny progeny1 = new() { Id = TestProgenyId, Name = "Progeny 1" };
            Progeny progeny2 = new() { Id = TestProgenyId2, Name = "Progeny 2" };
            Family family1 = new() { FamilyId = TestFamilyId, Name = "Family 1" };

            List<KanbanBoard> progeny1Boards = [CreateTestKanbanBoard(1, TestProgenyId, 0, "Progeny 1 Board")];
            List<KanbanBoard> progeny2Boards = [CreateTestKanbanBoard(2, TestProgenyId2, 0, "Progeny 2 Board")];
            List<KanbanBoard> family1Boards = [CreateTestKanbanBoard(3, 0, TestFamilyId, "Family 1 Board")];

            KanbanBoardsResponse expectedResponse = new()
            {
                KanbanBoards = [.. progeny1Boards, .. progeny2Boards, .. family1Boards],
                KanbanBoardsRequest = request
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(progeny1);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId2, _testUser))
                .ReturnsAsync(progeny2);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(family1);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(TestProgenyId, 0, _testUser, request))
                .ReturnsAsync(progeny1Boards);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(TestProgenyId2, 0, _testUser, request))
                .ReturnsAsync(progeny2Boards);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(0, TestFamilyId, _testUser, request))
                .ReturnsAsync(family1Boards);
            _mockKanbanBoardsService.Setup(x => x.CreateKanbanBoardsResponse(It.IsAny<List<KanbanBoard>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetProgeniesKanbanBoardsList(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoardsResponse response = Assert.IsType<KanbanBoardsResponse>(okResult.Value);
            Assert.Equal(3, response.KanbanBoards.Count);
        }

        [Fact]
        public async Task GetProgeniesKanbanBoardsList_Should_Return_NotFound_When_No_Progenies_Accessible()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [],
                Skip = 0,
                NumberOfItems = 10
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync((Progeny?)null);

            // Act
            IActionResult result = await _controller.GetProgeniesKanbanBoardsList(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProgeniesKanbanBoardsList_Should_Handle_Negative_Skip_Value()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [],
                Skip = -5,
                NumberOfItems = 10
            };

            Progeny progeny = new() { Id = TestProgenyId, Name = "Test Progeny" };
            List<KanbanBoard> boards = [CreateTestKanbanBoard(1, TestProgenyId, 0)];
            KanbanBoardsResponse expectedResponse = new() { KanbanBoards = boards, KanbanBoardsRequest = request };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(progeny);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(TestProgenyId, 0, _testUser, request))
                .ReturnsAsync(boards);
            _mockKanbanBoardsService.Setup(x => x.CreateKanbanBoardsResponse(It.IsAny<List<KanbanBoard>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetProgeniesKanbanBoardsList(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, request.Skip); // Should be reset to 0
        }

        [Fact]
        public async Task GetProgeniesKanbanBoardsList_Should_Skip_Invalid_Progenies_And_Families()
        {
            // Arrange
            KanbanBoardsRequest request = new()
            {
                ProgenyIds = [TestProgenyId, 999], // 999 doesn't exist
                FamilyIds = [TestFamilyId, 888], // 888 doesn't exist
                Skip = 0,
                NumberOfItems = 10
            };

            Progeny progeny = new() { Id = TestProgenyId, Name = "Test Progeny" };
            Family family = new() { FamilyId = TestFamilyId, Name = "Test Family" };
            List<KanbanBoard> progenyBoards = [CreateTestKanbanBoard(1, TestProgenyId, 0)];
            List<KanbanBoard> familyBoards = [CreateTestKanbanBoard(2, 0, TestFamilyId)];
            KanbanBoardsResponse expectedResponse = new() { KanbanBoards = [.. progenyBoards, .. familyBoards], KanbanBoardsRequest = request };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(progeny);
            _mockProgenyService.Setup(x => x.GetProgeny(999, _testUser))
                .ReturnsAsync((Progeny?)null);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(family);
            _mockFamiliesService.Setup(x => x.GetFamilyById(888, _testUser))
                .ReturnsAsync((Family?)null);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(TestProgenyId, 0, _testUser, request))
                .ReturnsAsync(progenyBoards);
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardsForProgenyOrFamily(0, TestFamilyId, _testUser, request))
                .ReturnsAsync(familyBoards);
            _mockKanbanBoardsService.Setup(x => x.CreateKanbanBoardsResponse(It.IsAny<List<KanbanBoard>>(), request))
                .Returns(expectedResponse);

            // Act
            IActionResult result = await _controller.GetProgeniesKanbanBoardsList(request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoardsResponse response = Assert.IsType<KanbanBoardsResponse>(okResult.Value);
            Assert.Equal(2, response.KanbanBoards.Count);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Board_With_Progeny()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, TestProgenyId, 0);
            KanbanBoard addedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockKanbanBoardsService.Setup(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync(addedBoard);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoard returnedBoard = Assert.IsType<KanbanBoard>(okResult.Value);
            Assert.Equal(TestKanbanBoardId, returnedBoard.KanbanBoardId);

            _mockKanbanBoardsService.Verify(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Board_With_Family()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, 0, TestFamilyId);
            KanbanBoard addedBoard = CreateTestKanbanBoard(TestKanbanBoardId, 0, TestFamilyId);

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockKanbanBoardsService.Setup(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync(addedBoard);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoard returnedBoard = Assert.IsType<KanbanBoard>(okResult.Value);
            Assert.Equal(TestKanbanBoardId, returnedBoard.KanbanBoardId);

            _mockKanbanBoardsService.Verify(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, TestProgenyId, TestFamilyId);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A Kanban board must have either a ProgenyId or a FamilyId set, but not both.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Neither_ProgenyId_Nor_FamilyId_Set()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, 0, 0);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A Kanban board must have either a ProgenyId or a FamilyId set.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_No_Progeny_Permission()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, TestProgenyId, 0);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_No_Family_Permission()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, 0, TestFamilyId);

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddKanbanBoard_Returns_Null()
        {
            // Arrange
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, TestProgenyId, 0);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockKanbanBoardsService.Setup(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_Should_Return_Ok_When_Valid_Update()
        {
            // Arrange
            KanbanBoard updateValues = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            updateValues.Title = "Updated Title";
            KanbanBoard updatedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            updatedBoard.Title = "Updated Title";

            _mockKanbanBoardsService.Setup(x => x.UpdateKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync(updatedBoard);

            // Act
            IActionResult result = await _controller.Update(TestKanbanBoardId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoard returnedBoard = Assert.IsType<KanbanBoard>(okResult.Value);
            Assert.Equal("Updated Title", returnedBoard.Title);

            _mockKanbanBoardsService.Verify(x => x.UpdateKanbanBoard(It.Is<KanbanBoard>(b =>
                b.KanbanBoardId == TestKanbanBoardId &&
                b.ModifiedBy == TestUserId &&
                b.ModifiedTime != default), _testUser), Times.Once);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_Id_Mismatch()
        {
            // Arrange
            KanbanBoard updateValues = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            // Act
            IActionResult result = await _controller.Update(999, updateValues);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_Board_Is_Null()
        {
            // Act
            IActionResult result = await _controller.Update(TestKanbanBoardId, null!);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            KanbanBoard updateValues = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, TestFamilyId);

            // Act
            IActionResult result = await _controller.Update(TestKanbanBoardId, updateValues);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_Should_Return_Unauthorized_When_UpdateKanbanBoard_Returns_Null()
        {
            // Arrange
            KanbanBoard updateValues = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.UpdateKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.Update(TestKanbanBoardId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Update_Should_Set_ModifiedBy_And_ModifiedTime()
        {
            // Arrange
            KanbanBoard updateValues = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            KanbanBoard updatedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            DateTime beforeUpdate = DateTime.UtcNow;

            _mockKanbanBoardsService.Setup(x => x.UpdateKanbanBoard(It.IsAny<KanbanBoard>(), _testUser))
                .ReturnsAsync(updatedBoard);

            // Act
            await _controller.Update(TestKanbanBoardId, updateValues);

            // Assert
            _mockKanbanBoardsService.Verify(x => x.UpdateKanbanBoard(It.Is<KanbanBoard>(b =>
                b.ModifiedBy == TestUserId &&
                b.ModifiedTime >= beforeUpdate), _testUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_Ok_When_Successfully_Deleted_With_Soft_Delete()
        {
            // Arrange
            KanbanBoard existingBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            KanbanBoard deletedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            deletedBoard.IsDeleted = true;

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(existingBoard);
            _mockKanbanBoardsService.Setup(x => x.DeleteKanbanBoard(existingBoard, _testUser, false))
                .ReturnsAsync(deletedBoard);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId, false);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KanbanBoard returnedBoard = Assert.IsType<KanbanBoard>(okResult.Value);
            Assert.True(returnedBoard.IsDeleted);

            _mockKanbanBoardsService.Verify(x => x.DeleteKanbanBoard(existingBoard, _testUser, false), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_Ok_When_Successfully_Deleted_With_Hard_Delete()
        {
            // Arrange
            KanbanBoard existingBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            KanbanBoard deletedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(existingBoard);
            _mockKanbanBoardsService.Setup(x => x.DeleteKanbanBoard(existingBoard, _testUser, true))
                .ReturnsAsync(deletedBoard);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId, true);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<KanbanBoard>(okResult.Value);

            _mockKanbanBoardsService.Verify(x => x.DeleteKanbanBoard(existingBoard, _testUser, true), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Board_Not_Exists()
        {
            // Arrange
            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Board_Has_Zero_Id()
        {
            // Arrange
            KanbanBoard existingBoard = CreateTestKanbanBoard(0, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(existingBoard);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteKanbanBoard_Returns_Null()
        {
            // Arrange
            KanbanBoard existingBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(existingBoard);
            _mockKanbanBoardsService.Setup(x => x.DeleteKanbanBoard(existingBoard, _testUser, false))
                .ReturnsAsync((KanbanBoard?)null);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Default_HardDelete_To_False_When_Not_Specified()
        {
            // Arrange
            KanbanBoard existingBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);
            KanbanBoard deletedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(existingBoard);
            _mockKanbanBoardsService.Setup(x => x.DeleteKanbanBoard(existingBoard, _testUser, false))
                .ReturnsAsync(deletedBoard);

            // Act
            IActionResult result = await _controller.Delete(TestKanbanBoardId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKanbanBoardsService.Verify(x => x.DeleteKanbanBoard(existingBoard, _testUser, false), Times.Once);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Post_Should_Use_Current_User_From_Claims()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);
            KanbanBoard boardToAdd = CreateTestKanbanBoard(0, TestProgenyId, 0);
            KanbanBoard addedBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _adminUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockKanbanBoardsService.Setup(x => x.AddKanbanBoard(It.IsAny<KanbanBoard>(), _adminUser))
                .ReturnsAsync(addedBoard);
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(It.IsAny<string>())).ReturnsAsync(_adminUser);

            // Act
            IActionResult result = await _controller.Post(boardToAdd);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetKanbanBoard_Should_Validate_User_Access()
        {
            // Arrange
            KanbanBoard kanbanBoard = CreateTestKanbanBoard(TestKanbanBoardId, TestProgenyId, 0);

            _mockKanbanBoardsService.Setup(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser))
                .ReturnsAsync(kanbanBoard);

            // Act
            IActionResult result = await _controller.GetKanbanBoard(TestKanbanBoardId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockKanbanBoardsService.Verify(x => x.GetKanbanBoardById(TestKanbanBoardId, _testUser), Times.Once);
        }

        #endregion

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}