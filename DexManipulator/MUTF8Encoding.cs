using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DexManipulator
{
    /// <summary>
    /// This class represents encoding used by Dalvik Virtual Machine, which encodes in-string U+0000 to (C0 80), and encodes supplementary planes by a surrogate pair encoded into UTF-8.
    /// </summary>
    public class MUTF8Encoding : Encoding
    {
        private const int LeadSurrogateMin = 0xD800;
        private const int LeadSurrogateMax = 0xDBFF;
        private const int TrailSurrogateMin = 0xDC00;
        private const int TrailSurrogateMax = 0xDFFF;

        public MUTF8Encoding() : base()
        {
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            //int retval = base.GetByteCount(chars, index, count);
            int retval = 0;
            int bound = chars.Length - 1;
            for (int i = 0; i < bound; i++)
            {
                if (chars[i] == '\0')
                    retval++;
                else if (char.IsSurrogate(chars[i]))
                    retval += 3;
                else
                    retval += base.GetByteCount(chars[i].ToString());
            }
            return retval;
        }

        // bytes from chars
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // Nullity check: Filters ArgumentNullException.
            if (chars == null || bytes == null)
                throw new ArgumentNullException();
            // Sanity check: filters ArgumentOutOfRangeException.
            if (charIndex < 0 || charCount < 0 || byteIndex < 0
                || charIndex + charCount > chars.Length || byteIndex >= bytes.Length)
                throw new ArgumentOutOfRangeException();
            // Capacity check: Filters ArgumentException.
            if (bytes.Length - byteIndex < GetByteCount(chars, charIndex, charCount))
                throw new ArgumentException();

            throw new NotImplementedException();
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            int bound = index + count;
            int i = index;
            int retval = 0;
            while (i < bound)
            {
                if (bytes[i] == 0)
                    break;
                if ((bytes[i] & 0x80) == 0)
                {
                    // ASCII Code: 1 byte to 1 character.
                    retval++;
                    i++;
                }
                else if ((bytes[i] & 0xE0) == 0xC0)
                {
                    // Two-byte code.
                    retval++;
                    i += 2;
                }
                else if ((bytes[i] & 0xF0) == 0xE0)
                {
                    // Three-byte code: Either a surrogate element or a character.
                    retval++;
                    i += 3;
                }
                else
                {
                    // MUTF-8 does NOT have a code with four or more bytes.
                    throw new DecoderFallbackException("A UTF-8 string using more than three bytes for a character is fed.", bytes, i);
                }
            }

            return retval;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // Nullity check: Filters ArgumentNullException.
            if (chars == null || bytes == null)
                throw new ArgumentNullException();
            // Sanity check: filters ArgumentOutOfRangeException.
            if (charIndex < 0 || byteCount < 0 || byteIndex < 0
                || byteIndex + byteCount > bytes.Length || charIndex >= chars.Length)
                throw new ArgumentOutOfRangeException();
            // Capacity check: Filters ArgumentException.
            if (chars.Length - charIndex < GetCharCount(bytes, byteIndex, byteCount))
                throw new ArgumentException();

            int i = byteIndex;
            int bound = byteIndex + byteCount;
            int j = charIndex;
            int k;
            int SurrogateStorage = 0;
            string SurrogateBuffer;
            while (i < bound)
            {
                if (SurrogateStorage != 0)
                {
                    try
                    {
                        if ((bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80)
                            throw new DecoderFallbackException("Lead surrogate exists, but trail surrogate does not follow it.", bytes, i);
                        k = (bytes[i] & 0x0F);
                        k <<= 6;
                        k |= (bytes[i + 1] & 0x3F);
                        k <<= 6;
                        k |= (bytes[i + 2] & 0x3F);
                        if (k >= TrailSurrogateMin && k <= TrailSurrogateMax)
                        {
                            k &= 0x3FF; // Remove "0xDC00" header from the lead.
                            SurrogateStorage |= k; // Glue trail with lead bits.
                            SurrogateStorage |= 0x10000; // Glue surrogate result with surrogate start.
                        }
                        else
                            throw new DecoderFallbackException("Lead surrogate exists, but the following character is not a trail surrogate.", bytes, i);

                        SurrogateBuffer = char.ConvertFromUtf32(SurrogateStorage);
                        chars[j - 1] = SurrogateBuffer[0];
                        chars[j] = SurrogateBuffer[1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new DecoderFallbackException("Trail surrogate expected, but unexpected end of data", bytes, i);
                    }
                    i += 3;
                    SurrogateStorage = 0;
                }
                else if ((bytes[i] & 0x80) == 0)
                {
                    // ASCII Code: 1 byte to 1 character.
                    if (bytes[i] == 0)
                        break;
                    chars[j] = char.ConvertFromUtf32(bytes[i])[0];
                    i++;
                }
                else if ((bytes[i] & 0xE0) == 0xC0)
                {
                    // Two-byte code.
                    try
                    {
                        if ((bytes[i + 1] & 0xC0) != 0x80)
                            throw new DecoderFallbackException("Trail bytes don't have the format expected in UTF-8.", bytes, i); // This is not even a UTF-8 string!
                        k = (bytes[i] & 0x1F);
                        k <<= 6;
                        k |= (bytes[i + 1] & 0x3F);
                        chars[j] = char.ConvertFromUtf32(k)[0];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new DecoderFallbackException("Trail bytes expected, but unexpected end of data", bytes, i);
                    }
                    i += 2;
                }
                else if ((bytes[i] & 0xF0) == 0xE0)
                {
                    // Three-byte code: Either a surrogate element or a character.
                    try
                    {
                        if ((bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80)
                            throw new DecoderFallbackException(); // This is not even a UTF-8 string!
                        k = (bytes[i] & 0x0F);
                        k <<= 6;
                        k |= (bytes[i + 1] & 0x3F);
                        k <<= 6;
                        k |= (bytes[i + 2] & 0x3F);
                        // Filter surrogate pairs here.
                        if (k >= LeadSurrogateMin && k <= LeadSurrogateMax)
                        {
                            // This is supposed to be a lead surrogate.
                            k &= 0x3FF; // Remove "0xD800" header from the lead.
                            k <<= 10; // Send lead bit to the appropriate position.
                            SurrogateStorage = k;
                        }
                        else if (k >= TrailSurrogateMin && k <= TrailSurrogateMax)
                        {
                            throw new DecoderFallbackException("Lead surrogate expected, but trail surrogate comes first", bytes, i);
                        }
                        else
                            chars[j] = char.ConvertFromUtf32(k)[0];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new DecoderFallbackException("Lead surrogate expected, but unexpected end of data", bytes, i);
                    }
                    i += 3;
                }
                else
                {
                    // MUTF-8 does NOT have a code with four or more bytes.
                    throw new DecoderFallbackException("A UTF-8 string using more than three bytes for a character is fed.", bytes, i);
                }
                j++;
            }
            return j - charIndex;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return 3 * charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        public override string EncodingName
        {
            get
            {
                return "Modified UTF-8";
            }
        }
    }
}
