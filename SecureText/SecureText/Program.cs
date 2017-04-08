using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SecureText
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        internal static void Startup()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            Configuration = builder.Build();
        }

        static void Main(string[] args)
        {
            Startup();
            string action = args[0];
            string directory = args[1];

            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            bool decrypt = string.Equals("Decrypt", action, StringComparison.OrdinalIgnoreCase);
            string[] files = Directory.GetFiles(directory, "*.securetext", SearchOption.AllDirectories);
            TransformJson json = new TransformJson();
            TransformXml xml = new TransformXml();
            foreach (string file in files)
            {
                json.Execute(file, decrypt);
                xml.Execute(file, decrypt);
            }
        }
    }
}