using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace LightRail
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public string ContentType { get { return "application/json"; } }

        private Dictionary<string, Type> knownTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        public void RegisterKnownType<T>()
        {
            RegisterKnownType(typeof(T));
        }

        public void RegisterKnownType(Type type)
        {
            knownTypes[GetTypeKeyPrivate(type)] = type;
        }

        public string GetTypeKey<T>()
        {
            return GetTypeKey(typeof(T));
        }

        public string GetTypeKey(Type type)
        {
            if (!knownTypes.ContainsKey(GetTypeKeyPrivate(type)))
            {
                RegisterKnownType(type);
            }
            return GetTypeKeyPrivate(type);
        }

        private static string GetTypeKeyPrivate(Type type)
        {
            return type.FullName;
        }

        public string Serialize(object data)
        {
            return Jil.JSON.Serialize(data);
        }

        public void Serialize(object data, TextWriter output)
        {
            JSON.Serialize(data, output);
        }

        public object Deserialize(string text, string type)
        {
            Type knownType = FindKnownType(type);
            return JSON.Deserialize(text, knownType);
        }

        public object Deserialize(TextReader reader, string type)
        {
            Type knownType = FindKnownType(type);
            return JSON.Deserialize(reader, knownType);
        }

        private Type FindKnownType(string type)
        {
            Type knownType;
            if (knownTypes.ContainsKey(type))
            {
                knownType = knownTypes[type];
            }
            else
            {
                knownType = Type.GetType(type);
            }
            if (knownType == null)
            {
                throw new InvalidOperationException(string.Format("Unknown Type To Deserialize [{0}]", type));
            }
            return knownType;
        }
    }
}
