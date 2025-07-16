using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KinaUna.OpenIddict.HostingExtensions.Interfaces
{
    public interface ICertificateProvider
    {
        X509Certificate2 GetCertificate(string thumbprint);
    }

    public class DefaultCertificateProvider : ICertificateProvider
    {
        public X509Certificate2 GetCertificate(string thumbprint)
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