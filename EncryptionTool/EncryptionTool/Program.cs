using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace CryptoTool
{
    // Use this powershell commands to create your own certificate
    // $cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -Type DocumentEncryptionCert -KeyUsage DataEncipherment -Subject 'encryption' -Provider 'Microsoft Enhanced Cryptographic Provider v1.0' -NotAfter(Get-Date).AddYears(30)
    // $securePassword = ConvertTo-SecureString -String "1234" -Force –AsPlainTextC:\git\AzureServiceFabricContainers\EncryptionTool\EncryptionTool\Program.cs
    // $cert | Export-PfxCertificate -FilePath 'private.pfx' -Password $securePassword
    // $cert | Export-Certificate -FilePath 'public.cer'

    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            Configuration = builder.Build();

            //Console.WriteLine(Decrypt(Encrypt("hola")));
            ////string file = @"C:\git\AzureServiceFabricContainers\FullFrameworkApps\ConsoleApp1\obj\ci\test-scus\Package\ConsoleApp1.exe.config";
            //string file = @"C:\git\AzureServiceFabricContainers\FullFrameworkApps\ConsoleApp1\App.test-scus.config";
            //TransformXml(file, false);
            if (args.Length < 2)
            {
                Console.WriteLine("usage: dotnet ConsoleApp1.dll [EncryptXml|DecryptXml] [File]");
                return;
            }

            if (string.Equals("EncryptXml", args[0]))
            {
                TransformXml(args[1], false);
            }
            else if (string.Equals("DecryptXml", args[0]))
            {
                TransformXml(args[1], true);
            }
        }

        static void TransformXml(string file, bool decrypt)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("File not found: {0}", file);
                return;
            }

            string name = "configSource";
            var xml = XDocument.Load(file);
            var elements = (from el in xml.Root.Descendants()
                            where el.Attribute(name) != null
                            select el);
            string path = Path.GetDirectoryName(file);
            foreach (var element in elements)
            {
                string configFile = Path.Combine(path, element.Attribute(name).Value);
                TransformXml(configFile, decrypt);
            }

            name = decrypt ? "decryptAttr" : "encryptAttr";
            elements = (from el in xml.Root.Descendants()
                        where el.Attribute(name) != null
                        select el);
            bool save = false;
            foreach (var element in elements)
            {
                var attribute = element.Attribute(name);
                var updateAttribute = element.Attribute(attribute.Value);
                if (updateAttribute != null)
                {
                    if (decrypt)
                    {
                        updateAttribute.Value = Decrypt(updateAttribute.Value);
                    }
                    else
                    {
                        updateAttribute.Value = Encrypt(updateAttribute.Value);
                        element.Add(new XAttribute("decryptAttr", attribute.Value));
                    }

                    attribute.Remove();
                }
                save = true;
            }

            if (save)
            {
                xml.Save(new FileStream(file, FileMode.Create));
            }
        }

        static X509Certificate2 GetCertificate(bool privateCert)
        {
            X509Certificate2 cert = null;
            if (!string.IsNullOrEmpty(Configuration["storeLocation"]) && !string.IsNullOrEmpty(Configuration["certThumbprint"]))
            {
                X509Store store = new X509Store(StoreName.My, (StoreLocation)Enum.Parse(typeof(StoreLocation), Configuration["storeLocation"]));
                store.Open(OpenFlags.ReadOnly);
                cert = store.Certificates.Find(X509FindType.FindByThumbprint, Configuration["certThumbprint"], true)[0];
            }
            else if (privateCert)
            {
                cert = new X509Certificate2(Configuration["certPfx"], Configuration["certPfxPassword"]);
            }
            else
            {
                cert = new X509Certificate2(Configuration["certPublic"]);
            }

            return cert;
        }

        static string Encrypt(string value)
        {
            var cert = GetCertificate(false);
            var rsa = cert.GetRSAPublicKey();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] encryptedBytes = rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedBytes);
        }

        static string Decrypt(string value)
        {
            var cert = GetCertificate(true);
            var rsa = cert.GetRSAPrivateKey();
            byte[] encryptedBytes = Convert.FromBase64String(value);
            byte[] bytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}