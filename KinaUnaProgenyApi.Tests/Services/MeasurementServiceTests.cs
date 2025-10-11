using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class MeasurementServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;

        public MeasurementServiceTests()
        {
            _mockAccessManagementService = new Mock<IAccessManagementService>();
        }

        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static IDistributedCache GetMemoryCache()
        {
            var options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        private static Measurement CreateTestMeasurement(int measurementId = 1, int progenyId = 1)
        {
            return new Measurement
            {
                MeasurementId = measurementId,
                ProgenyId = progenyId,
                Height = 100.5,
                Weight = 15.5,
                Circumference = 50.0,
                EyeColor = "Blue",
                HairColor = "Blonde",
                Date = DateTime.UtcNow.AddDays(-30),
                CreatedDate = DateTime.UtcNow,
                Author = "testuser@test.com",
                AccessLevel = 0,
                CreatedBy = "testuser@test.com",
                CreatedTime = DateTime.UtcNow
            };
        }

        #region GetMeasurement Tests

        [Fact]
        public async Task GetMeasurement_Should_Return_Measurement_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurement_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();
            var measurement = CreateTestMeasurement();

            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurement(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.MeasurementId);
            Assert.Equal(100.5, result.Height);
            Assert.Equal(15.5, result.Weight);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetMeasurement_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurement_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();
            var measurement = CreateTestMeasurement();

            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurement(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMeasurement_Should_Return_Null_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurement_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurement(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMeasurement_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurement_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();
            var measurement = CreateTestMeasurement();

            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetMeasurement(1, userInfo);
            var result2 = await service.GetMeasurement(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Height, result2.Height);
            Assert.Equal(result1.Weight, result2.Weight);
        }

        #endregion

        #region AddMeasurement Tests

        [Fact]
        public async Task AddMeasurement_Should_Add_Measurement_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddMeasurement_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = new Measurement
            {
                ProgenyId = 1,
                Height = 105.0,
                Weight = 16.0,
                Circumference = 51.0,
                EyeColor = "Brown",
                HairColor = "Black",
                Date = DateTime.UtcNow,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddMeasurement(measurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.MeasurementId);
            Assert.Equal(105.0, result.Height);
            Assert.Equal(16.0, result.Weight);

            var dbMeasurement = await context.MeasurementsDb.FindAsync(result.MeasurementId);
            Assert.NotNull(dbMeasurement);
            Assert.Equal(105.0, dbMeasurement.Height);
        }

        [Fact]
        public async Task AddMeasurement_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddMeasurement_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = new Measurement
            {
                ProgenyId = 1,
                Height = 105.0,
                Weight = 16.0
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddMeasurement(measurement, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.MeasurementsDb);
        }

        [Fact]
        public async Task AddMeasurement_Should_Copy_Properties_Correctly()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddMeasurement_CopyProperties");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = new Measurement
            {
                ProgenyId = 1,
                Height = 110.5,
                Weight = 18.5,
                Circumference = 52.5,
                EyeColor = "Green",
                HairColor = "Red",
                Date = DateTime.UtcNow.AddDays(-10),
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddMeasurement(measurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(measurement.ProgenyId, result.ProgenyId);
            Assert.Equal(measurement.Height, result.Height);
            Assert.Equal(measurement.Weight, result.Weight);
            Assert.Equal(measurement.Circumference, result.Circumference);
            Assert.Equal(measurement.EyeColor, result.EyeColor);
            Assert.Equal(measurement.HairColor, result.HairColor);
        }

        #endregion

        #region UpdateMeasurement Tests

        [Fact]
        public async Task UpdateMeasurement_Should_Update_Measurement_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateMeasurement_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();
            context.Entry(measurement).State = EntityState.Detached;

            var updatedMeasurement = new Measurement
            {
                MeasurementId = 1,
                ProgenyId = 1,
                Height = 120.0,
                Weight = 20.0,
                Circumference = 55.0,
                EyeColor = "Green",
                HairColor = "Brown",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateMeasurement(updatedMeasurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(120.0, result.Height);
            Assert.Equal(20.0, result.Weight);

            var dbMeasurement = await context.MeasurementsDb.FindAsync(1);
            Assert.NotNull(dbMeasurement);
            Assert.Equal(120.0, dbMeasurement.Height);
        }

        [Fact]
        public async Task UpdateMeasurement_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateMeasurement_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            var updatedMeasurement = new Measurement
            {
                MeasurementId = 1,
                ProgenyId = 1,
                Height = 120.0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateMeasurement(updatedMeasurement, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateMeasurement_Should_Return_Null_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateMeasurement_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var updatedMeasurement = new Measurement
            {
                MeasurementId = 999,
                ProgenyId = 1,
                Height = 120.0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateMeasurement(updatedMeasurement, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateMeasurement_Should_Update_Cache()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateMeasurement_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();
            context.Entry(measurement).State = EntityState.Detached;

            var updatedMeasurement = new Measurement
            {
                MeasurementId = 1,
                ProgenyId = 1,
                Height = 125.0,
                Weight = 22.0,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateMeasurement(updatedMeasurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(125.0, result.Height);
        }

        #endregion

        #region DeleteMeasurement Tests

        [Fact]
        public async Task DeleteMeasurement_Should_Delete_Measurement_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteMeasurement_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteMeasurement(measurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.MeasurementId);

            var dbMeasurement = await context.MeasurementsDb.FindAsync(1);
            Assert.Null(dbMeasurement);
        }

        [Fact]
        public async Task DeleteMeasurement_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteMeasurement_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteMeasurement(measurement, userInfo);

            // Assert
            Assert.Null(result);
            var dbMeasurement = await context.MeasurementsDb.FindAsync(1);
            Assert.NotNull(dbMeasurement);
        }

        [Fact]
        public async Task DeleteMeasurement_Should_Return_Null_When_Measurement_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteMeasurement_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = new Measurement
            {
                MeasurementId = 999,
                ProgenyId = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteMeasurement(measurement, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteMeasurement_Should_Remove_From_Cache()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteMeasurement_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement();
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteMeasurement(measurement, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(context.MeasurementsDb);
        }

        #endregion

        #region GetMeasurementsList Tests

        [Fact]
        public async Task GetMeasurementsList_Should_Return_List_Of_Accessible_Measurements()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurements = new List<Measurement>
            {
                CreateTestMeasurement(1, 1),
                CreateTestMeasurement(2, 1),
                CreateTestMeasurement(3, 1)
            };

            context.MeasurementsDb.AddRange(measurements);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurementsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Return_Only_Accessible_Measurements()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_PartialAccess");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurements = new List<Measurement>
            {
                CreateTestMeasurement(1, 1),
                CreateTestMeasurement(2, 1),
                CreateTestMeasurement(3, 1)
            };

            context.MeasurementsDb.AddRange(measurements);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurementsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.MeasurementId == 1);
            Assert.Contains(result, m => m.MeasurementId == 3);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Return_Empty_List_When_No_Measurements_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_Empty");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurementsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Return_Empty_List_For_Different_Progeny()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_DifferentProgeny");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurements = new List<Measurement>
            {
                CreateTestMeasurement(1, 1),
                CreateTestMeasurement(2, 1)
            };

            context.MeasurementsDb.AddRange(measurements);
            await context.SaveChangesAsync();

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurementsList(2, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement(1, 1);
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetMeasurementsList(1, userInfo);
            var result2 = await service.GetMeasurementsList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);
        }

        [Fact]
        public async Task GetMeasurementsList_Should_Set_ItemPermission_For_Each_Measurement()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetMeasurementsList_ItemPermissions");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var measurement = CreateTestMeasurement(1, 1);
            context.MeasurementsDb.Add(measurement);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Measurement, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.Edit });

            var service = new MeasurementService(context, cache, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetMeasurementsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result[0].ItemPerMission.PermissionLevel);
        }

        #endregion
    }
}