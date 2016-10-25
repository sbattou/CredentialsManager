using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simple.CredentialManager;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Credential.LoadAll().Where(c => c.Target.Contains("Qlik_")).ToList().ForEach(c => c.Delete());
            Console.WriteLine("The deletion of the Qlik targets is complete");
            Console.ReadLine();
        }
    }
}
