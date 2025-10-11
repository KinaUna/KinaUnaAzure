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
    public class ContactServiceTests
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<IImageStore> _mockImageStore;
        private readonly ContactService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _adminUser;
        private readonly UserInfo _otherUser;

        public ContactServiceTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _adminUser = new UserInfo { UserId = "admin1", UserEmail = "admin@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContext (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup in-memory cache
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockImageStore = new Mock<IImageStore>();

            // Initialize service
            _service = new ContactService(_progenyDbContext, memoryCache, _mockImageStore.Object, _mockAccessManagementService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_adminUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test Contact records
            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Author = "user1",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-10),
                AccessLevel = 0,
                Active = true,
                Context = "Family",
                DisplayName = "John Doe",
                DateAdded = DateTime.UtcNow.AddDays(-10),
                Email1 = "john.doe@example.com",
                Email2 = "j.doe@work.com",
                FirstName = "John",
                MiddleName = "Michael",
                LastName = "Doe",
                PictureLink = "contact1.jpg",
                Tags = "family,friend",
                MobileNumber = "555-1234",
                PhoneNumber = "555-5678",
                Notes = "Test contact 1",
                Website = "https://johndoe.com"
            };
            _progenyDbContext.ContactsDb.Add(contact1);

            Contact contact2 = new()
            {
                ContactId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Author = "user1",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-8),
                AccessLevel = 5,
                Active = true,
                Context = "Work",
                DisplayName = "Jane Smith",
                DateAdded = DateTime.UtcNow.AddDays(-8),
                Email1 = "jane.smith@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PictureLink = "contact2.jpg",
                Tags = "work,colleague",
                MobileNumber = "555-9999",
                Notes = "Test contact 2"
            };
            _progenyDbContext.ContactsDb.Add(contact2);

            Contact contact3 = new()
            {
                ContactId = 3,
                ProgenyId = 2,
                FamilyId = 0,
                Author = "user2",
                CreatedBy = "user2",
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                ModifiedBy = "user2",
                ModifiedTime = DateTime.UtcNow.AddDays(-5),
                AccessLevel = 0,
                Active = false,
                Context = "Other",
                DisplayName = "Bob Johnson",
                DateAdded = DateTime.UtcNow.AddDays(-5),
                Email1 = "bob@example.com",
                FirstName = "Bob",
                LastName = "Johnson",
                PictureLink = "contact3.jpg",
                Tags = "acquaintance"
            };
            _progenyDbContext.ContactsDb.Add(contact3);
            
            Contact familyContact = new()
            {
                ContactId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Author = "admin1",
                CreatedBy = "admin1",
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                ModifiedBy = "admin1",
                ModifiedTime = DateTime.UtcNow.AddDays(-3),
                AccessLevel = 0,
                Active = true,
                Context = "Family",
                DisplayName = "Family Contact",
                DateAdded = DateTime.UtcNow.AddDays(-3),
                Email1 = "family@example.com",
                FirstName = "Family",
                LastName = "Contact",
                PictureLink = "family.jpg",
                Tags = "family"
            };
            _progenyDbContext.ContactsDb.Add(familyContact);

            _progenyDbContext.SaveChanges();
        }

        #region GetContact Tests

        [Fact]
        public async Task GetContact_WhenUserHasAccess_ReturnsContact()
        {
            // Arrange
            int contactId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contactId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, contactId, 1, 0, _testUser))
                .ReturnsAsync(permission);

            // Act
            Contact result = await _service.GetContact(contactId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contactId, result.ContactId);
            Assert.Equal("John Doe", result.DisplayName);
            Assert.Equal("john.doe@example.com", result.Email1);
            Assert.NotNull(result.ItemPerMission);
            Assert.Equal(PermissionLevel.View, result.ItemPerMission.PermissionLevel);
        }

        [Fact]
        public async Task GetContact_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            int contactId = 1;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contactId, _otherUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            Contact result = await _service.GetContact(contactId, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetContact_WhenContactDoesNotExist_ReturnsNull()
        {
            // Arrange
            int contactId = 999;

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contactId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.GetContact(contactId, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetContact_SecondCall_UsesCache()
        {
            // Arrange
            int contactId = 1;
            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.View
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contactId, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, contactId, 1, 0, _testUser))
                .ReturnsAsync(permission);

            // Act
            Contact result1 = await _service.GetContact(contactId, _testUser);
            Contact result2 = await _service.GetContact(contactId, _testUser);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.ContactId, result2.ContactId);
            Assert.Equal(result1.DisplayName, result2.DisplayName);
        }

        #endregion

        #region AddContact Tests

        [Fact]
        public async Task AddContact_WhenUserHasProgenyAccess_AddsContact()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "New Contact",
                FirstName = "New",
                LastName = "Contact",
                Email1 = "new@example.com",
                CreatedBy = "user1",
                AccessLevel = 0,
                Active = true,
                Context = "Test",
                Tags = "test"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.AddContact(newContact, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContactId > 0);
            Assert.Equal("New Contact", result.DisplayName);
            Assert.Equal("new@example.com", result.Email1);
            Assert.Equal("user1", result.CreatedBy);
            Assert.Equal("user1", result.ModifiedBy);
            Assert.True(result.CreatedTime <= DateTime.UtcNow);
            Assert.True(result.ModifiedTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task AddContact_WhenUserHasFamilyAccess_AddsContact()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 0,
                FamilyId = 1,
                DisplayName = "New Family Contact",
                FirstName = "New",
                LastName = "Family",
                Email1 = "newfamily@example.com",
                CreatedBy = "admin1",
                AccessLevel = 0,
                Active = true
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _adminUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.AddContact(newContact, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContactId > 0);
            Assert.Equal("New Family Contact", result.DisplayName);
            Assert.Equal(0, result.ProgenyId);
            Assert.Equal(1, result.FamilyId);
        }

        [Fact]
        public async Task AddContact_WhenUserHasNoProgenyAccess_ReturnsNull()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Unauthorized Contact",
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(0, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            Contact result = await _service.AddContact(newContact, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddContact_WhenUserHasNoFamilyAccess_ReturnsNull()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 0,
                FamilyId = 1,
                DisplayName = "Unauthorized Family Contact",
                CreatedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(0, _otherUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            Contact result = await _service.AddContact(newContact, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddContact_CallsAddItemPermissions()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Test Contact",
                CreatedBy = "user1",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.AddContact(newContact, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(x => x.AddItemPermissions(
                KinaUnaTypes.TimeLineType.Contact,
                result.ContactId,
                1,
                0,
                It.IsAny<List<ItemPermissionDto>>(),
                _testUser), Times.Once);
        }

        #endregion

        #region UpdateContact Tests

        [Fact]
        public async Task UpdateContact_WhenUserHasAccess_UpdatesContact()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Updated John Doe",
                Email1 = "updated@example.com",
                AccessLevel = 5,
                ModifiedBy = "user1",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated John Doe", result.DisplayName);
            Assert.Equal("updated@example.com", result.Email1);
            Assert.Equal(5, result.AccessLevel);
        }

        [Fact]
        public async Task UpdateContact_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Unauthorized Update",
                ModifiedBy = "user2"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _otherUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _otherUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateContact_WhenContactDoesNotExist_ReturnsNull()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 999,
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Non-existent Contact"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 999, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateContact_WhenProgenyIdMismatch_ReturnsNull()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 1,
                ProgenyId = 2, // Different from original
                FamilyId = 0,
                DisplayName = "Updated Contact"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateContact_WhenFamilyIdMismatch_ReturnsNull()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 4,
                ProgenyId = 0,
                FamilyId = 2, // Different from original
                DisplayName = "Updated Family Contact"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 4, _adminUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateContact_WhenPictureLinkChanges_DeletesOldPictureIfNotUsed()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                PictureLink = "newpicture.jpg",
                DisplayName = "John Doe",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            _mockImageStore
                .Setup(x => x.DeleteImage("contact1.jpg", BlobContainers.Contacts))
                .ReturnsAsync("contact1.jpg");

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage("contact1.jpg", BlobContainers.Contacts), Times.Once);
        }

        [Fact]
        public async Task UpdateContact_WhenPictureLinkChangesButStillUsed_DoesNotDeletePicture()
        {
            Contact contactWithSamePicture = new()
            {
                ContactId = 5,
                ProgenyId = 1,
                FamilyId = 0,
                Author = "user1",
                CreatedBy = "user1",
                CreatedTime = DateTime.UtcNow.AddDays(-8),
                ModifiedBy = "user1",
                ModifiedTime = DateTime.UtcNow.AddDays(-8),
                AccessLevel = 5,
                Active = true,
                Context = "Work",
                DisplayName = "Jane Smith Copy",
                DateAdded = DateTime.UtcNow.AddDays(-8),
                Email1 = "jane.smith.copy@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PictureLink = "contact2.jpg",
                Tags = "work,colleague",
                MobileNumber = "555-9999",
                Notes = "Test contact 2"
            };

            _progenyDbContext.ContactsDb.Add(contactWithSamePicture);

            // Arrange
            Contact updateValues = new()
            {
                ContactId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                PictureLink = "contact1.jpg", // Same as contact1's picture
                DisplayName = "Jane Smith",
                FirstName = "Jane",
                LastName = "Smith"
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 2, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContact_CallsUpdateItemPermissions()
        {
            // Arrange
            Contact updateValues = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                DisplayName = "Updated Contact",
                FirstName = "John",
                LastName = "Doe",
                ItemPermissionsDtoList = new List<ItemPermissionDto>()
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.UpdateContact(updateValues, _testUser);

            // Assert
            Assert.NotNull(result);
            _mockAccessManagementService.Verify(x => x.UpdateItemPermissions(
                KinaUnaTypes.TimeLineType.Contact,
                1,
                1,
                0,
                It.IsAny<List<ItemPermissionDto>>(),
                _testUser), Times.Once);
        }

        #endregion

        #region DeleteContact Tests

        [Fact]
        public async Task DeleteContact_WhenUserHasAccess_DeletesContact()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockImageStore
                .Setup(x => x.DeleteImage("contact1.jpg", BlobContainers.Contacts))
                .ReturnsAsync("contact1.jpg");

            // Act
            Contact result = await _service.DeleteContact(contactToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);
            Contact? deletedContact = await _progenyDbContext.ContactsDb.FindAsync(1);
            Assert.Null(deletedContact);
            _mockImageStore.Verify(x => x.DeleteImage("contact1.jpg", BlobContainers.Contacts), Times.Once);
        }

        [Fact]
        public async Task DeleteContact_WhenUserHasNoAccess_ReturnsNull()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _otherUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            Contact result = await _service.DeleteContact(contactToDelete, _otherUser);

            // Assert
            Assert.Null(result);
            Contact? contact = await _progenyDbContext.ContactsDb.FindAsync(1);
            Assert.NotNull(contact); // Contact still exists
        }

        [Fact]
        public async Task DeleteContact_WhenContactDoesNotExist_ReturnsNull()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = 999,
                ProgenyId = 1,
                FamilyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 999, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.DeleteContact(contactToDelete, _adminUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteContact_WhenPictureStillUsed_DoesNotDeletePicture()
        {
            // Arrange
            // First, update contact2 to use the same picture as contact1
            Contact contact2 = (await _progenyDbContext.ContactsDb.FindAsync(2))!;
            contact2.PictureLink = "contact1.jpg";
            await _progenyDbContext.SaveChangesAsync();

            Contact contactToDelete = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0
            };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _adminUser, PermissionLevel.Admin))
                .ReturnsAsync(true);

            // Act
            Contact result = await _service.DeleteContact(contactToDelete, _adminUser);

            // Assert
            Assert.NotNull(result);
            _mockImageStore.Verify(x => x.DeleteImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region GetContactsList Tests

        [Fact]
        public async Task GetContactsList_ReturnsContactsWithAccess()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsList(progenyId, familyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(progenyId, c.ProgenyId));
            Assert.All(result, c => Assert.Equal(familyId, c.FamilyId));
            Assert.All(result, c => Assert.NotNull(c.ItemPerMission));
        }

        [Fact]
        public async Task GetContactsList_FiltersOutContactsWithoutAccess()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsList(progenyId, familyId, _testUser);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].ContactId);
        }

        [Fact]
        public async Task GetContactsList_WhenNoContactsExist_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 999;
            int familyId = 0;

            // Act
            List<Contact> result = await _service.GetContactsList(progenyId, familyId, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetContactsList_SecondCall_UsesCache()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result1 = await _service.GetContactsList(progenyId, familyId, _testUser);
            List<Contact> result2 = await _service.GetContactsList(progenyId, familyId, _testUser);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Count, result2.Count);
        }

        [Fact]
        public async Task GetContactsList_ForFamily_ReturnsOnlyFamilyContacts()
        {
            // Arrange
            int progenyId = 0;
            int familyId = 1;
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _adminUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _adminUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsList(progenyId, familyId, _adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, c => Assert.Equal(familyId, c.FamilyId));
            Assert.All(result, c => Assert.Equal(progenyId, c.ProgenyId));
        }

        #endregion

        #region GetContactsWithTag Tests

        [Fact]
        public async Task GetContactsWithTag_ReturnsContactsWithMatchingTag()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string tag = "family";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithTag(progenyId, familyId, tag, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, c => Assert.Contains(tag, c.Tags, StringComparison.CurrentCultureIgnoreCase));
        }

        [Fact]
        public async Task GetContactsWithTag_WithEmptyTag_ReturnsAllContacts()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string tag = "";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithTag(progenyId, familyId, tag, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetContactsWithTag_WithNonMatchingTag_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string tag = "nonexistent";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithTag(progenyId, familyId, tag, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetContactsWithTag_IsCaseInsensitive()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string tag = "FAMILY";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithTag(progenyId, familyId, tag, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region GetContactsWithContext Tests

        [Fact]
        public async Task GetContactsWithContext_ReturnsContactsWithMatchingContext()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string context = "Family";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithContext(progenyId, familyId, context, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.All(result, c => Assert.Contains(context, c.Context, StringComparison.CurrentCultureIgnoreCase));
        }

        [Fact]
        public async Task GetContactsWithContext_WithEmptyContext_ReturnsAllContacts()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string context = "";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithContext(progenyId, familyId, context, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetContactsWithContext_WithNonMatchingContext_ReturnsEmptyList()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string context = "nonexistent";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithContext(progenyId, familyId, context, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetContactsWithContext_IsCaseInsensitive()
        {
            // Arrange
            int progenyId = 1;
            int familyId = 0;
            string context = "WORK";
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser))
                .ReturnsAsync(permission);

            // Act
            List<Contact> result = await _service.GetContactsWithContext(progenyId, familyId, context, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion
    }
}