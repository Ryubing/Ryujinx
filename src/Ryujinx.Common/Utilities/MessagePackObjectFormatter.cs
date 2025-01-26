using MsgPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Common.Utilities
{
    public static class MessagePackObjectFormatter
    {
        public static string ToString(this MessagePackObject obj, bool pretty)
        {
            if (pretty)
            {
                return Format(obj);
            }

            return obj.ToString();
        }

        public static string Format(MessagePackObject obj)
        {
            IndentedStringBuilder builder = new();

            FormatMsgPackObj(obj, builder);

            return builder.ToString();
        }

        private static void FormatMsgPackObj(MessagePackObject obj, IndentedStringBuilder builder)
        {
            if (obj.IsMap || obj.IsDictionary)
            {
                FormatMsgPackMap(obj, builder);
            }
            else if (obj.IsArray || obj.IsList)
            {
                FormatMsgPackArray(obj, builder);
            }
            else if (obj.IsNil)
            {
                builder.Append("null");
            }
            else
            {
                object literal = obj.ToObject();

                if (literal is String)
                {
                    builder.AppendQuotedString(obj.AsStringUtf16());
                }
                else if (literal is byte[] byteArray)
                {
                    FormatByteArray(byteArray, builder);
                }
                else if (literal is MessagePackExtendedTypeObject extObject)
                {
                    builder.Append('{');

                    // Indent
                    builder.IncreaseIndent()
                           .AppendLine();

                    // Print TypeCode field
                    builder.AppendQuotedString("TypeCode")
                           .Append(": ")
                           .Append(extObject.TypeCode)
                           .AppendLine(",");

                    // Print Value field
                    builder.AppendQuotedString("Value")
                           .Append(": ");

                    FormatByteArrayAsString(extObject.GetBody(), builder, true);

                    // Unindent
                    builder.DecreaseIndent()
                           .AppendLine();

                    builder.Append('}');
                }
                else
                {
                    builder.Append(literal);
                }
            }
        }

        private static void FormatByteArray(byte[] arr, IndentedStringBuilder builder)
        {
            builder.Append("[ ");

            foreach (byte b in arr)
            {
                builder.Append("0x");
                builder.Append(ToHexChar(b >> 4));
                builder.Append(ToHexChar(b & 0xF));
                builder.Append(", ");
            }

            // Remove trailing comma
            builder.Remove(builder.Length - 2, 2);

            builder.Append(" ]");
        }

        private static void FormatByteArrayAsString(byte[] arr, IndentedStringBuilder builder, bool withPrefix)
        {
            builder.Append('"');

            if (withPrefix)
            {
                builder.Append("0x");
            }

            foreach (byte b in arr)
            {
                builder.Append(ToHexChar(b >> 4));
                builder.Append(ToHexChar(b & 0xF));
            }

            builder.Append('"');
        }

        private static void FormatMsgPackMap(MessagePackObject obj, IndentedStringBuilder builder)
        {
            MessagePackObjectDictionary map = obj.AsDictionary();

            builder.Append('{');

            // Indent
            builder.IncreaseIndent()
                   .AppendLine();

            foreach (KeyValuePair<MessagePackObject, MessagePackObject> item in map)
            {
                FormatMsgPackObj(item.Key, builder);

                builder.Append(": ");

                FormatMsgPackObj(item.Value, builder);

                builder.AppendLine(",");
            }

            // Remove the trailing new line and comma
            builder.TrimLastLine()
                   .Remove(builder.Length - 1, 1);

            // Unindent
            builder.DecreaseIndent()
                   .AppendLine();

            builder.Append('}');
        }

        private static void FormatMsgPackArray(MessagePackObject obj, IndentedStringBuilder builder)
        {
            IList<MessagePackObject> arr = obj.AsList();

            builder.Append("[ ");

            foreach (MessagePackObject item in arr)
            {
                FormatMsgPackObj(item, builder);

                builder.Append(", ");
            }

            // Remove trailing comma
            builder.Remove(builder.Length - 2, 2);

            builder.Append(" ]");
        }

        private static char ToHexChar(int b)
        {
            if (b < 10)
            {
                return unchecked((char)('0' + b));
            }

            return unchecked((char)('A' + (b - 10)));
        }

        internal class IndentedStringBuilder
        {
            const string DefaultIndent = "    ";

            private int _indentCount;
            private int _newLineIndex;
            private readonly StringBuilder _builder;

            public string IndentString { get; set; } = DefaultIndent;

            public IndentedStringBuilder(StringBuilder builder)
            {
                _builder = builder;
            }

            public IndentedStringBuilder()
                : this(new StringBuilder())
            { }

            public IndentedStringBuilder(string str)
                : this(new StringBuilder(str))
            { }

            public IndentedStringBuilder(int length)
                : this(new StringBuilder(length))
            { }

            public int Length { get => _builder.Length; }

            public IndentedStringBuilder IncreaseIndent()
            {
                _indentCount++;

                return this;
            }

            public IndentedStringBuilder DecreaseIndent()
            {
                _indentCount--;

                return this;
            }

            public IndentedStringBuilder Append(char value)
            {
                _builder.Append(value);

                return this;
            }

            public IndentedStringBuilder Append(string value)
            {
                _builder.Append(value);

                return this;
            }

            public IndentedStringBuilder Append(object value)
            {
                this.Append(value.ToString());

                return this;
            }

            public IndentedStringBuilder AppendQuotedString(string value)
            {
                _builder.Append('"');
                _builder.Append(value);
                _builder.Append('"');

                return this;
            }

            public IndentedStringBuilder AppendLine()
            {
                _newLineIndex = _builder.Length;

                _builder.AppendLine();

                for (int i = 0; i < _indentCount; i++)
                    _builder.Append(IndentString);

                return this;
            }

            public IndentedStringBuilder AppendLine(string value)
            {
                _builder.Append(value);

                this.AppendLine();

                return this;
            }

            public IndentedStringBuilder TrimLastLine()
            {
                _builder.Remove(_newLineIndex, _builder.Length - _newLineIndex);

                return this;
            }

            public IndentedStringBuilder Remove(int startIndex, int length)
            {
                _builder.Remove(startIndex, length);

                return this;
            }

            public override string ToString()
            {
                return _builder.ToString();
            }
        }
    }
}
