using System.Linq;
using System.Reflection;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// AMQP composite types are represented as a described list.
    /// </summary>
    /// <remarks>
    /// AMQP composite types are represented as a described list. Each element in the list is positionally correlated with
    /// the fields listed in the composite type definition. The permitted element values are determined by the type speci-
    /// fication and multiplicity of the corresponding field definitions. When the trailing elements of the list representation
    /// are null, they MAY be omitted. The descriptor of the list indicates the specific composite type being represented.
    /// </remarks>
    public abstract class DescribedList : DescribedType
    {
        protected DescribedList(Descriptor descriptor)
            : base(descriptor)
        {
        }

        protected override void EncodeValue(ByteBuffer buffer)
        {
            DescribedListCodec.EncodeValue(buffer, this);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            DescribedListCodec.DecodeValue(buffer, this);
        }

#if DEBUG

        private static int indentLevel = 0;

        public override string ToString()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .OrderBy(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>().Index)
                .ToList();

            var buffer = new System.Text.StringBuilder();
            // buffer.Append("\n");
            // for (int i = 0; i < indentLevel; i++)
            //     buffer.Append("\t");
            buffer.Append(Descriptor.ToString());
            indentLevel++;
            foreach (var p in properties)
            {
                if (p.GetValue(this) == null)
                    continue;
                buffer.Append("\n");
                for (int i = 0; i < indentLevel; i++)
                    buffer.Append("\t");
                var value = p.GetValue(this);
                if (value is byte[])
                    value = $"byte[{(value as byte[]).Length}]" + System.Convert.ToBase64String(value as byte[]);
                if (value is Symbol[])
                    value = string.Join(",", (object[])value);
                buffer.AppendFormat("{0}: {1}", p.Name, value);
            }
            indentLevel--;
            return buffer.ToString(); ;
        }
#endif
    }
}
