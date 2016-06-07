using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainNameResolver.Core
{
    public static class ByteArrayExtensions
    {
        public static string ReadString(this byte[] data, ref int position)
        {
            byte length = data[position++];
            var ret = Encoding.ASCII.GetString(data, position, length);
            position += length;
            return ret;
        }
    }
}
