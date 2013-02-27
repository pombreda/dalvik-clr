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
        enum Endianness
        {
            ENDIAN_NOT_SET,
            ENDIAN_LITTLE,
            ENDIAN_BIG
        }

        public readonly string[] StringList;
        readonly Endianness endianness;
        MUTF8Encoding stringdec;

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

        uint ReadULEB128(FileStream source)
        {
            int buffer;
            int offset = 0;
            uint result = 0;
            while (true)
            {
                buffer = source.ReadByte();
                if (buffer < 0)
                    throw new InvalidDataException("ULEB128 sequence expected, but end of file is reached.");
                result |= ((uint)(buffer & 0x7F)) << offset;
                offset += 7;
                if ((buffer & 0x80) == 0)
                    break;
            }
            return result;
        }

        private async Task<string> AsyncReadStringEntry(FileStream source, uint offset)
        {
            source.Seek(offset, SeekOrigin.Begin);
            uint strlen = ReadULEB128(source);
            byte[] buffer = new byte[stringdec.GetMaxByteCount((int)strlen)];
            int result = await source.ReadAsync(buffer, 0, buffer.Length);
            int bytelen;
            for (bytelen = 0; bytelen < result; bytelen++)
                if (buffer[bytelen] == 0)
                    break;
            return stringdec.GetString(buffer);
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
            uint filesize = ReadUInt(bytecode_source);
            if (filesize != bytecode_source.Length)
                throw new InvalidProgramException("File length does not match the actual file size.");

            // Ignore link section and map section.
            bytecode_source.Seek(20, SeekOrigin.Current);

            // Begin reading string section.
            StringList = new string[ReadUInt(bytecode_source)];
            uint string_id_offset = ReadUInt(bytecode_source);
            bytecode_source.Seek(string_id_offset, SeekOrigin.Begin);
            stringdec = new MUTF8Encoding();
            using (int length = StringList.Length)
            {

            }
            
            // Begin reading Type ID section.
            // Notice that the file stream should be seeked into the former place.
            bytecode_source.Seek(64, SeekOrigin.Begin);
        }

        public static string ConvertFullyQualifiedName(string classname)
        {
            return "JVM." + classname.Replace("/", ".");
        }
    }
}
