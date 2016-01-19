using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail.Client.Dispatch
{
    [TestFixture]
    public class MessageHandlerMethodDispatcherTests
    {
        public static bool TestPassed = false;

        [SetUp]
        public void SetUp()
        {
            TestPassed = false;
        }

        [Test]
        public void TestsShouldWork()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("TestTest"), null);
            Assert.Throws<AggregateException>(() => executor.Execute().Wait());
        }

        [Test]
        public void Can_Call_Static_No_Params()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleNoParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute().Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Static_Single_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleSingleParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute("p1").Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Static_Many_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleManyParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute("p1", "p2", "p3").Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_No_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleNoParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler()).Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_Single_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleSingleParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler(), "p1").Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_Many_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleManyParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler(), "p1", "p2", "p3").Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Lambda()
        {
            var executor = MessageHandlerMethodDispatcher.FromDelegate<object>(async (message, messageContext) =>
            {
                Assert.NotNull(message);
                Assert.NotNull(messageContext);
                MessageHandlerMethodDispatcherTests.TestPassed = true;
                await Task.Delay(0);
            });
            executor.Execute("Message", new MessageContext()).Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }
        [Test]
        public void Can_Call_Lambda_Alt()
        {
            Func<object, MessageContext, Task> f = async (message, messageContext) =>
            {
                Assert.NotNull(message);
                Assert.NotNull(messageContext);
                MessageHandlerMethodDispatcherTests.TestPassed = true;
                await Task.Delay(0);
            };
            var executor = new MessageHandlerMethodDispatcher(f, null);
            executor.Execute("Message", new MessageContext()).Wait();
            Assert.True(TestPassed, "Did Not Pass");
        }
    }

    public class MessageHandler
    {
        public static async Task TestTest()
        {
            await Task.Delay(0);
            throw new Exception("Test");
        }

        public static async Task StaticHandleNoParam()
        {
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }

        public static async Task StaticHandleSingleParam(string p1)
        {
            Assert.IsNotNullOrEmpty(p1);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }

        public static async Task StaticHandleManyParam(string p1, string p2, string p3)
        {
            Assert.IsNotNullOrEmpty(p1);
            Assert.IsNotNullOrEmpty(p2);
            Assert.IsNotNullOrEmpty(p3);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }

        public async Task InstanceHandleNoParam()
        {
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }

        public async Task InstanceHandleSingleParam(string p1)
        {
            Assert.IsNotNullOrEmpty(p1);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }

        public async Task InstanceHandleManyParam(string p1, string p2, string p3)
        {
            Assert.IsNotNullOrEmpty(p1);
            Assert.IsNotNullOrEmpty(p2);
            Assert.IsNotNullOrEmpty(p3);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
            await Task.Delay(0);
        }
    }
}
