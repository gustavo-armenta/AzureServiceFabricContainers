using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SecureText.Tests
{
    [TestClass]
    public class EncryptionLibTest
    {
        [TestInitialize]
        public void Initialize()
        {
            Program.Startup();
        }

        [TestMethod]
        public void PublicCertificate()
        {
            EncryptLib lib = new EncryptLib();
            var cert = lib.GetCertificate(false);
            Assert.IsNotNull(cert);
            Assert.IsFalse(cert.HasPrivateKey);
        }

        [TestMethod]
        public void PrivateCertificate()
        {
            EncryptLib lib = new EncryptLib();
            var cert = lib.GetCertificate(true);
            Assert.IsNotNull(cert);
            Assert.IsTrue(cert.HasPrivateKey);
        }

        [TestMethod]
        public void Encrypt()
        {
            EncryptLib lib = new EncryptLib();
            string original = "Hello World!";
            string encrypted = lib.Encrypt(original);
            Assert.AreNotEqual(original, encrypted);
        }

        [TestMethod]
        public void Decrypt()
        {
            EncryptLib lib = new EncryptLib();
            string original = "Hello World!";
            string encrypted = lib.Encrypt(original);
            string decrypted = lib.Decrypt(encrypted);
            Assert.AreEqual(original, decrypted);
        }
    }
}
