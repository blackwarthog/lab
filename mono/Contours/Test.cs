using System;
using System.Collections.Generic;
using System.Drawing;

namespace Contours {
    public class Test {
        class Exception: System.Exception { }

        public string name;
        
        public readonly Dictionary<string, List<List<List<Point>>>> input = new Dictionary<string, List<List<List<Point>>>>();
        public readonly Dictionary<string, List<List<List<Point>>>> output = new Dictionary<string, List<List<List<Point>>>>();
        public readonly Dictionary<string, bool> results = new Dictionary<string, bool>();
        public bool result = false;
        
        public static readonly List<Test> tests = new List<Test>();
        
        void check(string name, Shape shape) {
            if (!input.ContainsKey(name)) return;
            List<List<List<Point>>> contours = null;
            try {
                contours = shape.getContours();
            } catch(System.Exception) { }
            output.Add(name, contours);
            results.Add(name, compareContours(input[name], output[name]));
            if (!results[name]) result = false;
        }
        
        Shape tryCreateShape(List<List<List<Point>>> contours) {
            try {
                Shape shape = new Shape();
                shape.setContours(contours);
                return shape;
            } catch (System.Exception) {
                return null;
            }
        }

        Shape tryCombineShapes(Shape.CombinationMode mode, List<List<List<Point>>> a, List<List<List<Point>>> b) {
            try {
                Shape sa = new Shape();
                Shape sb = new Shape();
                sa.setContours(a);
                sb.setContours(b);
                return Shape.combine(mode, sa, sb);
            } catch (System.Exception) {
                return null;
            }
        }
                
        public bool run() {
            result = true;
            
            List<List<List<Point>>> a = null;
            List<List<List<Point>>> b = null;

            if (input.ContainsKey("dirtyA")) a = input["dirtyA"]; else
                if (input.ContainsKey("a")) a = input["a"];
            if (input.ContainsKey("dirtyB")) b = input["dirtyB"]; else
                if (input.ContainsKey("b")) b = input["b"];
            
            if (a != null)
                check("a", tryCreateShape(a));
            if (b != null)
                check("b", tryCreateShape(b));
                
            if (a != null && b != null) {
                check("add", tryCombineShapes(Shape.CombinationMode.Add, a, b));
                check("subtract", tryCombineShapes(Shape.CombinationMode.Subtract, a, b));
                check("intersection", tryCombineShapes(Shape.CombinationMode.Intersection, a, b));
                check("xor", tryCombineShapes(Shape.CombinationMode.Xor, a, b));
            }

            return result;
        }

        public static bool compareContours(List<Point> a, List<Point> b) {
            if (a == null || b == null) return false;
            if (a.Count == b.Count) {
                for(int offset = 0; offset < a.Count; ++offset) {
                    bool equal = true;
                    for(int i = 0; i < a.Count; ++i)
                        if (a[(i + offset)%a.Count] != b[i])
                            { equal = false; break; }
                    if (equal) return true;
                }
            }
            return false;
        }

        public static bool compareContours(List<List<Point>> a, List<List<Point>> b) {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            if (a.Count == 0) return true;
            if (!compareContours(a[0], b[0])) return false;
            bool[] compared = new bool[a.Count];
            for(int i = 1; i < a.Count; ++i) {
                bool equal = false;
                for(int j = 1; j < b.Count; ++j) {
                    if (!compared[j] && compareContours(a[i], b[j]))
                        { equal = true; compared[j] = true; break; }
                }
                if (!equal) return false;
            }
            return true;
        }

        public static bool compareContours(List<List<List<Point>>> a, List<List<List<Point>>> b) {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            bool[] compared = new bool[a.Count];
            for(int i = 0; i < a.Count; ++i) {
                bool equal = false;
                for(int j = i; j < b.Count; ++j) {
                    if (!compared[j] && compareContours(a[i], b[j]))
                        { equal = true; compared[j] = true; break; }
                }
                if (!equal) return false;
            }
            return true;
        }
                
        class Loader {
            public string text;
            public int position = 0;
            
            void error() { throw new Exception(); }
            void assert(bool expr) { if (!expr) error(); }
            
