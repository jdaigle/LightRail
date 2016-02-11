using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail.Amqp
{
    [TestFixture]
    public class RFCSeqNumTests
    {
        [Test]
        public void Test_Wrapping_And_Comparison()
        {
            Console.WriteLine($"{(unchecked(int.MinValue - 1)).ToString()} = {(unchecked((uint)(int.MinValue - 1))).ToString()}");
            Console.WriteLine($"{int.MinValue.ToString()} = {(unchecked((uint)(int.MinValue))).ToString()}");
            Console.WriteLine($"{(int.MinValue + 1).ToString()} = {(unchecked((uint)(int.MinValue + 1))).ToString()}");

            RFCSeqNum last = 1;
            RFCSeqNum next = 2;

            Assert.True(next > last);
            for (int i = 0; i < 1000; i++)
            {
                last = next;
                next++;
                Assert.True(next > last);
            }


            last = uint.MaxValue - 500;
            next = last + 1;
            for (int i = 0; i < 1000; i++)
            {
                last = next;
                next++;
                if (next == 0)
                    Console.WriteLine($"we're at 0! {(int)(uint)next} > {(int)(uint)last}");
                if (next == uint.MaxValue)
                    Console.WriteLine($"we're at uint.MaxValue! {(int)(uint)next} > {(int)(uint)last}");
                if (next == 1)
                    Console.WriteLine($"we're at 1! {(int)(uint)next} > {(int)(uint)last}");
                Assert.True(next > last);
            }

            last = int.MaxValue - 500;
            next = last + 1;
            for (int i = 0; i < 1000; i++)
            {
                last = next;
                next++;
                if (next == int.MaxValue)
                    Console.WriteLine($"we're at int.MaxValue! {(int)(uint)next} > {(int)(uint)last}");
                if (next == (uint)int.MaxValue+1)
                    Console.WriteLine($"we're at int.MaxValue+1! {(int)(uint)next} > {(int)(uint)last}");
                Assert.True(next > last);
            }
        }
    }
}
