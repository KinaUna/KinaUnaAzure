using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class AddressesControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly AddressesController _controller;

        private readonly Address _testAddress;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestAddressId = 100;

        public AddressesControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            UserInfo testUser = new()
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = 1,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User",
                ProfilePicture = "profile.jpg"
            };

            _testAddress = new Address
            {
                AddressId = TestAddressId,
                AddressLine1 = "123 Main Street",
                AddressLine2 = "Apt 4B",
                City = "Springfield",
                State = "IL",
                PostalCode = "62701",
                Country = "USA"
            };

            // Seed database
            _progenyDbContext.UserInfoDb.Add(testUser);
            _progenyDbContext.SaveChanges();

            // Setup mocks
            _mockLocationService = new Mock<ILocationService>();

            _controller = new AddressesController(_mockLocationService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, TestUserEmail),
                new(ClaimTypes.NameIdentifier, TestUserId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region GetAddressItem Tests

        [Fact]
        public async Task GetAddressItem_Should_Return_Ok_When_Address_Exists()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);

            // Act
            IActionResult result = await _controller.GetAddressItem(TestAddressId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(TestAddressId, returnedAddress.AddressId);
            Assert.Equal(_testAddress.AddressLine1, returnedAddress.AddressLine1);
            Assert.Equal(_testAddress.City, returnedAddress.City);

            _mockLocationService.Verify(x => x.GetAddressItem(TestAddressId), Times.Once);
        }

        [Fact]
        public async Task GetAddressItem_Should_Return_Ok_With_Null_When_Address_Does_Not_Exist()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(999))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.GetAddressItem(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);

            _mockLocationService.Verify(x => x.GetAddressItem(999), Times.Once);
        }

        [Fact]
        public async Task GetAddressItem_Should_Return_Complete_Address_Details()
        {
            // Arrange
            Address detailedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "456 Oak Avenue",
                AddressLine2 = "Suite 200",
                City = "Chicago",
                State = "IL",
                PostalCode = "60601",
                Country = "USA"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(detailedAddress);

            // Act
            IActionResult result = await _controller.GetAddressItem(TestAddressId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(detailedAddress.AddressId, returnedAddress.AddressId);
            Assert.Equal(detailedAddress.AddressLine1, returnedAddress.AddressLine1);
            Assert.Equal(detailedAddress.AddressLine2, returnedAddress.AddressLine2);
            Assert.Equal(detailedAddress.City, returnedAddress.City);
            Assert.Equal(detailedAddress.State, returnedAddress.State);
            Assert.Equal(detailedAddress.PostalCode, returnedAddress.PostalCode);
            Assert.Equal(detailedAddress.Country, returnedAddress.Country);
        }

        [Fact]
        public async Task GetAddressItem_Should_Handle_Zero_AddressId()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(0))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.GetAddressItem(0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);

            _mockLocationService.Verify(x => x.GetAddressItem(0), Times.Once);
        }

        [Fact]
        public async Task GetAddressItem_Should_Handle_Negative_AddressId()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(-1))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.GetAddressItem(-1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);

            _mockLocationService.Verify(x => x.GetAddressItem(-1), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Add_Address_And_Return_Ok()
        {
            // Arrange
            Address newAddress = new()
            {
                AddressLine1 = "789 Elm Street",
                City = "Boston",
                State = "MA",
                PostalCode = "02101",
                Country = "USA"
            };

            Address addedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = newAddress.AddressLine1,
                City = newAddress.City,
                State = newAddress.State,
                PostalCode = newAddress.PostalCode,
                Country = newAddress.Country
            };

            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(addedAddress);

            // Act
            IActionResult result = await _controller.Post(newAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(TestAddressId, returnedAddress.AddressId);
            Assert.Equal(newAddress.AddressLine1, returnedAddress.AddressLine1);
            Assert.Equal(newAddress.City, returnedAddress.City);

            _mockLocationService.Verify(x => x.AddAddressItem(It.Is<Address>(a =>
                a.AddressLine1 == newAddress.AddressLine1 &&
                a.City == newAddress.City)), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Add_Address_With_All_Fields()
        {
            // Arrange
            Address newAddress = new()
            {
                AddressLine1 = "321 Pine Road",
                AddressLine2 = "Building C",
                City = "Seattle",
                State = "WA",
                PostalCode = "98101",
                Country = "USA"
            };

            Address addedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = newAddress.AddressLine1,
                AddressLine2 = newAddress.AddressLine2,
                City = newAddress.City,
                State = newAddress.State,
                PostalCode = newAddress.PostalCode,
                Country = newAddress.Country
            };

            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(addedAddress);

            // Act
            IActionResult result = await _controller.Post(newAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(addedAddress.AddressLine1, returnedAddress.AddressLine1);
            Assert.Equal(addedAddress.AddressLine2, returnedAddress.AddressLine2);
            Assert.Equal(addedAddress.City, returnedAddress.City);
            Assert.Equal(addedAddress.State, returnedAddress.State);
            Assert.Equal(addedAddress.PostalCode, returnedAddress.PostalCode);
            Assert.Equal(addedAddress.Country, returnedAddress.Country);
        }

        [Fact]
        public async Task Post_Should_Add_Address_With_Minimal_Fields()
        {
            // Arrange
            Address newAddress = new()
            {
                AddressLine1 = "123 Street"
            };

            Address addedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = newAddress.AddressLine1
            };

            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(addedAddress);

            // Act
            IActionResult result = await _controller.Post(newAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(TestAddressId, returnedAddress.AddressId);
            Assert.Equal(newAddress.AddressLine1, returnedAddress.AddressLine1);

            _mockLocationService.Verify(x => x.AddAddressItem(newAddress), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Handle_Null_Optional_Fields()
        {
            // Arrange
            Address newAddress = new()
            {
                AddressLine1 = "999 Main St",
                AddressLine2 = null,
                City = "Portland",
                State = null,
                PostalCode = "97201",
                Country = "USA"
            };

            Address addedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = newAddress.AddressLine1,
                AddressLine2 = newAddress.AddressLine2,
                City = newAddress.City,
                State = newAddress.State,
                PostalCode = newAddress.PostalCode,
                Country = newAddress.Country
            };

            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(addedAddress);

            // Act
            IActionResult result = await _controller.Post(newAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddress = Assert.IsType<Address>(okResult.Value);
            Assert.Null(returnedAddress.AddressLine2);
            Assert.Null(returnedAddress.State);
        }

        [Fact]
        public async Task Post_Should_Return_Ok_When_LocationService_Returns_Null()
        {
            // Arrange
            Address newAddress = new()
            {
                AddressLine1 = "Failed Address"
            };

            _mockLocationService.Setup(x => x.AddAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Post(newAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Address_And_Return_Ok()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "Updated Address Line",
                City = "Updated City",
                State = "CA",
                PostalCode = "90001",
                Country = "USA"
            };

            Address returnedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = updatedAddress.AddressLine1,
                City = updatedAddress.City,
                State = updatedAddress.State,
                PostalCode = updatedAddress.PostalCode,
                Country = updatedAddress.Country
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(returnedAddress);

            // Act
            IActionResult result = await _controller.Put(TestAddressId, updatedAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddressResult = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(updatedAddress.AddressLine1, returnedAddressResult.AddressLine1);
            Assert.Equal(updatedAddress.City, returnedAddressResult.City);

            _mockLocationService.Verify(x => x.GetAddressItem(TestAddressId), Times.Once);
            _mockLocationService.Verify(x => x.UpdateAddressItem(It.Is<Address>(a =>
                a.AddressId == TestAddressId &&
                a.AddressLine1 == updatedAddress.AddressLine1)), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Address_Does_Not_Exist()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = 999,
                AddressLine1 = "Non-existent Address"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(999))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Put(999, updatedAddress);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockLocationService.Verify(x => x.GetAddressItem(999), Times.Once);
            _mockLocationService.Verify(x => x.UpdateAddressItem(It.IsAny<Address>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Update_All_Address_Fields()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "New Line 1",
                AddressLine2 = "New Line 2",
                City = "New City",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            };

            Address returnedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = updatedAddress.AddressLine1,
                AddressLine2 = updatedAddress.AddressLine2,
                City = updatedAddress.City,
                State = updatedAddress.State,
                PostalCode = updatedAddress.PostalCode,
                Country = updatedAddress.Country
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(returnedAddress);

            // Act
            IActionResult result = await _controller.Put(TestAddressId, updatedAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddressResult = Assert.IsType<Address>(okResult.Value);
            Assert.Equal(updatedAddress.AddressLine1, returnedAddressResult.AddressLine1);
            Assert.Equal(updatedAddress.AddressLine2, returnedAddressResult.AddressLine2);
            Assert.Equal(updatedAddress.City, returnedAddressResult.City);
            Assert.Equal(updatedAddress.State, returnedAddressResult.State);
            Assert.Equal(updatedAddress.PostalCode, returnedAddressResult.PostalCode);
            Assert.Equal(updatedAddress.Country, returnedAddressResult.Country);
        }

        [Fact]
        public async Task Put_Should_Handle_Null_Optional_Fields_In_Update()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "Only Required Fields",
                AddressLine2 = null,
                City = "City Name",
                State = null,
                PostalCode = null,
                Country = "USA"
            };

            Address returnedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = updatedAddress.AddressLine1,
                AddressLine2 = null,
                City = updatedAddress.City,
                State = null,
                PostalCode = null,
                Country = updatedAddress.Country
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(returnedAddress);

            // Act
            IActionResult result = await _controller.Put(TestAddressId, updatedAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Address returnedAddressResult = Assert.IsType<Address>(okResult.Value);
            Assert.Null(returnedAddressResult.AddressLine2);
            Assert.Null(returnedAddressResult.State);
            Assert.Null(returnedAddressResult.PostalCode);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_With_Null_When_UpdateAddressItem_Returns_Null()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "Failed Update"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.UpdateAddressItem(It.IsAny<Address>()))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Put(TestAddressId, updatedAddress);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task Put_Should_Handle_Zero_AddressId()
        {
            // Arrange
            Address updatedAddress = new()
            {
                AddressId = 0,
                AddressLine1 = "Zero Id Address"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(0))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Put(0, updatedAddress);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Address_And_Return_NoContent()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(_testAddress);
            _mockLocationService.Setup(x => x.RemoveAddressItem(TestAddressId))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestAddressId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockLocationService.Verify(x => x.GetAddressItem(TestAddressId), Times.Once);
            _mockLocationService.Verify(x => x.RemoveAddressItem(TestAddressId), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Address_Does_Not_Exist()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(999))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockLocationService.Verify(x => x.GetAddressItem(999), Times.Once);
            _mockLocationService.Verify(x => x.RemoveAddressItem(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Call_RemoveAddressItem_With_Correct_Id()
        {
            // Arrange
            int addressIdToDelete = 42;
            Address addressToDelete = new()
            {
                AddressId = addressIdToDelete,
                AddressLine1 = "Address to Delete"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(addressIdToDelete))
                .ReturnsAsync(addressToDelete);
            _mockLocationService.Setup(x => x.RemoveAddressItem(addressIdToDelete))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(addressIdToDelete);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockLocationService.Verify(x => x.RemoveAddressItem(addressIdToDelete), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Handle_Zero_AddressId()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(0))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Delete(0);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockLocationService.Verify(x => x.RemoveAddressItem(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Handle_Negative_AddressId()
        {
            // Arrange
            _mockLocationService.Setup(x => x.GetAddressItem(-1))
                .ReturnsAsync(null as Address);

            // Act
            IActionResult result = await _controller.Delete(-1);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            _mockLocationService.Verify(x => x.RemoveAddressItem(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_Even_When_RemoveAddressItem_Completes()
        {
            // Arrange
            Address addressToDelete = new()
            {
                AddressId = TestAddressId,
                AddressLine1 = "Test Address",
                City = "Test City"
            };

            _mockLocationService.Setup(x => x.GetAddressItem(TestAddressId))
                .ReturnsAsync(addressToDelete);
            _mockLocationService.Setup(x => x.RemoveAddressItem(TestAddressId))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.Delete(TestAddressId);

            // Assert
            NoContentResult noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContentResult.StatusCode);
        }

        #endregion
    }
}