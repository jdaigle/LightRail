using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.ServiceBus.Logging;

namespace LightRail.ServiceBus.Pipeline
{
    public abstract class PipelinedBehavior
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.PipelinedBehavior");

        protected abstract void Invoke(MessageContext context, Action next);

        public void Invoke(MessageContext context, Action<MessageContext> next)
        {
            int nextInvoked = 0;
#if DEBUG
            logger.Debug("Pre-Invoke Behavior [{0}]", GetType().FullName);
#endif
            Invoke(context, () =>
            {
                nextInvoked++;
                next(context);
            });
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached) // there is a weird issue here where Debug.Assert on a Task thread seems to sometimes lockup the program
            {
                System.Diagnostics.Debug.Assert(nextInvoked >= 1, "Inner pipelined behavior did not call next().", "All PipelinedBehavior must call next() exactly once.");
                System.Diagnostics.Debug.Assert(nextInvoked <= 1, "Inner pipelined behavior called next() too many times.", "All PipelinedBehavior must call next() exactly once.");
            }
#endif
#if DEBUG
            logger.Debug("Post-Invoke Behavior [{0}]", GetType().FullName);
#endif
        }

        private static readonly Action<MessageContext> EmptyAction = m => { };

        public static Action<MessageContext> CompileMessageHandlerPipeline(IEnumerable<PipelinedBehavior> behaviors)
        {
            if (!behaviors.Any())
            {
                return EmptyAction;
            }
            var behavior = behaviors.First();
            var compiledInnerBehaviors = CompileMessageHandlerPipeline(behaviors.Skip(1));
            return m => behavior.Invoke(m, compiledInnerBehaviors);
        }
    }
}
