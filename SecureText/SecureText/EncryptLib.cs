using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace SecureText
{
    // Use this powershell commands to create your own certificate
    // $cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -Type DocumentEncryptionCert -KeyUsage DataEncipherment -Subject 'encryption' -Provider 'Microsoft Enhanced Cryptographic Provider v1.0' -NotAfter(Get-Date).AddYears(30)
    // $securePassword = ConvertTo-SecureString -String "1234" -Force –AsPlainText
    // $cert | Export-PfxCertificate -FilePath 'private.pfx' -Password $securePassword
    // $cert | Export-Certificate -FilePath 'public.cer'

    internal class EncryptLib
    {
        internal X509Certificate2 GetCertificate(bool privateCert)
        {
            IConfigurationRoot config = Program.Configuration;
            X509Certificate2 cert = null;
            if (!string.IsNullOrEmpty(config["store:location"]) && !string.IsNullOrEmpty(config["store:thumbprint"]))
            {
                X509Store store = new X509Store(StoreName.My, (StoreLocation)Enum.Parse(typeof(StoreLocation), config["storeLocation"]));
                store.Open(OpenFlags.ReadOnly);
                cert = store.Certificates.Find(X509FindType.FindByThumbprint, config["certThumbprint"], true)[0];
            }
            else if (privateCert)
            {
                cert = new X509Certificate2(config["file:pfx"], config["file:pfxPassword"]);
            }
            else
            {
                cert = new X509Certificate2(config["file:cer"]);
            }

            return cert;
        }

        internal string Encrypt(string value)
        {
            var cert = GetCertificate(false);
            var rsa = cert.GetRSAPublicKey();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] encryptedBytes = rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedBytes);
        }

        internal string Decrypt(string value)
        {
            var cert = GetCertificate(true);
            var rsa = cert.GetRSAPrivateKey();
            byte[] encryptedBytes = Convert.FromBase64String(value);
            byte[] bytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(bytes);
        }

        internal bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length > 50 && value.EndsWith("==");
        }
    }
}