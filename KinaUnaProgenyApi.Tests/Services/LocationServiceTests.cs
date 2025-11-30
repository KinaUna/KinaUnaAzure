//using KinaUna.Data.Contexts;
//using KinaUna.Data.Models;
//using KinaUna.Data.Models.AccessManagement;
//using KinaUna.Data.Models.DTOs;
//using KinaUnaProgenyApi.Services;
//using KinaUnaProgenyApi.Services.AccessManagementService;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Options;
//using Moq;

//namespace KinaUnaProgenyApi.Tests.Services
//{
//    public class LocationServiceTests
//    {
//        private readonly Mock<IAccessManagementService> _mockAccessManagementService;

//        public LocationServiceTests()
//        {
//            _mockAccessManagementService = new Mock<IAccessManagementService>();
//        }

//        private static ProgenyDbContext GetInMemoryDbContext(string dbName)
//        {
//            var options = new DbContextOptionsBuilder<ProgenyDbContext>()
//                .UseInMemoryDatabase(databaseName: dbName)
//                .Options;
//            return new ProgenyDbContext(options);
//        }

//        private static IDistributedCache GetMemoryCache()
//        {
//            var options = Options.Create(new MemoryDistributedCacheOptions());
//            return new MemoryDistributedCache(options);
//        }

//        private UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
//        {
//            return new UserInfo
//            {
//                UserId = userId,
//                UserEmail = userId
//            };
//        }

//        #region GetLocation Tests

//        [Fact]
//        public async Task GetLocation_Should_Return_Location_When_User_Has_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocation_Valid");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location",
//                Latitude = 40.7128,
//                Longtitude = -74.0060,
//                City = "New York",
//                Country = "USA",
//                Tags = "Tag1, Tag2"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocation(1, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.LocationId);
//            Assert.Equal("Test Location", result.Name);
//            Assert.NotNull(result.ItemPerMission);
//        }

//        [Fact]
//        public async Task GetLocation_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocation_NoPermission");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(false);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocation(1, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task GetLocation_Should_Return_Null_When_Location_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocation_NotFound");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 999, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocation(999, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task GetLocation_Should_Use_Cache_On_Second_Call()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocation_Cache");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result1 = await service.GetLocation(1, userInfo);
//            var result2 = await service.GetLocation(1, userInfo);

//            // Assert
//            Assert.NotNull(result1);
//            Assert.NotNull(result2);
//            Assert.Equal(result1.Name, result2.Name);
//        }

//        #endregion

//        #region AddLocation Tests

//        [Fact]
//        public async Task AddLocation_Should_Add_Location_When_User_Has_Progeny_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddLocation_ValidProgeny");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "New Location",
//                City = "Boston",
//                Country = "USA",
//                ItemPermissionsDtoList = new List<ItemPermissionDto>()
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
//                .Returns(Task.CompletedTask);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddLocation(location, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEqual(0, result.LocationId);
//            Assert.Equal("New Location", result.Name);

//            var dbLocation = await context.LocationsDb.FindAsync(result.LocationId);
//            Assert.NotNull(dbLocation);
//            Assert.Equal("New Location", dbLocation.Name);
//        }

//        [Fact]
//        public async Task AddLocation_Should_Add_Location_When_User_Has_Family_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddLocation_ValidFamily");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                ProgenyId = 0,
//                FamilyId = 1,
//                Name = "New Location",
//                City = "Boston",
//                ItemPermissionsDtoList = new List<ItemPermissionDto>()
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasFamilyPermission(1, userInfo, PermissionLevel.Add))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
//                .Returns(Task.CompletedTask);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddLocation(location, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEqual(0, result.LocationId);
//            Assert.Equal("New Location", result.Name);
//        }

//        [Fact]
//        public async Task AddLocation_Should_Return_Null_When_Both_ProgenyId_And_FamilyId_Are_Set()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddLocation_BothIds");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                ProgenyId = 1,
//                FamilyId = 1,
//                Name = "Invalid Location"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddLocation(location, userInfo);

//            // Assert
//            Assert.Null(result);
//            Assert.Empty(context.LocationsDb);
//        }

