using System.Security.Cryptography;
using KinaUna.OpenIddict.HostingExtensions;
using KinaUna.OpenIddict.HostingExtensions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Cryptography.X509Certificates;
using OpenIddict.Validation;

namespace KinaUna.OpenIddict.Tests.HostingExtensions
{
    public class OpenIddictConfigurationTests
    {
        // Test certificates with the thumbprints below need to be present in the current user's certificate store.
        private const string TestEncryptionThumbprint = "97ce9ffd8e1a22bde47513719db5eb0a1addc4f0";
        private const string TestSigningThumbprint = "f0af1f3875f89d46d9d867fba664b86ca0af2b4f";

        [Fact]
        public void ConfigureServices_RegistersRequiredServices()
        {
            // Arrange
            ServiceCollection services = new();
            Mock<ICertificateProvider> mockCertProvider = new();

            
            mockCertProvider.Setup(p => 
                    p.GetCertificate(TestEncryptionThumbprint))
                .Returns(GetTestEncryptionCertificate(TestEncryptionThumbprint));

            mockCertProvider.Setup(p =>
                    p.GetCertificate(TestSigningThumbprint))
                .Returns(GetTestSigningCertificate(TestSigningThumbprint));

            OpenIddictConfiguration configuration = new(
                TestEncryptionThumbprint,
                TestSigningThumbprint,
                mockCertProvider.Object);
            
            // Act
            configuration.ConfigureServices(services);
            
            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            
            // Verify essential services were registered
            Assert.NotNull(serviceProvider.GetService<OpenIddictValidationService>());
            //Assert.NotNull(serviceProvider.GetService<OpenIddictServerService>());
            
            // Verify certificate provider was called twice (once for encryption, once for signing)
            mockCertProvider.Verify(p => p.GetCertificate(TestEncryptionThumbprint), Times.Exactly(2));
            mockCertProvider.Verify(p => p.GetCertificate(TestSigningThumbprint), Times.Exactly(2));
        }
        
        [Fact]
        public void SigningCertificate_IsValid_ForDigitalSignature()
        {
            // Arrange
            X509Certificate2 signingCertificate = GetTestSigningCertificate(TestSigningThumbprint);
            
            // Act & Assert
            Assert.True(signingCertificate.HasPrivateKey, "Signing certificate must have a private key");
            Assert.True(IsValidForDigitalSignature(signingCertificate), "Signing certificate must be valid for digital signatures");
            Assert.True(signingCertificate.NotAfter > DateTime.Now, "Signing certificate must not be expired");
            Assert.True(signingCertificate.NotBefore < DateTime.Now, "Signing certificate validity period must have started");
        }
        
        [Fact]
        public void EncryptionCertificate_IsValid_ForKeyEncipherment()
        {
            // Arrange
            X509Certificate2 encryptionCertificate = GetTestEncryptionCertificate(TestEncryptionThumbprint);
            
            // Act & Assert
            Assert.True(encryptionCertificate.HasPrivateKey, "Encryption certificate must have a private key");
            Assert.True(IsValidForKeyEncipherment(encryptionCertificate), "Encryption certificate must be valid for key encipherment");
            Assert.True(encryptionCertificate.NotAfter > DateTime.Now, "Encryption certificate must not be expired");
            Assert.True(encryptionCertificate.NotBefore < DateTime.Now, "Encryption certificate validity period must have started");
        }
        
        private bool IsValidForDigitalSignature(X509Certificate2 certificate)
        {
            // Check if the certificate has the Digital Signature key usage
            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsage)
                {
                    return keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);
                }
            }
            
            return false;
        }
        
        private bool IsValidForKeyEncipherment(X509Certificate2 certificate)
        {
            // Check if the certificate has the Key Encipherment key usage
            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsage)
                {
                    return keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.KeyEncipherment);
                }
            }
            
            return false;
        }
        
        private X509Certificate2 GetTestEncryptionCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Place all certificates in an X509Certificate2Collection object.
                X509Certificate2Collection certCollection = store.Certificates;
                // If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
                // currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                    throw new CryptographicException($"Certificate with thumbprint {thumbprint} not found in the current user's certificate store.");
                // Return the first certificate in the collection, has the thumbprint and is current.
                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }

        private X509Certificate2 GetTestSigningCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Place all certificates in an X509Certificate2Collection object.
                X509Certificate2Collection certCollection = store.Certificates;
                // If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
                // currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                    throw new CryptographicException($"Certificate with thumbprint {thumbprint} not found in the current user's certificate store.");
                // Return the first certificate in the collection, has the thumbprint and is current.
                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}