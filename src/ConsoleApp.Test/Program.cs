using ConsoleAppBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cb = new ConsoleBase("ConsoleApp.Test.exe");
            cb.Run(args, "ConsoleApp.Test");
        }
    }
}
