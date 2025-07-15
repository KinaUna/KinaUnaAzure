using KinaUna.OpenIddict.Services;
using KinaUna.OpenIddict.Services.Interfaces;
using Moq;
using OpenIddict.Abstractions;

namespace KinaUna.OpenIddict.Tests.Services
{
    public class OpenIddictSeedServiceTests
    {
        [Fact]
        public async Task SeedAsync_CreatesScopes_WhenTheyDoNotExist()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var mockScopeManager = new Mock<IOpenIddictScopeManager>();
            var mockConfigProvider = new Mock<IClientConfigProvider>();
            
            // Setup the scope manager to return null (scope doesn't exist)
            mockScopeManager.Setup(m => m.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);
            
            // Setup the client config provider
            mockConfigProvider.Setup(p => p.GetClientConfigs())
                .Returns(Array.Empty<ClientConfig>());
            
            var service = new OpenIddictSeedService(
                mockAppManager.Object,
                mockScopeManager.Object,
                mockConfigProvider.Object);
            
            // Act
            await service.SeedAsync();
            
            // Assert
            mockScopeManager.Verify(
                m => m.CreateAsync(It.IsAny<OpenIddictScopeDescriptor>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2)); // Verify that 2 scopes were created
        }
        
        [Fact]
        public async Task SeedAsync_DoesNotCreateScopes_WhenTheyAlreadyExist()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var mockScopeManager = new Mock<IOpenIddictScopeManager>();
            var mockConfigProvider = new Mock<IClientConfigProvider>();
            
            // Setup the scope manager to return a non-null value (scope exists)
            mockScopeManager.Setup(m => m.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new object());
            
            // Setup the client config provider
            mockConfigProvider.Setup(p => p.GetClientConfigs())
                .Returns(Array.Empty<ClientConfig>());
            
            var service = new OpenIddictSeedService(
                mockAppManager.Object,
                mockScopeManager.Object,
                mockConfigProvider.Object);
            
            // Act
            await service.SeedAsync();
            
            // Assert
            mockScopeManager.Verify(
                m => m.CreateAsync(It.IsAny<OpenIddictScopeDescriptor>(), It.IsAny<CancellationToken>()),
                Times.Never); // Verify that no scopes were created
        }
    }
}