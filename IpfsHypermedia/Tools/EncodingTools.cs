using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ipfs.Hypermedia.Tools
{
    internal class EncodingTools
    {
        public static string EncodeString(string input, Encoding encoding)
        {
            var bytes = encoding.GetBytes(input);
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b);
                builder.Append(' ');
            }
            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }

        public static string DecodeString(string input, Encoding encoding)
        {
            var bytes = encoding.GetBytes(input);
            List<byte> buffer = new List<byte>();
            string tmp = input + ' ';
            while(tmp != String.Empty)
            {
                string s = new string(tmp.TakeWhile(x => x != ' ').ToArray());
                tmp = tmp.Remove(0, s.Length + 1);
                buffer.Add(byte.Parse(s));
            }
            return encoding.GetString(buffer.ToArray());
        }
    }
}
