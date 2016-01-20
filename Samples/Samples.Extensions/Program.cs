using System;

namespace Samples.Extensions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Comment or uncomment the samples as you wish
            new Logging.Sample().Execute();
            new StructureMap.Sample().Execute();
            new Unity.Sample().Execute();


            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Press Any Key To Quite");
            Console.ReadKey();
        }
    }
}