//        [Fact]
//        public async Task AddLocation_Should_Return_Null_When_Neither_ProgenyId_Nor_FamilyId_Are_Set()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddLocation_NoIds");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                ProgenyId = 0,
//                FamilyId = 0,
//                Name = "Invalid Location"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddLocation(location, userInfo);

//            // Assert
//            Assert.Null(result);
//            Assert.Empty(context.LocationsDb);
//        }

//        [Fact]
//        public async Task AddLocation_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddLocation_NoPermission");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "New Location"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
//                .ReturnsAsync(false);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddLocation(location, userInfo);

//            // Assert
//            Assert.Null(result);
//            Assert.Empty(context.LocationsDb);
//        }

//        #endregion

//        #region UpdateLocation Tests

//        [Fact]
//        public async Task UpdateLocation_Should_Update_Location_When_User_Has_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateLocation_Valid");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Original Name",
//                City = "Original City"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();
//            context.Entry(location).State = EntityState.Detached;

//            var updatedLocation = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Updated Name",
//                City = "Updated City",
//                ItemPermissionsDtoList = new List<ItemPermissionDto>()
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.Edit))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
//                .ReturnsAsync(new List<TimelineItemPermission>());

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateLocation(updatedLocation, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("Updated Name", result.Name);

//            var dbLocation = await context.LocationsDb.FindAsync(1);
//            Assert.NotNull(dbLocation);
//            Assert.Equal("Updated Name", dbLocation.Name);
//        }

//        [Fact]
//        public async Task UpdateLocation_Should_Return_Null_When_Both_ProgenyId_And_FamilyId_Are_Set()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateLocation_BothIds");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var updatedLocation = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 1,
//                Name = "Invalid Location"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateLocation(updatedLocation, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateLocation_Should_Return_Null_When_Neither_ProgenyId_Nor_FamilyId_Are_Set()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateLocation_NoIds");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var updatedLocation = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 0,
//                FamilyId = 0,
//                Name = "Invalid Location"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateLocation(updatedLocation, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateLocation_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateLocation_NoPermission");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Original Name"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            var updatedLocation = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Updated Name"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.Edit))
//                .ReturnsAsync(false);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateLocation(updatedLocation, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task UpdateLocation_Should_Return_Null_When_Location_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateLocation_NotFound");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var updatedLocation = new Location
//            {
//                LocationId = 999,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Updated Name"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 999, userInfo, PermissionLevel.Edit))
//                .ReturnsAsync(true);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateLocation(updatedLocation, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region DeleteLocation Tests

//        [Fact]
//        public async Task DeleteLocation_Should_Delete_Location_When_User_Has_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("DeleteLocation_Valid");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.DeleteLocation(location, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.LocationId);

//            var dbLocation = await context.LocationsDb.FindAsync(1);
//            Assert.Null(dbLocation);
//        }

//        [Fact]
//        public async Task DeleteLocation_Should_Return_Null_When_User_Has_No_Permission()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("DeleteLocation_NoPermission");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 1,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location"
//            };

//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(false);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.DeleteLocation(location, userInfo);

//            // Assert
//            Assert.Null(result);
//            var dbLocation = await context.LocationsDb.FindAsync(1);
//            Assert.NotNull(dbLocation);
//        }

//        [Fact]
//        public async Task DeleteLocation_Should_Return_Null_When_Location_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("DeleteLocation_NotFound");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location
//            {
//                LocationId = 999,
//                ProgenyId = 1,
//                FamilyId = 0,
//                Name = "Test Location"
//            };

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 999, userInfo, PermissionLevel.Admin))
//                .ReturnsAsync(true);

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.DeleteLocation(location, userInfo);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region GetLocationsList Tests

//        [Fact]
//        public async Task GetLocationsList_Should_Return_List_Of_Accessible_Locations()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsList_Valid");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var locations = new List<Location>
//            {
//                new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1" },
//                new Location { LocationId = 2, ProgenyId = 1, FamilyId = 0, Name = "Location 2" },
//                new Location { LocationId = 3, ProgenyId = 1, FamilyId = 0, Name = "Location 3" }
//            };

//            context.LocationsDb.AddRange(locations);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsList(1, 0, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(3, result.Count);
//        }

