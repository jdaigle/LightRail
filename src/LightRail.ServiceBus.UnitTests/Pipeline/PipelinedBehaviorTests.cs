using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail.ServiceBus.Pipeline
{
    [TestFixture]
    public class PipelinedBehaviorTests
    {
        [Test]
        public void Simple_Set_Of_Behaviors()
        {
            var behaviors = new PipelinedBehavior[] { new Behavior1(), new Behavior2() };
            var pipeline = PipelinedBehavior.CompileMessageHandlerPipeline(behaviors);
            pipeline.Invoke(null);
            Assert.AreEqual(2, Assert.Counter);
        }

        [Test]
        public void MustCallNext()
        {
            var behaviors = new PipelinedBehavior[] { new Behavior1(), new BadBehavior(), new Behavior2() };
            var pipeline = PipelinedBehavior.CompileMessageHandlerPipeline(behaviors);
            pipeline.Invoke(null);
            Assert.AreEqual(2, Assert.Counter);
        }
    }

    public class Behavior1 : PipelinedBehavior
    {
        protected override void Invoke(MessageContext context, Action next)
        {
            Assert.True(true);
            next();
        }
    }

    public class Behavior2 : PipelinedBehavior
    {
        protected override void Invoke(MessageContext context, Action next)
        {
            Assert.True(true);
            next();
        }
    }

    public class BadBehavior : PipelinedBehavior
    {
        protected override void Invoke(MessageContext context, Action next)
        {
            Assert.True(true);
        }
    }
}
