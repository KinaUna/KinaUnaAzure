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
    public class ProgenyServiceTests
    {
        [Fact]
        public async Task GetProgeny_Should_Return_Progeny_Object_When_Id_Is_Valid()
        {
            Progeny progenyToAdd = new Progeny
                { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgeny_Should_Return_Progeny_Object_When_Id_Is_Valid").Options;
            using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(progenyToAdd);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny resultProgeny = await progenyService.GetProgeny(1);

            Assert.NotNull(resultProgeny);
            Assert.IsType<Progeny>(resultProgeny);
            Assert.Equal(resultProgeny.BirthDay, progenyToAdd.BirthDay);
            Assert.Equal(resultProgeny.Admins, progenyToAdd.Admins);
            Assert.Equal(resultProgeny.Name, progenyToAdd.Name);
            Assert.Equal(resultProgeny.NickName, progenyToAdd.NickName);
            Assert.Equal(resultProgeny.PictureLink, progenyToAdd.PictureLink);
            Assert.Equal(resultProgeny.TimeZone, progenyToAdd.TimeZone);
        }

        [Fact]
        public async Task GetProgeny_Should_Return_Null_Object_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgeny_Should_Return_Null_Object_When_Id_Is_Invalid").Options;
            using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(new Progeny { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone });
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny progeny = await progenyService.GetProgeny(0);

            Assert.Null(progeny);
        }
    }
}
