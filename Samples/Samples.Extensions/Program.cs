using System;
using System.Threading;

namespace Samples.Extensions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Logging.Sample().Execute();

            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Press Any Key To Quite");
            Console.ForegroundColor = prev;
            Console.ReadKey();
        }
    }
}
