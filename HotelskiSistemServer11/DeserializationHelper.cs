using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelskiSistemServer11
{
    public static class DeserializationHelper
    {
        public static List<byte[]> SplitByteArray(byte[] source, byte[] separator)
        {
            List<byte[]> result = new List<byte[]>();
            int start = 0;
            for (int i = 0; i < source.Length - separator.Length + 1; i++)
            {
                bool match = true;
                for (int j = 0; j < separator.Length; j++)
                {
                    if (source[i + j] != separator[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    int length = i - start;
                    byte[] chunk = new byte[length];
                    Buffer.BlockCopy(source, start, chunk, 0, length);
                    result.Add(chunk);
                    start = i + separator.Length;
                    i += separator.Length - 1;
                }
            }

            if (start < source.Length)
            {
                int length = source.Length - start;
                byte[] chunk = new byte[length];
                Buffer.BlockCopy(source, start, chunk, 0, length);
                result.Add(chunk);
            }

            return result;
        }
    }
}
