using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNLP.Util;

namespace OpenNLP.Tokenizer
{
    public abstract class AbstractTokenizer : ITokenizer
    {
        public abstract Span[] TokenizePositions(string input);

        /// <summary>Tokenize a string</summary>
        /// <param name="input">The string to be tokenized</param>
        /// <returns>A string array containing individual tokens as elements</returns>
        public virtual string[] Tokenize(string input)
        {
            int len;
            string temp;
            Span[] tokenSpans = TokenizePositions(input);
            List<string> tokens = new List<string>();
            for (int i = 0; i < tokenSpans.Length;i++ )
            {
                len = tokenSpans[i].Length();
                temp = input.Substring(tokenSpans[i].Start, len);

                if (len == 1)
                {
                    if (SymbolsMapper.ContainsKey(temp))
                    {
                        if (SymbolsMapper[temp] != "")
                            tokens.Add(SymbolsMapper[temp]);
                    }
                    else
                        tokens.Add(temp);
                    
                    continue;
                }

                if (temp == "'s")
                    continue;
                
                tokens.Add(temp);
            }
            return tokens.ToArray();
        }

        /// <summary>
        /// Constructs a list of Span objects, one for each whitespace delimited token.
        /// Token strings can be constructed form these spans as follows: input.Substring(span.Start, span.Length());
        /// </summary>
        /// <param name="input">string to tokenize</param>
        /// <returns>Array of spans</returns>
        internal static Span[] SplitOnWhitespaces(string input)
        {
            if (string.IsNullOrEmpty(input)) { return new Span[0]; }

            int tokenStart = -1;
            var tokens = new List<Span>();
            bool isInToken = false;

            //gather up potential tokens
            int endPosition = input.Length;
            for (int currentChar = 0; currentChar < endPosition; currentChar++)
            {
                if (char.IsWhiteSpace(input[currentChar]))
                {
                    if (isInToken)
                    {
                        tokens.Add(new Span(tokenStart, currentChar));
                        isInToken = false;
                        tokenStart = -1;
                    }
                }
                else
                {
                    if (!isInToken)
                    {
                        tokenStart = currentChar;
                        isInToken = true;
                    }
                }
            }
            if (isInToken)
            {
                tokens.Add(new Span(tokenStart, endPosition));
            }
            return tokens.ToArray();
        }

        private readonly Dictionary<string, string> SymbolsMapper = new Dictionary<string, string>()
        {
            {"'s",""},
            { "&", "and" },
            { "\"", "" },
            { "\\", "," },
            { "/", "," },
            { "|", "or" },
            { ">", "" },
            { "<", ""},
            { "=", "equals" },
            { "!", "." },
            { "*", "" },
            { "(", "" },
            { ")", "" },
            { "{", "" },
            { "}", "" },
            { "[", "" },
            { "]", "" },
            { "+", "plus" },
            { "~", "" },
            { "`", "" },
            { "'", "" },
            { ";", "," },
            { ":", "" },
            { "?", "." },
            { "“", "" },
            { "”", "" },
            { "‘", "" },
            { "—", "." }
        };
    }
}
