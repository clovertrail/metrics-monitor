// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// The certificate helper class
    /// </summary>
    internal class CertificateHelper
    {
        /// <summary>
        /// Finds and validates certificate.
        /// </summary>
        /// <param name="certificateThumbprint">The certificate thumbprint.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <returns>The certificate if validated.</returns>
        internal static X509Certificate2 FindAndValidateCertificate(string certificateThumbprint, StoreLocation certificateStoreLocation)
        {
            X509Certificate2 cert = FindX509Certificate(certificateThumbprint, certificateStoreLocation);
            if (!cert.HasPrivateKey)
            {
                throw new MetricsClientException(string.Format("Cert with Thumbprint [{0}] doesn't have a private key", cert.Thumbprint));
            }

            // Check expire and effective date
            DateTime now = DateTime.Now;
            if (cert.NotBefore > now)
            {
                throw new MetricsClientException(string.Format("The certificate is not valid until {0}.", cert.GetEffectiveDateString()));
            }

            if (cert.NotAfter < now)
            {
                throw new MetricsClientException(string.Format("The certificate is not valid after {0}.", cert.GetExpirationDateString()));
            }

            try
            {
                if (cert.PrivateKey == null)
                {
                    throw new MetricsClientException("The certificate has a private key but the PrivateKey property is null, and it is typically due to a permission issue.");
                }
            }
            catch (CryptographicException)
            {
                throw new MetricsClientException("The certificate has a private key but the PrivateKey property is null, and it is typically due to a permission issue.");
            }

            return cert;
        }

        /// <summary>
        /// Finds the X509 certificate.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <returns>
        /// The <see cref="X509Certificate2" /> certificate if found.
        /// </returns>
        private static X509Certificate2 FindX509Certificate(string thumbprint, StoreLocation storeLocation)
        {
            var store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certificates.Count == 0)
            {
                throw new MetricsClientException(
                    string.Format("No cert with Thumbprint [{0}] is found in the [{1}] store", thumbprint, storeLocation));
            }

            var cert = certificates.OfType<X509Certificate2>().FirstOrDefault(c => c.HasPrivateKey);
            if (cert == null)
            {
                throw new MetricsClientException(string.Format("No cert with Thumbprint [{0}] has a private key", thumbprint));
            }

            return cert;
        }
    }
}
