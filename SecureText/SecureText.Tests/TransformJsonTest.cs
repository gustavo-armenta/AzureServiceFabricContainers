using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SecureText.Tests
{
    [TestClass]
    public class TransformJsonTest
    {
        [TestInitialize]
        public void Initialize()
        {
            Program.Startup();
        }

        [TestMethod]
        public void TransformJson()
        {
            TransformJson t = new TransformJson();

            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "sample");
            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "jsonencrypt");
            this.CopyDirectory(sourcePath, destinationPath);
            t.Execute(@"jsonencrypt\sample.json.securetext", false);
            sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "jsonencrypt");
            destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "jsondecrypt");
            this.CopyDirectory(sourcePath, destinationPath);
            t.Execute(@"jsondecrypt\sample.json.securetext", true);

            FileInfo encrypt = new FileInfo(@"jsonencrypt\sample.json");
            FileInfo decrypt = new FileInfo(@"jsondecrypt\sample.json");
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
