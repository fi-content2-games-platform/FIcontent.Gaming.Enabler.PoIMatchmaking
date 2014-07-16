using System.Collections;
using System.Collections.Generic;

namespace Assets.POIMatchmaking
{
    public class JsonArray : IEnumerable
    {
        private readonly List<object> _data = new List<object>();

        public int Count { get { return _data.Count; }}

        public void Set(int index, object value)
        {
            _data[index] = value;
        }

        public int Add(object value)
        {
            _data.Add(value);
            return _data.Count - 1;
        }

        public string GetString(int index)
        {
            return (string)_data[index];
        }

        public JsonObject GetObject(int index)
        {
            return (JsonObject)_data[index];
        }

        public JsonArray GetArray(int index)
        {
            return (JsonArray)_data[index];
        }

        public double GetNumber(int index)
        {
            return (double)_data[index];
        }

        public int GetInteger(int index)
        {
            return (int)((double)_data[index] + 0.5);
        }

        public override string ToString()
        {
            var s = "[ ";

            var first = true;
            foreach (var value in _data)
            {
                if (!first)
                {
                    s += ", ";
                }
                first = false;

                s += Json.StringifyObject(value);
            }

            s += " ]";

            return s;
        }

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public JsonArray()
        {
        }

        public JsonArray (IEnumerable<string> values)
            : this()
        {
            foreach (var value in values)
                Add(value);
        }
    }
}