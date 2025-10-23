//using KinaUna.Data;
//using KinaUna.Data.Contexts;
//using KinaUna.Data.Models;
//using KinaUna.Data.Models.AccessManagement;
//using KinaUnaProgenyApi.Services;
//using KinaUnaProgenyApi.Services.AccessManagementService;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Options;
//using Moq;

//namespace KinaUnaProgenyApi.Tests.Services
//{
//    public class ProgenyServiceTests
//    {
//        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
//        private readonly Mock<IImageStore> _mockImageStore = new();
//        private readonly Mock<ILocationService> _mockLocationService = new();

//        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
//        {
//            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
//                .UseInMemoryDatabase(databaseName: dbName)
//                .Options;
//            return new ProgenyDbContext(options);
//        }

//        private static IDistributedCache GetMemoryCache()
//        {
//            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
//            return new MemoryDistributedCache(options);
//        }

//        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com", string email = "testuser@test.com")
//        {
//            return new UserInfo
//            {
//                UserId = userId,
//                UserEmail = email
//            };
//        }

//        #region GetProgeny Tests

//        [Fact]
//        public async Task GetProgeny_Should_Return_Progeny_When_User_Has_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgeny_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone,
//                Email = "child@test.com"
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetProgenyPermissionForUser(1, userInfo))
//                .ReturnsAsync(new ProgenyPermission { PermissionLevel = PermissionLevel.View });

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.GetProgeny(1, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.Id);
//            Assert.Equal("Test Child", result.Name);
//            Assert.Equal("Testy", result.NickName);
//        }

//        [Fact]
//        public async Task GetProgeny_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgeny_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.GetProgeny(1, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task GetProgeny_Should_Return_Progeny_From_Cache_On_Second_Call()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgeny_Cache");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetProgenyPermissionForUser(1, userInfo))
//                .ReturnsAsync(new ProgenyPermission { PermissionLevel = PermissionLevel.View });

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? firstResult = await service.GetProgeny(1, userInfo);
//            Progeny? secondResult = await service.GetProgeny(1, userInfo);

//            // Assert
//            Assert.NotNull(firstResult);
//            Assert.NotNull(secondResult);
//            Assert.Equal(firstResult.Name, secondResult.Name);
//        }

//        [Fact]
//        public async Task GetProgeny_Should_Return_Null_When_Progeny_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgeny_NotFound");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.GetProgeny(999, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region AddProgeny Tests

//        [Fact]
//        public async Task AddProgeny_Should_Add_Progeny_To_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgeny_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "New Child",
//                NickName = "Newbie",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone,
//                CreatedBy = "testuser@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.GrantProgenyPermission(It.IsAny<ProgenyPermission>(), userInfo))
//                .ReturnsAsync((ProgenyPermission p, UserInfo _) => p);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.AddProgeny(progeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEqual(0, result.Id);
//            Assert.Equal("New Child", result.Name);
//            Assert.Equal(DateTime.UtcNow.Date, result.CreatedTime.Date);
//            Assert.Equal(DateTime.UtcNow.Date, result.ModifiedTime.Date);

//            Progeny? dbProgeny = await context.ProgenyDb.FindAsync(result.Id);
//            Assert.NotNull(dbProgeny);
//            Assert.Equal("New Child", dbProgeny.Name);
//        }

//        [Fact]
//        public async Task AddProgeny_Should_Set_UserId_When_Email_Matches_UserInfo()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgeny_WithUserId");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            UserInfo existingUser = new()
//            {
//                UserId = "child-user-id",
//                UserEmail = "child@test.com"
//            };
//            context.UserInfoDb.Add(existingUser);
//            await context.SaveChangesAsync();

//            Progeny progeny = new()
//            {
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "New Child",
//                NickName = "Newbie",
//                Email = "child@test.com",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone,
//                CreatedBy = "testuser@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.GrantProgenyPermission(It.IsAny<ProgenyPermission>(), userInfo))
//                .ReturnsAsync((ProgenyPermission p, UserInfo _) => p);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.AddProgeny(progeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("child-user-id", result.UserId);
//        }

