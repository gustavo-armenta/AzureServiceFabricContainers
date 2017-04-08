using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SecureText
{
    internal class TransformXml
    {
        internal void Execute(string secureTextFile, bool decrypt)
        {
            if (string.IsNullOrEmpty(secureTextFile))
            {
                throw new ArgumentNullException("secureTextFile");
            }

            if (!File.Exists(secureTextFile))
            {
                throw new FileNotFoundException(secureTextFile);
            }

            string file = Path.Combine(Path.GetDirectoryName(secureTextFile), Path.GetFileNameWithoutExtension(secureTextFile));
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            XDocument xmlSecureText = null;
            try
            {
                xmlSecureText = XDocument.Load(secureTextFile);
            }
            catch
            {
                return;
            }

            EncryptLib lib = new EncryptLib();
            var xml = XDocument.Load(file);
            var resolver = new XmlNamespaceManager(new NameTable());
            var nspaces = xml.Root.CreateNavigator().GetNamespacesInScope(XmlNamespaceScope.All);
            foreach (var nspace in nspaces)
            {
                resolver.AddNamespace(nspace.Key, nspace.Value);
            }

            var xpaths = xmlSecureText.XPathSelectElements("/secureText/xpath");
            bool save = false;
            foreach (var xpath in xpaths)
            {
                string expression = xpath.Value;
                object obj = xml.XPathEvaluate(expression, resolver);
                IEnumerable list = (IEnumerable)obj;
                foreach (var item in list)
                {
                    if (item is XElement)
                    {
                        var element = (XElement)item;
                        bool isEncrypted = lib.IsEncrypted(element.Value);
                        if (decrypt && isEncrypted)
                        {
                            element.Value = lib.Decrypt(element.Value);
                            save = true;
                        }
                        else if (!decrypt && !isEncrypted)
                        {
                            element.Value = lib.Encrypt(element.Value);
                            save = true;
                        }
                    }
                    else if (item is XAttribute)
                    {
                        var attribute = (XAttribute)item;
                        bool isEncrypted = lib.IsEncrypted(attribute.Value);
                        if (decrypt && isEncrypted)
                        {
                            attribute.Value = lib.Decrypt(attribute.Value);
                            save = true;
                        }
                        else if (!decrypt && !isEncrypted)
                        {
                            attribute.Value = lib.Encrypt(attribute.Value);
                            save = true;
                        }
                    }
                }
            }

            if (save)
            {
                Console.WriteLine("Saving changes to {0}", file);
                using (var stream = new FileStream(file, FileMode.Create))
                {
                    xml.Save(stream);
                }
            }
        }
    }
}
