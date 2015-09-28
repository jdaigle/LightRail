using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public interface ISubscriptionStorage
    {
        void Subscribe(string destination, IEnumerable<string> messageTypes);
        void Unsubscribe(string destination, IEnumerable<string> messageTypes);
        List<string> GetSubscribersForMessage(IEnumerable<Type> messageTypes);
    }
}
