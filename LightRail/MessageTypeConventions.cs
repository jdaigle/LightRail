using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LightRail.Reflection;

namespace LightRail
{
    public class MessageTypeConventions
    {
        private List<Func<Type, bool>> isMessageType = new List<Func<Type, bool>>();

        public MessageTypeConventions()
        {
            isMessageType.Add(x => typeof(IMessage).IsAssignableFrom(x)); // default convention
        }

        public MessageTypeConventions(IEnumerable<Func<Type, bool>> conventions)
            : this()
        {
            isMessageType.AddRange(conventions);
        }

        public void AddConvention(Func<Type, bool> convention)
        {
            this.isMessageType.Add(convention);
        }

        public void AddConventions(IEnumerable<Func<Type, bool>> conventions)
        {
            this.isMessageType.AddRange(conventions);
        }

        public bool IsMessageType(Type type)
        {
            return isMessageType.Any(x => x(type));
        }

        public IEnumerable<Type> ScanAssembliesForMessageTypes(IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .SelectMany(a => a.GetTypesSafely()
                                  .Where(t => IsMessageType(t))).ToList();
        }
    }
}
