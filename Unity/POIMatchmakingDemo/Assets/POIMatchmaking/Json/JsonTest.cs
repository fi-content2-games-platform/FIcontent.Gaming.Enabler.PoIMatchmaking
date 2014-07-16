using UnityEngine;

namespace Assets.POIMatchmaking
{
    public class JsonTest : MonoBehaviour
    {
        public void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "JsonTest"))
                RunJsonTest();
        }

        private static void RunJsonTest()
        {
            var obj = new JsonObject();
            obj.Set("hello", "world");
            obj.Set("another", "string");
            Debug.Log(obj.ToString());

            var array = new JsonArray();
            array.Add("hello");
            var index = array.Add(new JsonObject());
            array.GetObject(index).Set("inner", "object");
            obj.Set("array", array);
            Debug.Log(obj);

            TestJsonParser("{  }");
            TestJsonParser("{ \"a\": \"b\" }");
            TestJsonParser("{ \"a\": \"b\", }", "{ \"a\": \"b\" }");
            TestJsonParser("{ \"a\": \"b\", \"c\": \"d\" }");
            TestJsonParser("{ \"a\": { \"b\": \"c\" }, \"d\": { \"e\": \"f\" } }");
            TestJsonParser("{ \"a\": [ \"b\", [ { \"c\": \"d\" } ] ] }");
            TestJsonParser("{ \"a\": 1 }");
            TestJsonParser("{ \"a\": -1 }");
            TestJsonParser("{ \"a\": 0.5 }");
            TestJsonParser("{ \"a\": [ -0.5, 1, 3 ] }");
        }

        private static void TestJsonParser(string s)
        {
            TestJsonParser(s, s);
        }
            
        private static void TestJsonParser(string s, string expected)
        {
            var obj = JsonObject.Parse(s);
            var t = obj.ToString();
            if (t != expected)
                Debug.LogError(string.Format("Json parser failed - {0} => {1}, expected {2}", s, t, expected));
            else
                Debug.Log("Json parsed OK - " + s);
        }
    }
}