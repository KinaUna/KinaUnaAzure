using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers;

public class UserGroupsControllerTests
{
    private readonly Mock<IUserGroupsService> _mockUserGroupsService;
    private readonly Mock<IUserInfoService> _mockUserInfoService;
    private readonly UserGroupsController _controller;
    private readonly UserInfo _testUser;
    private readonly UserGroup _testUserGroup;
    private readonly UserGroupMember _testUserGroupMember;

    private const string TestUserEmail = Constants.DefaultUserEmail;
    private const string TestUserId = Constants.DefaultUserId;
    private const int TestUserGroupId = 1;
    private const int TestProgenyId = 1;
    private const int TestFamilyId = 1;
    private const int TestUserGroupMemberId = 1;

    public UserGroupsControllerTests()
    {
        // Setup test data
        _testUser = new UserInfo
        {
            Id = 1,
            UserId = TestUserId,
            UserEmail = TestUserEmail,
            IsKinaUnaAdmin = false,
            FirstName = "Test",
            LastName = "User",
            Timezone = "UTC"
        };
        
        _testUserGroup = new UserGroup
        {
            UserGroupId = TestUserGroupId,
            Name = "Test Group",
            Description = "Test Description",
            ProgenyId = TestProgenyId,
            FamilyId = TestFamilyId,
            IsFamily = false,
            CreatedBy = TestUserId,
            ModifiedBy = TestUserId,
            CreatedTime = DateTime.UtcNow.AddDays(-10),
            ModifiedTime = DateTime.UtcNow,
            Members = new List<UserGroupMember>()
        };

        _testUserGroupMember = new UserGroupMember
        {
            UserGroupMemberId = TestUserGroupMemberId,
            UserGroupId = TestUserGroupId,
            UserId = TestUserId,
            Email = TestUserEmail,
            UserOwnerUserId = TestUserId,
            FamilyOwnerId = TestFamilyId,
            CreatedBy = TestUserId,
            ModifiedBy = TestUserId,
            CreatedTime = DateTime.UtcNow.AddDays(-5),
            ModifiedTime = DateTime.UtcNow
        };

        // Setup mocks
        _mockUserGroupsService = new Mock<IUserGroupsService>();
        _mockUserInfoService = new Mock<IUserInfoService>();

        // Initialize controller
        _controller = new UserGroupsController(
            _mockUserGroupsService.Object,
            _mockUserInfoService.Object
        );
    }

