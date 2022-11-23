using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class VaccinationServiceTests
    {
        [Fact]
        public async Task GetVaccination_Should_Return_Vaccination_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVaccination_Should_Return_Vaccination_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };
            
            Vaccination vaccination2 = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };

            context.Add(vaccination1);
            context.Add(vaccination2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            Vaccination resultVaccination1 = await vaccinationService.GetVaccination(1);
            Vaccination resultVaccination2 = await vaccinationService.GetVaccination(1); // Uses cache

            Assert.NotNull(resultVaccination1);
            Assert.IsType<Vaccination>(resultVaccination1);
            Assert.Equal(vaccination1.Author, resultVaccination1.Author);
            Assert.Equal(vaccination1.VaccinationName, resultVaccination1.VaccinationName);
            Assert.Equal(vaccination1.AccessLevel, resultVaccination1.AccessLevel);
            Assert.Equal(vaccination1.ProgenyId, resultVaccination1.ProgenyId);

            Assert.NotNull(resultVaccination2);
            Assert.IsType<Vaccination>(resultVaccination2);
            Assert.Equal(vaccination1.Author, resultVaccination2.Author);
            Assert.Equal(vaccination1.VaccinationName, resultVaccination2.VaccinationName);
            Assert.Equal(vaccination1.AccessLevel, resultVaccination2.AccessLevel);
            Assert.Equal(vaccination1.ProgenyId, resultVaccination2.ProgenyId);
        }

        [Fact]
        public async Task GetVaccination_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVaccination_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };
            
            context.Add(vaccination1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            Vaccination resultVaccination1 = await vaccinationService.GetVaccination(2);
            Vaccination resultVaccination2 = await vaccinationService.GetVaccination(2); // Using cache
            
            Assert.Null(resultVaccination1);
            Assert.Null(resultVaccination2);
        }

        [Fact]
        public async Task AddVaccination_Should_Save_Vaccination()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddVaccination_Should_Save_Vaccination").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };
            
            context.Add(vaccination1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);
            
            Vaccination vaccinationToAdd = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };

            Vaccination addedVaccination = await vaccinationService.AddVaccination(vaccinationToAdd);
            Vaccination? dbVaccination = await context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == addedVaccination.VaccinationId);
            Vaccination savedVaccination = await vaccinationService.GetVaccination(addedVaccination.VaccinationId);

            Assert.NotNull(addedVaccination);
            Assert.IsType<Vaccination>(addedVaccination);
            Assert.Equal(vaccinationToAdd.Author, addedVaccination.Author);
            Assert.Equal(vaccinationToAdd.VaccinationName, addedVaccination.VaccinationName);
            Assert.Equal(vaccinationToAdd.AccessLevel, addedVaccination.AccessLevel);
            Assert.Equal(vaccinationToAdd.ProgenyId, addedVaccination.ProgenyId);

            if (dbVaccination != null)
            {
                Assert.IsType<Vaccination>(dbVaccination);
                Assert.Equal(vaccinationToAdd.Author, dbVaccination.Author);
                Assert.Equal(vaccinationToAdd.VaccinationName, dbVaccination.VaccinationName);
                Assert.Equal(vaccinationToAdd.AccessLevel, dbVaccination.AccessLevel);
                Assert.Equal(vaccinationToAdd.ProgenyId, dbVaccination.ProgenyId);
            }
            Assert.NotNull(savedVaccination);
            Assert.IsType<Vaccination>(savedVaccination);
            Assert.Equal(vaccinationToAdd.Author, savedVaccination.Author);
            Assert.Equal(vaccinationToAdd.VaccinationName , savedVaccination.VaccinationName);
            Assert.Equal(vaccinationToAdd.AccessLevel, savedVaccination.AccessLevel);
            Assert.Equal(vaccinationToAdd.ProgenyId, savedVaccination.ProgenyId);

        }

        [Fact]
        public async Task UpdateVaccination_Should_Save_Vaccination()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateVaccination_Should_Save_Vaccination").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };

            Vaccination vaccination2 = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };
            
            context.Add(vaccination1);
            context.Add(vaccination2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            Vaccination vaccinationToUpdate = await vaccinationService.GetVaccination(1);
            vaccinationToUpdate.AccessLevel = 5;
            Vaccination updatedVaccination = await vaccinationService.UpdateVaccination(vaccinationToUpdate);
            Vaccination? dbVaccination = await context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == 1);
            Vaccination savedVaccination = await vaccinationService.GetVaccination(1);

            Assert.NotNull(updatedVaccination);
            Assert.IsType<Vaccination>(updatedVaccination);
            Assert.NotEqual(0, updatedVaccination.VaccinationId);
            Assert.Equal("User1", updatedVaccination.Author);
            Assert.Equal(5, updatedVaccination.AccessLevel);
            Assert.Equal(1, updatedVaccination.ProgenyId);

            if (dbVaccination != null)
            {
                Assert.IsType<Vaccination>(dbVaccination);
                Assert.NotEqual(0, dbVaccination.VaccinationId);
                Assert.Equal("User1", dbVaccination.Author);
                Assert.Equal(5, dbVaccination.AccessLevel);
                Assert.Equal(1, dbVaccination.ProgenyId);
            }

            Assert.NotNull(savedVaccination);
            Assert.IsType<Vaccination>(savedVaccination);
            Assert.NotEqual(0, savedVaccination.VaccinationId);
            Assert.Equal("User1", savedVaccination.Author);
            Assert.Equal(5, savedVaccination.AccessLevel);
            Assert.Equal(1, savedVaccination.ProgenyId);
        }

        [Fact]
        public async Task DeleteVaccination_Should_Remove_Vaccination()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteVaccination_Should_Remove_Vaccination").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };

            Vaccination vaccination2 = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };

            context.Add(vaccination1);
            context.Add(vaccination2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            int vaccinationItemsCountBeforeDelete = context.VaccinationsDb.Count();
            Vaccination vaccinationToDelete = await vaccinationService.GetVaccination(1);

            await vaccinationService.DeleteVaccination(vaccinationToDelete);
            Vaccination? deletedVaccination = await context.VaccinationsDb.SingleOrDefaultAsync(f => f.VaccinationId == 1);
            int vaccinationItemsCountAfterDelete = context.VaccinationsDb.Count();

            Assert.Null(deletedVaccination);
            Assert.Equal(2, vaccinationItemsCountBeforeDelete);
            Assert.Equal(1, vaccinationItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetVaccinationsList_Should_Return_List_Of_Vaccination_When_Progeny_Has_Saved_Vaccinations()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVaccinationsList_Should_Return_List_Of_Vaccination_When_Progeny_Has_Saved_Vaccinations").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };

            Vaccination vaccination2 = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };

            context.Add(vaccination1);
            context.Add(vaccination2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            List<Vaccination> vaccinationsList = await vaccinationService.GetVaccinationsList(1);
            List<Vaccination> vaccinationsList2 = await vaccinationService.GetVaccinationsList(1); // Test cached result.
            Vaccination firstVaccination = vaccinationsList.First();

            Assert.NotNull(vaccinationsList);
            Assert.IsType<List<Vaccination>>(vaccinationsList);
            Assert.Equal(2, vaccinationsList.Count);
            Assert.NotNull(vaccinationsList2);
            Assert.IsType<List<Vaccination>>(vaccinationsList2);
            Assert.Equal(2, vaccinationsList2.Count);
            Assert.NotNull(firstVaccination);
            Assert.IsType<Vaccination>(firstVaccination);
        }

        [Fact]
        public async Task GetVaccinationsList_Should_Return_Empty_List_Of_Vaccination_When_Progeny_Has_No_Saved_Vaccinations()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetVaccinationsList_Should_Return_Empty_List_Of_Vaccination_When_Progeny_Has_No_Saved_Vaccinations").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Vaccination vaccination1 = new Vaccination
            {
                VaccinationName = "Vaccination1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note1",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description1"
            };
            
            Vaccination vaccination2 = new Vaccination
            {
                VaccinationName = "Vaccination2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Notes = "Note2",
                VaccinationDate = DateTime.UtcNow,
                VaccinationDescription = "Description2"
            };

            context.Add(vaccination1);
            context.Add(vaccination2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            VaccinationService vaccinationService = new VaccinationService(context, memoryCache);

            List<Vaccination> vaccinationsList = await vaccinationService.GetVaccinationsList(2);
            List<Vaccination> vaccinationsList2 = await vaccinationService.GetVaccinationsList(2); // Test cached result.

            Assert.NotNull(vaccinationsList);
            Assert.IsType<List<Vaccination>>(vaccinationsList);
            Assert.Empty(vaccinationsList);
            Assert.NotNull(vaccinationsList2);
            Assert.IsType<List<Vaccination>>(vaccinationsList2);
            Assert.Empty(vaccinationsList2);
        }
    }
}
