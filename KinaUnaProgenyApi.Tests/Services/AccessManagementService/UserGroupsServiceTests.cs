using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.AccessManagementService
{
    public class UserGroupsServiceTests
    {
        private readonly DbContextOptions<ProgenyDbContext> _options;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IUserGroupAuditLogsService> _mockUserGroupAuditLogService;

        public UserGroupsServiceTests()
        {
            _options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockUserGroupAuditLogService = new Mock<IUserGroupAuditLogsService>();

            // Seed the database
            using ProgenyDbContext context = new(_options);
            SeedDatabase(context);
        }
        
        private static void SeedDatabase(ProgenyDbContext context)
        {
            // Add test user groups
            List<UserGroup> userGroups =
            [
                new()
                {
                    UserGroupId = 1,
                    Name = "Family Group",
                    Description = "A family group",
                    IsFamily = true,
                    FamilyId = 1,
                    ProgenyId = 0,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-10),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-10)
                },

                new()
                {
                    UserGroupId = 2,
                    Name = "Progeny Group",
                    Description = "A progeny group",
                    IsFamily = false,
                    FamilyId = 0,
                    ProgenyId = 1,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-5),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-5)
                }
            ];
            context.UserGroupsDb.AddRange(userGroups);

            // Add test user group members
            List<UserGroupMember> userGroupMembers =
            [
                new()
                {
                    UserGroupMemberId = 1,
                    UserId = "user1",
                    Email = "user1@test.com",
                    UserGroupId = 1,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-9),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-9)
                },

                new()
                {
                    UserGroupMemberId = 2,
                    UserId = "user2",
                    Email = "user2@test.com",
                    UserGroupId = 1,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-8),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-8)
                },

                new()
                {
                    UserGroupMemberId = 3,
                    UserId = "user1",
                    Email = "user1@test.com",
                    UserGroupId = 2,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-4),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-4)
                },

                new()
                {
                    UserGroupMemberId = 4,
                    UserId = "",
                    Email = "newuser@test.com",
                    UserGroupId = 2,
                    CreatedBy = "testuser@test.com",
                    CreatedTime = DateTime.UtcNow.AddDays(-3),
                    ModifiedBy = "testuser@test.com",
                    ModifiedTime = DateTime.UtcNow.AddDays(-3)
                }
            ];
            context.UserGroupMembersDb.AddRange(userGroupMembers);

            // Add test user info
            List<UserInfo> userInfos =
            [
                new()
                {
                    UserId = "user1",
                    UserEmail = "user1@test.com",
                    UserName = "User One"
                },

                new()
                {
                    UserId = "user2",
                    UserEmail = "user2@test.com",
                    UserName = "User Two"
                }
            ];
            context.UserInfoDb.AddRange(userInfos);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetUserGroup_WithValidIdAndAccess_ReturnsUserGroup()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "user1", UserEmail = "user1@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroup? result = await service.GetUserGroup(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserGroupId);
            Assert.Equal("Family Group", result.Name);
            Assert.Equal(2, result.Members.Count);
        }

        [Fact]
        public async Task GetUserGroup_WithInvalidAccess_ReturnsEmptyUserGroup()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "user3", UserEmail = "user3@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(false);

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroup? result = await service.GetUserGroup(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.UserGroupId);
            Assert.Empty(result.Members);
        }

        [Fact]
        public async Task GetUserGroupsForProgeny_WithValidAccessAndProgeny_ReturnsUserGroups()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "user1", UserEmail = "user1@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasProgenyPermission(1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.GetProgenyPermissionForGroup(1, 2, userInfo))
                .ReturnsAsync(new ProgenyPermission { PermissionLevel = PermissionLevel.Edit });

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            List<UserGroup>? result = await service.GetUserGroupsForProgeny(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(2, result[0].UserGroupId);
            Assert.Equal("Progeny Group", result[0].Name);
            Assert.Equal(PermissionLevel.Edit, result[0].PermissionLevel);
            Assert.Equal(2, result[0].Members.Count);
        }

        [Fact]
        public async Task GetUserGroupsForFamily_WithValidAccessAndFamily_ReturnsUserGroups()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "user1", UserEmail = "user1@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.GetFamilyPermissionForGroup(1, 1, userInfo))
                .ReturnsAsync(new FamilyPermission { PermissionLevel = PermissionLevel.Admin });

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            List<UserGroup>? result = await service.GetUserGroupsForFamily(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].UserGroupId);
            Assert.Equal("Family Group", result[0].Name);
            Assert.Equal(PermissionLevel.Admin, result[0].PermissionLevel);
            Assert.Equal(2, result[0].Members.Count);
        }

        [Fact]
        public async Task GetUsersUserGroupsByUserId_WithValidUser_ReturnsAccessibleGroups()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.HasProgenyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            List<UserGroup>? result = await service.GetUsersUserGroupsByUserId("user1", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetUsersUserGroupsByEmail_WithValidEmail_ReturnsAccessibleGroups()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.HasProgenyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            List<UserGroup>? result = await service.GetUsersUserGroupsByEmail("user1@test.com", userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task AddUserGroup_WithValidAccessAndData_CreatesNewGroup()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };
            UserGroup newGroup = new()
            {
                Name = "New Group",
                Description = "New test group",
                FamilyId = 1,
                ProgenyId = 0,
                IsFamily = true,
                CreatedBy = "admin@test.com",
                ModifiedBy = "admin@test.com",
                PermissionLevel = PermissionLevel.Edit
            };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.GrantFamilyPermission(It.IsAny<FamilyPermission>(), userInfo))
                .ReturnsAsync(new FamilyPermission());

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupCreatedAuditLogEntry(It.IsAny<UserGroup>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroup? result = await service.AddUserGroup(newGroup, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Group", result.Name);
            Assert.Equal(1, result.FamilyId);
            Assert.NotEqual(0, result.UserGroupId);

            // Verify the group was added to the database
            UserGroup? savedGroup = await context.UserGroupsDb.FindAsync(result.UserGroupId);
            Assert.NotNull(savedGroup);
            Assert.Equal("New Group", savedGroup.Name);
        }

        [Fact]
        public async Task UpdateUserGroup_WithValidAccessAndData_UpdatesExistingGroup()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };
            UserGroup updateGroup = new()
            {
                UserGroupId = 1,
                Name = "Updated Family Group",
                Description = "Updated description",
                FamilyId = 1,
                IsFamily = true,
                ModifiedBy = "admin@test.com",
                PermissionLevel = PermissionLevel.Admin
            };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.GetFamilyPermissionForGroup(1, 1, userInfo))
                .ReturnsAsync(new FamilyPermission { PermissionLevel = PermissionLevel.Edit });

            _mockAccessManagementService
                .Setup(m => m.UpdateFamilyPermission(It.IsAny<FamilyPermission>(), userInfo))
                .ReturnsAsync(new FamilyPermission());

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupUpdatedAuditLogEntry(It.IsAny<UserGroup>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            _mockUserGroupAuditLogService
                .Setup(m => m.UpdateUserGroupAuditLogEntry(It.IsAny<UserGroupAuditLog>()))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroup? result = await service.UpdateUserGroup(updateGroup, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Family Group", result.Name);
            Assert.Equal("Updated description", result.Description);

            // Verify the group was updated in the database
            UserGroup? updatedGroup = await context.UserGroupsDb.FindAsync(1);
            Assert.NotNull(updatedGroup);
            Assert.Equal("Updated Family Group", updatedGroup.Name);
            Assert.Equal("Updated description", updatedGroup.Description);
        }

        [Fact]
        public async Task RemoveUserGroup_WithValidAccessAndId_RemovesGroup()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(m => m.GetFamilyPermissionForGroup(1, 1, userInfo))
                .ReturnsAsync(new FamilyPermission { GroupId = 1 });

            _mockAccessManagementService
                .Setup(m => m.RevokeFamilyPermission(It.IsAny<FamilyPermission>(), userInfo))
                .ReturnsAsync(true);

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupDeletedAuditLogEntry(It.IsAny<UserGroup>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Ensure the group exists before deletion
            bool groupExists = await context.UserGroupsDb.AnyAsync(g => g.UserGroupId == 1);
            Assert.True(groupExists);

            // Act
            bool result = await service.RemoveUserGroup(1, userInfo);

            // Assert
            Assert.True(result);

            // Verify the group was removed from the database
            groupExists = await context.UserGroupsDb.AnyAsync(g => g.UserGroupId == 1);
            Assert.False(groupExists);

            // Verify the members were also removed
            bool membersExist = await context.UserGroupMembersDb.AnyAsync(m => m.UserGroupId == 1);
            Assert.False(membersExist);
        }

        [Fact]
        public async Task GetUserGroupMember_WithValidIdAndAccess_ReturnsMember()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroupMember? result = await service.GetUserGroupMember(1, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserGroupMemberId);
            Assert.Equal("user1", result.UserId);
            Assert.Equal("user1@test.com", result.Email);
        }

        [Fact]
        public async Task AddUserGroupMember_WithValidAccessAndData_CreatesNewMember()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };
            UserGroupMember newMember = new()
            {
                UserGroupId = 1,
                Email = "newuser@test.com",
                UserId = "newuser",
                CreatedBy = "admin@test.com",
                ModifiedBy = "admin@test.com"
            };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupMemberAddedAuditLogEntry(It.IsAny<UserGroupMember>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroupMember? result = await service.AddUserGroupMember(newMember, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newuser@test.com", result.Email);
            Assert.Equal("newuser", result.UserId);
            Assert.NotEqual(0, result.UserGroupMemberId);

            // Verify the member was added to the database
            UserGroupMember? savedMember = await context.UserGroupMembersDb.FindAsync(result.UserGroupMemberId);
            Assert.NotNull(savedMember);
            Assert.Equal("newuser@test.com", savedMember.Email);
        }

        [Fact]
        public async Task UpdateUserGroupMember_WithValidAccessAndData_UpdatesExistingMember()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };
            UserGroupMember updateMember = new()
            {
                UserGroupMemberId = 1,
                UserGroupId = 1,
                Email = "updated@test.com",
                UserId = "user1",
                ModifiedBy = "admin@test.com"
            };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupMemberUpdatedAuditLogEntry(It.IsAny<UserGroupMember>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            _mockUserGroupAuditLogService
                .Setup(m => m.UpdateUserGroupAuditLogEntry(It.IsAny<UserGroupAuditLog>()))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Act
            UserGroupMember? result = await service.UpdateUserGroupMember(updateMember, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("updated@test.com", result.Email);

            // Verify the member was updated in the database
            UserGroupMember? updatedMember = await context.UserGroupMembersDb.FindAsync(1);
            Assert.NotNull(updatedMember);
            Assert.Equal("updated@test.com", updatedMember.Email);
        }

        [Fact]
        public async Task RemoveUserGroupMember_WithValidAccessAndId_RemovesMember()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "admin", UserEmail = "admin@test.com" };

            _mockAccessManagementService
                .Setup(m => m.HasFamilyPermission(1, userInfo, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockUserGroupAuditLogService
                .Setup(m => m.AddUserGroupMemberDeletedAuditLogEntry(It.IsAny<UserGroupMember>(), userInfo))
                .ReturnsAsync(new UserGroupAuditLog());

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Ensure the member exists before deletion
            bool memberExists = await context.UserGroupMembersDb.AnyAsync(m => m.UserGroupMemberId == 1);
            Assert.True(memberExists);

            // Act
            bool result = await service.RemoveUserGroupMember(1, userInfo);

            // Assert
            Assert.True(result);

            // Verify the member was removed from the database
            memberExists = await context.UserGroupMembersDb.AnyAsync(m => m.UserGroupMemberId == 1);
            Assert.False(memberExists);
        }

        [Fact]
        public async Task ChangeUsersEmailForGroupMembers_WithValidUser_UpdatesEmailAddresses()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "user1", UserEmail = "user1@test.com" };
            string newEmail = "user1-new@test.com";

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Verify initial state
            List<UserGroupMember> members = await context.UserGroupMembersDb.Where(m => m.UserId == "user1").ToListAsync();
            Assert.Equal(2, members.Count);
            Assert.All(members, m => Assert.Equal("user1@test.com", m.Email));

            // Act
            await service.ChangeUsersEmailForGroupMembers(userInfo, newEmail);

            // Assert
            members = await context.UserGroupMembersDb.Where(m => m.UserId == "user1").ToListAsync();
            Assert.Equal(2, members.Count);
            Assert.All(members, m => Assert.Equal("user1-new@test.com", m.Email));
        }

        [Fact]
        public async Task UpdateUserGroupMembersForNewUser_WithValidUser_AssociatesUserIdWithEmail()
        {
            // Arrange
            UserInfo userInfo = new() { UserId = "newuser", UserEmail = "newuser@test.com" };

            await using ProgenyDbContext context = new(_options);
            UserGroupsService service = new(context, _mockAccessManagementService.Object, _mockUserGroupAuditLogService.Object);

            // Verify initial state
            List<UserGroupMember> members = await context.UserGroupMembersDb.Where(m => m.Email.ToLower() == "newuser@test.com").ToListAsync();
            Assert.Single(members);
            Assert.Equal("", members[0].UserId);

            // Act
            await service.UpdateUserGroupMembersForNewUser(userInfo);

            // Assert
            members = await context.UserGroupMembersDb.Where(m => m.Email.ToLower() == "newuser@test.com").ToListAsync();
            Assert.Single(members);
            Assert.Equal("newuser", members[0].UserId);
        }
    }
}