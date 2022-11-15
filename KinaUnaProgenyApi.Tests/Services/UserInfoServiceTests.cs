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
            UserInfo userInfoToAdd1 = new UserInfo { UserEmail = "test1@test.com", UserId = "UserId1", UserName = "Test1", FirstName = "FirstName1", MiddleName = "MiddleName1", LastName = "LastName1"};
            UserInfo userInfoToAdd2 = new UserInfo { UserEmail = "test2@test.com", UserId = "UserId2", UserName = "Test2", FirstName = "FirstName2", MiddleName = "MiddleName2", LastName = "LastName2" };
            UserInfo userInfoToAdd3 = new UserInfo { UserEmail = "test3@test.com", UserId = "UserId3", UserName = "Test3", FirstName = "FirstName3", MiddleName = "MiddleName3", LastName = "LastName3" };
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgeny_Should_Return_Progeny_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(userInfoToAdd1);
            context.Add(userInfoToAdd2);
            context.Add(userInfoToAdd3);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            UserInfoService userInfoService = new UserInfoService(context, memoryCache);

            var resultUserInfos = await userInfoService.GetAllUserInfos();
            var dbUserInfos = await context.UserInfoDb.ToListAsync();

            Assert.NotNull(resultUserInfos);
            Assert.IsType<List<UserInfo>>(resultUserInfos);
            Assert.Equal(resultUserInfos.Count, dbUserInfos.Count);
        }
    }
}
