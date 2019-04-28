using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace CertificateInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathToPfx = args[0];
            var pfxPassword = new SecureString();
            foreach (char c in args[1])
            {
                pfxPassword.AppendChar(c);
            }

            var cert = new X509Certificate2(pathToPfx, pfxPassword, X509KeyStorageFlags.PersistKeySet);

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
            {
                store.Add(cert);
            }
        }
    }
}
