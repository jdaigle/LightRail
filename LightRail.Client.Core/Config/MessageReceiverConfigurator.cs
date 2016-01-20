﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using LightRail.Client.Dispatch;

namespace LightRail.Client.Config
{
    public class MessageReceiverConfigurator<TConfig>
        where TConfig : IMessageReceiverConfiguration, new()
    {
        public TConfig Config { get; }

        public MessageReceiverConfigurator()
        {
            Config = new TConfig();
        }

        public void ScanForHandlersFromAssembly(Assembly assembly)
        {
            Config.MessageHandlers.ScanAssemblyAndMapMessageHandlers(assembly);
        }

        public void Handle<TMessage>(Action<TMessage> messageHandler)
        {
            Config.MessageHandlers.AddMessageHandler(MessageHandlerMethodDispatcher.FromDelegate(messageHandler));
        }

        public void Handle<TMessage>(Action<TMessage, MessageContext> messageHandler)
        {
            Config.MessageHandlers.AddMessageHandler(MessageHandlerMethodDispatcher.FromDelegate(messageHandler));
        }
    }
}
