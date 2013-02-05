using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeHandlingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Phase 1: Decoding C0 80.");
            byte[] local = new byte[2];
            //Encoding encoding = Encoding.GetEncoding("utf-8");
            Encoding encoding = new DexManipulator.MUTF8Encoding();
            local[0] = 0xC0;
            local[1] = 0x80;
            string cstring = "";
            try
            {
                cstring = encoding.GetString(local);
            }
            catch (DecoderFallbackException e)
            {
                Console.WriteLine("String is claimed to be invalid!");
                return;
            }
            Console.WriteLine("Character sequence length: " + cstring.Length);
            Console.WriteLine("Character is " + (cstring[0] == '\0' ? "" : "not ") + "null");
            Console.WriteLine("Phase 2: Decoding MUTF-8 Surrogate pair.");
            local = new byte[6];
            local[0] = 0xED;
            local[1] = 0xA0;
            local[2] = 0x81;
            local[3] = 0xED;
            local[4] = 0xB0;
            local[5] = 0x80;
            cstring = encoding.GetString(local);
            Console.WriteLine("Character sequence length: " + cstring.Length.ToString());
            Console.WriteLine("Sequence is printed as " + cstring);
            Console.WriteLine("Reference character is 𐐀");
        }
    }
}