//        [Fact]
//        public async Task GetLocationsList_Should_Return_Only_Accessible_Locations()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsList_PartialAccess");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var locations = new List<Location>
//            {
//                new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1" },
//                new Location { LocationId = 2, ProgenyId = 1, FamilyId = 0, Name = "Location 2" },
//                new Location { LocationId = 3, ProgenyId = 1, FamilyId = 0, Name = "Location 3" }
//            };

//            context.LocationsDb.AddRange(locations);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 2, userInfo, PermissionLevel.View))
//                .ReturnsAsync(false);

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 3, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsList(1, 0, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(2, result.Count);
//            Assert.Contains(result, l => l.LocationId == 1);
//            Assert.Contains(result, l => l.LocationId == 3);
//        }

//        [Fact]
//        public async Task GetLocationsList_Should_Return_Empty_List_When_No_Locations_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsList_Empty");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsList(1, 0, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Empty(result);
//        }

//        [Fact]
//        public async Task GetLocationsList_Should_Use_Cache_On_Second_Call()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsList_Cache");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1" };
//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result1 = await service.GetLocationsList(1, 0, userInfo);
//            var result2 = await service.GetLocationsList(1, 0, userInfo);

//            // Assert
//            Assert.NotNull(result1);
//            Assert.NotNull(result2);
//            Assert.Single(result1);
//            Assert.Single(result2);
//        }

//        #endregion

//        #region GetLocationsWithTag Tests

//        [Fact]
//        public async Task GetLocationsWithTag_Should_Return_Locations_With_Matching_Tag()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsWithTag_Valid");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var locations = new List<Location>
//            {
//                new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1", Tags = "park, playground" },
//                new Location { LocationId = 2, ProgenyId = 1, FamilyId = 0, Name = "Location 2", Tags = "museum, park" },
//                new Location { LocationId = 3, ProgenyId = 1, FamilyId = 0, Name = "Location 3", Tags = "beach, outdoor" }
//            };

//            context.LocationsDb.AddRange(locations);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsWithTag(1, 0, "park", userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(2, result.Count);
//            Assert.Contains(result, l => l.LocationId == 1);
//            Assert.Contains(result, l => l.LocationId == 2);
//        }

//        [Fact]
//        public async Task GetLocationsWithTag_Should_Return_All_Locations_When_Tag_Is_Null()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsWithTag_NoFilter");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var locations = new List<Location>
//            {
//                new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1", Tags = "park" },
//                new Location { LocationId = 2, ProgenyId = 1, FamilyId = 0, Name = "Location 2", Tags = "beach" }
//            };

//            context.LocationsDb.AddRange(locations);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, It.IsAny<int>(), 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsWithTag(1, 0, null, userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(2, result.Count);
//        }

//        [Fact]
//        public async Task GetLocationsWithTag_Should_Be_Case_Insensitive()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetLocationsWithTag_CaseInsensitive");
//            var cache = GetMemoryCache();
//            var userInfo = CreateTestUserInfo();

//            var location = new Location { LocationId = 1, ProgenyId = 1, FamilyId = 0, Name = "Location 1", Tags = "Park, Beach" };
//            context.LocationsDb.Add(location);
//            await context.SaveChangesAsync();

//            _mockAccessManagementService
//                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Location, 1, userInfo, PermissionLevel.View))
//                .ReturnsAsync(true);

//            _mockAccessManagementService
//                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, 1, 1, 0, userInfo, null))
//                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetLocationsWithTag(1, 0, "park", userInfo);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Single(result);
//        }

//        #endregion

//        #region GetAddressItem Tests

//        [Fact]
//        public async Task GetAddressItem_Should_Return_Address_When_Valid_Id()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetAddressItem_Valid");
//            var cache = GetMemoryCache();

//            var address = new Address
//            {
//                AddressId = 1,
//                AddressLine1 = "123 Main St",
//                City = "Boston",
//                State = "MA",
//                PostalCode = "02101",
//                Country = "USA"
//            };

//            context.AddressDb.Add(address);
//            await context.SaveChangesAsync();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetAddressItem(1);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(1, result.AddressId);
//            Assert.Equal("123 Main St", result.AddressLine1);
//        }

//        [Fact]
//        public async Task GetAddressItem_Should_Return_Null_When_Address_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetAddressItem_NotFound");
//            var cache = GetMemoryCache();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.GetAddressItem(999);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task GetAddressItem_Should_Use_Cache_On_Second_Call()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("GetAddressItem_Cache");
//            var cache = GetMemoryCache();

//            var address = new Address
//            {
//                AddressId = 1,
//                AddressLine1 = "123 Main St",
//                City = "Boston"
//            };

//            context.AddressDb.Add(address);
//            await context.SaveChangesAsync();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result1 = await service.GetAddressItem(1);
//            var result2 = await service.GetAddressItem(1);

//            // Assert
//            Assert.NotNull(result1);
//            Assert.NotNull(result2);
//            Assert.Equal(result1.AddressLine1, result2.AddressLine1);
//        }

//        #endregion

//        #region AddAddressItem Tests

//        [Fact]
//        public async Task AddAddressItem_Should_Add_Address()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("AddAddressItem_Valid");
//            var cache = GetMemoryCache();

//            var address = new Address
//            {
//                AddressLine1 = "456 Oak Ave",
//                City = "Chicago",
//                State = "IL",
//                PostalCode = "60601",
//                Country = "USA"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.AddAddressItem(address);

//            // Assert
//            Assert.NotNull(result);
//            Assert.NotEqual(0, result.AddressId);
//            Assert.Equal("456 Oak Ave", result.AddressLine1);

//            var dbAddress = await context.AddressDb.FindAsync(result.AddressId);
//            Assert.NotNull(dbAddress);
//            Assert.Equal("456 Oak Ave", dbAddress.AddressLine1);
//        }

//        #endregion

//        #region UpdateAddressItem Tests

//        [Fact]
//        public async Task UpdateAddressItem_Should_Update_Address()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateAddressItem_Valid");
//            var cache = GetMemoryCache();

//            var address = new Address
//            {
//                AddressId = 1,
//                AddressLine1 = "Original Address",
//                City = "Original City"
//            };

//            context.AddressDb.Add(address);
//            await context.SaveChangesAsync();
//            context.Entry(address).State = EntityState.Detached;

//            var updatedAddress = new Address
//            {
//                AddressId = 1,
//                AddressLine1 = "Updated Address",
//                City = "Updated City"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateAddressItem(updatedAddress);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("Updated Address", result.AddressLine1);

//            var dbAddress = await context.AddressDb.FindAsync(1);
//            Assert.NotNull(dbAddress);
//            Assert.Equal("Updated Address", dbAddress.AddressLine1);
//        }

//        [Fact]
//        public async Task UpdateAddressItem_Should_Return_Null_When_Address_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("UpdateAddressItem_NotFound");
//            var cache = GetMemoryCache();

//            var updatedAddress = new Address
//            {
//                AddressId = 999,
//                AddressLine1 = "Updated Address"
//            };

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            var result = await service.UpdateAddressItem(updatedAddress);

//            // Assert
//            Assert.Null(result);
//        }

//        #endregion

//        #region RemoveAddressItem Tests

//        [Fact]
//        public async Task RemoveAddressItem_Should_Remove_Address()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("RemoveAddressItem_Valid");
//            var cache = GetMemoryCache();

//            var address = new Address
//            {
//                AddressId = 1,
//                AddressLine1 = "Test Address"
//            };

//            context.AddressDb.Add(address);
//            await context.SaveChangesAsync();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act
//            await service.RemoveAddressItem(1);

//            // Assert
//            var dbAddress = await context.AddressDb.FindAsync(1);
//            Assert.Null(dbAddress);
//        }

//        [Fact]
//        public async Task RemoveAddressItem_Should_Not_Throw_When_Address_Does_Not_Exist()
//        {
//            // Arrange
//            await using var context = GetInMemoryDbContext("RemoveAddressItem_NotFound");
//            var cache = GetMemoryCache();

//            var service = new LocationService(context, cache, _mockAccessManagementService.Object);

//            // Act & Assert
//            await service.RemoveAddressItem(999); // Should not throw
//        }

//        #endregion
//    }
//}