            void skipSpaces() {
                while(position < text.Length && char.IsWhiteSpace(text[position])) ++position;
            }
            
            int loadInt() {
                skipSpaces();
                int startPosition = position;
                while(position < text.Length && char.IsDigit(text[position])) ++position;
                assert(startPosition < position);
                return int.Parse(text.Substring(startPosition, position-startPosition));
            }

            string tryLoadKey(string key) {
                return tryLoadKey(new string[] { key });
            }

            string tryLoadKey(string key0, string key1) {
                return tryLoadKey(new string[] { key0, key1 });
            }

            string tryLoadKey(string[] keys) {
                skipSpaces();
                foreach(string key in keys)
                    if (text.Substring(position, key.Length) == key)
                        { position += key.Length; return key; }
                return null;
            }
            
            string loadKey(string key) {
                return loadKey(new string[] { key });
            }

            string loadKey(string key0, string key1) {
                return loadKey(new string[] { key0, key1 });
            }

            string loadKey(string[] keys) {
                string result = tryLoadKey(keys);
                assert(result != null);
                return result;
            }
            
            Point loadPoint() {
                loadKey("(");
                int x = loadInt();
                loadKey(",");
                int y = loadInt();
                loadKey(")");
                return new Point(x, y);
            }
            
            List<Point> loadPointList() {
                List<Point> list = new List<Point>();
                loadKey("(");
                if (tryLoadKey(")") == null) do {
                    list.Add(loadPoint());
                } while(loadKey(",", ")") == ",");
                return list;
            }
            
            List<List<Point>> loadPointListList() {
                List<List<Point>> list = new List<List<Point>>();
                loadKey("(");
                if (tryLoadKey(")") == null) do {
                    list.Add(loadPointList());
                } while(loadKey(",", ")") == ",");
                return list;
            }
            
            List<List<List<Point>>> loadPointListListList() {
                List<List<List<Point>>> list = new List<List<List<Point>>>();
                loadKey("(");
                if (tryLoadKey(")") == null) do {
                    list.Add(loadPointListList());
                } while(loadKey(",", ")") == ",");
                return list;
            }
            
            string loadFieldName() {
                skipSpaces();
                int startPosition = position;
                while(position < text.Length && char.IsLetterOrDigit(text[position])) ++position;
                assert(startPosition < position);
                return text.Substring(startPosition, position-startPosition);
            }

            string loadName() {
                string name = "";
                loadKey("(");
                while(text[position] != ')')
                    name += text[position++];
                ++position;
                return name.Trim();
            }

            Test loadTest() {
                Test test = new Test();
                loadKey("{");
                while(tryLoadKey("}") == null) {
                    string name = loadFieldName();
                    loadKey(":");
                    if (name == "name")
                        test.name = loadName();
                    else
                        test.input.Add(name, loadPointListListList());
                }
                return test;
            }

            public List<Test> loadTestListToEof() {
                List<Test> list = new List<Test>();
                while(true) {
                    skipSpaces();
                    if (position >= text.Length) break;
                    list.Add(loadTest());
                }
                return list;
            }
        };
        
        static void loadTests(string text) {
            Loader loader = new Loader();
            loader.text = text;
            tests.Clear();
            tests.AddRange(loader.loadTestListToEof());
        }
        
        public static void loadTestsFromFile(string filename) {
            loadTests(System.IO.File.ReadAllText(filename));
        }
        
        public static bool runAll() {
            bool result = true;
            foreach(Test test in tests)
                if (!test.run()) result = false;
            return result;
        }

        static string resultToString(string name, bool result) {
            return name + ": " + (result ? "+" : "FAILED") + "\n";
        }

        public static string makeReport() {
            string report = "";
            foreach(Test test in tests) {
                report += resultToString(test.name, test.result);
                foreach(KeyValuePair<string, bool> pair in test.results)
                    report += resultToString("    " + pair.Key, pair.Value);
            }
            return report;
        }
        
        public static void saveReport(string filename) {
            System.IO.File.WriteAllText(filename, makeReport());
        }
    }
}

