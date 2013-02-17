using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DexManipulator
{
    internal class TypeIdentifier
    {
        /*  V void
            Z boolean
            B byte
            S short
            C char
            I int
            J long
            F float
            D double
            Lfully/qualified/Name fully.qualified.Name
            [(type) one-dimensional array of (type)
            [[(type) two-dimensional array of (type)
            ...
         * */
        public TypeIdentifier(string dalvik_typeid)
        {
            int first = 0;
            array_dimension = 0;
            for (first = 0; dalvik_typeid[first] == '['; first++) array_dimension++;
            switch (dalvik_typeid[first])
            {
                case 'V':
                    if (array_dimension > 0)
                        throw new InvalidProgramException();
                    basic_type = BasicTypes.TYPE_VOID;
                    break;
                case 'Z':
                    basic_type = BasicTypes.TYPE_BOOLEAN;
                    break;
                case 'B':
                    basic_type = BasicTypes.TYPE_BYTE;
                    break;
                case 'S':
                    basic_type = BasicTypes.TYPE_SHORT;
                    break;
                case 'C':
                    basic_type = BasicTypes.TYPE_CHAR;
                    break;
                case 'I':
                    basic_type = BasicTypes.TYPE_INT;
                    break;
                case 'J':
                    basic_type = BasicTypes.TYPE_LONG;
                    break;
                case 'F':
                    basic_type = BasicTypes.TYPE_FLOAT;
                    break;
                case 'D':
                    basic_type = BasicTypes.TYPE_DOUBLE;
                    break;
                case 'L':
                    basic_type = BasicTypes.TYPE_CLASS;
                    classname = dalvik_typeid.Substring(first + 1);
                    break;
                default:
                    throw new InvalidProgramException();
            }
        }

        public enum BasicTypes
        {
            TYPE_VOID,
            TYPE_BOOLEAN,
            TYPE_BYTE,
            TYPE_SHORT,
            TYPE_CHAR,
            TYPE_INT,
            TYPE_LONG,
            TYPE_FLOAT,
            TYPE_DOUBLE,
            TYPE_CLASS
        }
        readonly BasicTypes basic_type;
        readonly uint array_dimension;
        readonly string classname;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            switch (basic_type)
            {
                case BasicTypes.TYPE_VOID:
                    if (array_dimension > 0)
                        throw new IndexOutOfRangeException();
                    return "void";
                case BasicTypes.TYPE_BOOLEAN:
                    sb.Append("bool");
                    break;
                case BasicTypes.TYPE_BYTE:
                    sb.Append("byte");
                    break;
                case BasicTypes.TYPE_SHORT:
                    sb.Append("short");
                    break;
                case BasicTypes.TYPE_CHAR:
                    sb.Append("char");
                    break;
                case BasicTypes.TYPE_INT:
                    sb.Append("int");
                    break;
                case BasicTypes.TYPE_LONG:
                    sb.Append("long");
                    break;
                case BasicTypes.TYPE_FLOAT:
                    sb.Append("float");
                    break;
                case BasicTypes.TYPE_DOUBLE:
                    sb.Append("double");
                    break;
                case BasicTypes.TYPE_CLASS:
                    sb.Append(DalvikProgram.ConvertFullyQualifiedName(classname));
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (array_dimension > 0)
            {
                sb.Append('[');
                for (uint i = 1; i < array_dimension; i++)
                    sb.Append(',');
                sb.Append(']');
            }
            return sb.ToString();
        }
    }
}
