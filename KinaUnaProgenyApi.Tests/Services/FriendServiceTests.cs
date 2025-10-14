using KinaUna.Data;
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
    public class FriendServiceTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IImageStore> _mockImageStore;

        public FriendServiceTests()
        {
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockImageStore = new Mock<IImageStore>();
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

        private UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        #region GetFriend Tests

        [Fact]
        public async Task GetFriend_Should_Return_Friend_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriend_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend",
                Description = "Test Description",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Context = "Testing",
                CreatedBy = "testuser@test.com",
                CreatedTime = DateTime.UtcNow
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriend(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FriendId);
            Assert.Equal("Test Friend", result.Name);
            Assert.NotNull(result.ItemPerMission);
        }

        [Fact]
        public async Task GetFriend_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriend_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriend(1, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFriend_Should_Return_Null_When_Friend_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriend_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 999, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriend(999, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFriend_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriend_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetFriend(1, userInfo);
            var result2 = await service.GetFriend(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Name, result2.Name);
        }

        #endregion

        #region AddFriend Tests

        [Fact]
        public async Task AddFriend_Should_Add_Friend_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddFriend_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                ProgenyId = 1,
                Name = "New Friend",
                Description = "Test Description",
                PictureLink = Constants.ProfilePictureUrl,
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.AddItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .Returns(Task.CompletedTask);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddFriend(friend, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.FriendId);
            Assert.Equal("New Friend", result.Name);

            var dbFriend = await context.FriendsDb.FindAsync(result.FriendId);
            Assert.NotNull(dbFriend);
            Assert.Equal("New Friend", dbFriend.Name);
        }

        [Fact]
        public async Task AddFriend_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("AddFriend_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                ProgenyId = 1,
                Name = "New Friend"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.Add))
                .ReturnsAsync(false);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.AddFriend(friend, userInfo);

            // Assert
            Assert.Null(result);
            Assert.Empty(context.FriendsDb);
        }

        #endregion

        #region UpdateFriend Tests

        [Fact]
        public async Task UpdateFriend_Should_Update_Friend_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateFriend_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Original Name",
                PictureLink = "original.jpg"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();
            context.Entry(friend).State = EntityState.Detached;

            var updatedFriend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Updated Name",
                PictureLink = "updated.jpg",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateFriend(updatedFriend, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);

            var dbFriend = await context.FriendsDb.FindAsync(1);
            Assert.NotNull(dbFriend);
            Assert.Equal("Updated Name", dbFriend.Name);
        }

        [Fact]
        public async Task UpdateFriend_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateFriend_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Original Name"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            var updatedFriend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Updated Name"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateFriend(updatedFriend, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFriend_Should_Return_Null_When_Friend_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateFriend_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var updatedFriend = new Friend
            {
                FriendId = 999,
                ProgenyId = 1,
                Name = "Updated Name"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 999, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateFriend(updatedFriend, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFriend_Should_Delete_Old_Picture_When_Picture_Changed_And_Not_Used_By_Others()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateFriend_DeleteOldPicture");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend",
                PictureLink = "old-picture.jpg"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();
            context.Entry(friend).State = EntityState.Detached;

            var updatedFriend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend",
                PictureLink = "new-picture.jpg",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            _mockImageStore
                .Setup(x => x.DeleteImage("old-picture.jpg", BlobContainers.Friends))
                .ReturnsAsync("old-picture.jpg");

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateFriend(updatedFriend, userInfo);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage("old-picture.jpg", BlobContainers.Friends), Times.Once);
        }

        [Fact]
        public async Task UpdateFriend_Should_Not_Delete_Old_Picture_When_Used_By_Other_Friends()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("UpdateFriend_KeepSharedPicture");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend1 = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Friend 1",
                PictureLink = "shared-picture.jpg"
            };

            var friend2 = new Friend
            {
                FriendId = 2,
                ProgenyId = 1,
                Name = "Friend 2",
                PictureLink = "shared-picture.jpg"
            };

            context.FriendsDb.AddRange(friend1, friend2);
            await context.SaveChangesAsync();
            context.Entry(friend1).State = EntityState.Detached;

            var updatedFriend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Friend 1",
                PictureLink = "new-picture.jpg",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.UpdateItemPermissions(It.IsAny<KinaUnaTypes.TimeLineType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ItemPermissionDto>>(), userInfo))
                .ReturnsAsync(new List<TimelineItemPermission>());

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.UpdateFriend(updatedFriend, userInfo);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region DeleteFriend Tests

        [Fact]
        public async Task DeleteFriend_Should_Delete_Friend_When_User_Has_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteFriend_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend",
                PictureLink = "test-picture.jpg"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockImageStore
                .Setup(x => x.DeleteImage("test-picture.jpg", BlobContainers.Friends))
                .ReturnsAsync("test-picture.jpg");

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteFriend(friend, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FriendId);

            var dbFriend = await context.FriendsDb.FindAsync(1);
            Assert.Null(dbFriend);
        }

        [Fact]
        public async Task DeleteFriend_Should_Return_Null_When_User_Has_No_Permission()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteFriend_NoPermission");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Test Friend"
            };

            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(false);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteFriend(friend, userInfo);

            // Assert
            Assert.Null(result);
            var dbFriend = await context.FriendsDb.FindAsync(1);
            Assert.NotNull(dbFriend);
        }

        [Fact]
        public async Task DeleteFriend_Should_Return_Null_When_Friend_Does_Not_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteFriend_NotFound");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend
            {
                FriendId = 999,
                ProgenyId = 1,
                Name = "Test Friend"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 999, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteFriend(friend, userInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteFriend_Should_Not_Delete_Picture_When_Used_By_Other_Friends()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("DeleteFriend_KeepSharedPicture");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend1 = new Friend
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "Friend 1",
                PictureLink = "shared-picture.jpg"
            };

            var friend2 = new Friend
            {
                FriendId = 2,
                ProgenyId = 1,
                Name = "Friend 2",
                PictureLink = "shared-picture.jpg"
            };

            context.FriendsDb.AddRange(friend1, friend2);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.DeleteFriend(friend1, userInfo);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region GetFriendsList Tests

        [Fact]
        public async Task GetFriendsList_Should_Return_List_Of_Accessible_Friends()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsList_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2" },
                new Friend { FriendId = 3, ProgenyId = 1, Name = "Friend 3" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetFriendsList_Should_Return_Only_Accessible_Friends()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsList_PartialAccess");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2" },
                new Friend { FriendId = 3, ProgenyId = 1, Name = "Friend 3" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 3, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.FriendId == 1);
            Assert.Contains(result, f => f.FriendId == 3);
        }

        [Fact]
        public async Task GetFriendsList_Should_Return_Empty_List_When_No_Friends_Exist()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsList_Empty");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsList(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFriendsList_Should_Use_Cache_On_Second_Call()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsList_Cache");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1" };
            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result1 = await service.GetFriendsList(1, userInfo);
            var result2 = await service.GetFriendsList(1, userInfo);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Single(result1);
            Assert.Single(result2);
        }

        #endregion

        #region GetFriendsWithTag Tests

        [Fact]
        public async Task GetFriendsWithTag_Should_Return_Friends_With_Matching_Tag()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithTag_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Tags = "school, sports" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2", Tags = "family, school" },
                new Friend { FriendId = 3, ProgenyId = 1, Name = "Friend 3", Tags = "sports, music" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithTag(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.FriendId == 1);
            Assert.Contains(result, f => f.FriendId == 2);
        }

        [Fact]
        public async Task GetFriendsWithTag_Should_Return_All_Friends_When_Tag_Is_Null()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithTag_NoFilter");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Tags = "school" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2", Tags = "sports" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithTag(1, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetFriendsWithTag_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithTag_CaseInsensitive");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Tags = "School, Sports" };
            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithTag(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region GetFriendsWithContext Tests

        [Fact]
        public async Task GetFriendsWithContext_Should_Return_Friends_With_Matching_Context()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithContext_Valid");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Context = "school activities" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2", Context = "family events" },
                new Friend { FriendId = 3, ProgenyId = 1, Name = "Friend 3", Context = "school sports" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithContext(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.FriendId == 1);
            Assert.Contains(result, f => f.FriendId == 3);
        }

        [Fact]
        public async Task GetFriendsWithContext_Should_Return_All_Friends_When_Context_Is_Null()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithContext_NoFilter");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friends = new List<Friend>
            {
                new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Context = "school" },
                new Friend { FriendId = 2, ProgenyId = 1, Name = "Friend 2", Context = "sports" }
            };

            context.FriendsDb.AddRange(friends);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithContext(1, null, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetFriendsWithContext_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithContext_CaseInsensitive");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Context = "School Activities" };
            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithContext(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetFriendsWithContext_Should_Match_Substring()
        {
            // Arrange
            await using var context = GetInMemoryDbContext("GetFriendsWithContext_Substring");
            var cache = GetMemoryCache();
            var userInfo = CreateTestUserInfo();

            var friend = new Friend { FriendId = 1, ProgenyId = 1, Name = "Friend 1", Context = "After school activities" };
            context.FriendsDb.Add(friend);
            await context.SaveChangesAsync();

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, userInfo, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            var service = new FriendService(context, cache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Act
            var result = await service.GetFriendsWithContext(1, "school", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion
    }
}