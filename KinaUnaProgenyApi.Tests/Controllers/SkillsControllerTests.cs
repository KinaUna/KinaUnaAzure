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
    public class SkillsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<ISkillService> _mockSkillService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly SkillsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Skill _testSkill;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestSkillId = 100;

        public SkillsControllerTests()
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

            _testSkill = new Skill
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Test Skill",
                Description = "Test Description",
                Category = "Physical",
                SkillFirstObservation = DateTime.UtcNow.AddDays(-30),
                SkillAddedDate = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Skill,
                ItemId = TestSkillId.ToString(),
                ProgenyTime = _testSkill.SkillFirstObservation ?? DateTime.UtcNow
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockSkillService = new Mock<ISkillService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new SkillsController(
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockSkillService.Object,
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
        public async Task Progeny_Should_Return_Ok_With_Skills_List()
        {
            // Arrange
            List<Skill> skillsList =
            [
                _testSkill,
                new Skill { SkillId = TestSkillId + 1, ProgenyId = TestProgenyId, Name = "Skill 2" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(skillsList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Skill> returnedSkills = Assert.IsType<List<Skill>>(okResult.Value);
            Assert.Equal(2, returnedSkills.Count);
            Assert.Equal(_testSkill.SkillId, returnedSkills[0].SkillId);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockSkillService.Verify(x => x.GetSkillsList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Empty_List_When_No_Skills()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Skill> returnedSkills = Assert.IsType<List<Skill>>(okResult.Value);
            Assert.Empty(returnedSkills);
        }

        #endregion

        #region GetSkillItem Tests

        [Fact]
        public async Task GetSkillItem_Should_Return_Ok_When_Skill_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);

            // Act
            IActionResult result = await _controller.GetSkillItem(TestSkillId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Skill returnedSkill = Assert.IsType<Skill>(okResult.Value);
            Assert.Equal(TestSkillId, returnedSkill.SkillId);
            Assert.Equal(_testSkill.Name, returnedSkill.Name);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockSkillService.Verify(x => x.GetSkill(TestSkillId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetSkillItem_Should_Return_NotFound_When_Skill_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(999, _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.GetSkillItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSkillService.Verify(x => x.GetSkill(999, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Skill_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Skill newSkill = new()
            {
                ProgenyId = TestProgenyId,
                Name = "New Skill",
                Description = "New Description",
                Category = "Cognitive",
                SkillFirstObservation = DateTime.UtcNow.AddDays(-10)
            };

            Skill createdSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "New Skill",
                Description = "New Description",
                Category = "Cognitive",
                SkillFirstObservation = DateTime.UtcNow.AddDays(-10),
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.AddSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(createdSkill);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(createdSkill);

            // Act
            IActionResult result = await _controller.Post(newSkill);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Skill returnedSkill = Assert.IsType<Skill>(okResult.Value);
            Assert.Equal(TestSkillId, returnedSkill.SkillId);
            Assert.Equal(TestUserId, returnedSkill.Author);
            Assert.Equal(TestUserId, returnedSkill.CreatedBy);
            Assert.Equal(TestUserId, returnedSkill.ModifiedBy);

            _mockSkillService.Verify(x => x.AddSkill(It.Is<Skill>(s =>
                s.Author == TestUserId &&
                s.CreatedBy == TestUserId &&
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendSkillNotification(
                It.IsAny<Skill>(), _testUser, It.Is<string>(s => s.Contains("added"))), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddSkill_Returns_Null()
        {
            // Arrange
            Skill newSkill = new()
            {
                ProgenyId = TestProgenyId,
                Name = "Failed Skill"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.AddSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.Post(newSkill);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
            _mockWebNotificationsService.Verify(x => x.SendSkillNotification(
                It.IsAny<Skill>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_SkillId_Is_Zero()
        {
            // Arrange
            Skill newSkill = new()
            {
                ProgenyId = TestProgenyId,
                Name = "Zero Id Skill"
            };

            Skill createdSkill = new()
            {
                SkillId = 0,
                ProgenyId = TestProgenyId,
                Name = "Zero Id Skill"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.AddSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(createdSkill);

            // Act
            IActionResult result = await _controller.Post(newSkill);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Use_User_Id_For_Tracking_Fields()
        {
            // Arrange
            Skill newSkill = new()
            {
                ProgenyId = TestProgenyId,
                Name = "Test Tracking",
                Author = "wrong-user",
                CreatedBy = "wrong-user",
                ModifiedBy = "wrong-user"
            };

            Skill createdSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Test Tracking",
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.AddSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(createdSkill);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Post(newSkill);

            // Assert
            _mockSkillService.Verify(x => x.AddSkill(It.Is<Skill>(s =>
                s.Author == TestUserId &&
                s.CreatedBy == TestUserId &&
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Skill_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Skill updatedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Updated Skill",
                Description = "Updated Description"
            };

            Skill returnedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Updated Skill",
                Description = "Updated Description",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.UpdateSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(returnedSkill);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestSkillId, updatedSkill);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Skill resultSkill = Assert.IsType<Skill>(okResult.Value);
            
            Assert.Equal(TestUserId, resultSkill.ModifiedBy);

            _mockSkillService.Verify(x => x.GetSkill(TestSkillId, _testUser), Times.Exactly(2));
            _mockSkillService.Verify(x => x.UpdateSkill(It.Is<Skill>(s =>
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Skill_Does_Not_Exist()
        {
            // Arrange
            Skill updatedSkill = new()
            {
                SkillId = 999,
                ProgenyId = TestProgenyId,
                Name = "Non-existent Skill"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(999, _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.Put(999, updatedSkill);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSkillService.Verify(x => x.UpdateSkill(It.IsAny<Skill>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateSkill_Returns_Null()
        {
            // Arrange
            Skill updatedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Failed Update"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.UpdateSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.Put(TestSkillId, updatedSkill);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_SkillId_Is_Zero()
        {
            // Arrange
            Skill updatedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Zero Id Update"
            };

            Skill returnedSkill = new()
            {
                SkillId = 0,
                ProgenyId = TestProgenyId,
                Name = "Zero Id Update"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.UpdateSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(returnedSkill);

            // Act
            IActionResult result = await _controller.Put(TestSkillId, updatedSkill);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Skill updatedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Updated Skill"
            };

            Skill returnedSkill = new()
            {
                SkillId = TestSkillId,
                ProgenyId = TestProgenyId,
                Name = "Updated Skill",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.UpdateSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(returnedSkill);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestSkillId, updatedSkill);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Skill_And_Timeline_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.DeleteSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(_testSkill);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestSkillId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockSkillService.Verify(x => x.GetSkill(TestSkillId, _testUser), Times.Once);
            _mockSkillService.Verify(x => x.DeleteSkill(It.Is<Skill>(s =>
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendSkillNotification(
                It.IsAny<Skill>(), _testUser, It.Is<string>(s => s.Contains("deleted"))), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Skill_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(999, _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSkillService.Verify(x => x.DeleteSkill(It.IsAny<Skill>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteSkill_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.DeleteSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(null as Skill);

            // Act
            IActionResult result = await _controller.Delete(TestSkillId);

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
            _mockSkillService.Setup(x => x.GetSkill(TestSkillId, _testUser))
                .ReturnsAsync(_testSkill);
            _mockSkillService.Setup(x => x.DeleteSkill(It.IsAny<Skill>(), _testUser))
                .ReturnsAsync(_testSkill);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, _testUser))
                .ReturnsAsync(null as TimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            IActionResult result = await _controller.Delete(TestSkillId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
            _mockWebNotificationsService.Verify(x => x.SendSkillNotification(
                It.IsAny<Skill>(), _testUser, It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region GetSkillsListPage Tests

        [Fact]
        public async Task GetSkillsListPage_Should_Return_Ok_With_Paged_List()
        {
            // Arrange
            List<Skill> allSkills = [];
            for (int i = 1; i <= 20; i++)
            {
                allSkills.Add(new Skill
                {
                    SkillId = i,
                    ProgenyId = TestProgenyId,
                    Name = $"Skill {i}",
                    SkillFirstObservation = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(allSkills);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Equal(8, page.SkillsList.Count);
            Assert.Equal(3, page.TotalPages); // 20 items / 8 per page = 2.5, rounded up to 3
            Assert.Equal(1, page.PageNumber);
            Assert.Equal(1, page.SortBy);
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Sort_Oldest_First_When_SortBy_Zero()
        {
            // Arrange
            List<Skill> allSkills =
            [
                new() { SkillId = 1, ProgenyId = TestProgenyId, Name = "Newest", SkillFirstObservation = DateTime.UtcNow },
                new() { SkillId = 2, ProgenyId = TestProgenyId, Name = "Oldest", SkillFirstObservation = DateTime.UtcNow.AddDays(-30) },
                new() { SkillId = 3, ProgenyId = TestProgenyId, Name = "Middle", SkillFirstObservation = DateTime.UtcNow.AddDays(-15) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(allSkills);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(10, 1, TestProgenyId, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Equal("Oldest", page.SkillsList[0].Name);
            Assert.Equal("Middle", page.SkillsList[1].Name);
            Assert.Equal("Newest", page.SkillsList[2].Name);
            Assert.Equal(1, page.SkillsList[0].SkillNumber);
            Assert.Equal(2, page.SkillsList[1].SkillNumber);
            Assert.Equal(3, page.SkillsList[2].SkillNumber);
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Sort_Newest_First_When_SortBy_One()
        {
            // Arrange
            List<Skill> allSkills =
            [
                new() { SkillId = 1, ProgenyId = TestProgenyId, Name = "Newest", SkillFirstObservation = DateTime.UtcNow },
                new() { SkillId = 2, ProgenyId = TestProgenyId, Name = "Oldest", SkillFirstObservation = DateTime.UtcNow.AddDays(-30) },
                new() { SkillId = 3, ProgenyId = TestProgenyId, Name = "Middle", SkillFirstObservation = DateTime.UtcNow.AddDays(-15) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(allSkills);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(10, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Equal("Newest", page.SkillsList[0].Name);
            Assert.Equal("Middle", page.SkillsList[1].Name);
            Assert.Equal("Oldest", page.SkillsList[2].Name);
            Assert.Equal(3, page.SkillsList[0].SkillNumber);
            Assert.Equal(2, page.SkillsList[1].SkillNumber);
            Assert.Equal(1, page.SkillsList[2].SkillNumber);
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Return_Second_Page_Correctly()
        {
            // Arrange
            List<Skill> allSkills = [];
            for (int i = 1; i <= 20; i++)
            {
                allSkills.Add(new Skill
                {
                    SkillId = i,
                    ProgenyId = TestProgenyId,
                    Name = $"Skill {i}",
                    SkillFirstObservation = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(allSkills);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(8, 2, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Equal(8, page.SkillsList.Count);
            Assert.Equal(2, page.PageNumber);
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Use_Default_PageSize_When_Not_Provided()
        {
            // Arrange
            List<Skill> allSkills = [];
            for (int i = 1; i <= 20; i++)
            {
                allSkills.Add(new Skill
                {
                    SkillId = i,
                    ProgenyId = TestProgenyId,
                    Name = $"Skill {i}",
                    SkillFirstObservation = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync(allSkills);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Equal(8, page.SkillsList.Count); // Default page size
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Use_DefaultChildId_When_ProgenyId_Not_Provided()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(Constants.DefaultChildId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetSkillsListPage();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockSkillService.Verify(x => x.GetSkillsList(Constants.DefaultChildId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetSkillsListPage_Should_Return_Empty_Page_When_No_Skills()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSkillService.Setup(x => x.GetSkillsList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetSkillsListPage(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SkillsListPage page = Assert.IsType<SkillsListPage>(okResult.Value);
            Assert.Empty(page.SkillsList);
            Assert.Equal(0, page.TotalPages);
        }

        #endregion
    }
}