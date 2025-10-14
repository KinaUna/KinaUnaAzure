using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.AccessManagementService
{
    public class AccessManagementServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IPermissionAuditLogsService> _mockPermissionAuditLogService;
        private readonly KinaUnaProgenyApi.Services.AccessManagementService.AccessManagementService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;
        private readonly int _progenyId;
        private readonly int _familyId;

        public AccessManagementServiceTests()
        {
            _progenyId = 1;
            _familyId = 1;

            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _adminUser = new UserInfo { UserId = "admin1", UserEmail = "admin@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContexts (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            DbContextOptions<MediaDbContext> mediaOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            MediaDbContext mediaDbContext = new(mediaOptions);

            _mockPermissionAuditLogService = new Mock<IPermissionAuditLogsService>();

            // Initialize service with real in-memory contexts and mocked audit service
            _service = new KinaUnaProgenyApi.Services.AccessManagementService.AccessManagementService(
                _progenyDbContext,
                mediaDbContext,
                _mockPermissionAuditLogService.Object,
                GetMemoryCache());
        }

        private static IDistributedCache GetMemoryCache()
        {
            IOptions<MemoryDistributedCacheOptions> options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }

        #region HasItemPermission Tests

        [Fact]
        public async Task HasItemPermission_WhenUserIsNull_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 1, null, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasItemPermission_WhenItemIdIsZero_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasItemPermission(KinaUnaTypes.TimeLineType.Note, 0, _testUser, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasItemPermission_WhenRequiredLevelIsCreatorOnly_CallsHasCreatorOnlyPermission()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup note in db
            Note note = new() { NoteId = itemId, CreatedBy = _testUser.UserId, ProgenyId = _progenyId };
            _progenyDbContext.NotesDb.Add(note);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasItemPermission(timelineType, itemId, _testUser, PermissionLevel.CreatorOnly);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasItemPermission_WhenRequiredLevelIsPrivate_CallsHasPrivatePermission()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup progeny with the user as owner
            Progeny progeny = new() { Id = _progenyId, UserId = _testUser.UserId };
            _progenyDbContext.ProgenyDb.Add(progeny);

            // Setup note in db
            Note note = new() { NoteId = itemId, ProgenyId = _progenyId };
            _progenyDbContext.NotesDb.Add(note);

            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasItemPermission(timelineType, itemId, _testUser, PermissionLevel.Private);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasItemPermission_WithSufficientPermissionLevel_ReturnsTrue()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup user with View permission
            TimelineItemPermission permission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(permission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasItemPermission(timelineType, itemId, _testUser, PermissionLevel.View);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasItemPermission_WithInsufficientPermissionLevel_ReturnsFalse()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup user with None permission
            TimelineItemPermission permission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.None
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(permission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasItemPermission(timelineType, itemId, _testUser, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetItemPermissionForUser Tests

        [Fact]
        public async Task GetItemPermissionForUser_ReturnsDirectUserPermission()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup direct user permission
            TimelineItemPermission userPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(userPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            TimelineItemPermission? result = await _service.GetItemPermissionForUser(timelineType, itemId, _progenyId, _familyId, _testUser);

            // Assert
            Assert.Equal(PermissionLevel.Edit, result.PermissionLevel);
        }

        [Fact]
        public async Task GetItemPermissionForUser_ReturnsInheritedProgenyPermission()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup inherited permission
            TimelineItemPermission inheritedPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                InheritPermissions = true
            };

            // Setup progeny permission
            ProgenyPermission progenyPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(inheritedPermission);
            _progenyDbContext.ProgenyPermissionsDb.Add(progenyPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            TimelineItemPermission? result = await _service.GetItemPermissionForUser(timelineType, itemId, _progenyId, _familyId, _testUser);

            // Assert
            Assert.Equal(PermissionLevel.Admin, result.PermissionLevel);
        }

        [Fact]
        public async Task GetItemPermissionForUser_ReturnsGroupPermission()
        {
            // Arrange
            int itemId = 1;
            const KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;
            int groupId = 1;

            // Setup group permission
            TimelineItemPermission groupPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                GroupId = groupId,
                PermissionLevel = PermissionLevel.Add
            };

            // Setup user group membership
            UserGroupMember groupMembership = new()
            {
                UserGroupId = groupId,
                UserId = _testUser.UserId
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(groupPermission);
            _progenyDbContext.UserGroupMembersDb.Add(groupMembership);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            TimelineItemPermission? result = await _service.GetItemPermissionForUser(timelineType, itemId, _progenyId, _familyId, _testUser);

            // Assert
            Assert.Equal(PermissionLevel.Add, result.PermissionLevel);
        }

        #endregion

        #region HasProgenyPermission Tests

        [Fact]
        public async Task HasProgenyPermission_WhenUserIsNull_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasProgenyPermission(_progenyId, null, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasProgenyPermission_WhenProgenyIdIsZero_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasProgenyPermission(0, _testUser, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasProgenyPermission_WithDirectSufficientPermission_ReturnsTrue()
        {
            // Arrange
            ProgenyPermission permission = new()
            {
                ProgenyId = _progenyId,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(permission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasProgenyPermission(_progenyId, _testUser, PermissionLevel.View);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasProgenyPermission_WithGroupSufficientPermission_ReturnsTrue()
        {
            // Arrange
            int groupId = 1;

            ProgenyPermission groupPermission = new()
            {
                ProgenyId = _progenyId,
                GroupId = groupId,
                PermissionLevel = PermissionLevel.Edit
            };

            UserGroupMember groupMembership = new()
            {
                UserGroupId = groupId,
                UserId = _testUser.UserId
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(groupPermission);
            _progenyDbContext.UserGroupMembersDb.Add(groupMembership);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasProgenyPermission(_progenyId, _testUser, PermissionLevel.View);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region HasFamilyPermission Tests

        [Fact]
        public async Task HasFamilyPermission_WhenUserIsNull_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasFamilyPermission(_familyId, null, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasFamilyPermission_WhenFamilyIdIsZero_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasFamilyPermission(0, _testUser, PermissionLevel.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasFamilyPermission_WithDirectSufficientPermission_ReturnsTrue()
        {
            // Arrange
            FamilyPermission permission = new()
            {
                FamilyId = _familyId,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.FamilyPermissionsDb.Add(permission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasFamilyPermission(_familyId, _testUser, PermissionLevel.View);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasFamilyPermission_WithGroupSufficientPermission_ReturnsTrue()
        {
            // Arrange
            int groupId = 1;

            FamilyPermission groupPermission = new()
            {
                FamilyId = _familyId,
                GroupId = groupId,
                PermissionLevel = PermissionLevel.Edit
            };

            UserGroupMember groupMembership = new()
            {
                UserGroupId = groupId,
                UserId = _testUser.UserId
            };

            _progenyDbContext.FamilyPermissionsDb.Add(groupPermission);
            _progenyDbContext.UserGroupMembersDb.Add(groupMembership);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            bool result = await _service.HasFamilyPermission(_familyId, _testUser, PermissionLevel.View);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region GrantItemPermission Tests

        [Fact]
        public async Task GrantItemPermission_WhenUserIsNotAccessManager_ReturnsNull()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            TimelineItemPermission permission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            // No admin permissions setup for user

            // Act
            TimelineItemPermission? result = await _service.GrantItemPermission(permission, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GrantItemPermission_WhenPermissionAlreadyExists_ReturnsNull()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup user as admin
            ProgenyPermission adminPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _adminUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            // Setup existing permission
            TimelineItemPermission existingPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            // New permission to grant
            TimelineItemPermission newPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(adminPermission);
            _progenyDbContext.TimelineItemPermissionsDb.Add(existingPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            TimelineItemPermission? result = await _service.GrantItemPermission(newPermission, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GrantItemPermission_WhenValidRequest_CreatesPermission()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup user as admin
            ProgenyPermission adminPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _adminUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            // New permission to grant
            TimelineItemPermission newPermission = new()
            {
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(adminPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Mock audit log service
            _mockPermissionAuditLogService.Setup(s => s.AddTimelineItemPermissionAuditLogEntry(
                    It.IsAny<PermissionAction>(),
                    It.IsAny<TimelineItemPermission>(),
                    It.IsAny<UserInfo>()))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            TimelineItemPermission? result = await _service.GrantItemPermission(newPermission, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_adminUser.UserId, result.CreatedBy);
            Assert.Equal(PermissionLevel.Edit, result.PermissionLevel);

            TimelineItemPermission? saved = await _progenyDbContext.TimelineItemPermissionsDb.FirstOrDefaultAsync(t => t.ItemId == itemId && t.UserId == _otherUser.UserId);
            Assert.NotNull(saved);
        }

        #endregion

        #region GrantProgenyPermission Tests

        [Fact]
        public async Task GrantProgenyPermission_WhenUserIsNotAccessManager_ReturnsNull()
        {
            // Arrange
            ProgenyPermission permission = new()
            {
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            // No admin permissions setup for user

            // Act
            ProgenyPermission? result = await _service.GrantProgenyPermission(permission, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GrantProgenyPermission_WithCreatorOnlyPermission_ReturnsNull()
        {
            // Arrange
            // Setup user as admin
            ProgenyPermission adminPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _adminUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            // New permission with CreatorOnly level
            ProgenyPermission newPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.CreatorOnly
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(adminPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            ProgenyPermission? result = await _service.GrantProgenyPermission(newPermission, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GrantProgenyPermission_WhenValidRequest_CreatesPermission()
        {
            // Arrange
            // Setup user as admin
            ProgenyPermission adminPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _adminUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            // New permission to grant
            ProgenyPermission newPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                Email = _otherUser.UserEmail,
                PermissionLevel = PermissionLevel.View
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(adminPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Mock audit log service
            _mockPermissionAuditLogService.Setup(s => s.AddProgenyPermissionAuditLogEntry(
                    It.IsAny<PermissionAction>(),
                    It.IsAny<ProgenyPermission>(),
                    It.IsAny<UserInfo>()))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            ProgenyPermission? result = await _service.GrantProgenyPermission(newPermission, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_adminUser.UserId, result.CreatedBy);
            Assert.Equal(PermissionLevel.View, result.PermissionLevel);

            ProgenyPermission? saved = await _progenyDbContext.ProgenyPermissionsDb.FirstOrDefaultAsync(p => p.UserId == _otherUser.UserId && p.ProgenyId == _progenyId);
            Assert.NotNull(saved);
        }

        #endregion

        #region UpdateItemPermission Tests

        [Fact]
        public async Task UpdateItemPermission_WhenPermissionDoesNotExist_ReturnsNull()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            TimelineItemPermission permission = new()
            {
                TimelineItemPermissionId = 1,
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            // No existing permissions in DB

            // Act
            TimelineItemPermission? result = await _service.UpdateItemPermission(permission, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemPermission_WhenUserIsNotAccessManager_ReturnsNull()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            TimelineItemPermission existingPermission = new()
            {
                TimelineItemPermissionId = 1,
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            TimelineItemPermission updatedPermission = new()
            {
                TimelineItemPermissionId = 1,
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.TimelineItemPermissionsDb.Add(existingPermission);
            await _progenyDbContext.SaveChangesAsync();

            // No admin permissions setup for user

            // Act
            TimelineItemPermission? result = await _service.UpdateItemPermission(updatedPermission, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemPermission_WhenValidRequest_UpdatesPermission()
        {
            // Arrange
            int itemId = 1;
            KinaUnaTypes.TimeLineType timelineType = KinaUnaTypes.TimeLineType.Note;

            // Setup user as admin
            ProgenyPermission adminPermission = new()
            {
                ProgenyId = _progenyId,
                UserId = _adminUser.UserId,
                PermissionLevel = PermissionLevel.Admin
            };

            TimelineItemPermission existingPermission = new()
            {
                TimelineItemPermissionId = 1,
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            TimelineItemPermission updatedPermission = new()
            {
                TimelineItemPermissionId = 1,
                ItemId = itemId,
                TimelineType = timelineType,
                ProgenyId = _progenyId,
                UserId = _otherUser.UserId,
                PermissionLevel = PermissionLevel.Edit
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(adminPermission);
            _progenyDbContext.TimelineItemPermissionsDb.Add(existingPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Mock audit log service
            _mockPermissionAuditLogService.Setup(s => s.AddTimelineItemPermissionAuditLogEntry(
                    It.IsAny<PermissionAction>(),
                    It.IsAny<TimelineItemPermission>(),
                    It.IsAny<UserInfo>()))
                .ReturnsAsync(new PermissionAuditLog());

            _mockPermissionAuditLogService.Setup(s => s.UpdatePermissionAuditLogEntry(
                    It.IsAny<PermissionAuditLog>()))
                .ReturnsAsync(new PermissionAuditLog());

            // Act
            TimelineItemPermission? result = await _service.UpdateItemPermission(updatedPermission, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PermissionLevel.Edit, result.PermissionLevel);
            Assert.Equal(_adminUser.UserId, result.ModifiedBy);

            TimelineItemPermission? saved = await _progenyDbContext.TimelineItemPermissionsDb.FirstOrDefaultAsync(t => t.TimelineItemPermissionId == existingPermission.TimelineItemPermissionId);
            Assert.NotNull(saved);
            Assert.Equal(PermissionLevel.Edit, saved.PermissionLevel);
            Assert.Equal(_adminUser.UserId, saved.ModifiedBy);
        }

        #endregion

        #region ProgeniesUserCanAccess and FamiliesUserCanAccess Tests

        [Fact]
        public async Task ProgeniesUserCanAccess_ReturnsCorrectList()
        {
            // Arrange
            int progenyId1 = 1;
            int progenyId2 = 2;
            int groupId = 1;

            // Direct permission
            ProgenyPermission directPermission = new()
            {
                ProgenyId = progenyId1,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            // Group permission
            ProgenyPermission groupPermission = new()
            {
                ProgenyId = progenyId2,
                GroupId = groupId,
                PermissionLevel = PermissionLevel.View
            };

            UserGroupMember groupMembership = new()
            {
                UserGroupId = groupId,
                UserId = _testUser.UserId
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(directPermission);
            _progenyDbContext.ProgenyPermissionsDb.Add(groupPermission);
            _progenyDbContext.UserGroupMembersDb.Add(groupMembership);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            List<int>? result = await _service.ProgeniesUserCanAccess(_testUser, PermissionLevel.View);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(progenyId1, result);
            Assert.Contains(progenyId2, result);
        }

        [Fact]
        public async Task FamiliesUserCanAccess_ReturnsCorrectList()
        {
            // Arrange
            int familyId1 = 1;
            int familyId2 = 2;
            int groupId = 1;

            // Direct permission
            FamilyPermission directPermission = new()
            {
                FamilyId = familyId1,
                UserId = _testUser.UserId,
                PermissionLevel = PermissionLevel.View
            };

            // Group permission
            FamilyPermission groupPermission = new()
            {
                FamilyId = familyId2,
                GroupId = groupId,
                PermissionLevel = PermissionLevel.View
            };

            UserGroupMember groupMembership = new()
            {
                UserGroupId = groupId,
                UserId = _testUser.UserId
            };

            _progenyDbContext.FamilyPermissionsDb.Add(directPermission);
            _progenyDbContext.FamilyPermissionsDb.Add(groupPermission);
            _progenyDbContext.UserGroupMembersDb.Add(groupMembership);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            List<int>? result = await _service.FamiliesUserCanAccess(_testUser, PermissionLevel.View);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(familyId1, result);
            Assert.Contains(familyId2, result);
        }

        #endregion

        #region ChangeUsersEmailForPermissions and UpdatePermissionsForNewUser Tests

        [Fact]
        public async Task ChangeUsersEmailForPermissions_UpdatesAllPermissionTypes()
        {
            // Arrange
            string newEmail = "newemail@example.com";

            ProgenyPermission progenyPermission = new()
            {
                UserId = _testUser.UserId,
                Email = _testUser.UserEmail
            };

            FamilyPermission familyPermission = new()
            {
                UserId = _testUser.UserId,
                Email = _testUser.UserEmail
            };

            TimelineItemPermission timelineItemPermission = new()
            {
                UserId = _testUser.UserId,
                Email = _testUser.UserEmail
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(progenyPermission);
            _progenyDbContext.FamilyPermissionsDb.Add(familyPermission);
            _progenyDbContext.TimelineItemPermissionsDb.Add(timelineItemPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            await _service.ChangeUsersEmailForPermissions(_testUser, newEmail);

            // Assert
            ProgenyPermission? p = await _progenyDbContext.ProgenyPermissionsDb.FirstOrDefaultAsync(x => x.UserId == _testUser.UserId);
            FamilyPermission? f = await _progenyDbContext.FamilyPermissionsDb.FirstOrDefaultAsync(x => x.UserId == _testUser.UserId);
            TimelineItemPermission? t = await _progenyDbContext.TimelineItemPermissionsDb.FirstOrDefaultAsync(x => x.UserId == _testUser.UserId);

            Assert.Equal(newEmail, p?.Email);
            Assert.Equal(newEmail, f?.Email);
            Assert.Equal(newEmail, t?.Email);
        }

        [Fact]
        public async Task UpdatePermissionsForNewUser_UpdatesAllPermissionTypes()
        {
            // Arrange
            ProgenyPermission progenyPermission = new()
            {
                UserId = "",
                Email = _testUser.UserEmail
            };

            FamilyPermission familyPermission = new()
            {
                UserId = "",
                Email = _testUser.UserEmail
            };

            TimelineItemPermission timelineItemPermission = new()
            {
                UserId = "",
                Email = _testUser.UserEmail
            };

            _progenyDbContext.ProgenyPermissionsDb.Add(progenyPermission);
            _progenyDbContext.FamilyPermissionsDb.Add(familyPermission);
            _progenyDbContext.TimelineItemPermissionsDb.Add(timelineItemPermission);
            await _progenyDbContext.SaveChangesAsync();

            // Act
            await _service.UpdatePermissionsForNewUser(_testUser);

            // Assert
            ProgenyPermission? p = await _progenyDbContext.ProgenyPermissionsDb.FirstOrDefaultAsync(x => x.Email == _testUser.UserEmail);
            FamilyPermission? f = await _progenyDbContext.FamilyPermissionsDb.FirstOrDefaultAsync(x => x.Email == _testUser.UserEmail);
            TimelineItemPermission? t = await _progenyDbContext.TimelineItemPermissionsDb.FirstOrDefaultAsync(x => x.Email == _testUser.UserEmail);

            Assert.Equal(_testUser.UserId, p?.UserId);
            Assert.Equal(_testUser.UserId, f?.UserId);
            Assert.Equal(_testUser.UserId, t?.UserId);
        }

        #endregion
    }
}