//        [Fact]
//        public async Task AddProgeny_Should_Grant_Admin_Permissions_To_Admins()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgeny_AdminPermissions");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            UserInfo adminUser = new()
//            {
//                UserId = "admin-user-id",
//                UserEmail = "admin@test.com"
//            };
//            context.UserInfoDb.Add(adminUser);
//            await context.SaveChangesAsync();

//            Progeny progeny = new()
//            {
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin@test.com",
//                Name = "New Child",
//                NickName = "Newbie",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone,
//                CreatedBy = "testuser@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.GrantProgenyPermission(It.IsAny<ProgenyPermission>(), userInfo))
//                .ReturnsAsync((ProgenyPermission p, UserInfo _) => p);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.AddProgeny(progeny, userInfo);

//            // Assert
//            _mockAccessManagementService.Verify(
//                x => x.GrantProgenyPermission(
//                    It.Is<ProgenyPermission>(p => p.Email == "admin@test.com" && p.PermissionLevel == PermissionLevel.Admin),
//                    userInfo),
//                Times.Once);
//        }

//        [Fact]
//        public async Task AddProgeny_Should_Grant_View_Permission_To_Progeny_User()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgeny_ProgenyPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            UserInfo childUser = new()
//            {
//                UserId = "child-user-id",
//                UserEmail = "child@test.com"
//            };
//            context.UserInfoDb.Add(childUser);
//            await context.SaveChangesAsync();

//            Progeny progeny = new()
//            {
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "New Child",
//                NickName = "Newbie",
//                Email = "child@test.com",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone,
//                CreatedBy = "testuser@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.GrantProgenyPermission(It.IsAny<ProgenyPermission>(), userInfo))
//                .ReturnsAsync((ProgenyPermission p, UserInfo _) => p);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.AddProgeny(progeny, userInfo);

//            // Assert
//            _mockAccessManagementService.Verify(
//                x => x.GrantProgenyPermission(
//                    It.Is<ProgenyPermission>(p => p.Email == "child@test.com" && p.PermissionLevel == PermissionLevel.View),
//                    userInfo),
//                Times.Once);
//        }

//        #endregion

//        #region UpdateProgeny Tests

//        [Fact]
//        public async Task UpdateProgeny_Should_Update_Progeny_In_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgeny_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Old Name",
//                NickName = "OldNick",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();
//            context.Entry(progeny).State = EntityState.Detached;

//            Progeny updatedProgeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "New Name",
//                NickName = "NewNick",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.UpdateProgeny(updatedProgeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("New Name", result.Name);
//            Assert.Equal("NewNick", result.NickName);
//            Assert.Equal(userInfo.UserId, result.ModifiedBy);
//        }

//        [Fact]
//        public async Task UpdateProgeny_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgeny_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.UpdateProgeny(progeny, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateProgeny_Should_Return_Null_When_Progeny_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgeny_NotFound");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 999,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.UpdateProgeny(progeny, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateProgeny_Should_Update_Admins_Permissions_When_Admins_Changed()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgeny_AdminsChanged");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin1@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();
//            context.Entry(progeny).State = EntityState.Detached;

//            Progeny updatedProgeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin1@test.com,admin2@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.ProgenyAdminsUpdated(1))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.UpdateProgeny(updatedProgeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            _mockAccessManagementService.Verify(x => x.ProgenyAdminsUpdated(1), Times.Once);
//        }

//        [Fact]
//        public async Task UpdateProgeny_Should_Resize_Image_When_Picture_Is_Local()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgeny_ResizeImage");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = "local-image.jpg",
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();
//            context.Entry(progeny).State = EntityState.Detached;

//            Progeny updatedProgeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = "new-local-image.jpg",
//                TimeZone = Constants.DefaultTimezone
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockImageStore
//                .Setup(x => x.GetStream("new-local-image.jpg", BlobContainers.Progeny))
//                .ReturnsAsync((MemoryStream)null!);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.UpdateProgeny(updatedProgeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//        }

//        #endregion

//        #region DeleteProgeny Tests

