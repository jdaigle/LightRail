using Autofac;
using LightRail.ServiceBus;
using LightRail.ServiceBus.Autofac;
using LightRail.ServiceBus.SqlServer;

namespace QGenda.Email
{
    public static class ServiceBusConfig
    {
        public static IBusControl CreateServiceBus()
        {
            return ServiceBus.Factory.CreateFromServiceBroker(cfg =>
            {
                cfg.ServiceLocator = BuildContainer();
                cfg.ServiceBrokerConnectionStringName = "ServiceBus";
                cfg.ServiceBrokerSendingService = "//QGenda/Email";
                cfg.ReceiveFrom(r =>
                {
                    r.ScanForMessageHandlersFromCurrentAssembly();

                    r.ServiceBrokerService = "[//QGenda/Email]";
                    r.ServiceBrokerQueue = "QGendaEmailService";

                    r.MaxConcurrency = 10;
                    r.MaxRetries = 5;
                });
            });
        }

        private static IServiceLocator BuildContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DotNetEmailSender>()
                   .As<IEmailSender>()
                   .InstancePerLifetimeScope();

            return new AutofacServiceLocator(builder.Build());
        }
    }
}
