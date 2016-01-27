using System.Text;

namespace LightRail.Amqp
{
    internal static class ByteExtensions
    {
        public static string ToHex(this byte _byte)
        {
            char[] c = new char[4];
            c[0] = '0';
            c[1] = 'x';

            byte b;
            b = ((byte)(_byte >> 4));
            c[2] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

            b = ((byte)(_byte & 0x0F));
            c[3] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

            return new string(c);
        }

        public static string ToHex(this byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0 && i % 8 == 0)
                    sb.AppendLine();
                sb.Append(bytes[i].ToHex() + " ");
            }
            return sb.ToString();
        }
    }
}