//        [Fact]
//        public async Task DeleteProgeny_Should_Delete_Progeny_From_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgeny_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockImageStore
//                .Setup(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()))
//                .ReturnsAsync(Constants.ProfilePictureUrl);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.DeleteProgeny(progeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.Id);

//            Progeny? dbProgeny = await context.ProgenyDb.FindAsync(1);
//            Assert.Null(dbProgeny);
//        }

//        [Fact]
//        public async Task DeleteProgeny_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgeny_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.DeleteProgeny(progeny, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task DeleteProgeny_Should_Return_Null_When_Progeny_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgeny_NotFound");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 999,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.DeleteProgeny(progeny, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task DeleteProgeny_Should_Delete_Picture_From_ImageStore()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgeny_DeletePicture");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = "custom-picture.jpg",
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockImageStore
//                .Setup(x => x.DeleteImage("custom-picture.jpg", "progeny"))
//                .ReturnsAsync("custom-picture.jpg");

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            Progeny? result = await service.DeleteProgeny(progeny, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            _mockImageStore.Verify(x => x.DeleteImage("custom-picture.jpg", "progeny"), Times.Once);
//        }

//        #endregion

//        #region GetProgenyInfo Tests

//        [Fact]
//        public async Task GetProgenyInfo_Should_Return_ProgenyInfo_When_Exists()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgenyInfo_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            Address address = new() { AddressId = 1 };
//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                AddressIdNumber = 1,
//                Email = "info@test.com",
//                Notes = "Test notes"
//            };

//            context.ProgenyDb.Add(progeny);
//            context.ProgenyInfoDb.Add(progenyInfo);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetProgenyPermissionForUser(1, userInfo))
//                .ReturnsAsync(new ProgenyPermission { PermissionLevel = PermissionLevel.View });

//            _mockLocationService
//                .Setup(x => x.GetAddressItem(1))
//                .ReturnsAsync(address);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.GetProgenyInfo(1, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.ProgenyId);
//            Assert.Equal("info@test.com", result.Email);
//            Assert.Equal("Test notes", result.Notes);
//            Assert.NotNull(result.Address);
//        }

//        [Fact]
//        public async Task GetProgenyInfo_Should_Create_New_ProgenyInfo_When_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgenyInfo_Create");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Progeny progeny = new()
//            {
//                Id = 1,
//                BirthDay = DateTime.Now.AddYears(-5),
//                Admins = "testuser@test.com",
//                Name = "Test Child",
//                NickName = "Testy",
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            Address address = new() { AddressId = 1 };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetProgenyPermissionForUser(1, userInfo))
//                .ReturnsAsync(new ProgenyPermission { PermissionLevel = PermissionLevel.Admin });

//            _mockLocationService
//                .Setup(x => x.AddAddressItem(It.IsAny<Address>()))
//                .ReturnsAsync(address);

//            _mockLocationService
//                .Setup(x => x.GetAddressItem(1))
//                .ReturnsAsync(address);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.GetProgenyInfo(1, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.ProgenyId);
//            Assert.NotNull(result.Address);
//        }

//        [Fact]
//        public async Task GetProgenyInfo_Should_Return_Null_When_Progeny_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("GetProgenyInfo_NoProgeny");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.GetProgenyInfo(999, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region AddProgenyInfo Tests

//        [Fact]
//        public async Task AddProgenyInfo_Should_Add_ProgenyInfo_To_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgenyInfo_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Address address = new() { AddressId = 1, City = "Test City" };
//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyId = 1,
//                Email = "info@test.com",
//                Notes = "Test notes",
//                Address = address
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockLocationService
//                .Setup(x => x.AddAddressItem(It.IsAny<Address>()))
//                .ReturnsAsync(address);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.AddProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEqual(0, result.ProgenyInfoId);
//            Assert.Equal(1, result.ProgenyId);
//            Assert.Equal("info@test.com", result.Email);
//        }

//        [Fact]
//        public async Task AddProgenyInfo_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgenyInfo_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyId = 1,
//                Email = "info@test.com",
//                Notes = "Test notes"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.AddProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task AddProgenyInfo_Should_Create_Address_When_Not_Provided()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("AddProgenyInfo_CreateAddress");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyId = 1,
//                Email = "info@test.com",
//                Notes = "Test notes"
//            };

