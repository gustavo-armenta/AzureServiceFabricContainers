using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace SecureText
{
    internal class TransformJson
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

            JObject jsonSecureText = null;
            try
            {
                jsonSecureText = JObject.Parse(File.ReadAllText(secureTextFile));
            }
            catch (Exception ex)
            {
                return;
            }

            EncryptLib lib = new EncryptLib();
            JObject json = JObject.Parse(File.ReadAllText(file));
            var jsonpaths = jsonSecureText["secureText"];
            bool save = false;
            foreach (var jsonpath in jsonpaths)
            {
                string expression = jsonpath.Value<string>();
                JToken token = json.SelectToken(expression);
                JProperty property = (JProperty)token.Parent;
                bool isEncrypted = lib.IsEncrypted(property.Value.Value<string>());
                if (decrypt && isEncrypted)
                {
                    property.Replace(new JProperty(property.Name, lib.Decrypt(property.Value.Value<string>())));
                    save = true;
                }
                else if (!decrypt && !isEncrypted)
                {
                    property.Replace(new JProperty(property.Name, lib.Encrypt(property.Value.Value<string>())));
                    save = true;
                }
            }

            if (save)
            {
                Console.WriteLine("Saving changes to {0}", file);
                File.WriteAllText(file, json.ToString());
            }
        }
    }
}