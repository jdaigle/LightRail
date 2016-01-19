﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Client.Config;
using LightRail.Client.Dispatch;
using LightRail.Client.Logging;
using LightRail.Client.Pipeline;
using LightRail.Client.Reflection;
using LightRail.Client.Transport;

namespace LightRail.Client
{
    public class PipelineMessageReceiver
    {
        public PipelineMessageReceiver(IMessageReceiverConfiguration config, IServiceBusConfig serviceBusConfig)
        {
            this.Name = Guid.NewGuid().ToString();

            this.ServiceLocator = serviceBusConfig.ServiceLocator;
            this.MessageMapper = new ReflectionMessageMapper();
            this.MessageHandlers = config.GetCombinedMessageHandlers();
            this.compiledMessageHandlerPipeline = config.GetCompiledMessageHandlerPipeline();

            this.Transport.MessageAvailable += (sender, args) => OnMessageAvailable(args);
            this.Transport.PoisonMessageDetected += (sender, args) => OnPoisonMessageDetected(args);
            this.startupActions.Add(() => this.Transport.Start());
        }

        public PipelineServiceBus Bus { get; }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client");

        public string Name { get; }
        public ITransportReceiver Transport { get; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IServiceLocator ServiceLocator { get; }
        public IMessageMapper MessageMapper { get; }

        private readonly List<Action> startupActions = new List<Action>();
        private readonly Func<MessageContext, Task> compiledMessageHandlerPipeline;

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
            throw new NotImplementedException();
            //using (var childServiceLocator = this.ServiceLocator.CreateNestedContainer())
            //{
            //    var currentMessageContext = new MessageContext(this, value.TransportMessage.MessageId, value.TransportMessage.Headers, childServiceLocator);

            //    // register a bunch of things we might want to use during the message handling
            //    childServiceLocator.RegisterSingleton<IBus>(this);
            //    childServiceLocator.RegisterSingleton(this.MessageHandlers);
            //    childServiceLocator.RegisterSingleton<IMessageMapper>(this.MessageMapper);
            //    childServiceLocator.RegisterSingleton<ITransport>(this.Transport);
            //    childServiceLocator.RegisterSingleton<MessageContext>(currentMessageContext);

            //    try
            //    {
            //        object message = null;
            //        try
            //        {
            //            message = DeserializeMessage(value);
            //        }
            //        catch (Exception e)
            //        {
            //            logger.Error(e, "Cannot deserialize message.");
            //            // The message cannot be deserialized. There is no reason
            //            // to retry.
            //            throw new CannotDeserializeMessageException(e);
            //        }
            //        currentMessageContext.CurrentMessage = message;
            //        currentMessageContext.SerializedMessageData = value.TransportMessage.SerializedMessageData;

            //        var stopwatch = Stopwatch.StartNew();
            //        var startTimestamp = DateTime.UtcNow;

            //        compiledMessageHandlerPipeline(currentMessageContext);

            //        var endTimestamp = DateTime.UtcNow;
            //        stopwatch.Stop();

            //        OnMessageProcessed(new MessageProcessedEventArgs(currentMessageContext, startTimestamp, endTimestamp, stopwatch.Elapsed.TotalMilliseconds));
            //    }
            //    finally
            //    {
            //        currentMessageContext = null;
            //    }
            //}
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
