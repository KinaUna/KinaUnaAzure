using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class UserAccessServiceTests
    {
        private static DbContextOptions<ProgenyDbContext> CreateDbOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        private static DbContextOptions<MediaDbContext> CreateMediaDbOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        private static IDistributedCache CreateMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(memoryCacheOptions);
        }

        #region GetProgenyUserIsAdmin Tests

        [Fact]
        public async Task GetProgenyUserIsAdmin_Should_Return_List_Of_Progeny_When_Email_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserIsAdmin_Valid_Email");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserIsAdmin_Valid_Email_Media"));

            Progeny progenyToAdd1 = new()
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child A",
                NickName = "A",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            Progeny progenyToAdd2 = new()
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child B",
                NickName = "B",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progenyToAdd1);
            context.Add(progenyToAdd2);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin("test@test.com");
            List<Progeny> progenyList2 = await userAccessService.GetProgenyUserIsAdmin("test@test.com"); // Test cached result.
            Progeny firstProgeny = progenyList.First();

            Assert.NotNull(progenyList);
            Assert.Equal(2, progenyList.Count);
            Assert.NotNull(progenyList2);
            Assert.Equal(2, progenyList2.Count);
            Assert.NotNull(firstProgeny);
            Assert.IsType<Progeny>(firstProgeny);
        }

        [Fact]
        public async Task GetProgenyUserIsAdmin_Should_Return_Empty_List_When_Email_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserIsAdmin_Invalid_Email");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserIsAdmin_Invalid_Email_Media"));

            Progeny progenyToAdd1 = new()
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child A",
                NickName = "A",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progenyToAdd1);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin("nonexistent@test.com");
            List<Progeny> progenyList2 = await userAccessService.GetProgenyUserIsAdmin("nonexistent@test.com"); // Test cached result.

            Assert.NotNull(progenyList);
            Assert.Empty(progenyList);
            Assert.NotNull(progenyList2);
            Assert.Empty(progenyList2);
        }

        #endregion

        #region GetProgenyUserAccessList Tests

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Return_List_When_Valid_And_User_Has_Access()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessList_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessList_Valid_Media"));

            UserAccess userAccessToAdd1 = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };
            UserAccess userAccessToAdd2 = new() { UserId = "User2", AccessLevel = 1, ProgenyId = 1 };

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<List<UserAccess>> accessList = await userAccessService.GetProgenyUserAccessList(1, "User1");
            CustomResult<List<UserAccess>> accessList2 = await userAccessService.GetProgenyUserAccessList(1, "User1"); // Test cached

            Assert.True(accessList.IsSuccess);
            Assert.NotNull(accessList.Value);
            Assert.Equal(2, accessList.Value.Count);
            Assert.True(accessList2.IsSuccess);
            Assert.Equal(2, accessList2.Value.Count);
        }

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Allow_SystemAccount()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessList_SystemAccount");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessList_SystemAccount_Media"));

            UserAccess userAccessToAdd1 = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };

            context.Add(userAccessToAdd1);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<List<UserAccess>> accessList = await userAccessService.GetProgenyUserAccessList(1, Constants.SystemAccountEmail);

            Assert.True(accessList.IsSuccess);
            Assert.NotNull(accessList.Value);
            Assert.Single(accessList.Value);
        }

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Return_Error_When_User_Not_Authorized()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessList_Unauthorized");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessList_Unauthorized_Media"));

            UserAccess userAccessToAdd1 = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };

            context.Add(userAccessToAdd1);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<List<UserAccess>> accessList = await userAccessService.GetProgenyUserAccessList(1, "UnauthorizedUser");

            Assert.False(accessList.IsSuccess);
            Assert.NotNull(accessList.Error);
            Assert.Contains("not authorized", accessList.Error.Message);
        }

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Allow_DefaultChild()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessList_DefaultChild");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessList_DefaultChild_Media"));

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<List<UserAccess>> accessList = await userAccessService.GetProgenyUserAccessList(Constants.DefaultChildId, "AnyUser");

            Assert.True(accessList.IsSuccess);
            Assert.NotNull(accessList.Value);
        }

        #endregion

        #region GetUsersUserAccessList Tests

        [Fact]
        public async Task GetUsersUserAccessList_Should_Return_List_When_User_Has_Access()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetUsersUserAccessList_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetUsersUserAccessList_Valid_Media"));

            UserAccess userAccessToAdd1 = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };
            UserAccess userAccessToAdd2 = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 2 };

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetUsersUserAccessList("User1");
            List<UserAccess> accessList2 = await userAccessService.GetUsersUserAccessList("User1"); // Test cached

            Assert.NotNull(accessList);
            Assert.Equal(2, accessList.Count);
            Assert.NotNull(accessList2);
            Assert.Equal(2, accessList2.Count);
        }

        [Fact]
        public async Task GetUsersUserAccessList_Should_Return_Empty_List_When_User_Has_No_Access()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetUsersUserAccessList_Empty");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetUsersUserAccessList_Empty_Media"));

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetUsersUserAccessList("NonExistentUser");

            Assert.NotNull(accessList);
            Assert.Empty(accessList);
        }

        #endregion

        #region GetUsersUserAdminAccessList Tests

        [Fact]
        public async Task GetUsersUserAdminAccessList_Should_Return_Only_Admin_Access()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetUsersUserAdminAccessList_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetUsersUserAdminAccessList_Valid_Media"));

            UserAccess adminAccess = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };
            UserAccess familyAccess = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 2 };

            context.Add(adminAccess);
            context.Add(familyAccess);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetUsersUserAdminAccessList("User1");

            Assert.NotNull(accessList);
            Assert.Single(accessList);
            Assert.Equal(0, accessList[0].AccessLevel);
        }

        #endregion

        #region GetUserAccess Tests

        [Fact]
        public async Task GetUserAccess_Should_Return_UserAccess_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetUserAccess_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetUserAccess_Valid_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccess = await userAccessService.GetUserAccess(1);
            UserAccess userAccess2 = await userAccessService.GetUserAccess(1); // Test cached

            Assert.NotNull(userAccess);
            Assert.Equal("User1", userAccess.UserId);
            Assert.NotNull(userAccess2);
            Assert.Equal("User1", userAccess2.UserId);
        }

        [Fact]
        public async Task GetUserAccess_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetUserAccess_Invalid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetUserAccess_Invalid_Media"));

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccess = await userAccessService.GetUserAccess(999);

            Assert.Null(userAccess);
        }

        #endregion

        #region AddUserAccess Tests

        [Fact]
        public async Task AddUserAccess_Should_Save_UserAccess()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("AddUserAccess_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("AddUserAccess_Valid_Media"));

            Progeny progeny = new()
            {
                Admins = "",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progeny);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccessToAdd = new()
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1,
                Progeny = progeny
            };

            UserAccess addedUserAccess = await userAccessService.AddUserAccess(userAccessToAdd);
            UserAccess? dbUserAccess = await context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(ua => ua.AccessId == addedUserAccess.AccessId);

            Assert.NotNull(addedUserAccess);
            Assert.NotEqual(0, addedUserAccess.AccessId);
            Assert.Equal("User1", addedUserAccess.UserId);
            Assert.NotNull(dbUserAccess);
            Assert.Equal("User1", dbUserAccess.UserId);
        }

        [Fact]
        public async Task AddUserAccess_Should_Replace_Existing_UserAccess_For_Same_User_And_Progeny()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("AddUserAccess_Replace");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("AddUserAccess_Replace_Media"));

            Progeny progeny = new()
            {
                Admins = "",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            UserAccess existingAccess = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 1 };

            context.Add(progeny);
            context.Add(existingAccess);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess newAccess = new()
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1,
                Progeny = progeny
            };

            UserAccess addedUserAccess = await userAccessService.AddUserAccess(newAccess);
            List<UserAccess> allAccess = await context.UserAccessDb.Where(ua => ua.UserId == "User1" && ua.ProgenyId == 1).ToListAsync();

            Assert.NotNull(addedUserAccess);
            Assert.Single(allAccess);
            Assert.Equal(0, allAccess[0].AccessLevel);
        }

        #endregion

        #region UpdateUserAccess Tests

        [Fact]
        public async Task UpdateUserAccess_Should_Update_Existing_UserAccess()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("UpdateUserAccess_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("UpdateUserAccess_Valid_Media"));

            Progeny progeny = new()
            {
                Admins = "",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 1 };

            context.Add(progeny);
            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccessToUpdate = await context.UserAccessDb.FirstAsync();
            userAccessToUpdate.AccessLevel = 5;
            userAccessToUpdate.Progeny = progeny;

            UserAccess updatedUserAccess = await userAccessService.UpdateUserAccess(userAccessToUpdate);
            UserAccess? dbUserAccess = await context.UserAccessDb.AsNoTracking().FirstOrDefaultAsync();

            Assert.NotNull(updatedUserAccess);
            Assert.Equal(5, updatedUserAccess.AccessLevel);
            Assert.NotNull(dbUserAccess);
            Assert.Equal(5, dbUserAccess.AccessLevel);
        }

        [Fact]
        public async Task UpdateUserAccess_Should_Return_Null_When_UserAccess_Not_Found()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("UpdateUserAccess_NotFound");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("UpdateUserAccess_NotFound_Media"));

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccessToUpdate = new() { AccessId = 999, UserId = "User1", AccessLevel = 5, ProgenyId = 1 };

            UserAccess? updatedUserAccess = await userAccessService.UpdateUserAccess(userAccessToUpdate);

            Assert.Null(updatedUserAccess);
        }

        #endregion

        #region RemoveUserAccess Tests

        [Fact]
        public async Task RemoveUserAccess_Should_Delete_UserAccess()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("RemoveUserAccess_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("RemoveUserAccess_Valid_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 1 };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            await userAccessService.RemoveUserAccess(1, 1, "User1");
            UserAccess? deletedUserAccess = await context.UserAccessDb.FirstOrDefaultAsync();

            Assert.Null(deletedUserAccess);
        }

        [Fact]
        public async Task RemoveUserAccess_Should_Remove_From_Admins_List_When_Admin()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("RemoveUserAccess_Admin");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("RemoveUserAccess_Admin_Media"));

            Progeny progeny = new()
            {
                Admins = "User1, User2",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };

            context.Add(progeny);
            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            await userAccessService.RemoveUserAccess(1, 1, "User1");
            Progeny? updatedProgeny = await context.ProgenyDb.FirstOrDefaultAsync();

            Assert.NotNull(updatedProgeny);
            Assert.DoesNotContain("User1", updatedProgeny.Admins);
        }

        #endregion

        #region GetProgenyUserAccessForUser Tests

        [Fact]
        public async Task GetProgenyUserAccessForUser_Should_Return_UserAccess_When_Exists()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessForUser_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessForUser_Valid_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 0, ProgenyId = 1 };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(1, "User1");
            UserAccess userAccess2 = await userAccessService.GetProgenyUserAccessForUser(1, "User1"); // Test cached

            Assert.NotNull(userAccess);
            Assert.Equal("User1", userAccess.UserId);
            Assert.NotNull(userAccess2);
        }

        [Fact]
        public async Task GetProgenyUserAccessForUser_Should_Return_Null_When_Not_Exists()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetProgenyUserAccessForUser_NotFound");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetProgenyUserAccessForUser_NotFound_Media"));

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(1, "NonExistentUser");

            Assert.Null(userAccess);
        }

        #endregion

        #region GetValidatedAccessLevel Tests

        [Fact]
        public async Task GetValidatedAccessLevel_Should_Return_AccessLevel_When_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetValidatedAccessLevel_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetValidatedAccessLevel_Valid_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 1, ProgenyId = 1 };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<int> result = await userAccessService.GetValidatedAccessLevel(1, "User1", null);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value);
        }

        [Fact]
        public async Task GetValidatedAccessLevel_Should_Return_Error_When_Item_AccessLevel_Too_Low()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetValidatedAccessLevel_TooLow");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetValidatedAccessLevel_TooLow_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 3, ProgenyId = 1 };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<int> result = await userAccessService.GetValidatedAccessLevel(1, "User1", 1);

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task GetValidatedAccessLevel_Should_Allow_DefaultChild()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("GetValidatedAccessLevel_DefaultChild");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("GetValidatedAccessLevel_DefaultChild_Media"));

            UserAccess userAccessToAdd = new() { UserId = "User1", AccessLevel = 5, ProgenyId = Constants.DefaultChildId };

            context.Add(userAccessToAdd);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            CustomResult<int> result = await userAccessService.GetValidatedAccessLevel(Constants.DefaultChildId, "User1", null);

            Assert.True(result.IsSuccess);
        }

        #endregion

        #region UpdateProgenyAdmins Tests

        [Fact]
        public async Task UpdateProgenyAdmins_Should_Update_Admins_List()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("UpdateProgenyAdmins_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("UpdateProgenyAdmins_Valid_Media"));

            Progeny progeny = new()
            {
                Admins = "User1",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progeny);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            Progeny progenyToUpdate = await context.ProgenyDb.FirstAsync();
            progenyToUpdate.Admins = "User1, User2";

            await userAccessService.UpdateProgenyAdmins(progenyToUpdate);
            Progeny updatedProgeny = await context.ProgenyDb.AsNoTracking().FirstAsync();

            Assert.NotNull(updatedProgeny);
            Assert.Equal("User1, User2", updatedProgeny.Admins);
        }

        #endregion

        #region IsUserInUserAccessList Tests

        [Fact]
        public void IsUserInUserAccessList_Should_Return_True_When_User_In_List()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("IsUserInUserAccessList_True");
            using ProgenyDbContext context = new(dbOptions);
            using MediaDbContext mediaContext = new(CreateMediaDbOptions("IsUserInUserAccessList_True_Media"));

            List<UserAccess> accessList = new()
            {
                new UserAccess { UserId = "user1@test.com", AccessLevel = 0, ProgenyId = 1 },
                new UserAccess { UserId = "user2@test.com", AccessLevel = 1, ProgenyId = 1 }
            };

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            bool result = userAccessService.IsUserInUserAccessList(accessList, "user1@test.com");

            Assert.True(result);
        }

        [Fact]
        public void IsUserInUserAccessList_Should_Return_False_When_User_Not_In_List()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("IsUserInUserAccessList_False");
            using ProgenyDbContext context = new(dbOptions);
            using MediaDbContext mediaContext = new(CreateMediaDbOptions("IsUserInUserAccessList_False_Media"));

            List<UserAccess> accessList = new()
            {
                new UserAccess { UserId = "user1@test.com", AccessLevel = 0, ProgenyId = 1 }
            };

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            bool result = userAccessService.IsUserInUserAccessList(accessList, "user3@test.com");

            Assert.False(result);
        }

        [Fact]
        public void IsUserInUserAccessList_Should_Return_True_For_SystemAccount()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("IsUserInUserAccessList_SystemAccount");
            using ProgenyDbContext context = new(dbOptions);
            using MediaDbContext mediaContext = new(CreateMediaDbOptions("IsUserInUserAccessList_SystemAccount_Media"));

            List<UserAccess> accessList = new();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            bool result = userAccessService.IsUserInUserAccessList(accessList, Constants.SystemAccountEmail);

            Assert.True(result);
        }

        #endregion

        #region ConvertUserAccessesToUserGroups Tests

        [Fact]
        public async Task ConvertUserAccessesToUserGroups_Should_Create_Groups_And_Members()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("ConvertUserAccessesToUserGroups_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            await using MediaDbContext mediaContext = new(CreateMediaDbOptions("ConvertUserAccessesToUserGroups_Valid_Media"));

            Progeny progeny = new()
            {
                Admins = "User1",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            UserAccess userAccess = new() { UserId = "user1@test.com", AccessLevel = 0, ProgenyId = 1 };
            UserInfo userInfo = new() { UserEmail = "user1@test.com", UserId = "User1" };

            context.Add(progeny);
            context.Add(userAccess);
            context.Add(userInfo);
            await context.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            await userAccessService.ConvertUserAccessesToUserGroups();

            List<UserGroup> groups = await context.UserGroupsDb.ToListAsync();
            List<UserGroupMember> members = await context.UserGroupMembersDb.ToListAsync();

            Assert.NotEmpty(groups);
            Assert.NotEmpty(members);
        }

        #endregion

        #region ConvertItemAccessLevelToItemPermissionsForGroups Tests

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Should_Convert_Pictures()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("ConvertPictures_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            DbContextOptions<MediaDbContext> mediaDbOptions = CreateMediaDbOptions("ConvertPictures_Valid_Media");
            await using MediaDbContext mediaContext = new(mediaDbOptions);

            Progeny progeny = new()
            {
                Admins = "admin@test.com",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progeny);
            await context.SaveChangesAsync();

            UserGroup userGroup = new()
            {
                ProgenyId = 1,
                Name = "Administrators",
                Description = "Admin group"
            };

            context.Add(userGroup);
            await context.SaveChangesAsync();

            Picture picture = new()
            {
                ProgenyId = 1,
                AccessLevel = 0,
                PictureTime = DateTime.UtcNow
            };

            mediaContext.Add(picture);
            await mediaContext.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            bool hasMore = await userAccessService.ConvertItemAccessLevelToItemPermissionsForGroups(KinaUnaTypes.TimeLineType.Photo, 10);

            Picture? updatedPicture = await mediaContext.PicturesDb.FirstOrDefaultAsync();
            List<TimelineItemPermission> permissions = await context.TimelineItemPermissionsDb.ToListAsync();

            Assert.NotNull(updatedPicture);
            Assert.Equal(99, updatedPicture.AccessLevel);
            Assert.NotEmpty(permissions);
            Assert.False(hasMore);
        }

        [Fact]
        public async Task ConvertItemAccessLevelToItemPermissionsForGroups_Should_Return_True_When_More_Items_Exist()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = CreateDbOptions("ConvertMore_Valid");
            await using ProgenyDbContext context = new(dbOptions);
            DbContextOptions<MediaDbContext> mediaDbOptions = CreateMediaDbOptions("ConvertMore_Valid_Media");
            await using MediaDbContext mediaContext = new(mediaDbOptions);

            Progeny progeny = new()
            {
                Admins = "admin@test.com",
                BirthDay = DateTime.UtcNow,
                Name = "Progeny1",
                NickName = "NickName1",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            context.Add(progeny);
            await context.SaveChangesAsync();

            UserGroup userGroup = new()
            {
                ProgenyId = 1,
                Name = "Administrators",
                Description = "Admin group"
            };

            context.Add(userGroup);
            await context.SaveChangesAsync();

            for (int i = 0; i < 5; i++)
            {
                Picture picture = new()
                {
                    ProgenyId = 1,
                    AccessLevel = 0,
                    PictureTime = DateTime.UtcNow
                };
                mediaContext.Add(picture);
            }
            await mediaContext.SaveChangesAsync();

            IDistributedCache memoryCache = CreateMemoryCache();
            UserAccessService userAccessService = new(context, mediaContext, memoryCache);

            bool hasMore = await userAccessService.ConvertItemAccessLevelToItemPermissionsForGroups(KinaUnaTypes.TimeLineType.Photo, 2);

            Assert.True(hasMore);
        }

        #endregion
    }
}