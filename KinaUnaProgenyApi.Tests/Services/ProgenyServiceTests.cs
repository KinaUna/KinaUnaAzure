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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(progenyToAdd);
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny resultProgeny = await progenyService.GetProgeny(1);
            // 2nd call to GetProgeny is retrieving the Progeny object from cache.
            Progeny resultProgenyCached = await progenyService.GetProgeny(1);

            Assert.NotNull(resultProgeny);
            Assert.IsType<Progeny>(resultProgeny);
            Assert.Equal(resultProgeny.BirthDay, progenyToAdd.BirthDay);
            Assert.Equal(resultProgeny.Admins, progenyToAdd.Admins);
            Assert.Equal(resultProgeny.Name, progenyToAdd.Name);
            Assert.Equal(resultProgeny.NickName, progenyToAdd.NickName);
            Assert.Equal(resultProgeny.PictureLink, progenyToAdd.PictureLink);
            Assert.Equal(resultProgeny.TimeZone, progenyToAdd.TimeZone);

            Assert.NotNull(resultProgenyCached);
            Assert.IsType<Progeny>(resultProgenyCached);
            Assert.Equal(resultProgenyCached.BirthDay, progenyToAdd.BirthDay);
            Assert.Equal(resultProgenyCached.Admins, progenyToAdd.Admins);
            Assert.Equal(resultProgenyCached.Name, progenyToAdd.Name);
            Assert.Equal(resultProgenyCached.NickName, progenyToAdd.NickName);
            Assert.Equal(resultProgenyCached.PictureLink, progenyToAdd.PictureLink);
            Assert.Equal(resultProgenyCached.TimeZone, progenyToAdd.TimeZone);
        }

        [Fact]
        public async Task GetProgeny_Should_Return_Null_Object_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetProgeny_Should_Return_Null_Object_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            context.Add(new Progeny { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone });
            await context.SaveChangesAsync();
            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny progeny = await progenyService.GetProgeny(0);

            Assert.Null(progeny);
        }

        [Fact]
        public async Task AddProgenyShouldSaveProgeny()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddProgenyShouldSaveProgeny").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny progenyToAdd = new Progeny
                { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };

            Progeny addedProgeny = await progenyService.AddProgeny(progenyToAdd);
            Progeny? dbProgeny = await context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == addedProgeny.Id);
            Progeny savedProgeny = await progenyService.GetProgeny(addedProgeny.Id);

            Assert.NotNull(addedProgeny);
            Assert.IsType<Progeny>(addedProgeny);
            Assert.NotEqual(0, addedProgeny.Id);

            Assert.NotNull(savedProgeny);
            Assert.IsType<Progeny>(savedProgeny);
            Assert.NotEqual(0, savedProgeny.Id);
            Assert.Equal(savedProgeny.BirthDay, progenyToAdd.BirthDay);
            Assert.Equal(savedProgeny.Admins, progenyToAdd.Admins);
            Assert.Equal(savedProgeny.Name, progenyToAdd.Name);
            Assert.Equal(savedProgeny.NickName, progenyToAdd.NickName);
            Assert.Equal(savedProgeny.PictureLink, progenyToAdd.PictureLink);
            Assert.Equal(savedProgeny.TimeZone, progenyToAdd.TimeZone);

            if (dbProgeny != null)
            {
                Assert.NotNull(dbProgeny);
                Assert.IsType<Progeny>(dbProgeny);
                Assert.NotEqual(0, dbProgeny.Id);
                Assert.Equal(dbProgeny.BirthDay, progenyToAdd.BirthDay);
                Assert.Equal(dbProgeny.Admins, progenyToAdd.Admins);
                Assert.Equal(dbProgeny.Name, progenyToAdd.Name);
                Assert.Equal(dbProgeny.NickName, progenyToAdd.NickName);
                Assert.Equal(dbProgeny.PictureLink, progenyToAdd.PictureLink);
                Assert.Equal(dbProgeny.TimeZone, progenyToAdd.TimeZone);
            }
        }

        [Fact]
        public async Task UpdateProgenyShouldSaveProgeny()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateProgenyShouldSaveProgeny").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            Progeny originalProgeny = new Progeny
                { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };
            context.Add(originalProgeny);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny progenyToUpdate = await progenyService.GetProgeny(1);
            progenyToUpdate.Name = "B";

            Progeny resultProgeny = await progenyService.UpdateProgeny(progenyToUpdate);
            Progeny? dbProgeny = await context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == resultProgeny.Id);
            Progeny savedProgeny = await progenyService.GetProgeny(resultProgeny.Id);

            Assert.NotNull(savedProgeny);
            Assert.IsType<Progeny>(savedProgeny);
            Assert.Equal(savedProgeny.Id, progenyToUpdate.Id);
            Assert.Equal(savedProgeny.BirthDay, progenyToUpdate.BirthDay);
            Assert.Equal(savedProgeny.Admins, progenyToUpdate.Admins);
            Assert.Equal(savedProgeny.Name, progenyToUpdate.Name);
            Assert.Equal(savedProgeny.NickName, progenyToUpdate.NickName);
            Assert.Equal(savedProgeny.PictureLink, progenyToUpdate.PictureLink);
            Assert.Equal(savedProgeny.TimeZone, progenyToUpdate.TimeZone);

            if (dbProgeny != null)
            {
                Assert.NotNull(dbProgeny);
                Assert.IsType<Progeny>(dbProgeny);
                Assert.Equal(dbProgeny.Id, progenyToUpdate.Id);
                Assert.Equal(dbProgeny.BirthDay, progenyToUpdate.BirthDay);
                Assert.Equal(dbProgeny.Admins, progenyToUpdate.Admins);
                Assert.Equal(dbProgeny.Name, progenyToUpdate.Name);
                Assert.Equal(dbProgeny.NickName, progenyToUpdate.NickName);
                Assert.Equal(dbProgeny.PictureLink, progenyToUpdate.PictureLink);
                Assert.Equal(dbProgeny.TimeZone, progenyToUpdate.TimeZone);
            }
        }

        [Fact]
        public async Task DeleteProgenyShouldRemoveProgeny()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteProgenyShouldRemoveProgeny").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            Progeny originalProgeny = new Progeny
            { BirthDay = DateTime.Now, Admins = "test@test.com", Name = "Test Child A", NickName = "A", PictureLink = Constants.ProfilePictureUrl, TimeZone = Constants.DefaultTimezone };
            context.Add(originalProgeny);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ProgenyService progenyService = new ProgenyService(context, memoryCache);

            Progeny progenyToDelete = await progenyService.GetProgeny(1);

            _ = await progenyService.DeleteProgeny(progenyToDelete);
            Progeny? dbProgeny = await context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == 1);
            Progeny savedProgeny = await progenyService.GetProgeny(1);

            Assert.Null(savedProgeny);
            Assert.Null(dbProgeny);
        }
    }
}
