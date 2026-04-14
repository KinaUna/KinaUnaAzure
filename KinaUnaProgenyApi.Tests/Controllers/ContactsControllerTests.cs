using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class ContactsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IImageStore> _mockImageStore;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IContactService> _mockContactService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly ContactsController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;
        private readonly Contact _testContactProgeny;
        private readonly Contact _testContactFamily;
        private readonly TimeLineItem _testTimeLineItem;
        private readonly Address _testAddress;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 5;
        private const int TestFamilyId = 10;
        private const int TestContactId = 100;
        private const int TestAddressId = 200;
        private const int TestTimelineItemId = 300;

        private readonly HttpClient _httpClient = new();

        public ContactsControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                Timezone = "UTC"
            };
            
            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick"
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family"
            };

            _testAddress = new Address
            {
                AddressId = TestAddressId,
                AddressLine1 = "123 Test St",
                City = "Test City",
                Country = "Test Country"
            };

            _testContactProgeny = new Contact
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "Middle",
                LastName = "Doe",
                DisplayName = "John Doe",
                AddressIdNumber = TestAddressId,
                Address = _testAddress,
                Author = TestUserId,
                PictureLink = "test-picture.jpg",
                Tags = "tag1,tag2",
                Context = "TestContext",
                Notes = "Test notes",
                CreatedBy = TestUserId
            };

            _testContactFamily = new Contact
            {
                ContactId = TestContactId + 1,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                FirstName = "Jane",
                MiddleName = "M",
                LastName = "Smith",
                DisplayName = "Jane Smith",
                AddressIdNumber = null,
                Author = TestUserId,
                PictureLink = Constants.ProfilePictureUrl,
                CreatedBy = TestUserId
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = TestTimelineItemId,
                ProgenyId = TestProgenyId,
                ItemId = TestContactId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Contact
            };

            // Setup mocks
            _mockImageStore = new Mock<IImageStore>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockContactService = new Mock<IContactService>();
            _mockLocationService = new Mock<ILocationService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Initialize controller
            _controller = new ContactsController(
                _mockImageStore.Object,
                _mockUserInfoService.Object,
                _mockContactService.Object,
                _mockLocationService.Object,
                _mockTimelineService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                _mockWebNotificationsService.Object,
                _mockAccessManagementService.Object,
                _httpClient
            );

            // Setup controller context with claims
            SetupControllerContext(TestUserEmail, TestUserId);
        }

        private void SetupControllerContext(string email, string userId)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, email),
                new("sub", userId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public void Dispose()
        {
            _progenyDbContext.Dispose();
        }

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_Contacts_When_User_Has_Access()
        {
            // Arrange
            List<Contact> contacts = [_testContactProgeny];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(contacts);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Contact> returnedContacts = Assert.IsType<List<Contact>>(okResult.Value);
            Assert.Single(returnedContacts);
            Assert.Equal(TestContactId, returnedContacts[0].ContactId);
        }

        [Fact]
        public async Task Progeny_Should_Return_NotFound_When_No_Contacts_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContactsList(TestProgenyId, 0, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Family Tests

        [Fact]
        public async Task Family_Should_Return_Ok_With_Contacts_When_User_Has_Access()
        {
            // Arrange
            List<Contact> contacts = [_testContactFamily];
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContactsList(0, TestFamilyId, _testUser))
                .ReturnsAsync(contacts);

            // Act
            IActionResult result = await _controller.Family(TestFamilyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Contact> returnedContacts = Assert.IsType<List<Contact>>(okResult.Value);
            Assert.Single(returnedContacts);
            Assert.Equal(TestContactId + 1, returnedContacts[0].ContactId);
        }

        [Fact]
        public async Task Family_Should_Return_NotFound_When_No_Contacts_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContactsList(0, TestFamilyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Family(TestFamilyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region GetContactItem Tests

        [Fact]
        public async Task GetContactItem_Should_Return_Ok_With_Contact()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(_testContactProgeny);

            // Act
            IActionResult result = await _controller.GetContactItem(TestContactId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Contact contact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal(TestContactId, contact.ContactId);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Contact_For_Progeny()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.DefaultPictureLink
            };

            Contact addedContact = new()
            {
                ContactId = TestContactId + 10,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.ProfilePictureUrl,
                Author = TestUserId,
                CreatedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.AddContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(addedContact);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockContactService.Setup(x => x.GetContact(addedContact.ContactId, _testUser))
                .ReturnsAsync(addedContact);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Contact returnedContact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal(TestContactId + 10, returnedContact.ContactId);
            Assert.Equal(Constants.ProfilePictureUrl, returnedContact.PictureLink);
        }

        [Fact]
        public async Task Post_Should_Return_Ok_When_Valid_Contact_For_Family()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.DefaultPictureLink
            };

            Contact addedContact = new()
            {
                ContactId = TestContactId + 11,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.ProfilePictureUrl,
                Author = TestUserId,
                CreatedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.AddContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(addedContact);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockContactService.Setup(x => x.GetContact(addedContact.ContactId, _testUser))
                .ReturnsAsync(addedContact);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Contact returnedContact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal(TestContactId + 11, returnedContact.ContactId);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                FirstName = "Invalid",
                LastName = "Contact"
            };

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A contact cannot be linked to both a Progeny and a Family.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_BadRequest_When_Neither_ProgenyId_Nor_FamilyId_Set()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 0,
                FamilyId = 0,
                FirstName = "Invalid",
                LastName = "Contact"
            };

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A contact must be linked to either a Progeny or a Family.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Progeny_Permission()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Lacks_Family_Permission()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                FirstName = "New",
                LastName = "Contact"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_Should_Add_Address_When_Provided()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.DefaultPictureLink,
                Address = new Address
                {
                    AddressLine1 = "456 New St",
                    City = "New City"
                }
            };

            Address addedAddress = new()
            {
                AddressId = TestAddressId + 1,
                AddressLine1 = "456 New St",
                City = "New City"
            };

            Contact addedContact = new()
            {
                ContactId = TestContactId + 12,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact",
                DisplayName = "New Contact",
                PictureLink = Constants.ProfilePictureUrl,
                AddressIdNumber = TestAddressId + 1,
                Author = TestUserId,
                CreatedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(addedAddress);
            _mockContactService.Setup(x => x.AddContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(addedContact);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockContactService.Setup(x => x.GetContact(addedContact.ContactId, _testUser))
                .ReturnsAsync(addedContact);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockLocationService.Verify(x => x.AddAddressItem(It.IsAny<Address>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_ContactService_Returns_Null()
        {
            // Arrange
            Contact newContact = new()
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "New",
                LastName = "Contact",
                PictureLink = Constants.DefaultPictureLink
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.AddContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.Post(newContact);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Return_Ok_When_Update_Successful()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "Updated",
                LastName = "Name",
                DisplayName = "Updated Name",
                PictureLink = "test-picture.jpg"
            };

            Contact existingContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "Old",
                LastName = "Name",
                AddressIdNumber = TestAddressId
            };

            Contact updatedContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                FirstName = "Updated",
                LastName = "Name",
                DisplayName = "Updated Name",
                PictureLink = "test-picture.jpg",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(existingContact);
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(_testAddress);
            _mockContactService.Setup(x => x.UpdateContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(updatedContact);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(updatedContact);

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Contact returnedContact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal("Updated", returnedContact.FirstName);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Id_Mismatch()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId + 999,
                ProgenyId = TestProgenyId,
                FamilyId = 0
            };

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Both_ProgenyId_And_FamilyId_Set()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId
            };

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_BadRequest_When_Neither_ProgenyId_Nor_FamilyId_Set()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = 0,
                FamilyId = 0
            };

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Lacks_Edit_Permission()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Contact_Does_Not_Exist()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateContact_Returns_Null()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                PictureLink = "test.jpg"
            };

            Contact existingContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(existingContact);
            _mockContactService.Setup(x => x.UpdateContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.Put(TestContactId, updateContact);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Remove_Address_When_Set_To_Null()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Address = null,
                PictureLink = "test.jpg"
            };

            Contact existingContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = TestAddressId
            };

            Contact updatedContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = null,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(existingContact);
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.RemoveAddressItem(TestAddressId))
                .Returns(Task.CompletedTask);
            _mockContactService.Setup(x => x.UpdateContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(updatedContact);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            
            // Act
            await _controller.Put(TestContactId, updateContact);

            // Assert
            _mockLocationService.Verify(x => x.RemoveAddressItem(TestAddressId), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Add_New_Address_When_No_Previous_Address()
        {
            // Arrange
            Contact updateContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Address = new Address
                {
                    AddressLine1 = "New Address",
                    City = "New City"
                },
                PictureLink = "test.jpg"
            };
            
            Address newAddress = new()
            {
                AddressId = TestAddressId + 5,
                AddressLine1 = "New Address",
                City = "New City"
            };

            Contact existingContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = null,
                Address = newAddress
            };

            Contact updatedContact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = TestAddressId + 5,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Edit))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(existingContact);
            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(newAddress);
            _mockContactService.Setup(x => x.UpdateContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(updatedContact);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            
            // Act
            await _controller.Put(TestContactId, updateContact);

            // Assert
            _mockLocationService.Verify(x => x.AddAddressItem(It.IsAny<Address>()), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_Delete_Successful()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = TestAddressId,
                PictureLink = "test-picture.jpg"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contactToDelete);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.DeleteContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(contactToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.RemoveAddressItem(TestAddressId))
                .Returns(Task.CompletedTask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestContactId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Contact_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.Delete(TestContactId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Lacks_Admin_Permission()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(_testContactProgeny);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestContactId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteContact_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(_testContactProgeny);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.DeleteContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.Delete(TestContactId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Delete_Address_When_Present()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                AddressIdNumber = TestAddressId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contactToDelete);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.DeleteContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(contactToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(TestContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.RemoveAddressItem(TestAddressId))
                .Returns(Task.CompletedTask);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(TestContactId);

            // Assert
            _mockLocationService.Verify(x => x.RemoveAddressItem(TestAddressId), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Handle_Family_Contact()
        {
            // Arrange
            Contact contactToDelete = new()
            {
                ContactId = TestContactId + 1,
                ProgenyId = 0,
                FamilyId = TestFamilyId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId + 1, _testUser))
                .ReturnsAsync(contactToDelete);
            _mockAccessManagementService.Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, TestContactId + 1, _testUser, PermissionLevel.Admin))
                .ReturnsAsync(true);
            _mockContactService.Setup(x => x.DeleteContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(contactToDelete);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId((TestContactId + 1).ToString(), (int)KinaUnaTypes.TimeLineType.Contact, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);
            _mockWebNotificationsService.Setup(x => x.SendContactNotification(It.IsAny<Contact>(), _testUser, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestContactId + 1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockFamiliesService.Verify(x => x.GetFamilyById(TestFamilyId, _testUser), Times.Once);
        }

        #endregion

        #region DownloadPicture Tests

        [Fact]
        public async Task DownloadPicture_Should_Return_Ok_When_Picture_Downloaded()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                PictureLink = "http://example.com/image.jpg",
                ItemPerMission = new TimelineItemPermission() { PermissionLevel = PermissionLevel.Edit }
            };

            string newPictureLink = "downloaded-image.jpg";
            Contact updatedContact = new()
            {
                ContactId = TestContactId,
                PictureLink = newPictureLink
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contact);
            _mockImageStore.Setup(x => x.SaveImage(It.IsAny<Stream>(), BlobContainers.Contacts, It.IsAny<string>()))
                .ReturnsAsync(newPictureLink);
            _mockContactService.Setup(x => x.UpdateContact(It.IsAny<Contact>(), _testUser))
                .ReturnsAsync(updatedContact);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestContactId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Contact returnedContact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal(newPictureLink, returnedContact.PictureLink);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_Contact_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync((Contact)null!);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestContactId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_Contact_Id_Is_Zero()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = 0
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contact);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestContactId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_User_Lacks_Edit_Permission()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                PictureLink = "http://example.com/image.jpg",
                ItemPerMission = new TimelineItemPermission() { PermissionLevel = PermissionLevel.View }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contact);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestContactId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadPicture_Should_Return_NotFound_When_PictureLink_Not_Http_Url()
        {
            // Arrange
            Contact contact = new()
            {
                ContactId = TestContactId,
                ProgenyId = TestProgenyId,
                PictureLink = "local-file.jpg",
                ItemPerMission = new TimelineItemPermission() { PermissionLevel = PermissionLevel.Edit }
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockContactService.Setup(x => x.GetContact(TestContactId, _testUser))
                .ReturnsAsync(contact);

            // Act
            IActionResult result = await _controller.DownloadPicture(TestContactId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion
    }
}