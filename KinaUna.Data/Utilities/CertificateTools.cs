using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KinaUna.Data.Utilities
{
    /// <summary>
    /// Provides methods for loading X.509 certificates from the certificate store or from PFX files.
    /// </summary>
    /// <remarks>
    /// On Windows, certificates are loaded from the current user's personal certificate store using a thumbprint.
    /// In Docker/Linux environments where the certificate store is unavailable, certificates can be loaded from
    /// PFX files instead by setting environment variables such as <c>EncryptionCertificatePfxPath</c>/<c>EncryptionCertificatePfxPassword</c>,
    /// <c>SigningCertificatePfxPath</c>/<c>SigningCertificatePfxPassword</c>, or <c>CertificatePfxPath</c>/<c>CertificatePfxPassword</c>, depending on the certificate type.
    /// </remarks>
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

        /// <summary>
        /// Loads an X.509 certificate from a PFX file.
        /// </summary>
        /// <remarks>This method is intended for Docker/Linux environments where the Windows certificate store is not available.
        /// The PFX file should contain both the certificate and its private key.</remarks>
        /// <param name="pfxPath">The file path to the PFX file.</param>
        /// <param name="pfxPassword">The password for the PFX file. Can be null if the PFX file is not password-protected.</param>
        /// <returns>The <see cref="X509Certificate2"/> loaded from the PFX file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the PFX file does not exist at the specified path.</exception>
        public static X509Certificate2 GetCertificateFromPfxFile(string pfxPath, string? pfxPassword)
        {
            if (!File.Exists(pfxPath))
                throw new FileNotFoundException($"Certificate PFX file not found at path: {pfxPath}");

            return X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
        }
    }
}
