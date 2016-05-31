using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.Pipeline;
using LightRail.ServiceBus.Reflection;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus
{
    public class PipelineMessageReceiver
    {
        public PipelineMessageReceiver(PipelineServiceBus bus, BaseMessageReceiverConfiguration config, BaseServiceBusConfig serviceBusConfig)
        {
            this.Bus = bus;
            this.Name = Guid.NewGuid().ToString();

            this.ServiceLocator = serviceBusConfig.ServiceLocator;
            this.MessageMapper = serviceBusConfig.MessageMapper;
            this.MessageHandlers = config.GetCombinedMessageHandlers();
            this.compiledMessageHandlerPipeline = config.GetCompiledMessageHandlerPipeline();

            // initialize all known message types
            this.MessageMapper.Initialize(this.MessageHandlers.Select(x => x.HandledMessageType));

            this.Transport = config.CreateTransportReceiver();
            this.Transport.MessageAvailable += (sender, args) => OnMessageAvailable(args);
            this.Transport.PoisonMessageDetected += (sender, args) => OnPoisonMessageDetected(args);
            this.startupActions.Add(() => this.Transport.Start());
        }

        public PipelineServiceBus Bus { get; }

        private static ILogger logger = LogManager.GetLogger("LightRail.ServiceBus");

        public string Name { get; }
        public ITransportReceiver Transport { get; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IServiceLocator ServiceLocator { get; }
        public IMessageMapper MessageMapper { get; }

        private readonly List<Action> startupActions = new List<Action>();
        private readonly Action<MessageContext> compiledMessageHandlerPipeline;

        public void Start()
        {
            logger.Info("Starting PipelineMessageReceiver[{0}]", Name);
            startupActions.ForEach(a => a());
        }

        public void Stop(TimeSpan timeSpan)
        {
            logger.Info("Stopping PipelineMessageReceiver[{0}]", Name);
            this.Transport.Stop(timeSpan);
        }

        private void OnMessageAvailable(MessageAvailableEventArgs value)
        {
            using (var childServiceLocator = this.ServiceLocator.CreateNestedContainer())
            {
                var currentMessageContext = new MessageContext(
                    bus: this.Bus,
                    messageID: value.TransportMessage.MessageId,
                    headers: value.TransportMessage.Headers,
                    currentMessage: value.TransportMessage.DecodedMessage,
                    serviceLocator: childServiceLocator);

                // register a bunch of things we might want to use during the message handling
                childServiceLocator.RegisterSingleton<IBus>(this.Bus);
                childServiceLocator.RegisterSingleton(this.MessageHandlers);
                childServiceLocator.RegisterSingleton<IMessageMapper>(this.MessageMapper);
                childServiceLocator.RegisterSingleton<MessageContext>(currentMessageContext);

                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var startTimestamp = DateTime.UtcNow;

                    compiledMessageHandlerPipeline(currentMessageContext);

                    stopwatch.Stop();
                    var endTimestamp = startTimestamp.AddTicks(stopwatch.ElapsedTicks);

                    OnMessageProcessed(new MessageProcessedEventArgs(currentMessageContext, startTimestamp, endTimestamp, stopwatch.Elapsed.TotalMilliseconds));
                }
                finally
                {
                    currentMessageContext = null;
                }
            }
        }

        private void OnMessageProcessed(MessageProcessedEventArgs args)
        {
            Bus.OnMessageProcessed(this, args);
        }

        private void OnPoisonMessageDetected(PoisonMessageDetectedEventArgs args)
        {
            Bus.OnPoisonMessageDetected(this, args);
        }
    }
}
