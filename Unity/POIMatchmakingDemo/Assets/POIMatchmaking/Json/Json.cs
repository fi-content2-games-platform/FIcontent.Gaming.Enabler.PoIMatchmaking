using System;
using System.Text;

namespace Assets.SpatialMatchmaking
{
    public static class Json
    {
        public static string StringifyObject(object value)
        {
            if (value is JsonObject)
                return (value as JsonObject).ToString();

            if (value is JsonArray)
                return (value as JsonArray).ToString();

            if (value is string)
                return "\"" + value + "\"";

            if (value == null)
                return "null";

            if (value is int || value is float || value is double)
                return value.ToString();

            throw new ApplicationException("Invalid object type " + value.GetType());
        }

        public static void ConsumeWhitespace(string s, ref int position)
        {
            while (position < s.Length && Char.IsWhiteSpace(s[position]))
                ++position;
        }

        public static JsonObject ParseObject(string s, ref int position)
        {
            var obj = new JsonObject();
            obj.DoParse(s, ref position);
            return obj;
        }

        public static JsonArray ParseArray(string s, ref int position)
        {
            var array = new JsonArray();
            var pos = position;

            ConsumeWhitespace(s, ref pos);

            if (s[pos] != '[')
                throw new ParseException("Expected '['", s, pos);
            ++pos;

            ConsumeWhitespace(s, ref pos);

            while (s[pos] != ']')
            {
                ConsumeWhitespace(s, ref pos);

                array.Add(Parse(s, ref pos));

                ConsumeWhitespace(s, ref pos);

                if (s[pos] != ',' && s[pos] != ']')
                    throw new ParseException("Expected ',' or ']'", s, pos);

                if (s[pos] == ',')
                {
                    ++pos;
                    ConsumeWhitespace(s, ref pos);
                }
            }
            ++pos;

            ConsumeWhitespace(s, ref pos);

            position = pos;
            return array;
        }

        public static object Parse(string s, ref int position)
        {
            if (s[position] == '"')
                return ParseString(s, ref position);
            
            if (s[position] == '{')
                return ParseObject(s, ref position);
            
            if (s[position] == '[')
                return ParseArray(s, ref position);

            if (position + 4 <= s.Length && s.Substring(position, 4) == "null")
            {
                position += 4;
                return null;
            }

            var endpos = position;
            while (endpos < s.Length && s[endpos] != ',' && s[endpos] != ']' && s[endpos] != '}')
                ++endpos;

            var substring = s.Substring(position, endpos - position);
            double d;
            if (double.TryParse(substring, out d))
            {
                position = endpos;
                return d;
            }

            throw new ParseException("Expected '\"', '{', '[', null, or number", s, position);
        }

        public static string ParseString(string s, ref int position)
        {
            var result = "";
            var pos = position;

            if (s[pos] != '"')
                throw new ParseException("Expected '\"'", s, pos);
            ++pos;

            while (s[pos] != '"')
            {
                if (s[pos] == '\\')
                    ++pos;
                result += s[pos];
                ++pos;
            }

            ++pos;

            position = pos;
            return result;
        }

        public class ParseException : Exception
        {
            public ParseException(string error, string data, int position)
                : base(String.Format("Json parse error: {0} at position {1} in \"{2}\"", error, position, data))
            {
            }
        }

        public static string BytesToString(byte[] data)
        {
            var decoder = Encoding.UTF8.GetDecoder();
            var chars = new char[decoder.GetCharCount(data, 0, data.Length, true)];
            var count = decoder.GetChars(data, 0, data.Length, chars, 0, true);
            return new string(chars, 0, count);
        }

        public static byte[] StringToBytes(string data)
        {
            var encoder = Encoding.UTF8.GetEncoder();
            var chars = data.ToCharArray();
            var bytes = new byte[encoder.GetByteCount(chars, 0, chars.Length, true)];
            encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            return bytes;
        }
    }
}
