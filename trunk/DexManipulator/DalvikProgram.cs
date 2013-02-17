using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DexManipulator
{
    public class DalvikProgram
    {
        public readonly string[] StringList;
        enum Endianness
        {
            ENDIAN_NOT_SET,
            ENDIAN_LITTLE,
            ENDIAN_BIG
        }
        readonly Endianness endianness;

        #region array constants
        public static byte[] magic
        {
            get
            {
                return new byte[] { 0x64, 0x65, 0x78, 0x0a, 0x30, 0x33, 0x35, 0x00 };
            }
        }
        public static byte[] little_endian_constant
        {
            get
            {
                return new byte[] { 0x78, 0x56, 0x34, 0x12 };
            }
        }
        public static byte[] big_endian_constant
        {
            get
            {
                return new byte[] { 0x12, 0x34, 0x56, 0x78 };
            }
        }
        #endregion

        uint ReadUInt(FileStream source)
        {
            byte[] buffer = new byte[4];
            source.Read(buffer, 0, 4);
            if (endianness == Endianness.ENDIAN_BIG && BitConverter.IsLittleEndian
                || endianness == Endianness.ENDIAN_LITTLE && !BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public DalvikProgram(string filename)
        {
            // Initialize
            endianness = Endianness.ENDIAN_NOT_SET;

            // Open file
            FileStream bytecode_source = File.OpenRead(filename);

            // Compare magic
            byte[] buffer = new byte[8];
            bytecode_source.Read(buffer, 0, 8);
            if (!magic.SequenceEqual(buffer))
                throw new InvalidProgramException();
            bytecode_source.Seek(0x20, SeekOrigin.Current);
            buffer = new byte[4];
            bytecode_source.Read(buffer, 0, 4);

            // Determine endianness
            if (little_endian_constant.SequenceEqual(buffer))
                endianness = Endianness.ENDIAN_LITTLE;
            else if (big_endian_constant.SequenceEqual(buffer))
                endianness = Endianness.ENDIAN_BIG;
            else
                throw new NotImplementedException("This endianness is not yet implemented.");

            // Verify Adler32 checksum
            bytecode_source.Seek(12, SeekOrigin.Begin);
            Adler32Managed hash1 = new Adler32Managed();
            hash1.Initialize();
            uint hashcode = BitConverter.ToUInt32(hash1.ComputeHash(bytecode_source), 0);
            hash1.Dispose();
            bytecode_source.Seek(8, SeekOrigin.Begin);
            uint hashcode_desired = ReadUInt(bytecode_source);
            if (hashcode_desired != hashcode)
                throw new InvalidProgramException("Illegal Adler32 checksum");
            
            // Verify SHA-1 signature
            bytecode_source.Seek(20, SeekOrigin.Current);
            SHA1 hash2 = SHA1.Create();
            hash2.Initialize();
            byte[] sha1_hash = hash2.ComputeHash(bytecode_source);
            hash2.Dispose();
            bytecode_source.Seek(12, SeekOrigin.Begin);
            byte[] sha1_desired = new byte[20];
            bytecode_source.Read(sha1_desired, 0, 20);
            if (!sha1_desired.SequenceEqual(sha1_hash))
                throw new InvalidProgramException("Illegal SHA-1 Hash");

            // Verify file size
        }

        public static string ConvertFullyQualifiedName(string classname)
        {
            return "JVM." + classname.Replace("/", ".");
        }
    }
}
