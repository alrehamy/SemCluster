using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections;

namespace WordNetApi.Core
{
    public static class DictionaryExtensions
    {
        // Methods
        public static void Add<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, Dictionary<KeyType, ValueType> toAdd, ValueCombinationDelegate<ValueType> combine)
        {
            foreach (KeyType local in toAdd.Keys)
            {
                if (!dictionary.ContainsKey(local))
                {
                    dictionary.Add(local, toAdd[local]);
                }
                else
                {
                    dictionary[local] = combine(dictionary[local], toAdd[local]);
                }
            }
        }
        public static void EnsureContainsKey<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, KeyType key, Type valueType)
        {
            dictionary.EnsureContainsKey<KeyType, ValueType>(key, valueType, null);
        }
        public static void EnsureContainsKey<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, KeyType key, Type valueType, params object[] constructorParameters)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, (ValueType)Activator.CreateInstance(valueType, constructorParameters));
            }
        }
        public static void Save<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, StreamWriter file, Func<KeyType, string> keyConversion, Func<ValueType, string> valueConversion, Sort sort, bool reverse, bool writeCount, string linePrefix, string keyValSeparator, bool throwExceptionOnContainsSeparator)
        {
            IEnumerable<KeyType> keys;
            if (reverse && (sort == Sort.None))
            {
                throw new Exception("Error 500. Cannot reverse without sorting");
            }
            if (writeCount)
            {
                file.WriteLine(dictionary.Count);
            }
            if (sort != Sort.None)
            {
                KeyType[] array = null;
                if (sort == Sort.KeysByValues)
                {
                    array = dictionary.SortKeysByValues<KeyType, ValueType>();
                }
                else
                {
                    array = dictionary.Keys.ToArray<KeyType>();
                    Array.Sort<KeyType>(array);
                }
                if (reverse)
                {
                    Array.Reverse(array);
                }
                keys = array;
            }
            else
            {
                keys = dictionary.Keys;
            }
            foreach (KeyType local in keys)
            {
                string str = keyConversion(local);
                string str2 = valueConversion(dictionary[local]);
                file.WriteLine(linePrefix + str + keyValSeparator + str2);
            }
        }
        public static void Save<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, string path, Func<KeyType, string> keyConversion, Func<ValueType, string> valueConversion, Sort sort, bool reverse, bool writeCount, string linePrefix, string keyValSeparator, bool throwExceptionOnContainsSeparator)
        {
            StreamWriter file = new StreamWriter(path);
            dictionary.Save<KeyType, ValueType>(file, keyConversion, valueConversion, sort, reverse, writeCount, linePrefix, keyValSeparator, throwExceptionOnContainsSeparator);
            file.Close();
        }
        public static KeyType[] SortKeysByValues<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary) { return dictionary.SortKeysByValues<KeyType, ValueType>(false); }
        public static ValueType[] SortValuesByKeys<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary) { return dictionary.SortValuesByKeys<KeyType, ValueType>(false); }
        public static KeyType[] SortKeysByValues<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, bool reverse)
        {
            KeyType[] array = new KeyType[dictionary.Count];
            dictionary.Keys.CopyTo(array, 0);
            ValueType[] localArray2 = new ValueType[dictionary.Count];
            dictionary.Values.CopyTo(localArray2, 0);
            Array.Sort<ValueType, KeyType>(localArray2, array);
            if (reverse)
            {
                Array.Reverse(array);
            }
            return array;
        }
        public static ValueType[] SortValuesByKeys<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dictionary, bool reverse)
        {
            KeyType[] array = new KeyType[dictionary.Count];
            dictionary.Keys.CopyTo(array, 0);
            ValueType[] localArray2 = new ValueType[dictionary.Count];
            dictionary.Values.CopyTo(localArray2, 0);
            Array.Sort<KeyType, ValueType>(array, localArray2);
            if (reverse)
            {
                Array.Reverse(localArray2);
            }
            return localArray2;
        }
        // Nested Types
        public enum Sort
        {
            None,
            Keys,
            KeysByValues
        }
        public delegate ValueType ValueCombinationDelegate<ValueType>(ValueType value1, ValueType value2);
    }

    public static class StreamReaderExtensions
    {
        // Methods
        public static string ReadLine(this StreamReader reader, ref uint position)
        {
            string str = reader.ReadLine();
            if (str != null)
            {
                uint num = position;
                position += (uint)reader.CurrentEncoding.GetByteCount(str + Environment.NewLine);
            }
            return str;
        }

        public static void SetPosition(this StreamReader reader, long position)
        {
            reader.BaseStream.Position = position;
            reader.DiscardBufferedData();
        }

        public static bool TryReadLine(this StreamReader reader, out string line)
        {
            line = reader.ReadLine();
            if (line == null)
            {
                reader.Close();
            }
            return (line != null);
        }
    }

    public static class StringExtensions
    {
        // Fields
        private static Regex _leadingPunctRE;
        private static Regex _trailingPunctRE;

        // Methods
        static StringExtensions()
        {
            string str = "a-zA-Z0-9";
            _leadingPunctRE = new Regex("^[^" + str + "]+");
            _trailingPunctRE = new Regex("[^" + str + "]+$");
        }

        public static void Disallow(this string s, params char[] chars)
        {
            foreach (char ch in chars)
            {
                if (s.Contains<char>(ch))
                {
                    throw new Exception("No '" + ch + "' characters are allowed");
                }
            }
        }

        public static string EscapeXML(this string text)
        {
            MemoryStream w = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(w, Encoding.UTF8);
            writer.WriteString(text);
            writer.Flush();
            w.Position = 0L;
            StreamReader reader = new StreamReader(w);
            string str = reader.ReadToEnd();
            reader.Close();
            writer.Close();
            return str;
        }

        public static string GetRelativePathTo(this string path, string absolutePath)
        {
            return absolutePath.Substring(path.Length).Trim(new char[] { '\\' });
        }

        public static string Replace(this string s, Dictionary<string, string> replacements, bool repeatUntilNoChange)
        {
            bool flag = true;
            while (flag)
            {
                flag = false;
                foreach (string str in replacements.Keys)
                {
                    string str2 = s;
                    s = s.Replace(str, replacements[str]);
                    if (s != str2)
                    {
                        flag = true;
                    }
                }
                if (!repeatUntilNoChange)
                {
                    return s;
                }
            }
            return s;
        }

        public static string[] Split(this string s, int expectedParts)
        {
            string[] strArray = new string[expectedParts];
            int startIndex = 0;
            int num2 = 0;
            while (true)
            {
                while ((startIndex < s.Length) && (s[startIndex] == ' '))
                {
                    startIndex++;
                }
                if (startIndex == s.Length)
                {
                    break;
                }
                int index = s.IndexOf(' ', startIndex);
                if (index == -1)
                {
                    index = s.Length;
                }
                strArray[num2++] = s.Substring(startIndex, index - startIndex);
                startIndex = index;
            }
            return strArray;
        }

        public static string TrimPunctuation(this string s) { return s.TrimPunctuation(true, true); }

        private static string TrimPunctuation(string s, Regex re)
        {
            bool flag = true;
            while (flag)
            {
                string str = re.Replace(s, "");
                flag = str != s;
                s = str;
            }
            return s;
        }

        public static string TrimPunctuation(this string s, bool leading, bool trailing)
        {
            if (leading)
            {
                s = TrimPunctuation(s, _leadingPunctRE);
            }
            if (trailing)
            {
                s = TrimPunctuation(s, _trailingPunctRE);
            }
            return s;
        }

        public static string UnescapeXML(this string text)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write("<s>" + text + "</s>");
            writer.Flush();
            stream.Position = 0L;
            XmlTextReader reader = new XmlTextReader(stream);
            reader.Read();
            string str = reader.ReadString();
            writer.Close();
            reader.Close();
            stream.Close();
            return str;
        }
    }

    public class HashSearchTextStream : SearchTextStream
    {
        // Fields
        private Dictionary<int, uint[]> _hashFilePositions;
        private HashProviderDelegate _hashProvider;
        private MatchProviderDelegate _matchProvider;

        // Methods
        public HashSearchTextStream(Stream stream, HashProviderDelegate hashProvider, MatchProviderDelegate matchProvider)
            : base(stream)
        {
            this._hashProvider = hashProvider;
            this._matchProvider = matchProvider;
            this.Initialize();
        }

        public HashSearchTextStream(string path, HashProviderDelegate hashProvider, MatchProviderDelegate matchProvider)
            : this(new FileStream(path, FileMode.OpenOrCreate), hashProvider, matchProvider)
        {
        }

        public override void Close()
        {
            base.Close();
            if (this._hashFilePositions != null)
            {
                this._hashFilePositions.Clear();
                this._hashFilePositions = null;
            }
        }

        private void Initialize()
        {
            string str;
            int num2;
            base.Stream.SetPosition(0L);
            int capacity = 0;
            while ((str = base.Stream.ReadLine()) != null)
            {
                if (this._hashProvider(str, HashType.Index, out num2))
                {
                    capacity++;
                }
            }
            this._hashFilePositions = new Dictionary<int, uint[]>(capacity);
            base.Stream.SetPosition(0L);
            uint position = 0;
            for (uint i = position; (str = base.Stream.ReadLine(ref position)) != null; i = position)
            {
                if (this._hashProvider(str, HashType.Index, out num2))
                {
                    uint[] numArray;
                    if (this._hashFilePositions.TryGetValue(num2, out numArray))
                    {
                        uint[] array = new uint[numArray.Length + 1];
                        numArray.CopyTo(array, 0);
                        numArray = array;
                        this._hashFilePositions[num2] = numArray;
                    }
                    else
                    {
                        numArray = new uint[1];
                        this._hashFilePositions.Add(num2, numArray);
                    }
                    numArray[numArray.Length - 1] = i;
                }
            }
        }

        public override void ReInitialize(Stream stream)
        {
            base.ReInitialize(stream);
            this.Initialize();
        }

        public override string Search(string key, long start, long end)
        {
            int num;
            uint[] numArray;
            base.CheckSearchRange(start, end);
            if (this._hashProvider(key, HashType.Search, out num) && this._hashFilePositions.TryGetValue(num, out numArray))
            {
                foreach (uint num2 in numArray)
                {
                    if ((num2 >= start) && (num2 <= end))
                    {
                        base.Stream.SetPosition((long)num2);
                        string currentLine = base.Stream.ReadLine();
                        if (this._matchProvider(key, currentLine))
                        {
                            return currentLine;
                        }
                    }
                }
            }
            return null;
        }

        // Nested Types
        public delegate bool HashProviderDelegate(string toHash, HashSearchTextStream.HashType action, out int hashCode);

        public enum HashType
        {
            Index,
            Search
        }

        public delegate bool MatchProviderDelegate(string key, string currentLine);
    }

    public abstract class SearchTextStream
    {
        // Fields
        private StreamReader _stream;

        // Methods
        protected SearchTextStream(Stream stream)
        {
            this._stream = new StreamReader(stream);
        }

        protected void CheckSearchRange(long start, long end)
        {
            if (start < 0L)
            {
                throw new ArgumentOutOfRangeException("Error 500", "Start byte position must be non-negative");
            }
            if (end >= this.Stream.BaseStream.Length)
            {
                throw new ArgumentOutOfRangeException("Error 500", "End byte position must be less than the length of the stream");
            }
            if (start > end)
            {
                throw new ArgumentOutOfRangeException("Error 500", "Start byte position must be less than or equal to end byte position");
            }
        }

        public virtual void Close()
        {
            if (this._stream != null)
            {
                this._stream.Close();
                this._stream = null;
            }
        }

        public virtual void ReInitialize(Stream stream)
        {
            this.Close();
            this._stream = new StreamReader(stream);
        }

        public string Search(string key)
        {
            if (this._stream.BaseStream.Length == 0L)
            {
                return null;
            }
            return this.Search(key, 0L, this._stream.BaseStream.Length - 1L);
        }

        public abstract string Search(string key, long start, long end);

        // Properties
        public StreamReader Stream
        {
            get { return this._stream; }
        }
    }

    public class BinarySearchTextStream : SearchTextStream
    {
        // Fields
        private SearchComparisonDelegate _searchComparison;

        // Methods
        public BinarySearchTextStream(Stream stream, SearchComparisonDelegate searchComparison) : base(stream)
        {
            this._searchComparison = searchComparison;
        }

        public BinarySearchTextStream(string path, SearchComparisonDelegate searchComparison) : this(new FileStream(path, FileMode.OpenOrCreate), searchComparison)
        {
        }

        public override string Search(string key, long start, long end)
        {
            base.CheckSearchRange(start, end);
            while (start <= end)
            {
                base.Stream.BaseStream.Position = (long)(((double)(start + end)) / 2.0);
                int num = 0;
                while (base.Stream.BaseStream.Position > 0L)
                {
                    int num2 = base.Stream.BaseStream.ReadByte();
                    char ch = (char)num2;
                    if ((++num > 1) && (ch == '\n'))
                    {
                        break;
                    }
                    Stream baseStream = base.Stream.BaseStream;
                    baseStream.Position -= 2L;
                }
                long position = base.Stream.BaseStream.Position;
                uint num4 = (uint)position;
                base.Stream.DiscardBufferedData();
                string currentLine = base.Stream.ReadLine(ref num4);
                num4--;
                int num5 = this._searchComparison(key, currentLine);
                if (num5 == 0)
                {
                    return currentLine;
                }
                if (num5 < 0)
                {
                    end = position - 1L;
                }
                else if (num5 > 0)
                {
                    start = num4 + 1;
                }
            }
            return null;
        }

        // Nested Types
        public delegate int SearchComparisonDelegate(string key, string currentLine);
    }
}