//            Address address = new() { AddressId = 1 };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockLocationService
//                .Setup(x => x.AddAddressItem(It.IsAny<Address>()))
//                .ReturnsAsync(address);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.AddProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.AddressIdNumber);
//        }

//        #endregion

//        #region UpdateProgenyInfo Tests

//        [Fact]
//        public async Task UpdateProgenyInfo_Should_Update_ProgenyInfo_In_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgenyInfo_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            Address address = new() { AddressId = 1, City = "Old City" };
//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                AddressIdNumber = 1,
//                Email = "old@test.com",
//                Notes = "Old notes"
//            };

//            context.ProgenyInfoDb.Add(progenyInfo);
//            await context.SaveChangesAsync();
//            context.Entry(progenyInfo).State = EntityState.Detached;

//            Address updatedAddress = new() { AddressId = 1, City = "New City" };
//            ProgenyInfo updatedProgenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                AddressIdNumber = 1,
//                Email = "new@test.com",
//                Notes = "New notes",
//                Address = updatedAddress,
//                ModifiedBy = "testuser@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            _mockLocationService
//                .Setup(x => x.GetAddressItem(1))
//                .ReturnsAsync(address);

//            _mockLocationService
//                .Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
//                .ReturnsAsync(updatedAddress);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.UpdateProgenyInfo(updatedProgenyInfo, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("new@test.com", result.Email);
//            Assert.Equal("New notes", result.Notes);
//        }

//        [Fact]
//        public async Task UpdateProgenyInfo_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgenyInfo_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                Email = "info@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.UpdateProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateProgenyInfo_Should_Return_Null_When_ProgenyInfo_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgenyInfo_NotFound");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyId = 999,
//                Email = "info@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.UpdateProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region DeleteProgenyInfo Tests

//        [Fact]
//        public async Task DeleteProgenyInfo_Should_Delete_ProgenyInfo_From_Database()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgenyInfo_Valid");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                AddressIdNumber = 1,
//                Email = "info@test.com"
//            };

//            context.ProgenyInfoDb.Add(progenyInfo);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            //_mockLocationService
//            //    .Setup(x => x.RemoveAddressItem(1))
//            //    .ReturnsAsync();

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.DeleteProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.ProgenyId);

//            ProgenyInfo? dbProgenyInfo = await context.ProgenyInfoDb.FindAsync(1);
//            Assert.Null(dbProgenyInfo);
//        }

//        [Fact]
//        public async Task DeleteProgenyInfo_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgenyInfo_NoPermission");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                Email = "info@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.DeleteProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task DeleteProgenyInfo_Should_Return_Null_When_ProgenyInfo_Does_Not_Exist()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgenyInfo_NotFound");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyId = 999,
//                Email = "info@test.com"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(999, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.DeleteProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task DeleteProgenyInfo_Should_Delete_Associated_Address()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("DeleteProgenyInfo_DeleteAddress");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo();

//            ProgenyInfo progenyInfo = new()
//            {
//                ProgenyInfoId = 1,
//                ProgenyId = 1,
//                AddressIdNumber = 1,
//                Email = "info@test.com"
//            };

//            context.ProgenyInfoDb.Add(progenyInfo);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            //_mockLocationService
//            //    .Setup(x => x.RemoveAddressItem(1))
//            //    .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            ProgenyInfo? result = await service.DeleteProgenyInfo(progenyInfo, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            _mockLocationService.Verify(x => x.RemoveAddressItem(1), Times.Once);
//        }

//        #endregion

//        #region ChangeUsersEmailForProgenies Tests

//        [Fact]
//        public async Task ChangeUsersEmailForProgenies_Should_Update_Email_For_Progeny_User()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("ChangeEmail_ProgenyUser");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo("user1", "old@test.com");

//            Progeny progeny = new()
//            {
//                Id = 1,
//                Email = "old@test.com",
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                BirthDay = DateTime.Now.AddYears(-5),
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.UserInfoDb.Add(userInfo);
//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.ChangeUsersEmailForProgenies(userInfo, "new@test.com");

