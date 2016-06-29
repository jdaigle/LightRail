using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QGenda.Email
{
    public interface IEmailSender
    {
    }

    public class DotNetEmailSender : IEmailSender { }
}
