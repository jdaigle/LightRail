using System;
using System.Threading;
using System.Threading.Tasks;
using LightRail.ServiceBus.SqlServer;
using NLog;
using QGenda.Email;
using Topshelf;

namespace QGenda.ServiceBus.Service
{
    public class ServiceHost : ServiceControl
    {
        public static int Main(string[] args)
        {
            return (int)HostFactory.Run(x =>
            {
                x.UseNLog();
                x.Service<ServiceHost>();
                x.SetServiceName("QGenda.ServiceBus.Service");
                x.SetDisplayName("QGenda.ServiceBus.Service");
                x.SetDescription("QGenda.ServiceBus.Service");
            });
        }

        public bool Start(HostControl hostControl)
        {
            QGenda.Email.ServiceBusConfig.CreateServiceBus().Start();
            StartDemoThread();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            return true;
        }

        private void StartDemoThread()
        {
            Task.Run(() =>
            {
                var logger = LogManager.GetLogger("DemoThread");
                try
                {
                    var bus = LightRail.ServiceBus.ServiceBus.Factory.CreateFromServiceBroker(cfg =>
                    {
                        cfg.ServiceBrokerConnectionStringName = "ServiceBus";
                        cfg.ServiceBrokerSendingService = "//QGenda/DemoThread";
                        cfg.AddMessageEndpointMappingFromAssemblyContaining<SendEmail>();
                        cfg.ReceiveFrom(r =>
                        {
                            r.ServiceBrokerService = "//QGenda/DemoThread";
                            r.ServiceBrokerQueue = "QGendaDemoThread";
                            r.MaxConcurrency = 0; // receive only
                        });
                    });
                    bus.Start();
                    while (true)
                    {
                        bus.Send(new SendEmail()
                        {
                        });
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    System.Diagnostics.Debugger.Break();
                }
            });
        }
    }
}
