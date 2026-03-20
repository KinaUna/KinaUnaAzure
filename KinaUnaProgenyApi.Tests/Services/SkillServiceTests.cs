using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class SkillServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IKinaUnaCacheService> _mockKinaUnaCacheService;

        public SkillServiceTests()
        {
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockKinaUnaCacheService = new Mock<IKinaUnaCacheService>();
        }

        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        private static Skill CreateTestSkill(int skillId = 1, int progenyId = 1)
        {
            return new Skill
            {
                SkillId = skillId,
                ProgenyId = progenyId,
                Name = "Test Skill",
                Description = "Test Description",
                Category = "Test Category",
                SkillFirstObservation = DateTime.UtcNow.AddDays(-10),
                SkillAddedDate = DateTime.UtcNow,
                Author = "testuser@test.com",
                CreatedBy = "testuser@test.com",
                CreatedTime = DateTime.UtcNow
            };
        }

        private void SetupKinaUnaCacheServiceMocks()
        {
            _mockKinaUnaCacheService
                .Setup(x => x.GetSkillsListCache(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((SkillsListCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.GetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .ReturnsAsync((TimelineUpdatedCacheEntry)null!);

            _mockKinaUnaCacheService
                .Setup(x => x.SetSkillsListCache(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Skill[]>()))
                .Returns(Task.CompletedTask);

            _mockKinaUnaCacheService
                .Setup(x => x.SetProgenyOrFamilyTimelineUpdatedCache(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<KinaUnaTypes.TimeLineType>()))
                .Returns(Task.CompletedTask);
        }

        #region GetSkill Tests

        [Fact]
        public async Task GetSkill_Should_Return_Skill_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkill_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.GetSkill(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SkillId);
            Assert.Equal("Test Skill", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("Test Category", result.Category);
            Assert.NotNull(result.ItemPerMission);
            Assert.Equal(PermissionLevel.View, result.ItemPerMission.PermissionLevel);
        }

        [Fact]
        public async Task GetSkill_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkill_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.GetSkill(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSkill_Should_Return_Null_When_Skill_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkill_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.GetSkill(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSkill_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkill_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result1 = await service.GetSkill(1, userInfo);
            Skill? result2 = await service.GetSkill(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.SkillId, result2.SkillId);
            Assert.Equal(result1.Name, result2.Name);
        }

        #endregion

        #region AddSkill Tests

        [Fact]
        public async Task AddSkill_Should_Add_Skill_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddSkill_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skillToAdd = CreateTestSkill(0);
            skillToAdd.ItemPermissionsDtoList = [];

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(
                    KinaUnaTypes.TimeLineType.Skill,
                    It.IsAny<int>(),
                    1,
                    0,
                    It.IsAny<List<ItemPermissionDto>>(),
                    userInfo))
                .Returns(Task.CompletedTask);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.AddSkill(skillToAdd, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.SkillId);
            Assert.Equal("Test Skill", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("Test Category", result.Category);
            Assert.Equal(1, result.ProgenyId);

            Skill? dbSkill = await context.SkillsDb.FindAsync(result.SkillId);
            Assert.NotNull(dbSkill);
            Assert.Equal(result.SkillId, dbSkill.SkillId);
        }

        [Fact]
        public async Task AddSkill_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddSkill_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skillToAdd = CreateTestSkill(0);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.AddSkill(skillToAdd, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.SkillsDb);
        }

        [Fact]
        public async Task AddSkill_Should_Set_Created_And_Modified_Times()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("AddSkill_Timestamps");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skillToAdd = CreateTestSkill(0);
            skillToAdd.ItemPermissionsDtoList = new List<ItemPermissionDto>();
            DateTime beforeAdd = DateTime.UtcNow;

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(
                    KinaUnaTypes.TimeLineType.Skill,
                    It.IsAny<int>(),
                    1,
                    0,
                    It.IsAny<List<ItemPermissionDto>>(),
                    userInfo))
                .Returns(Task.CompletedTask);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.AddSkill(skillToAdd, userInfo);
            DateTime afterAdd = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedTime >= beforeAdd && result.CreatedTime <= afterAdd);
            Assert.True(result.ModifiedTime >= beforeAdd && result.ModifiedTime <= afterAdd);
        }

        #endregion

        #region UpdateSkill Tests

        [Fact]
        public async Task UpdateSkill_Should_Update_Skill_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateSkill_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();
            context.Entry(skill).State = EntityState.Detached;

            Skill updatedSkill = CreateTestSkill();
            updatedSkill.Name = "Updated Name";
            updatedSkill.Description = "Updated Description";
            updatedSkill.Category = "Updated Category";
            updatedSkill.ItemPermissionsDtoList = new List<ItemPermissionDto>();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(
                    KinaUnaTypes.TimeLineType.Skill,
                    1,
                    1,
                    0,
                    It.IsAny<List<ItemPermissionDto>>(),
                    userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.UpdateSkill(updatedSkill, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("Updated Category", result.Category);

            Skill? dbSkill = await context.SkillsDb.FindAsync(1);
            Assert.NotNull(dbSkill);
            Assert.Equal("Updated Name", dbSkill.Name);
        }

        [Fact]
        public async Task UpdateSkill_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateSkill_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            Skill updatedSkill = CreateTestSkill();
            updatedSkill.Name = "Updated Name";

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.UpdateSkill(updatedSkill, userInfo);

            // Assert
            Assert.Null(result);

            Skill? dbSkill = await context.SkillsDb.FindAsync(1);
            Assert.Equal("Test Skill", dbSkill!.Name); // Name should not be updated
        }

        [Fact]
        public async Task UpdateSkill_Should_Return_Null_When_Skill_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateSkill_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill updatedSkill = CreateTestSkill(999);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.UpdateSkill(updatedSkill, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSkill_Should_Update_ModifiedTime()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateSkill_ModifiedTime");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();
            DateTime originalModifiedTime = skill.ModifiedTime;

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();
            context.Entry(skill).State = EntityState.Detached;

            await Task.Delay(10); // Ensure time difference

            Skill updatedSkill = CreateTestSkill();
            updatedSkill.Name = "Updated Name";
            updatedSkill.ItemPermissionsDtoList = new List<ItemPermissionDto>();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(
                    KinaUnaTypes.TimeLineType.Skill,
                    1,
                    1,
                    0,
                    It.IsAny<List<ItemPermissionDto>>(),
                    userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.UpdateSkill(updatedSkill, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ModifiedTime > originalModifiedTime);
        }

        #endregion

        #region DeleteSkill Tests

        [Fact]
        public async Task DeleteSkill_Should_Delete_Skill_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteSkill_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.DeleteSkill(skill, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SkillId);

            Skill? dbSkill = await context.SkillsDb.FindAsync(1);
            Assert.Null(dbSkill);
        }

        [Fact]
        public async Task DeleteSkill_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteSkill_NoPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.DeleteSkill(skill, userInfo);

            // Assert
            Assert.Null(result);

            Skill? dbSkill = await context.SkillsDb.FindAsync(1);
            Assert.NotNull(dbSkill); // Skill should still exist
        }

        [Fact]
        public async Task DeleteSkill_Should_Return_Null_When_Skill_Does_Not_Exist()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteSkill_NotFound");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill(999);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            Skill? result = await service.DeleteSkill(skill, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteSkill_Should_Remove_From_Cache()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteSkill_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();
            Skill skill = CreateTestSkill();

            context.SkillsDb.Add(skill);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            _mockAccessManagementService
                .Setup(x => x.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, 1, userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Pre-load cache
            await service.GetSkill(1, userInfo);

            // Act
            await service.DeleteSkill(skill, userInfo);

            // Assert
            string? cachedValue = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + 1);
            Assert.Null(cachedValue);
        }

        #endregion

        #region GetSkillsList Tests

        [Fact]
        public async Task GetSkillsList_Should_Return_List_Of_Skills_When_User_Has_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsList_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            Skill skill2 = CreateTestSkill(2);
            skill2.Name = "Second Skill";

            context.SkillsDb.AddRange(skill1, skill2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "Test Skill");
            Assert.Contains(result, s => s.Name == "Second Skill");
        }

        [Fact]
        public async Task GetSkillsList_Should_Return_Empty_List_When_Progeny_Has_No_Skills()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsList_Empty");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSkillsList_Should_Filter_By_Progeny()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsList_FilterProgeny");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            Skill skill2 = CreateTestSkill(2, 2);

            context.SkillsDb.AddRange(skill1, skill2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].ProgenyId);
        }

        [Fact]
        public async Task GetSkillsList_Should_Filter_By_Permission()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsList_FilterPermission");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            Skill skill2 = CreateTestSkill(2);

            context.SkillsDb.AddRange(skill1, skill2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].SkillId);
        }

        [Fact]
        public async Task GetSkillsList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsList_Cache");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            context.SkillsDb.Add(skill1);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result1 = await service.GetSkillsList(1, userInfo);

            // Add another skill after first call
            Skill skill2 = CreateTestSkill(2);
            context.SkillsDb.Add(skill2);
            await context.SaveChangesAsync();

            List<Skill>? result2 = await service.GetSkillsList(1, userInfo);

            // Assert - Should still return 1 item from cache
            Assert.Single(result1);
            Assert.Single(result2);
        }

        #endregion

        #region GetSkillsWithCategory Tests

        [Fact]
        public async Task GetSkillsWithCategory_Should_Return_Skills_Matching_Category()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsWithCategory_Valid");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            skill1.Category = "Physical";
            Skill skill2 = CreateTestSkill(2);
            skill2.Category = "Cognitive";
            Skill skill3 = CreateTestSkill(3);
            skill3.Category = "Physical Development";

            context.SkillsDb.AddRange(skill1, skill2, skill3);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsWithCategory(1, "Physical", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Contains("Physical", s.Category, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetSkillsWithCategory_Should_Return_Empty_List_When_No_Match()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsWithCategory_NoMatch");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            skill1.Category = "Physical";

            context.SkillsDb.Add(skill1);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsWithCategory(1, "Cognitive", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSkillsWithCategory_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsWithCategory_CaseInsensitive");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            skill1.Category = "Physical";

            context.SkillsDb.Add(skill1);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsWithCategory(1, "PHYSICAL", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Physical", result[0].Category);
        }

        [Fact]
        public async Task GetSkillsWithCategory_Should_Handle_Null_Category()
        {
            // Arrange
            await using ProgenyDbContext context = GetInMemoryDbContext("GetSkillsWithCategory_NullCategory");
            IDistributedCache cache = GetMemoryCache();
            UserInfo userInfo = CreateTestUserInfo();

            Skill skill1 = CreateTestSkill();
            skill1.Category = null;

            context.SkillsDb.Add(skill1);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SetupKinaUnaCacheServiceMocks();

            SkillService service = new(context, cache, _mockAccessManagementService.Object, _mockKinaUnaCacheService.Object);

            // Act
            List<Skill>? result = await service.GetSkillsWithCategory(1, "Physical", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}