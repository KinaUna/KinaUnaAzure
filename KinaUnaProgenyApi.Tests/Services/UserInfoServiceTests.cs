using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using KinaUna.Data;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class UserInfoServiceTests
    {
        private static ProgenyDbContext CreateContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache CreateMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(memoryCacheOptions);
        }

        private static UserInfoService CreateService(
            ProgenyDbContext context,
            IDistributedCache cache,
            Mock<IImageStore> imageStoreMock,
            out Mock<IProgenyService> progenyServiceMock,
            out Mock<IAccessManagementService> accessManagementServiceMock,
            out Mock<IUserGroupsService> userGroupsServiceMock,
            out Mock<IFamilyMembersService> familyMembersServiceMock)
        {
            progenyServiceMock = new Mock<IProgenyService>();
            accessManagementServiceMock = new Mock<IAccessManagementService>();
            userGroupsServiceMock = new Mock<IUserGroupsService>();
            familyMembersServiceMock = new Mock<IFamilyMembersService>();

            // Default setups for methods the service calls during add/update.
            progenyServiceMock.Setup(p => p.UpdateProgeniesForNewUser(It.IsAny<UserInfo>())).Returns(Task.CompletedTask);
            accessManagementServiceMock.Setup(a => a.UpdatePermissionsForNewUser(It.IsAny<UserInfo>())).Returns(Task.CompletedTask);
            userGroupsServiceMock.Setup(u => u.UpdateUserGroupMembersForNewUser(It.IsAny<UserInfo>())).Returns(Task.CompletedTask);
            familyMembersServiceMock.Setup(f => f.UpdateFamilyMembersForNewUser(It.IsAny<UserInfo>())).Returns(Task.CompletedTask);

            return new UserInfoService(context, cache, imageStoreMock.Object,
                progenyServiceMock.Object, accessManagementServiceMock.Object,
                userGroupsServiceMock.Object, familyMembersServiceMock.Object);
        }

        [Fact]
        public async Task GetAllUserInfos_Returns_All_Records()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);
            context.UserInfoDb.AddRange(new[]
            {
                new UserInfo { UserEmail = "a@a.com", UserId = "u1", UserName = "A" },
                new UserInfo { UserEmail = "b@b.com", UserId = "u2", UserName = "B" },
                new UserInfo { UserEmail = "c@c.com", UserId = "u3", UserName = "C" }
            });
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            List<UserInfo>? result = await service.GetAllUserInfos();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, u => u.UserEmail == "a@a.com");
        }

        [Fact]
        public async Task GetUserInfoByEmail_Returns_Object_And_Caches()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);
            UserInfo user = new() { UserEmail = "test@test.com", UserId = "uid1", UserName = "Tester" };
            context.UserInfoDb.Add(user);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? first = await service.GetUserInfoByEmail("test@test.com");
            UserInfo? second = await service.GetUserInfoByEmail("test@test.com"); // should hit cache path internally

            Assert.NotNull(first);
            Assert.Equal("test@test.com", first.UserEmail);
            Assert.NotNull(second);
            Assert.Equal(first.UserEmail, second.UserEmail);
        }

        [Fact]
        public async Task GetUserInfoByEmail_Returns_Null_When_NotFound()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? result = await service.GetUserInfoByEmail("no@no.com");
            Assert.Null(result);
        }

        [Fact]
        public async Task AddUserInfo_Saves_And_Calls_Dependent_Services()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out Mock<IProgenyService> progenyMock, out Mock<IAccessManagementService> accessMock, out Mock<IUserGroupsService> groupsMock, out Mock<IFamilyMembersService> familyMock);

            UserInfo newUser = new()
            {
                UserEmail = "new@user.com",
                UserId = "newid",
                UserName = "NewUser",
                FirstName = null, // null to exercise defaulting
                MiddleName = null,
                LastName = null,
                ProfilePicture = null,
                Timezone = null
            };

            UserInfo? added = await service.AddUserInfo(newUser);

            Assert.NotNull(added);
            Assert.NotEqual(0, added.Id);
            UserInfo? dbUser = await context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail == "new@user.com");
            Assert.NotNull(dbUser);

            progenyMock.Verify(p => p.UpdateProgeniesForNewUser(It.Is<UserInfo>(u => u.UserEmail == "new@user.com")), Times.Once);
            accessMock.Verify(a => a.UpdatePermissionsForNewUser(It.Is<UserInfo>(u => u.UserEmail == "new@user.com")), Times.Once);
            groupsMock.Verify(g => g.UpdateUserGroupMembersForNewUser(It.Is<UserInfo>(u => u.UserEmail == "new@user.com")), Times.Once);
            familyMock.Verify(f => f.UpdateFamilyMembersForNewUser(It.Is<UserInfo>(u => u.UserEmail == "new@user.com")), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfo_Updates_Record_And_Deletes_Old_Image()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo original = new()
            {
                UserEmail = "up@date.com",
                UserId = "upid",
                UserName = "Old",
                ProfilePicture = "oldlink"
            };
            context.UserInfoDb.Add(original);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            imgMock.Setup(i => i.DeleteImage(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("oldlink");

            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            // retrieve, modify and update
            UserInfo? toUpdate = await service.GetUserInfoByEmail("up@date.com");
            toUpdate.UserName = "New";
            toUpdate.ProfilePicture = "newlink";

            UserInfo? result = await service.UpdateUserInfo(toUpdate);

            Assert.NotNull(result);
            Assert.Equal("New", result.UserName);
            Assert.Equal("newlink", result.ProfilePicture);

            imgMock.Verify(i => i.DeleteImage("oldlink", BlobContainers.Profiles), Times.Once);
        }

        [Fact]
        public async Task DeleteUserInfo_Removes_From_Db_And_Deletes_Image_And_Removes_Cache()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo u = new()
            {
                UserEmail = "del@me.com",
                UserId = "delid",
                UserName = "DeleteMe",
                ProfilePicture = "todrop"
            };
            context.UserInfoDb.Add(u);
            await context.SaveChangesAsync();

            string userInfoSerialized = System.Text.Json.JsonSerializer.Serialize(u);
            IDistributedCache cache = CreateMemoryCache();
            // Seed cache entries matching keys used by service so RemoveUserInfoByEmail will clear them
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + u.UserEmail.ToUpper(), userInfoSerialized);
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + u.UserId, userInfoSerialized);
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + u.Id, userInfoSerialized);

            Mock<IImageStore> imgMock = new();
            imgMock.Setup(i => i.DeleteImage(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("todrop");

            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? fetched = await service.GetUserInfoByEmail("del@me.com");
            Assert.NotNull(fetched);

            UserInfo? deleted = await service.DeleteUserInfo(fetched);

            Assert.NotNull(deleted);
            UserInfo? dbEntry = await context.UserInfoDb.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fetched.Id);
            Assert.Null(dbEntry);

            // cache entries removed
            string? cachedByEmail = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + u.UserEmail.ToUpper());
            string? cachedByUserId = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + u.UserId);
            string? cachedById = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + u.Id);
            Assert.Null(cachedByEmail);
            Assert.Null(cachedByUserId);
            Assert.Null(cachedById);

            imgMock.Verify(i => i.DeleteImage("todrop", BlobContainers.Profiles), Times.Once);
        }

        [Fact]
        public async Task RemoveUserInfoByEmail_Removes_Cache_Keys()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            IDistributedCache cache = CreateMemoryCache();
            string email = "cache@me.com";
            string userId = "cacheid";
            int id = 42;

            string keyMail = Constants.AppName + Constants.ApiVersion + "userinfobymail" + email.ToUpper();
            string keyUserId = Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId;
            string keyId = Constants.AppName + Constants.ApiVersion + "userinfobyid" + id;

            await cache.SetStringAsync(keyMail, "x");
            await cache.SetStringAsync(keyUserId, "x");
            await cache.SetStringAsync(keyId, "x");

            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            await service.RemoveUserInfoByEmail(email, userId, id);

            Assert.Null(await cache.GetStringAsync(keyMail));
            Assert.Null(await cache.GetStringAsync(keyUserId));
            Assert.Null(await cache.GetStringAsync(keyId));
        }

        [Fact]
        public async Task GetUserInfoById_Returns_When_Exists_And_Null_When_Not()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo u = new() { UserEmail = "id@me.com", UserId = "id1", UserName = "I" };
            context.UserInfoDb.Add(u);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? ok = await service.GetUserInfoById(u.Id);
            Assert.NotNull(ok);
            Assert.Equal(u.UserEmail, ok.UserEmail);

            UserInfo? no = await service.GetUserInfoById(9999);
            Assert.Null(no);
        }

        [Fact]
        public async Task GetUserInfoByUserId_Returns_When_Exists_And_Null_When_Not()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo u = new() { UserEmail = "uid@me.com", UserId = "myuser", UserName = "U" };
            context.UserInfoDb.Add(u);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? ok = await service.GetUserInfoByUserId("myuser");
            Assert.NotNull(ok);
            Assert.Equal("myuser", ok.UserId);

            UserInfo? none = await service.GetUserInfoByUserId("no-such");
            Assert.Null(none);
        }

        [Fact]
        public async Task GetDeletedUserInfos_Returns_Only_Deleted()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            context.UserInfoDb.AddRange(new[]
            {
                new UserInfo { UserEmail = "a@a.com", UserId = "u1", Deleted = false },
                new UserInfo { UserEmail = "b@b.com", UserId = "u2", Deleted = true },
                new UserInfo { UserEmail = "c@c.com", UserId = "u3", Deleted = true }
            });
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            List<UserInfo>? deleted = await service.GetDeletedUserInfos();

            Assert.NotNull(deleted);
            Assert.Equal(2, deleted.Count);
            Assert.All(deleted, d => Assert.True(d.Deleted));
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_Adds_New_When_Not_Existing()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo user = new() { UserEmail = "du@du.com", UserId = "duid", UserName = "DeletedUser" };
            // No DeletedUsers entry present initially.

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? added = await service.AddUserInfoToDeletedUserInfos(user);

            Assert.NotNull(added);
            // The service stores serialized ProfilePicture (string) and populates fields; check that entry exists in DeletedUsers
            UserInfo? dbDeleted = await context.DeletedUsers.AsNoTracking().SingleOrDefaultAsync(d => d.UserId == "duid");
            Assert.NotNull(dbDeleted);
            Assert.Equal("du@du.com", dbDeleted.UserEmail);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_Updates_Existing()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo existing = new()
            {
                UserEmail = "ex@ex.com",
                UserId = "exist",
                UserName = "Existing",
                Deleted = true
            };
            context.DeletedUsers.Add(existing);
            await context.SaveChangesAsync();

            UserInfo user = new() { UserEmail = "ex@ex.com", UserId = "exist", UserName = "ExistingChanged" };

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? updated = await service.AddUserInfoToDeletedUserInfos(user);

            Assert.NotNull(updated);
            Assert.Equal("ExistingChanged", updated.UserName);
            UserInfo? db = await context.DeletedUsers.AsNoTracking().SingleOrDefaultAsync(d => d.UserId == "exist");
            Assert.NotNull(db);
            Assert.False(db.Deleted); // service sets Deleted = false in Update path
        }

        [Fact]
        public async Task RemoveUserInfoFromDeletedUserInfos_Removes_And_Returns_Object()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo toRemove = new() { UserEmail = "r@r.com", UserId = "rid", UserName = "R" };
            context.DeletedUsers.Add(toRemove);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            UserInfo? removed = await service.RemoveUserInfoFromDeletedUserInfos(toRemove);

            Assert.NotNull(removed);
            UserInfo? dbCheck = await context.DeletedUsers.AsNoTracking().SingleOrDefaultAsync(d => d.UserId == "rid");
            Assert.Null(dbCheck);
        }

        [Fact]
        public async Task UpdateDeletedUserInfo_Updates_Record_When_Exists()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo entry = new() { UserEmail = "u@u.com", UserId = "uid", Deleted = true, DeletedTime = DateTime.UtcNow.AddDays(-1), UpdatedTime = DateTime.UtcNow.AddDays(-1) };
            context.DeletedUsers.Add(entry);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            entry.Deleted = false;
            entry.DeletedTime = DateTime.UtcNow;
            entry.UpdatedTime = DateTime.UtcNow;

            UserInfo? updated = await service.UpdateDeletedUserInfo(entry);

            Assert.NotNull(updated);
            Assert.Equal(entry.Id, updated.Id);
            Assert.Equal(entry.Deleted, updated.Deleted);
        }

        [Fact]
        public async Task IsAdminUserId_Returns_Correct_Value()
        {
            string dbName = Guid.NewGuid().ToString();
            await using ProgenyDbContext context = CreateContext(dbName);

            UserInfo admin = new() { UserEmail = "a@a.com", UserId = "admin", IsKinaUnaAdmin = true };
            UserInfo non = new() { UserEmail = "b@b.com", UserId = "non", IsKinaUnaAdmin = false };
            context.UserInfoDb.AddRange(admin, non);
            await context.SaveChangesAsync();

            IDistributedCache cache = CreateMemoryCache();
            Mock<IImageStore> imgMock = new();
            UserInfoService service = CreateService(context, cache, imgMock, out _, out _, out _, out _);

            Assert.True(await service.IsAdminUserId("admin"));
            Assert.False(await service.IsAdminUserId("non"));
            Assert.False(await service.IsAdminUserId("no-such"));
        }
    }
}