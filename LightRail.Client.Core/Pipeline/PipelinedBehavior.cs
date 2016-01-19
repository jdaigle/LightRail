using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Client.Logging;

namespace LightRail.Client.Pipeline
{
    public abstract class PipelinedBehavior
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Client.PipelinedBehavior");

        protected abstract Task Invoke(MessageContext context, Func<Task> next);

        public async Task Invoke(MessageContext context, Func<MessageContext, Task> next)
        {
            int nextInvoked = 0;
#if DEBUG
            logger.Debug("Pre-Invoke Behavior [{0}]", GetType().FullName);
#endif
            await Invoke(context, async () =>
            {
                nextInvoked++;
                await next(context);
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

        private static readonly Func<MessageContext, Task> EmptyAction = new Func<MessageContext, Task>(m => Task.Delay(0));

        public static Func<MessageContext, Task> CompileMessageHandlerPipeline(IEnumerable<PipelinedBehavior> behaviors)
        {
            if (!behaviors.Any())
            {
                return EmptyAction;
            }
            var behavior = behaviors.First();
            var compiledInnerBehaviors = CompileMessageHandlerPipeline(behaviors.Skip(1));
            return new Func<MessageContext, Task>(m => behavior.Invoke(m, compiledInnerBehaviors));
        }
    }
}
