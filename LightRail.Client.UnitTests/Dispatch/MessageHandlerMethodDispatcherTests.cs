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
            Assert.Throws<Exception>(() => executor.Execute(), "Test");
        }

        [Test]
        public void Can_Call_Static_No_Params()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleNoParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute();
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Static_Single_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleSingleParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute("p1");
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Static_Many_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("StaticHandleManyParam"), null);
            Assert.False(executor.IsInstanceMethod);
            executor.Execute("p1", "p2", "p3");
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_No_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleNoParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler());
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_Single_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleSingleParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler(), "p1");
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Instance_Many_Param()
        {
            var executor = new MessageHandlerMethodDispatcher(typeof(MessageHandler).GetMethod("InstanceHandleManyParam"), null);
            Assert.True(executor.IsInstanceMethod);
            executor.Execute(new MessageHandler(), "p1", "p2", "p3");
            Assert.True(TestPassed, "Did Not Pass");
        }

        [Test]
        public void Can_Call_Lambda()
        {
            var executor = MessageHandlerMethodDispatcher.FromDelegate<object>((message, messageContext) =>
            {
                Assert.NotNull(message);
                Assert.NotNull(messageContext);
                MessageHandlerMethodDispatcherTests.TestPassed = true;
            });
            executor.Execute("Message", Helpers.GetUninitializedObject<MessageContext>());
            Assert.True(TestPassed, "Did Not Pass");
        }
        [Test]
        public void Can_Call_Lambda_Alt()
        {
            Action<object, MessageContext> f = (message, messageContext) =>
            {
                Assert.NotNull(message);
                Assert.NotNull(messageContext);
                MessageHandlerMethodDispatcherTests.TestPassed = true;
            };
            var executor = new MessageHandlerMethodDispatcher(f, null);
            executor.Execute("Message", Helpers.GetUninitializedObject<MessageContext>());
            Assert.True(TestPassed, "Did Not Pass");
        }
    }

    public class MessageHandler
    {
        public static void TestTest()
        {
            throw new Exception("Test");
        }

        public static void StaticHandleNoParam()
        {
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }

        public static void StaticHandleSingleParam(string p1)
        {
            Assert.IsNotNullOrEmpty(p1);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }

        public static void StaticHandleManyParam(string p1, string p2, string p3)
        {
            Assert.IsNotNullOrEmpty(p1);
            Assert.IsNotNullOrEmpty(p2);
            Assert.IsNotNullOrEmpty(p3);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }

        public void InstanceHandleNoParam()
        {
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }

        public void InstanceHandleSingleParam(string p1)
        {
            Assert.IsNotNullOrEmpty(p1);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }

        public void InstanceHandleManyParam(string p1, string p2, string p3)
        {
            Assert.IsNotNullOrEmpty(p1);
            Assert.IsNotNullOrEmpty(p2);
            Assert.IsNotNullOrEmpty(p3);
            MessageHandlerMethodDispatcherTests.TestPassed = true;
        }
    }
}