    private void SetupControllerContext(string userEmail, string userId)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, userEmail)
        ];
        ClaimsIdentity identity = new(claims, "TestAuthType");
        ClaimsPrincipal claimsPrincipal = new(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetUserGroup Tests

    [Fact]
    public async Task GetUserGroup_ReturnsOkResult_WithUserGroup_WhenUserHasAccess()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(_testUserGroup);

        // Act
        IActionResult result = await _controller.GetUserGroup(TestUserGroupId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroup returnedGroup = Assert.IsType<UserGroup>(okResult.Value);
        Assert.Equal(TestUserGroupId, returnedGroup.UserGroupId);
        Assert.Equal("Test Group", returnedGroup.Name);
    }

    [Fact]
    public async Task GetUserGroup_ReturnsUnauthorized_WhenUserGroupIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);

        UserGroup unauthorizedGroup = new() { UserGroupId = 0 };
        _mockUserGroupsService
            .Setup(s => s.GetUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(unauthorizedGroup);

        // Act
        IActionResult result = await _controller.GetUserGroup(TestUserGroupId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUserGroup_UsesDefaultUserEmail_WhenClaimIsNull()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(Constants.DefaultUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(_testUserGroup);

        // Act
        await _controller.GetUserGroup(TestUserGroupId);

        // Assert
        _mockUserInfoService.Verify(s => s.GetUserInfoByEmail(Constants.DefaultUserEmail), Times.Once);
    }

    #endregion

    #region GetCurrentUsersUserGroups Tests

    [Fact]
    public async Task GetCurrentUsersUserGroups_ReturnsOkResult_WithUserGroups()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        List<UserGroup> userGroups = [_testUserGroup, new UserGroup { UserGroupId = 2, Name = "Another Group" }];
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUsersUserGroupsByUserId(_testUser.UserId, _testUser))
            .ReturnsAsync(userGroups);

        // Act
        IActionResult result = await _controller.GetCurrentUsersUserGroups();

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Equal(2, returnedGroups.Count);
    }

    [Fact]
    public async Task GetCurrentUsersUserGroups_ReturnsEmptyList_WhenNoGroups()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUsersUserGroupsByUserId(_testUser.UserId, _testUser))
            .ReturnsAsync(new List<UserGroup>());

        // Act
        IActionResult result = await _controller.GetCurrentUsersUserGroups();

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Empty(returnedGroups);
    }

    #endregion

    #region GetUserGroupsForProgeny Tests

    [Fact]
    public async Task GetUserGroupsForProgeny_ReturnsOkResult_WithUserGroups()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        List<UserGroup> progenyGroups = [_testUserGroup];
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupsForProgeny(TestProgenyId, _testUser))
            .ReturnsAsync(progenyGroups);

        // Act
        IActionResult result = await _controller.GetUserGroupsForProgeny(TestProgenyId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Single(returnedGroups);
        Assert.Equal(TestProgenyId, returnedGroups[0].ProgenyId);
    }

    [Fact]
    public async Task GetUserGroupsForProgeny_ReturnsEmptyList_WhenNoGroupsForProgeny()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupsForProgeny(TestProgenyId, _testUser))
            .ReturnsAsync(new List<UserGroup>());

        // Act
        IActionResult result = await _controller.GetUserGroupsForProgeny(TestProgenyId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Empty(returnedGroups);
    }

    #endregion

    #region GetUserGroupsForFamily Tests

    [Fact]
    public async Task GetUserGroupsForFamily_ReturnsOkResult_WithUserGroups()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        List<UserGroup> familyGroups = [_testUserGroup];
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupsForFamily(TestFamilyId, _testUser))
            .ReturnsAsync(familyGroups);

        // Act
        IActionResult result = await _controller.GetUserGroupsForFamily(TestFamilyId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Single(returnedGroups);
        Assert.Equal(TestFamilyId, returnedGroups[0].FamilyId);
    }

    [Fact]
    public async Task GetUserGroupsForFamily_ReturnsEmptyList_WhenNoGroupsForFamily()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupsForFamily(TestFamilyId, _testUser))
            .ReturnsAsync(new List<UserGroup>());

        // Act
        IActionResult result = await _controller.GetUserGroupsForFamily(TestFamilyId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        List<UserGroup> returnedGroups = Assert.IsType<List<UserGroup>>(okResult.Value);
        Assert.Empty(returnedGroups);
    }

    #endregion

    #region AddUserGroup Tests

    [Fact]
    public async Task AddUserGroup_ReturnsOkResult_WithNewUserGroup()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroup newGroup = new()
        {
            Name = "New Group",
            Description = "New Description",
            ProgenyId = TestProgenyId
        };

        UserGroup addedGroup = new()
        {
            UserGroupId = 5,
            Name = "New Group",
            Description = "New Description",
            ProgenyId = TestProgenyId
        };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.AddUserGroup(newGroup, _testUser))
            .ReturnsAsync(addedGroup);

        // Act
        IActionResult result = await _controller.AddUserGroup(newGroup);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroup returnedGroup = Assert.IsType<UserGroup>(okResult.Value);
        Assert.Equal(5, returnedGroup.UserGroupId);
        Assert.Equal("New Group", returnedGroup.Name);
    }

    [Fact]
    public async Task AddUserGroup_ReturnsUnauthorized_WhenUserGroupIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroup newGroup = new() { Name = "New Group" };
        UserGroup unauthorizedGroup = new() { UserGroupId = 0 };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.AddUserGroup(newGroup, _testUser))
            .ReturnsAsync(unauthorizedGroup);

        // Act
        IActionResult result = await _controller.AddUserGroup(newGroup);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region UpdateUserGroup Tests

    [Fact]
    public async Task UpdateUserGroup_ReturnsOkResult_WithUpdatedUserGroup()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroup updatedGroup = new()
        {
            UserGroupId = TestUserGroupId,
            Name = "Updated Group",
            Description = "Updated Description"
        };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.UpdateUserGroup(updatedGroup, _testUser))
            .ReturnsAsync(updatedGroup);

        // Act
        IActionResult result = await _controller.UpdateUserGroup(updatedGroup);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroup returnedGroup = Assert.IsType<UserGroup>(okResult.Value);
        Assert.Equal("Updated Group", returnedGroup.Name);
        Assert.Equal("Updated Description", returnedGroup.Description);
    }

    [Fact]
    public async Task UpdateUserGroup_ReturnsUnauthorized_WhenUserGroupIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroup updateGroup = new() { UserGroupId = TestUserGroupId };
        UserGroup unauthorizedGroup = new() { UserGroupId = 0 };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.UpdateUserGroup(updateGroup, _testUser))
            .ReturnsAsync(unauthorizedGroup);

        // Act
        IActionResult result = await _controller.UpdateUserGroup(updateGroup);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region RemoveUserGroup Tests

    [Fact]
    public async Task RemoveUserGroup_ReturnsOkResult_WithTrue_WhenSuccessful()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.RemoveUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await _controller.RemoveUserGroup(TestUserGroupId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        bool returnedValue = Assert.IsType<bool>(okResult.Value);
        Assert.True(returnedValue);
    }

    [Fact]
    public async Task RemoveUserGroup_ReturnsUnauthorized_WhenRemovalFails()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.RemoveUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.RemoveUserGroup(TestUserGroupId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region GetUserGroupMember Tests

    [Fact]
    public async Task GetUserGroupMember_ReturnsOkResult_WithUserGroupMember()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupMember(TestUserGroupMemberId, _testUser))
            .ReturnsAsync(_testUserGroupMember);

        // Act
        IActionResult result = await _controller.GetUserGroupMember(TestUserGroupMemberId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroupMember returnedMember = Assert.IsType<UserGroupMember>(okResult.Value);
        Assert.Equal(TestUserGroupMemberId, returnedMember.UserGroupMemberId);
        Assert.Equal(TestUserEmail, returnedMember.Email);
    }

    [Fact]
    public async Task GetUserGroupMember_ReturnsUnauthorized_WhenUserGroupMemberIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroupMember unauthorizedMember = new() { UserGroupMemberId = 0 };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroupMember(TestUserGroupMemberId, _testUser))
            .ReturnsAsync(unauthorizedMember);

        // Act
        IActionResult result = await _controller.GetUserGroupMember(TestUserGroupMemberId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region AddUserGroupMember Tests

    [Fact]
    public async Task AddUserGroupMember_ReturnsOkResult_WithNewUserGroupMember()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroupMember newMember = new()
        {
            UserGroupId = TestUserGroupId,
            UserId = "newUserId",
            Email = "newuser@example.com"
        };

        UserGroupMember addedMember = new()
        {
            UserGroupMemberId = 10,
            UserGroupId = TestUserGroupId,
            UserId = "newUserId",
            Email = "newuser@example.com"
        };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.AddUserGroupMember(newMember, _testUser))
            .ReturnsAsync(addedMember);

        // Act
        IActionResult result = await _controller.AddUserGroupMember(newMember);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroupMember returnedMember = Assert.IsType<UserGroupMember>(okResult.Value);
        Assert.Equal(10, returnedMember.UserGroupMemberId);
        Assert.Equal("newuser@example.com", returnedMember.Email);
    }

    [Fact]
    public async Task AddUserGroupMember_ReturnsUnauthorized_WhenUserGroupMemberIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroupMember newMember = new() { UserGroupId = TestUserGroupId };
        UserGroupMember unauthorizedMember = new() { UserGroupMemberId = 0 };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.AddUserGroupMember(newMember, _testUser))
            .ReturnsAsync(unauthorizedMember);

        // Act
        IActionResult result = await _controller.AddUserGroupMember(newMember);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region UpdateUserGroupMember Tests

    [Fact]
    public async Task UpdateUserGroupMember_ReturnsOkResult_WithUpdatedUserGroupMember()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroupMember updatedMember = new()
        {
            UserGroupMemberId = TestUserGroupMemberId,
            UserGroupId = TestUserGroupId,
            Email = "updated@example.com"
        };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.UpdateUserGroupMember(updatedMember, _testUser))
            .ReturnsAsync(updatedMember);

        // Act
        IActionResult result = await _controller.UpdateUserGroupMember(updatedMember);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserGroupMember returnedMember = Assert.IsType<UserGroupMember>(okResult.Value);
        Assert.Equal("updated@example.com", returnedMember.Email);
    }

    [Fact]
    public async Task UpdateUserGroupMember_ReturnsUnauthorized_WhenUserGroupMemberIdIsZero()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroupMember updateMember = new() { UserGroupMemberId = TestUserGroupMemberId };
        UserGroupMember unauthorizedMember = new() { UserGroupMemberId = 0 };

        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.UpdateUserGroupMember(updateMember, _testUser))
            .ReturnsAsync(unauthorizedMember);

        // Act
        IActionResult result = await _controller.UpdateUserGroupMember(updateMember);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region RemoveUserGroupMember Tests

    [Fact]
    public async Task RemoveUserGroupMember_ReturnsOkResult_WithTrue_WhenSuccessful()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.RemoveUserGroupMember(TestUserGroupMemberId, _testUser))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await _controller.RemoveUserGroupMember(TestUserGroupMemberId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        bool returnedValue = Assert.IsType<bool>(okResult.Value);
        Assert.True(returnedValue);
    }

    [Fact]
    public async Task RemoveUserGroupMember_ReturnsUnauthorized_WhenRemovalFails()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.RemoveUserGroupMember(TestUserGroupMemberId, _testUser))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.RemoveUserGroupMember(TestUserGroupMemberId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region Service Interaction Tests

    [Fact]
    public async Task GetUserGroup_CallsUserInfoServiceOnce()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.GetUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(_testUserGroup);

        // Act
        await _controller.GetUserGroup(TestUserGroupId);

        // Assert
        _mockUserInfoService.Verify(s => s.GetUserInfoByEmail(TestUserEmail), Times.Once);
        _mockUserGroupsService.Verify(s => s.GetUserGroup(TestUserGroupId, _testUser), Times.Once);
    }

    [Fact]
    public async Task AddUserGroup_CallsAddUserGroupServiceOnce()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        UserGroup newGroup = new() { Name = "New Group" };
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.AddUserGroup(newGroup, _testUser))
            .ReturnsAsync(_testUserGroup);

        // Act
        await _controller.AddUserGroup(newGroup);

        // Assert
        _mockUserGroupsService.Verify(s => s.AddUserGroup(newGroup, _testUser), Times.Once);
    }

    [Fact]
    public async Task UpdateUserGroup_CallsUpdateUserGroupServiceOnce()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.UpdateUserGroup(_testUserGroup, _testUser))
            .ReturnsAsync(_testUserGroup);

        // Act
        await _controller.UpdateUserGroup(_testUserGroup);

        // Assert
        _mockUserGroupsService.Verify(s => s.UpdateUserGroup(_testUserGroup, _testUser), Times.Once);
    }

    [Fact]
    public async Task RemoveUserGroup_CallsRemoveUserGroupServiceOnce()
    {
        // Arrange
        SetupControllerContext(TestUserEmail, TestUserId);
        
        _mockUserInfoService
            .Setup(s => s.GetUserInfoByEmail(TestUserEmail))
            .ReturnsAsync(_testUser);
        _mockUserGroupsService
            .Setup(s => s.RemoveUserGroup(TestUserGroupId, _testUser))
            .ReturnsAsync(true);

        // Act
        await _controller.RemoveUserGroup(TestUserGroupId);

        // Assert
        _mockUserGroupsService.Verify(s => s.RemoveUserGroup(TestUserGroupId, _testUser), Times.Once);
    }

    #endregion
}