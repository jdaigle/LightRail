using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail.Client.Dispatch
{
    [TestFixture]
    public class MessageHandlerCollectionTests
    {
        MessageHandlerCollection messageHandlerCollection;

        [SetUp]
        public void SetUp()
        {
            messageHandlerCollection = new MessageHandlerCollection();
            messageHandlerCollection.ScanAssembliesAndMapMessageHandlers(new[] { typeof(MessageHandlerCollectionTests).Assembly });
        }

        [Test]
        public void Can_Find_Instance_Handlers()
        {
            var handlers = messageHandlerCollection.GetDispatchersForMessageType(typeof(SampleMessage1));
            Assert.AreEqual(1, handlers.Count());
        }

        [Test]
        public void Can_Find_Static_Handlers()
        {
            var handlers = messageHandlerCollection.GetDispatchersForMessageType(typeof(SampleMessage2));
            Assert.AreEqual(1, handlers.Count());
        }

        [Test]
        public void Can_Find_Interfaces()
        {
            var handlers = messageHandlerCollection.GetDispatchersForMessageType(typeof(SampleInterface1));
            Assert.AreEqual(2, handlers.Count());
        }

        [Test]
        public void Can_Find_Assignable()
        {
            var handlers = messageHandlerCollection.GetDispatchersForMessageType(typeof(SampleInterface2));
            Assert.AreEqual(3, handlers.Count());

            handlers = messageHandlerCollection.GetDispatchersForMessageType(typeof(SampleMessage3));
            Assert.AreEqual(4, handlers.Count());
        }

        [Test]
        public void Can_Add_Delegeate()
        {

        }
    }

    public class SampleMessage1 { }
    public class SampleMessage2 { }
    public interface SampleInterface1 { }
    public interface SampleInterface2 : SampleInterface1 { }
    public class SampleMessage3 : SampleInterface2 { }
    public class SampleMessage4 { }

    public class MessageHandlers
    {
        [MessageHandler]
        public async Task Handle(SampleMessage1 message)
        {
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleMessage2 message)
        {
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleInterface1 message)
        {
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleMessage3 message)
        {
            await Task.Delay(0);
        }
    }

    public static class MessageHandlers2
    {
        [MessageHandler]
        public static async Task Handle(SampleInterface1 message)
        {
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleInterface2 message)
        {
            await Task.Delay(0);
        }
    }
}
