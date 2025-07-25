using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KinaUna.Data.Utilities
{
    /// <summary>
    /// Retrieves an X.509 certificate from the current user's certificate store using the specified thumbprint.
    /// </summary>
    /// <remarks>This method searches the current user's personal certificate store for a certificate that is
    /// valid at the current time and matches the specified thumbprint. If multiple certificates match, the first one
    /// found is returned.</remarks>
    public static class CertificateTools
    {
        /// <summary>
        /// Retrieves an X.509 certificate from the current user's certificate store using the specified thumbprint.
        /// </summary>
        /// <remarks>This method searches the current user's certificate store for a certificate that
        /// matches the provided thumbprint and is valid at the current time. It returns the first matching certificate
        /// found. Ensure that the thumbprint is correct and that the certificate is present in the store.</remarks>
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve. This must be a valid thumbprint string.</param>
        /// <returns>The <see cref="X509Certificate2"/> object that matches the specified thumbprint and is currently valid.</returns>
        /// <exception cref="CryptographicException">Thrown if a certificate with the specified thumbprint is not found in the current user's certificate store.</exception>
        public static X509Certificate2 GetCertificate(string thumbprint)
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
