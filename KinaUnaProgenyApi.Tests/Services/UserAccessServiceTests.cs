using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class UserAccessServiceTests
    {
        [Fact]
        public async Task GetProgenyUserIsAdmin_Should_Return_List_Of_Progeny_When_Email_Is_Valid()
        {
            Progeny progenyToAdd1 = new Progeny
            {
                BirthDay = DateTime.Now, Admins = "test@test.com, test1@abc.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone
            };

            Progeny progenyToAdd2 = new Progeny
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child B",
                NickName = "B",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserIsAdmin_Should_Return_List_Of_Progeny_When_Email_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(progenyToAdd1);
            context.Add(progenyToAdd2);
            await context.SaveChangesAsync();
            
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

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
        public async Task GetProgenyUserIsAdmin_Should_Return_Empty_List_Of_Progeny_When_Email_Is_Invalid()
        {
            Progeny progenyToAdd1 = new Progeny
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child A",
                NickName = "A",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            Progeny progenyToAdd2 = new Progeny
            {
                BirthDay = DateTime.Now,
                Admins = "test@test.com, test1@abc.com",
                Name = "Test Child B",
                NickName = "B",
                PictureLink = Constants.ProfilePictureUrl,
                TimeZone = Constants.DefaultTimezone
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserIsAdmin_Should_Return_Empty_List_Of_Progeny_When_Email_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(progenyToAdd1);
            context.Add(progenyToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin("test3@test.com");
            List<Progeny> progenyList2 = await userAccessService.GetProgenyUserIsAdmin("test3@test.com"); // Test cached result.

            Assert.NotNull(progenyList);
            Assert.IsType<List<Progeny>>(progenyList);
            Assert.Empty(progenyList);
            Assert.NotNull(progenyList2);
            Assert.IsType<List<Progeny>>(progenyList2);
            Assert.Empty(progenyList2);

        }

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Return_List_Of_UserAccess_When_Id_Is_Valid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1", AccessLevel = 0, ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User2", AccessLevel = 0, ProgenyId = 1
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserAccessList_Should_Return_List_Of_UserAccess_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            
            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(1);
            List<UserAccess> accessList2 = await userAccessService.GetProgenyUserAccessList(1); // Test cached results.
            UserAccess firstUserAccess = accessList.First();

            Assert.NotNull(accessList);
            Assert.Equal(2, accessList.Count);
            Assert.NotNull(accessList2);
            Assert.Equal(2, accessList2.Count);
            Assert.NotNull(firstUserAccess);
            Assert.IsType<UserAccess>(firstUserAccess);

        }

        [Fact]
        public async Task GetProgenyUserAccessList_Should_Return_Empty_List_Of_UserAccess_When_Id_Is_Invalid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User2",
                AccessLevel = 0,
                ProgenyId = 1
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserAccessList_Should_Return_Empty_List_Of_UserAccess_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            
            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(2);
            List<UserAccess> accessList2 = await userAccessService.GetProgenyUserAccessList(2); // Test cached results.
            
            Assert.NotNull(accessList);
            Assert.Empty( accessList);
            Assert.NotNull(accessList2);
            Assert.Empty(accessList2);

        }

        [Fact]
        public async Task GetUsersUserAccessList_Should_Return_List_Of_UserAccess_When_Id_Is_Valid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            UserAccess userAccessToAdd3 = new UserAccess
            {
                UserId = "User2",
                AccessLevel = 0,
                ProgenyId = 1
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersUserAccessList_Should_Return_List_Of_UserAccess_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            context.Add(userAccessToAdd3);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetUsersUserAccessList(userAccessToAdd1.UserId);
            List<UserAccess> accessList2 = await userAccessService.GetUsersUserAccessList(userAccessToAdd1.UserId); // Test cached results.
            UserAccess firstUserAccess = accessList.First();

            Assert.NotNull(accessList);
            Assert.Equal(2, accessList.Count);
            Assert.NotNull(accessList2);
            Assert.Equal(2, accessList2.Count);
            Assert.NotNull(firstUserAccess);
            Assert.IsType<UserAccess>(firstUserAccess);

        }

        [Fact]
        public async Task GetUsersUserAccessList_Should_Return_Empty_List_Of_UserAccess_When_Id_Is_Invalid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersUserAccessList_Should_Return_Empty_List_Of_UserAccess_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            List<UserAccess> accessList = await userAccessService.GetUsersUserAccessList("User2");
            List<UserAccess> accessList2 = await userAccessService.GetUsersUserAccessList("User2"); // Test cached results.

            Assert.NotNull(accessList);
            Assert.Empty(accessList);
            Assert.NotNull(accessList2);
            Assert.Empty(accessList2);

        }

        [Fact]
        public async Task GetUserAccess_Returns_UserAccess_Object_When_Id_Is_Valid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUserAccess_Returns_UserAccess_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccess = await userAccessService.GetUserAccess(1);
            UserAccess userAccess2 = await userAccessService.GetUserAccess(1);

            Assert.NotNull(userAccess);
            Assert.IsType<UserAccess>(userAccess);
            Assert.Equal(userAccessToAdd1.UserId, userAccess.UserId);
            Assert.Equal(userAccessToAdd1.AccessLevel, userAccess.AccessLevel);
            Assert.Equal(userAccessToAdd1.ProgenyId, userAccess.ProgenyId);

            Assert.NotNull(userAccess2);
            Assert.IsType<UserAccess>(userAccess2);
            Assert.Equal(userAccessToAdd1.UserId, userAccess2.UserId);
            Assert.Equal(userAccessToAdd1.AccessLevel, userAccess2.AccessLevel);
            Assert.Equal(userAccessToAdd1.ProgenyId, userAccess2.ProgenyId);
        }

        [Fact]
        public async Task GetUserAccess_Should_Return_Null_When_Id_Is_Invalid()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUserAccess_Returns_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccess = await userAccessService.GetUserAccess(2);
            UserAccess userAccess2 = await userAccessService.GetUserAccess(2);

            Assert.Null(userAccess);
            
            Assert.Null(userAccess2);
        }

        [Fact]
        public async Task AddUserAccess_Should_Save_UserAccess()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddUserAccess_Should_Save_UserAccess").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            UserAccess addedUserAccess = await userAccessService.AddUserAccess(userAccessToAdd2);
            UserAccess? dbUserAccess = await context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(ua => ua.AccessId == addedUserAccess.AccessId);
            UserAccess savedUserAccess = await userAccessService.GetUserAccess(addedUserAccess.AccessId);

            Assert.NotNull(addedUserAccess);
            Assert.IsType<UserAccess>(addedUserAccess);
            Assert.NotEqual(0, addedUserAccess.AccessId);
            Assert.Equal("User1", addedUserAccess.UserId);
            Assert.Equal(0, addedUserAccess.AccessLevel);
            Assert.Equal(2, addedUserAccess.ProgenyId);

            if (dbUserAccess != null)
            {
                Assert.IsType<UserAccess>(dbUserAccess);
                Assert.NotEqual(0, dbUserAccess.AccessId);
                Assert.Equal("User1", dbUserAccess.UserId);
                Assert.Equal(0, dbUserAccess.AccessLevel);
                Assert.Equal(2, dbUserAccess.ProgenyId);
            }
            Assert.NotNull(savedUserAccess);
            Assert.IsType<UserAccess>(savedUserAccess);
            Assert.NotEqual(0, savedUserAccess.AccessId);
            Assert.Equal("User1", savedUserAccess.UserId);
            Assert.Equal(0, savedUserAccess.AccessLevel);
            Assert.Equal(2, savedUserAccess.ProgenyId);

        }

        [Fact]
        public async Task UpdateUserAccess_Should_Save_UserAccess()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateUserAccess_Should_Save_UserAccess").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccessToUpdate = await userAccessService.GetUserAccess(1);
            userAccessToUpdate.AccessLevel = 5;
            UserAccess updatedUserAccess = await userAccessService.UpdateUserAccess(userAccessToUpdate);
            UserAccess? dbUserAccess = await context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(ua => ua.AccessId == 1);
            UserAccess savedUserAccess = await userAccessService.GetUserAccess(1);

            Assert.NotNull(updatedUserAccess);
            Assert.IsType<UserAccess>(updatedUserAccess);
            Assert.NotEqual(0, updatedUserAccess.AccessId);
            Assert.Equal("User1", updatedUserAccess.UserId);
            Assert.Equal(5, updatedUserAccess.AccessLevel);
            Assert.Equal(1, updatedUserAccess.ProgenyId);

            if (dbUserAccess != null)
            {
                Assert.IsType<UserAccess>(dbUserAccess);
                Assert.NotEqual(0, dbUserAccess.AccessId);
                Assert.Equal("User1", dbUserAccess.UserId);
                Assert.Equal(5, dbUserAccess.AccessLevel);
                Assert.Equal(1, dbUserAccess.ProgenyId);
            }

            Assert.NotNull(savedUserAccess);
            Assert.IsType<UserAccess>(savedUserAccess);
            Assert.NotEqual(0, savedUserAccess.AccessId);
            Assert.Equal("User1", savedUserAccess.UserId);
            Assert.Equal(5, savedUserAccess.AccessLevel);
            Assert.Equal(1, savedUserAccess.ProgenyId);
        }

        [Fact]
        public async Task RemoveUserAccess_Should_Remove_UserAccess()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("RemoveUserAccess_Should_Remove_UserAccess").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            int userAccessesBeforeDelete = context.UserAccessDb.Count();
            UserAccess userAccessToDelete = await userAccessService.GetUserAccess(1);
            
            await userAccessService.RemoveUserAccess(userAccessToDelete.AccessId, userAccessToDelete.ProgenyId, userAccessToDelete.UserId);
            UserAccess? deletedUserAccess = await context.UserAccessDb.SingleOrDefaultAsync(ua => ua.AccessId == 1);
            int userAccessesAfterDelete = context.UserAccessDb.Count();

            Assert.Null(deletedUserAccess);
            Assert.Equal(2, userAccessesBeforeDelete);
            Assert.Equal(1, userAccessesAfterDelete);
        }

        [Fact]
        public async Task GetProgenyUserAccessForUser_Should_Return_UserAccess_When_It_Exists()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserAccessForUser_Should_Return_UserAccess_When_It_Exists").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(userAccessToAdd1.ProgenyId, userAccessToAdd1.UserId);
            UserAccess userAccess2 = await userAccessService.GetProgenyUserAccessForUser(userAccessToAdd1.ProgenyId, userAccessToAdd1.UserId); // Test with cache.
            Assert.NotNull(userAccess);
            Assert.NotNull(userAccess2);
            Assert.IsType<UserAccess>(userAccess);
            Assert.IsType<UserAccess>(userAccess2);
            Assert.Equal(1, userAccess.AccessId);
            Assert.Equal(1, userAccess2.AccessId);
        }

        [Fact]
        public async Task GetProgenyUserAccessForUser_Should_Return_Null_When_It_Does_Not_Exists()
        {
            UserAccess userAccessToAdd1 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 1
            };

            UserAccess userAccessToAdd2 = new UserAccess
            {
                UserId = "User1",
                AccessLevel = 0,
                ProgenyId = 2
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserAccessForUser_Should_Return_Null_When_It_Does_Not_Exists").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            context.Add(userAccessToAdd1);
            context.Add(userAccessToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(1, "User2");
            UserAccess userAccess2 = await userAccessService.GetProgenyUserAccessForUser(1, "User2"); // Test with cache.
            Assert.Null(userAccess);
            Assert.Null(userAccess2);
        }

        [Fact]
        public async Task UpdateProgenyAdmins_Should_Save_Progeny()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgenyUserAccessForUser_Should_Return_Null_When_It_Does_Not_Exists").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Progeny progenyToAdd1 = new Progeny
                { BirthDay = DateTime.Now, Admins = "test1@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };
            Progeny progenyToAdd2 = new Progeny
                { BirthDay = DateTime.Now, Admins = "test2@test.com", Name = "Test Child B", NickName = "B", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };
            context.Add(progenyToAdd1);
            context.Add(progenyToAdd2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserAccessService userAccessService = new UserAccessService(context, memoryCache);

            Progeny? progeny1 = await context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == 1);
            Progeny? progeny2 = await context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == 2);
            string updatedAdmins = string.Join(',', progenyToAdd1.Admins, progenyToAdd2.Admins);
            if (progeny1 != null && progeny2 != null)
            {
                progeny1.Admins = updatedAdmins;
                progeny2.Admins = updatedAdmins;

                await userAccessService.UpdateProgenyAdmins(progeny1);
                await userAccessService.UpdateProgenyAdmins(progeny2);
            }

            Progeny? resultProgeny1 = await context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == 1);
            Progeny? resultProgeny2 = await context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == 2);

            Assert.NotNull(resultProgeny1);
            Assert.NotNull(resultProgeny2);
            Assert.Equal(updatedAdmins, resultProgeny1.Admins);
            Assert.Equal(updatedAdmins, resultProgeny2.Admins);
            Assert.Equal(progenyToAdd1.BirthDay, resultProgeny1.BirthDay);
            Assert.Equal(progenyToAdd1.NickName, resultProgeny1.NickName);
            Assert.Equal(progenyToAdd1.Name, resultProgeny1.Name);
            Assert.Equal(progenyToAdd1.PictureLink, resultProgeny1.PictureLink);
            Assert.Equal(progenyToAdd1.TimeZone, resultProgeny1.TimeZone);
        }
    }
}
