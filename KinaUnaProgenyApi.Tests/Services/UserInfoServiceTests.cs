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
    public class UserInfoServiceTests
    {
        [Fact]
        public async Task GetAllUserInfos_Should_Return_List_Of_UserInfo()
        {
            UserInfo userInfo1 = new UserInfo
            {
                UserEmail = "test1@test.com", UserId = "UserId1", UserName = "Test1", FirstName = "FirstName1", MiddleName = "MiddleName1", LastName = "LastName1",
                ViewChild = 1, ProfilePicture = Constants.ProfilePictureUrl, Timezone = Constants.DefaultTimezone
            };
            UserInfo userInfo2 = new UserInfo 
            {
                UserEmail = "test2@test.com", UserId = "UserId2", UserName = "Test2", FirstName = "FirstName2", MiddleName = "MiddleName2", LastName = "LastName2",
                ViewChild = 1, ProfilePicture = Constants.ProfilePictureUrl, Timezone = Constants.DefaultTimezone
            };
            UserInfo userInfo3 = new UserInfo
            {
                UserEmail = "test3@test.com", UserId = "UserId3", UserName = "Test3", FirstName = "FirstName3", MiddleName = "MiddleName3", LastName = "LastName3" ,
                ViewChild = 1, ProfilePicture = Constants.ProfilePictureUrl, Timezone = Constants.DefaultTimezone
            };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllUserInfos_Should_Return_List_Of_UserInfo").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(userInfo1);
            context.Add(userInfo2);
            context.Add(userInfo3);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserInfoService userInfoService = new UserInfoService(context, memoryCache);

            var resultUserInfos = await userInfoService.GetAllUserInfos();
            var dbUserInfos = await context.UserInfoDb.ToListAsync();

            Assert.NotNull(resultUserInfos);
            Assert.IsType<List<UserInfo>>(resultUserInfos);
            Assert.Equal(resultUserInfos.Count, dbUserInfos.Count);
        }

        [Fact]
        public async Task GetUserInfoByEmail_Should_Return_UserInfo_Object_When_Email_Is_Valid()
        {
            UserInfo userInfo1 = new UserInfo 
            { 
                UserEmail = "test1@test.com", UserId = "UserId1", UserName = "Test1", FirstName = "FirstName1", MiddleName = "MiddleName1", LastName = "LastName1",
                ViewChild = 1, ProfilePicture = Constants.ProfilePictureUrl, Timezone = Constants.DefaultTimezone
            };
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUserInfoByEmail_Should_Return_UserInfo_Object_When_Email_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(userInfo1);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserInfoService userInfoService = new UserInfoService(context, memoryCache);

            UserInfo resultUserInfo = await userInfoService.GetUserInfoByEmail(userInfo1.UserEmail);
            UserInfo resultUserInfoCached = await userInfoService.GetUserInfoByEmail(userInfo1.UserEmail);
            
            Assert.NotNull(resultUserInfo);
            Assert.IsType<UserInfo>(resultUserInfo);
            Assert.Equal(userInfo1.UserEmail, resultUserInfo.UserEmail);
            Assert.Equal(userInfo1.UserId, resultUserInfo.UserId);
            Assert.Equal(userInfo1.UserName, resultUserInfo.UserName);
            Assert.Equal(userInfo1.FirstName, resultUserInfo.FirstName);
            Assert.Equal(userInfo1.MiddleName, resultUserInfo.MiddleName);
            Assert.Equal(userInfo1.LastName, resultUserInfo.LastName);
            Assert.Equal(userInfo1.ViewChild, resultUserInfo.ViewChild);

            Assert.NotNull(resultUserInfoCached);
            Assert.IsType<UserInfo>(resultUserInfoCached);
        }

        [Fact]
        public async Task GetUserInfoByEmail_Should_Return_Null_When_Email_Is_Invalid()
        {
            UserInfo userInfo1 = new UserInfo
            {
                UserEmail = "test1@test.com",
                UserId = "UserId1",
                UserName = "Test1",
                FirstName = "FirstName1",
                MiddleName = "MiddleName1",
                LastName = "LastName1",
                ViewChild = 1,
                ProfilePicture = Constants.ProfilePictureUrl,
                Timezone = Constants.DefaultTimezone
            };
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUserInfoByEmail_Should_Return_Null_When_Email_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(userInfo1);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserInfoService userInfoService = new UserInfoService(context, memoryCache);

            UserInfo resultUserInfo = await userInfoService.GetUserInfoByEmail("abc@abc.com");

            Assert.Null(resultUserInfo);
        }

        [Fact]
        public async Task AddUserInfo_Should_Save_UserInfo()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddUserInfo_Should_Save_UserInfo").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserInfoService userInfoService = new UserInfoService(context, memoryCache);

            UserInfo userInfoToAdd = new UserInfo
            {
                UserEmail = "test1@test.com",
                UserId = "UserId1",
                UserName = "Test1",
                FirstName = "FirstName1",
                MiddleName = "MiddleName1",
                LastName = "LastName1",
                ViewChild = 1,
                ProfilePicture = Constants.ProfilePictureUrl,
                Timezone = Constants.DefaultTimezone
            };

            UserInfo addedUserInfo = await userInfoService.AddUserInfo(userInfoToAdd);
            UserInfo? dbUserInfo = await context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.Id == addedUserInfo.Id);
            UserInfo savedUserInfo = await userInfoService.GetUserInfoByEmail(userInfoToAdd.UserEmail);

            Assert.NotNull(savedUserInfo);
            Assert.IsType<UserInfo>(savedUserInfo);
            Assert.NotEqual(0, addedUserInfo.Id);
            Assert.Equal(savedUserInfo.UserEmail, userInfoToAdd.UserEmail);
            Assert.Equal(savedUserInfo.UserId, userInfoToAdd.UserId);
            Assert.Equal(savedUserInfo.UserName, userInfoToAdd.UserName);
            Assert.Equal(savedUserInfo.FirstName, userInfoToAdd.FirstName);
            Assert.Equal(savedUserInfo.MiddleName, userInfoToAdd.MiddleName);
            Assert.Equal(savedUserInfo.LastName, userInfoToAdd.LastName);
            Assert.Equal(savedUserInfo.ViewChild, userInfoToAdd.ViewChild);
            Assert.Equal(savedUserInfo.ProfilePicture, userInfoToAdd.ProfilePicture);
            Assert.Equal(savedUserInfo.Timezone, userInfoToAdd.Timezone);

            if (dbUserInfo != null)
            {
                Assert.Equal(dbUserInfo.UserEmail, userInfoToAdd.UserEmail);
                Assert.Equal(dbUserInfo.UserId, userInfoToAdd.UserId);
                Assert.Equal(dbUserInfo.UserName, userInfoToAdd.UserName);
                Assert.Equal(dbUserInfo.FirstName, userInfoToAdd.FirstName);
                Assert.Equal(dbUserInfo.MiddleName, userInfoToAdd.MiddleName);
                Assert.Equal(dbUserInfo.LastName, userInfoToAdd.LastName);
                Assert.Equal(dbUserInfo.ViewChild, userInfoToAdd.ViewChild);
                Assert.Equal(dbUserInfo.ProfilePicture, userInfoToAdd.ProfilePicture);
                Assert.Equal(dbUserInfo.Timezone, userInfoToAdd.Timezone);
            }
        }
    }
}
