using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.ServiceBus;
using QGenda.Email;

namespace QGenda.Email
{
    public static class SendEmailHandler
    {
        [MessageHandler]
        public static void Handle(SendEmail message, MessageContext context)
        {
        }
    }
}
