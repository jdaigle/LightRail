using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class CannotDeserializeMessageException : Exception
    {
        public CannotDeserializeMessageException(string message)
            : base(message)
        {
        }

        public CannotDeserializeMessageException(Exception innerException)
            : base("Cannot Deserialize Message", innerException)
        {
        }
    }
}