//            // Assert
//            Progeny? updatedProgeny = await context.ProgenyDb.FindAsync(1);
//            Assert.NotNull(updatedProgeny);
//            Assert.Equal("new@test.com", updatedProgeny.Email);
//        }

//        [Fact]
//        public async Task ChangeUsersEmailForProgenies_Should_Update_Email_In_Admin_List()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("ChangeEmail_AdminList");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo("user1", "old@test.com");

//            Progeny progeny = new()
//            {
//                Id = 1,
//                Email = "child@test.com",
//                Admins = "old@test.com,other@test.com",
//                Name = "Test Child",
//                BirthDay = DateTime.Now.AddYears(-5),
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.UserInfoDb.Add(userInfo);
//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.ChangeUsersEmailForProgenies(userInfo, "new@test.com");

//            // Assert
//            Progeny? updatedProgeny = await context.ProgenyDb.FindAsync(1);
//            Assert.NotNull(updatedProgeny);
//            Assert.Contains("new@test.com", updatedProgeny.Admins);
//            Assert.DoesNotContain("old@test.com", updatedProgeny.Admins);
//        }

//        #endregion

//        #region UpdateProgeniesForNewUser Tests

//        [Fact]
//        public async Task UpdateProgeniesForNewUser_Should_Set_UserId_When_Email_Matches()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgenies_NewUser");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo("new-user-id", "child@test.com");

//            Progeny progeny = new()
//            {
//                Id = 1,
//                Email = "child@test.com",
//                UserId = string.Empty,
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                BirthDay = DateTime.Now.AddYears(-5),
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.UserInfoDb.Add(userInfo);
//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.UpdateProgeniesForNewUser(userInfo);

//            // Assert
//            Progeny? updatedProgeny = await context.ProgenyDb.FindAsync(1);
//            Assert.NotNull(updatedProgeny);
//            Assert.Equal("new-user-id", updatedProgeny.UserId);
//        }

//        [Fact]
//        public async Task UpdateProgeniesForNewUser_Should_Not_Update_When_Email_Does_Not_Match()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("UpdateProgenies_NoMatch");
//            IDistributedCache cache = GetMemoryCache();
//            UserInfo userInfo = CreateTestUserInfo("new-user-id", "different@test.com");

//            Progeny progeny = new()
//            {
//                Id = 1,
//                Email = "child@test.com",
//                UserId = "existing-user-id",
//                Admins = "admin@test.com",
//                Name = "Test Child",
//                BirthDay = DateTime.Now.AddYears(-5),
//                PictureLink = Constants.ProfilePictureUrl,
//                TimeZone = Constants.DefaultTimezone
//            };

//            context.UserInfoDb.Add(userInfo);
//            context.ProgenyDb.Add(progeny);
//            await context.SaveChangesAsync();

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            await service.UpdateProgeniesForNewUser(userInfo);

//            // Assert
//            Progeny? updatedProgeny = await context.ProgenyDb.FindAsync(1);
//            Assert.NotNull(updatedProgeny);
//            Assert.Equal("existing-user-id", updatedProgeny.UserId);
//        }

//        #endregion

//        #region ResizeImage Tests

//        [Fact]
//        public async Task ResizeImage_Should_Return_Original_ImageId_When_Stream_Is_Null()
//        {
//            // Arrange
//            await using ProgenyDbContext context = GetInMemoryDbContext("ResizeImage_NullStream");
//            IDistributedCache cache = GetMemoryCache();

//            _mockImageStore
//                .Setup(x => x.GetStream("image.jpg", BlobContainers.Progeny))
//                .ReturnsAsync((MemoryStream)null!);

//            ProgenyService service = new(context, cache, _mockImageStore.Object, _mockLocationService.Object, _mockAccessManagementService.Object);

//            // Act
//            string? result = await service.ResizeImage("image.jpg");

//            // Assert
//            Assert.Equal("image.jpg", result);
//        }

//        #endregion
//    }
//}