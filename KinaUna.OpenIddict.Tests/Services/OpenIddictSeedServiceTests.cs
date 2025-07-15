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
            Mock<IOpenIddictApplicationManager> mockAppManager = new();
            Mock<IOpenIddictScopeManager> mockScopeManager = new();
            Mock<IClientConfigProvider> mockConfigProvider = new();
            
            // Set up the scope manager to return null (scope doesn't exist)
            mockScopeManager.Setup(m => m.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as string);
            
            // Set up the client config provider
            mockConfigProvider.Setup(p => p.GetClientConfigs())
                .Returns([]);
            
            OpenIddictSeedService service = new(
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
            Mock<IOpenIddictApplicationManager> mockAppManager = new();
            Mock<IOpenIddictScopeManager> mockScopeManager = new();
            Mock<IClientConfigProvider> mockConfigProvider = new();
            
            // Set up the scope manager to return a non-null value (scope exists)
            mockScopeManager.Setup(m => m.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new object());
            
            // Set up the client config provider
            mockConfigProvider.Setup(p => p.GetClientConfigs())
                .Returns([]);
            
            OpenIddictSeedService service = new(
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