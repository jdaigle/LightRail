using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.ServiceBus;
using QGenda.Email;

namespace QGenda.Email
{
    public class SendEmailHandler : IMessageHandler<SendEmail>
    {
        public SendEmailHandler(IEmailSender emailSender, MessageContext context)
        {
            _emailSender = emailSender;
            _context = context;
        }

        private readonly IEmailSender _emailSender;
        private readonly MessageContext _context;

        public void Handle(SendEmail message)
        {
        }
    }
}
