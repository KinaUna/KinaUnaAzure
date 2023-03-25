using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class MeasurementServiceTests
    {
        [Fact]
        public async Task GetMeasurement_Should_Return_Measurement_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMeasurement_Should_Return_Measurement_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0, Weight = 10, ProgenyId = 1, Author = "User1", AccessLevel = 0, Circumference = 0, CreatedDate = DateTime.UtcNow, Date = DateTime.UtcNow, EyeColor = "", HairColor = "", MeasurementNumber = 1
            };


            Measurement measurement2 = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };

            context.Add(measurement1);
            context.Add(measurement2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            Measurement resultMeasurement1 = await measurementService.GetMeasurement(1);
            Measurement resultMeasurement2 = await measurementService.GetMeasurement(1); // Uses cache

            Assert.NotNull(resultMeasurement1);
            Assert.IsType<Measurement>(resultMeasurement1);
            Assert.Equal(measurement1.Author, resultMeasurement1.Author);
            Assert.Equal(measurement1.Height, resultMeasurement1.Height);
            Assert.Equal(measurement1.AccessLevel, resultMeasurement1.AccessLevel);
            Assert.Equal(measurement1.ProgenyId, resultMeasurement1.ProgenyId);

            Assert.NotNull(resultMeasurement2);
            Assert.IsType<Measurement>(resultMeasurement2);
            Assert.Equal(measurement1.Author, resultMeasurement2.Author);
            Assert.Equal(measurement1.Height, resultMeasurement2.Height);
            Assert.Equal(measurement1.AccessLevel, resultMeasurement2.AccessLevel);
            Assert.Equal(measurement1.ProgenyId, resultMeasurement2.ProgenyId);
        }

        [Fact]
        public async Task GetMeasurement_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMeasurement_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0,
                Weight = 10,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 1
            };
            
            context.Add(measurement1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            Measurement resultMeasurement1 = await measurementService.GetMeasurement(2);
            Measurement resultMeasurement2 = await measurementService.GetMeasurement(2); // Using cache
            
            Assert.Null(resultMeasurement1);
            Assert.Null(resultMeasurement2);
        }

        [Fact]
        public async Task AddMeasurement_Should_Save_Measurement()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddMeasurement_Should_Save_Measurement").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0, Weight = 10, ProgenyId = 1, Author = "User1", AccessLevel = 0, Circumference = 0, CreatedDate = DateTime.UtcNow, Date = DateTime.UtcNow, EyeColor = "", HairColor = "", MeasurementNumber = 1
            };
            context.Add(measurement1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            Measurement measurementToAdd = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };

            Measurement addedMeasurement = await measurementService.AddMeasurement(measurementToAdd);
            Measurement? dbMeasurement = await context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(f => f.MeasurementId == addedMeasurement.MeasurementId);
            Measurement savedMeasurement = await measurementService.GetMeasurement(addedMeasurement.MeasurementId);

            Assert.NotNull(addedMeasurement);
            Assert.IsType<Measurement>(addedMeasurement);
            Assert.Equal(measurementToAdd.Author, addedMeasurement.Author);
            Assert.Equal(measurementToAdd.Height, addedMeasurement.Height);
            Assert.Equal(measurementToAdd.AccessLevel, addedMeasurement.AccessLevel);
            Assert.Equal(measurementToAdd.ProgenyId, addedMeasurement.ProgenyId);

            if (dbMeasurement != null)
            {
                Assert.IsType<Measurement>(dbMeasurement);
                Assert.Equal(measurementToAdd.Author, dbMeasurement.Author);
                Assert.Equal(measurementToAdd.Height, dbMeasurement.Height);
                Assert.Equal(measurementToAdd.AccessLevel, dbMeasurement.AccessLevel);
                Assert.Equal(measurementToAdd.ProgenyId, dbMeasurement.ProgenyId);
            }
            Assert.NotNull(savedMeasurement);
            Assert.IsType<Measurement>(savedMeasurement);
            Assert.Equal(measurementToAdd.Author, savedMeasurement.Author);
            Assert.Equal(measurementToAdd.Height, savedMeasurement.Height);
            Assert.Equal(measurementToAdd.AccessLevel, savedMeasurement.AccessLevel);
            Assert.Equal(measurementToAdd.ProgenyId, savedMeasurement.ProgenyId);

        }

        [Fact]
        public async Task UpdateMeasurement_Should_Save_Measurement()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateMeasurement_Should_Save_Measurement").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0,
                Weight = 10,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 1
            };


            Measurement measurement2 = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };
            context.Add(measurement1);
            context.Add(measurement2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            Measurement measurementToUpdate = await measurementService.GetMeasurement(1);
            measurementToUpdate.AccessLevel = 5;
            Measurement updatedMeasurement = await measurementService.UpdateMeasurement(measurementToUpdate);
            Measurement? dbMeasurement = await context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(f => f.MeasurementId == 1);
            Measurement savedMeasurement = await measurementService.GetMeasurement(1);

            Assert.NotNull(updatedMeasurement);
            Assert.IsType<Measurement>(updatedMeasurement);
            Assert.NotEqual(0, updatedMeasurement.MeasurementId);
            Assert.Equal("User1", updatedMeasurement.Author);
            Assert.Equal(5, updatedMeasurement.AccessLevel);
            Assert.Equal(1, updatedMeasurement.ProgenyId);

            if (dbMeasurement != null)
            {
                Assert.IsType<Measurement>(dbMeasurement);
                Assert.NotEqual(0, dbMeasurement.MeasurementId);
                Assert.Equal("User1", dbMeasurement.Author);
                Assert.Equal(5, dbMeasurement.AccessLevel);
                Assert.Equal(1, dbMeasurement.ProgenyId);
            }

            Assert.NotNull(savedMeasurement);
            Assert.IsType<Measurement>(savedMeasurement);
            Assert.NotEqual(0, savedMeasurement.MeasurementId);
            Assert.Equal("User1", savedMeasurement.Author);
            Assert.Equal(5, savedMeasurement.AccessLevel);
            Assert.Equal(1, savedMeasurement.ProgenyId);
        }

        [Fact]
        public async Task DeleteMeasurement_Should_Remove_Measurement()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteMeasurement_Should_Remove_Measurement").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0,
                Weight = 10,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 1
            };


            Measurement measurement2 = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };
            context.Add(measurement1);
            context.Add(measurement2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            int measurementItemsCountBeforeDelete = context.MeasurementsDb.Count();
            Measurement measurementToDelete = await measurementService.GetMeasurement(1);

            await measurementService.DeleteMeasurement(measurementToDelete);
            Measurement? deletedMeasurement = await context.MeasurementsDb.SingleOrDefaultAsync(f => f.MeasurementId == 1);
            int measurementItemsCountAfterDelete = context.MeasurementsDb.Count();

            Assert.Null(deletedMeasurement);
            Assert.Equal(2, measurementItemsCountBeforeDelete);
            Assert.Equal(1, measurementItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Return_List_Of_Measurement_When_Progeny_Has_Saved_Measurements()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMeasurementsList_Should_Return_List_Of_Measurement_When_Progeny_Has_Saved_Measurements").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0,
                Weight = 10,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 1
            };


            Measurement measurement2 = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };

            context.Add(measurement1);
            context.Add(measurement2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            List<Measurement> measurementsList = await measurementService.GetMeasurementsList(1);
            List<Measurement> measurementsList2 = await measurementService.GetMeasurementsList(1); // Test cached result.
            Measurement firstMeasurement = measurementsList.First();

            Assert.NotNull(measurementsList);
            Assert.IsType<List<Measurement>>(measurementsList);
            Assert.Equal(2, measurementsList.Count);
            Assert.NotNull(measurementsList2);
            Assert.IsType<List<Measurement>>(measurementsList2);
            Assert.Equal(2, measurementsList2.Count);
            Assert.NotNull(firstMeasurement);
            Assert.IsType<Measurement>(firstMeasurement);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Return_Empty_List_Of_Measurement_When_Progeny_Has_No_Saved_Measurements()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMeasurementsList_Should_Return_Empty_List_Of_Measurement_When_Progeny_Has_No_Saved_Measurements").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Measurement measurement1 = new()
            {
                Height = 100.0,
                Weight = 10,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 1
            };


            Measurement measurement2 = new()
            {
                Height = 120.0,
                Weight = 20,
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Circumference = 0,
                CreatedDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                EyeColor = "",
                HairColor = "",
                MeasurementNumber = 2
            };

            context.Add(measurement1);
            context.Add(measurement2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            MeasurementService measurementService = new(context, memoryCache);

            List<Measurement> measurementsList = await measurementService.GetMeasurementsList(2);
            List<Measurement> measurementsList2 = await measurementService.GetMeasurementsList(2); // Test cached result.

            Assert.NotNull(measurementsList);
            Assert.IsType<List<Measurement>>(measurementsList);
            Assert.Empty(measurementsList);
            Assert.NotNull(measurementsList2);
            Assert.IsType<List<Measurement>>(measurementsList2);
            Assert.Empty(measurementsList2);
        }
    }
}
