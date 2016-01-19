using System;
using System.IO;
using System.Reflection;
using LightRail.Client.Reflection;

namespace LightRail.Client.Config
{
    public class MessageEndpointMapping : IComparable<MessageEndpointMapping>
    {
        public MessageEndpointMapping() { }

        public MessageEndpointMapping(string endpoint, string assemblyName, string typeFullName)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint cannot be null or whitespace", nameof(endpoint));
            }
            this.Endpoint = endpoint;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("assemblyName cannot be null or whitespace", nameof(assemblyName));
            }
            this.AssemblyName = assemblyName;
            this.TypeFullName = typeFullName;
        }

        /// <summary>
        /// The endpoint named according the requirements of the transport.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Map all message types in this assembly. Unless a TypeFullName is specified.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// The fully qualified name of the message type. Define this if you want to map a single message type to the endpoint.
        /// </summary>
        public string TypeFullName { get; set; }

        public int CompareTo(MessageEndpointMapping other)
        {
            if (!String.IsNullOrWhiteSpace(TypeFullName))
            {
                if (!String.IsNullOrWhiteSpace(other.TypeFullName))
                {
                    return 0;
                }

                return -1;
            }

            if (!String.IsNullOrWhiteSpace(other.TypeFullName))
            {
                return 1;
            }

            if (!String.IsNullOrWhiteSpace(other.AssemblyName))
            {
                return 0;
            }

            return -1;
        }

        public void Configure(Action<Type, string> mapTypeToEndpoint)
        {
            if (string.IsNullOrWhiteSpace(AssemblyName))
            {
                throw new ArgumentException("Could not process message endpoint mapping. The Assembly property is not defined. Either the Assembly or Messages property is required.");
            }

            var assembly = GetMessageAssembly(AssemblyName);

            if (!string.IsNullOrWhiteSpace(TypeFullName))
            {
                try
                {
                    var t = assembly.GetType(TypeFullName, false);
                    if (t == null)
                    {
                        throw new ArgumentException($"Could not process message endpoint mapping. Cannot find the type '{TypeFullName}' in the assembly '{AssemblyName}'. Ensure that you are using the full name for the type.");
                    }
                    mapTypeToEndpoint(t, Endpoint);
                    return;
                }
                catch (BadImageFormatException ex)
                {
                    throw new ArgumentException($"Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type '{TypeFullName}' in the assembly '{AssemblyName}'", ex);
                }
                catch (FileLoadException ex)
                {
                    throw new ArgumentException($"Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type '{TypeFullName}' in the assembly '{AssemblyName}'", ex);
                }
            }

            foreach (var t in assembly.GetTypesSafely())
            {
                mapTypeToEndpoint(t, Endpoint);
            }
        }

        private static Assembly GetMessageAssembly(string assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Could not process message endpoint mapping. Problem loading message assembly: {assemblyName}", ex);
            }
        }
    }
}