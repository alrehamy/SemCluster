using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WordNetApi.Lemmatizer
{
    public class Util
    {
        public static string GetLemma(string[] tokens, BitArray bits, string delimiter)
        {
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i != 0 && !bits.Get(i - 1))
                {
                    buf.Append(delimiter);
                }
                buf.Append(tokens[i]);
            }
            return buf.ToString();
        }

        public static bool Increment(BitArray bits, int size)
        {
            int i = size - 1;
            while (i >= 0 && bits.Get(i))
            {
                bits.Set(i--, false);
            }
            if (i < 0)
            {
                return false;
            }
            bits.Set(i, true);
            return true;
        }

        public static string[] Split(string str)
        {
            char[] chars = str.ToCharArray();
            List<string> tokens = new List<string>();
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if ((chars[i] >= 'a' && chars[i] <= 'z') || chars[i] == '\'')
                {
                    buf.Append(chars[i]);
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        tokens.Add(buf.ToString());
                        buf = new StringBuilder();
                    }
                }
            }
            if (buf.Length > 0)
            {
                tokens.Add(buf.ToString());
            }
            return (tokens.ToArray());
        }

        private Util()
        {
        }
    }
}
