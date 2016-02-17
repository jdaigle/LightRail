using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// The descriptor of a described type.
    /// </summary>
    public sealed class Descriptor
    {
        public string Name { get; }
        public ulong Code { get; }

        public Descriptor(object desc)
        {
            Code = (desc is ulong) ? (ulong)desc : 0;
            Name = (desc is string) || (desc is Symbol) ? (string)desc : null;
        }

        public Descriptor(ulong code)
        {
            Code = code;
            Name = null;
        }

        public Descriptor(string name)
        {
            Code = 0;
            Name = name;
        }

        public Descriptor(ulong code, string name)
        {
            Code = code;
            Name = name;
        }

        public override string ToString()
        {
            return Name ?? Code.ToString();
        }

        public override int GetHashCode()
        {
            return (Name ?? Code.ToString()).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Descriptor)
            {
                return Equals(this, obj as Descriptor);
            }
            return false;
        }

        public static bool Equals(Descriptor first, Descriptor second)
        {
            if (first == null && second == null)
            {
                return true;
            }
            if (first == null && second != null)
            {
                return false;
            }
            if (first != null && second == null)
            {
                return false;
            }
            if (first.Code == second.Code)
            {
                return true;
            }
            if (first.Name == second.Name)
            {
                return true;
            }
            return false;
        }
    }
}
