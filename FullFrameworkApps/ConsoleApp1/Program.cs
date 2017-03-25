using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("environment: {0}", ConfigurationManager.AppSettings["environment"]);
            Console.WriteLine("region: {0}", ConfigurationManager.AppSettings["region"]);
            Console.ReadLine();
        }
    }
}
