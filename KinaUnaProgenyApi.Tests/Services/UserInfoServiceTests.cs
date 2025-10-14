using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Newtonsoft.Json;
using System.Text;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class UserInfoServiceTests
    {
        private readonly Mock<IDistributedCache> _mockCache = new();
        private readonly Mock<IImageStore> _mockImageStore = new();
        private readonly Mock<IProgenyService> _mockProgenyService = new();
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<IUserGroupsService> _mockUserGroupsService = new();
        private readonly Mock<IFamilyMembersService> _mockFamilyMembersService = new();

        private ProgenyDbContext CreateInMemoryDatabase()
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ProgenyDbContext(options);
        }

        private UserInfoService CreateService(ProgenyDbContext context)
        {
            return new UserInfoService(
                context,
                _mockCache.Object,
                _mockImageStore.Object,
                _mockProgenyService.Object,
                _mockAccessManagementService.Object,
                _mockUserGroupsService.Object,
                _mockFamilyMembersService.Object);
        }

        private UserInfo CreateTestUserInfo(int id = 1, string email = "test@test.com", string userId = "user123")
        {
            return new UserInfo
            {
                Id = id,
                UserEmail = email,
                UserId = userId,
                UserName = "Test User",
                FirstName = "Test",
                MiddleName = "Middle",
                LastName = "User",
                ProfilePicture = Constants.ProfilePictureUrl,
                Timezone = "UTC",
                PhoneNumber = "1234567890",
                IsKinaUnaAdmin = false,
                Deleted = false
            };
        }

        #region GetAllUserInfos Tests

        [Fact]
        public async Task GetAllUserInfos_ReturnsAllUsers()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            UserInfo user1 = CreateTestUserInfo(1, "user1@test.com", "userId1");
            UserInfo user2 = CreateTestUserInfo(2, "user2@test.com", "userId2");
            UserInfo user3 = CreateTestUserInfo(3, "user3@test.com", "userId3");

            context.UserInfoDb.AddRange(user1, user2, user3);
            await context.SaveChangesAsync();

            // Act
            List<UserInfo>? result = await service.GetAllUserInfos();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, u => u.UserEmail == "user1@test.com");
            Assert.Contains(result, u => u.UserEmail == "user2@test.com");
            Assert.Contains(result, u => u.UserEmail == "user3@test.com");
        }

        [Fact]
        public async Task GetAllUserInfos_ReturnsEmptyList_WhenNoUsers()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            // Act
            List<UserInfo>? result = await service.GetAllUserInfos();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetUserInfoByEmail Tests

        [Fact]
        public async Task GetUserInfoByEmail_ReturnsUserFromCache_WhenCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();
            string cachedJson = JsonConvert.SerializeObject(testUser);

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

            // Act
            UserInfo? result = await service.GetUserInfoByEmail("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserEmail, result.UserEmail);
            Assert.Equal(testUser.Id, result.Id);
        }

        [Fact]
        public async Task GetUserInfoByEmail_ReturnsUserFromDatabase_WhenNotCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoByEmail("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserEmail, result.UserEmail);
            Assert.Equal(testUser.Id, result.Id);

            // Verify cache was set
            _mockCache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetUserInfoByEmail_TrimsEmail()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoByEmail("  test@test.com  ");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserEmail, result.UserEmail);
        }

        [Fact]
        public async Task GetUserInfoByEmail_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoByEmail("nonexistent@test.com");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region AddUserInfo Tests

        [Fact]
        public async Task AddUserInfo_AddsUserSuccessfully()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo newUser = CreateTestUserInfo();
            newUser.Id = 0; // EF will assign ID

            _mockProgenyService.Setup(s => s.UpdateProgeniesForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockAccessManagementService.Setup(s => s.UpdatePermissionsForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockUserGroupsService.Setup(s => s.UpdateUserGroupMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockFamilyMembersService.Setup(s => s.UpdateFamilyMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);

            // Act
            UserInfo? result = await service.AddUserInfo(newUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newUser.UserEmail, result.UserEmail);
            UserInfo? savedUser = await context.UserInfoDb.FirstOrDefaultAsync(u => u.UserEmail == newUser.UserEmail);
            Assert.NotNull(savedUser);

            // Verify all dependent services were called
            _mockProgenyService.Verify(s => s.UpdateProgeniesForNewUser(It.IsAny<UserInfo>()), Times.Once);
            _mockAccessManagementService.Verify(s => s.UpdatePermissionsForNewUser(It.IsAny<UserInfo>()), Times.Once);
            _mockUserGroupsService.Verify(s => s.UpdateUserGroupMembersForNewUser(It.IsAny<UserInfo>()), Times.Once);
            _mockFamilyMembersService.Verify(s => s.UpdateFamilyMembersForNewUser(It.IsAny<UserInfo>()), Times.Once);
        }

        [Fact]
        public async Task AddUserInfo_SetsDefaultFirstName_WhenNull()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo newUser = CreateTestUserInfo();
            newUser.FirstName = null;

            _mockProgenyService.Setup(s => s.UpdateProgeniesForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockAccessManagementService.Setup(s => s.UpdatePermissionsForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockUserGroupsService.Setup(s => s.UpdateUserGroupMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockFamilyMembersService.Setup(s => s.UpdateFamilyMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);

            // Act
            UserInfo? result = await service.AddUserInfo(newUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.FirstName);
        }

        [Fact]
        public async Task AddUserInfo_SetsDefaultProfilePicture_WhenNull()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo newUser = CreateTestUserInfo();
            newUser.ProfilePicture = null;

            _mockProgenyService.Setup(s => s.UpdateProgeniesForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockAccessManagementService.Setup(s => s.UpdatePermissionsForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockUserGroupsService.Setup(s => s.UpdateUserGroupMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);
            _mockFamilyMembersService.Setup(s => s.UpdateFamilyMembersForNewUser(It.IsAny<UserInfo>()))
                .Returns(Task.CompletedTask);

            // Act
            UserInfo? result = await service.AddUserInfo(newUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Constants.ProfilePictureUrl, result.ProfilePicture);
        }

        #endregion

        #region UpdateUserInfo Tests

        [Fact]
        public async Task UpdateUserInfo_UpdatesUserSuccessfully()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo existingUser = CreateTestUserInfo();

            context.UserInfoDb.Add(existingUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedUser = CreateTestUserInfo();
            updatedUser.FirstName = "Updated";
            updatedUser.LastName = "Name";
            updatedUser.Timezone = "America/New_York";

            // Act
            UserInfo? result = await service.UpdateUserInfo(updatedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated", result.FirstName);
            Assert.Equal("Name", result.LastName);
            Assert.Equal("America/New_York", result.Timezone);
        }

        [Fact]
        public async Task UpdateUserInfo_DeletesOldProfilePicture_WhenChanged()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo existingUser = CreateTestUserInfo();
            existingUser.ProfilePicture = "old-picture.jpg";

            context.UserInfoDb.Add(existingUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedUser = CreateTestUserInfo();
            updatedUser.ProfilePicture = "new-picture.jpg";

            // Act
            UserInfo? result = await service.UpdateUserInfo(updatedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-picture.jpg", result.ProfilePicture);
            _mockImageStore.Verify(i => i.DeleteImage("old-picture.jpg", BlobContainers.Profiles), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfo_DoesNotDeletePicture_WhenUnchanged()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo existingUser = CreateTestUserInfo();
            existingUser.ProfilePicture = "same-picture.jpg";

            context.UserInfoDb.Add(existingUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedUser = CreateTestUserInfo();
            updatedUser.ProfilePicture = "same-picture.jpg";

            // Act
            UserInfo? result = await service.UpdateUserInfo(updatedUser);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(i => i.DeleteImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserInfo_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo nonExistentUser = CreateTestUserInfo(999);

            // Act
            UserInfo? result = await service.UpdateUserInfo(nonExistentUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserInfo_SetsDefaultProfilePicture_WhenEmpty()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo existingUser = CreateTestUserInfo();

            context.UserInfoDb.Add(existingUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedUser = CreateTestUserInfo();
            updatedUser.ProfilePicture = "";

            // Act
            UserInfo? result = await service.UpdateUserInfo(updatedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Constants.ProfilePictureUrl, result.ProfilePicture);
        }

        #endregion

        #region DeleteUserInfo Tests

        [Fact]
        public async Task DeleteUserInfo_DeletesUserSuccessfully()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo userToDelete = CreateTestUserInfo();

            context.UserInfoDb.Add(userToDelete);
            await context.SaveChangesAsync();

            // Act
            UserInfo? result = await service.DeleteUserInfo(userToDelete);

            // Assert
            Assert.NotNull(result);
            UserInfo? deletedUser = await context.UserInfoDb.FirstOrDefaultAsync(u => u.Id == userToDelete.Id);
            Assert.Null(deletedUser);
            _mockImageStore.Verify(i => i.DeleteImage(userToDelete.ProfilePicture, BlobContainers.Profiles), Times.Once);
        }

        [Fact]
        public async Task DeleteUserInfo_RemovesCacheEntries()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo userToDelete = CreateTestUserInfo();

            context.UserInfoDb.Add(userToDelete);
            await context.SaveChangesAsync();

            // Act
            await service.DeleteUserInfo(userToDelete);

            // Assert
            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobymail")),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobyuserid")),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobyid")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUserInfo_ReturnsUser_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo nonExistentUser = CreateTestUserInfo(999);

            // Act
            UserInfo? result = await service.DeleteUserInfo(nonExistentUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nonExistentUser.Id, result.Id);
        }

        #endregion

        #region GetUserInfoById Tests

        [Fact]
        public async Task GetUserInfoById_ReturnsUserFromCache_WhenCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();
            string cachedJson = JsonConvert.SerializeObject(testUser);

            _mockCache.Setup(c => c.GetAsync(
                It.Is<string>(s => s.Contains("userinfobyid")),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

            // Act
            UserInfo? result = await service.GetUserInfoById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.Id, result.Id);
        }

        [Fact]
        public async Task GetUserInfoById_ReturnsUserFromDatabase_WhenNotCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.Id, result.Id);
        }

        [Fact]
        public async Task GetUserInfoById_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoById(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserInfoByUserId Tests

        [Fact]
        public async Task GetUserInfoByUserId_ReturnsUserFromCache_WhenCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();
            string cachedJson = JsonConvert.SerializeObject(testUser);

            _mockCache.Setup(c => c.GetAsync(
                It.Is<string>(s => s.Contains("userinfobyuserid")),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

            // Act
            UserInfo? result = await service.GetUserInfoByUserId("user123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserId, result.UserId);
        }

        [Fact]
        public async Task GetUserInfoByUserId_ReturnsUserFromDatabase_WhenNotCached()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoByUserId("user123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserId, result.UserId);
        }

        [Fact]
        public async Task GetUserInfoByUserId_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null!);

            // Act
            UserInfo? result = await service.GetUserInfoByUserId("nonexistent");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetDeletedUserInfos Tests

        [Fact]
        public async Task GetDeletedUserInfos_ReturnsOnlyDeletedUsers()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            UserInfo activeUser = CreateTestUserInfo(1, "active@test.com", "active123");
            activeUser.Deleted = false;

            UserInfo deletedUser1 = CreateTestUserInfo(2, "deleted1@test.com", "deleted1");
            deletedUser1.Deleted = true;

            UserInfo deletedUser2 = CreateTestUserInfo(3, "deleted2@test.com", "deleted2");
            deletedUser2.Deleted = true;

            context.UserInfoDb.AddRange(activeUser, deletedUser1, deletedUser2);
            await context.SaveChangesAsync();

            // Act
            List<UserInfo>? result = await service.GetDeletedUserInfos();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, u => Assert.True(u.Deleted));
            Assert.DoesNotContain(result, u => u.UserEmail == "active@test.com");
        }

        [Fact]
        public async Task GetDeletedUserInfos_ReturnsEmptyList_WhenNoDeletedUsers()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            UserInfo activeUser = CreateTestUserInfo();
            activeUser.Deleted = false;

            context.UserInfoDb.Add(activeUser);
            await context.SaveChangesAsync();

            // Act
            List<UserInfo>? result = await service.GetDeletedUserInfos();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region AddUserInfoToDeletedUserInfos Tests

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_AddsNewEntry_WhenNotExists()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo userInfo = CreateTestUserInfo();

            // Act
            UserInfo? result = await service.AddUserInfoToDeletedUserInfos(userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userInfo.UserName, result.UserName);
            Assert.Equal(userInfo.UserId, result.UserId);
            Assert.Equal(userInfo.UserEmail, result.UserEmail);
            Assert.False(result.Deleted);
            
            UserInfo? deletedUser = await context.DeletedUsers.FirstOrDefaultAsync(u => u.UserId == userInfo.UserId);
            Assert.NotNull(deletedUser);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_UpdatesExisting_WhenExists()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo existingDeleted = CreateTestUserInfo();
            existingDeleted.UserName = "Old Name";

            context.DeletedUsers.Add(existingDeleted);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedInfo = CreateTestUserInfo();
            updatedInfo.UserName = "New Name";

            // Act
            UserInfo? result = await service.AddUserInfoToDeletedUserInfos(updatedInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.UserName);

            UserInfo? deletedUser = await context.DeletedUsers.FirstOrDefaultAsync(u => u.UserId == updatedInfo.UserId);
            Assert.NotNull(deletedUser);
            Assert.Equal("New Name", deletedUser.UserName);
        }

        [Fact]
        public async Task AddUserInfoToDeletedUserInfos_SerializesUserInfoToProfilePicture()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo userInfo = CreateTestUserInfo();

            // Act
            UserInfo? result = await service.AddUserInfoToDeletedUserInfos(userInfo);

            // Assert
            Assert.NotNull(result.ProfilePicture);
            UserInfo? deserializedUser = JsonConvert.DeserializeObject<UserInfo>(result.ProfilePicture);
            Assert.NotNull(deserializedUser);
            Assert.Equal(userInfo.UserEmail, deserializedUser.UserEmail);
        }

        #endregion

        #region RemoveUserInfoFromDeletedUserInfos Tests

        [Fact]
        public async Task RemoveUserInfoFromDeletedUserInfos_RemovesSuccessfully()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo deletedUser = CreateTestUserInfo();

            context.DeletedUsers.Add(deletedUser);
            await context.SaveChangesAsync();

            // Act
            UserInfo? result = await service.RemoveUserInfoFromDeletedUserInfos(deletedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deletedUser.UserId, result.UserId);

            UserInfo? stillExists = await context.DeletedUsers.FirstOrDefaultAsync(u => u.UserId == deletedUser.UserId);
            Assert.Null(stillExists);
        }

        [Fact]
        public async Task RemoveUserInfoFromDeletedUserInfos_ReturnsNull_WhenNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo nonExistentUser = CreateTestUserInfo(999, "nonexistent@test.com", "nonexistent");

            // Act
            UserInfo? result = await service.RemoveUserInfoFromDeletedUserInfos(nonExistentUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateDeletedUserInfo Tests

        [Fact]
        public async Task UpdateDeletedUserInfo_UpdatesSuccessfully()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo deletedUser = CreateTestUserInfo();
            deletedUser.Deleted = false;
            deletedUser.DeletedTime = DateTime.UtcNow.AddDays(-1);

            context.DeletedUsers.Add(deletedUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UserInfo updatedInfo = CreateTestUserInfo();
            updatedInfo.Deleted = true;
            updatedInfo.DeletedTime = DateTime.UtcNow;
            updatedInfo.UpdatedTime = DateTime.UtcNow;

            // Act
            UserInfo? result = await service.UpdateDeletedUserInfo(updatedInfo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Deleted);
            Assert.NotEqual(deletedUser.DeletedTime, result.DeletedTime);
        }

        [Fact]
        public async Task UpdateDeletedUserInfo_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo nonExistentUser = CreateTestUserInfo(999);

            // Act
            UserInfo? result = await service.UpdateDeletedUserInfo(nonExistentUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region IsAdminUserId Tests

        [Fact]
        public async Task IsAdminUserId_ReturnsTrue_WhenUserIsAdmin()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo adminUser = CreateTestUserInfo();
            adminUser.IsKinaUnaAdmin = true;

            context.UserInfoDb.Add(adminUser);
            await context.SaveChangesAsync();

            // Act
            bool result = await service.IsAdminUserId("user123");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAdminUserId_ReturnsFalse_WhenUserIsNotAdmin()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo regularUser = CreateTestUserInfo();
            regularUser.IsKinaUnaAdmin = false;

            context.UserInfoDb.Add(regularUser);
            await context.SaveChangesAsync();

            // Act
            bool result = await service.IsAdminUserId("user123");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsAdminUserId_ReturnsFalse_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            // Act
            bool result = await service.IsAdminUserId("nonexistent");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SetUserInfoByEmail Tests

        [Fact]
        public async Task SetUserInfoByEmail_ReturnsAndCachesUser_WhenFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            // Act
            UserInfo? result = await service.SetUserInfoByEmail("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUser.UserEmail, result.UserEmail);

            // Verify cache was set for all three keys
            _mockCache.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains("userinfobymail")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains("userinfobyuserid")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains("userinfobyid")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetUserInfoByEmail_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            // Act
            UserInfo? result = await service.SetUserInfoByEmail("nonexistent@test.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetUserInfoByEmail_IsCaseInsensitive()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);
            UserInfo testUser = CreateTestUserInfo();
            testUser.UserEmail = "Test@Test.com";

            context.UserInfoDb.Add(testUser);
            await context.SaveChangesAsync();

            // Act
            UserInfo? result = await service.SetUserInfoByEmail("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test@Test.com", result.UserEmail);
        }

        #endregion

        #region RemoveUserInfoByEmail Tests

        [Fact]
        public async Task RemoveUserInfoByEmail_RemovesAllCacheKeys()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            // Act
            await service.RemoveUserInfoByEmail("test@test.com", "user123", 1);

            // Assert
            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobymail") && s.Contains("TEST@TEST.COM")),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobyuserid") && s.Contains("user123")),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("userinfobyid") && s.Contains("1")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveUserInfoByEmail_ConvertsEmailToUpperCase()
        {
            // Arrange
            await using ProgenyDbContext context = CreateInMemoryDatabase();
            UserInfoService service = CreateService(context);

            // Act
            await service.RemoveUserInfoByEmail("test@test.com", "user123", 1);

            // Assert
            _mockCache.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains("TEST@TEST.COM")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}