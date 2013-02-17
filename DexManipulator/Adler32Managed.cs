using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DexManipulator
{
    public class Adler32Managed : System.Security.Cryptography.HashAlgorithm
    {
        const uint Base = 65521; // Largest prime smaller than 65536
        ulong a, b;

        // TODO: More efficient method described on zlib
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            int index;
            int len = ibStart + cbSize;

            for (index = ibStart; index < len; ++index)
            {
                a = (a + array[index]) % Base;
                b = (b + a) % Base;
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes((uint)((b << 16) | a));
        }

        public override void Initialize()
        {
            a = 1;
            b = 0;
        }
    }
}
