using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SecureText.Tests
{
    [TestClass]
    public class TransformXmlTest
    {
        [TestInitialize]
        public void Initialize()
        {
            Program.Startup();
        }

        [TestMethod]
        public void TransformXml()
        {
            TransformXml t = new TransformXml();

            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "sample");
            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "xmlencrypt");
            this.CopyDirectory(sourcePath, destinationPath);
            t.Execute(@"xmlencrypt\config\appSettings\sample.config.securetext", false);
            t.Execute(@"xmlencrypt\config\connectionStrings\sample.config.securetext", false);
            sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "xmlencrypt");
            destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "xmldecrypt");
            this.CopyDirectory(sourcePath, destinationPath);
            t.Execute(@"xmldecrypt\config\appSettings\sample.config.securetext", true);
            t.Execute(@"xmldecrypt\config\connectionStrings\sample.config.securetext", true);

            FileInfo encrypt = new FileInfo(@"xmlencrypt\sample.config");
            FileInfo decrypt = new FileInfo(@"xmldecrypt\sample.config");
            Assert.IsTrue(encrypt.Length == decrypt.Length);
            encrypt = new FileInfo(@"xmlencrypt\config\appSettings\sample.config");
            decrypt = new FileInfo(@"xmldecrypt\config\appSettings\sample.config");
            Assert.IsTrue(encrypt.Length > decrypt.Length);
            encrypt = new FileInfo(@"xmlencrypt\config\connectionStrings\sample.config");
            decrypt = new FileInfo(@"xmldecrypt\config\connectionStrings\sample.config");
            Assert.IsTrue(encrypt.Length > decrypt.Length);
        }

        private void CopyDirectory(string sourcePath, string destinationPath)
        {
            foreach (string path in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(path.Replace(sourcePath, destinationPath));
            }

            foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourcePath, destinationPath), true);
            }
        }
    }
}
