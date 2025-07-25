using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KinaUna.OpenIddict.HostingExtensions.Interfaces
{
    /// <summary>
    /// Defines a contract for retrieving X.509 certificates based on their thumbprint.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for locating and returning the appropriate
    /// X.509 certificate corresponding to the specified thumbprint.</remarks>
    public interface ICertificateProvider
    {
        X509Certificate2 GetCertificate(string thumbprint);
    }

    /// <summary>
    /// Provides functionality to retrieve X.509 certificates from the current user's certificate store.
    /// </summary>
    /// <remarks>This class implements the <see cref="ICertificateProvider"/> interface to provide a method
    /// for obtaining certificates based on their thumbprint. It searches the current user's certificate store for a
    /// valid certificate with the specified thumbprint.</remarks>
    public class DefaultCertificateProvider : ICertificateProvider
    {
        public X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
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