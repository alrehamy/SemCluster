using System;

namespace OpenNLP.Util
{
    /// <summary>
    /// Class providing simple tokenization of a string, for manipulation.  
    /// For NLP tokenizing, see the OpenNLP.Tokenizer namespace.
    /// </summary>
    public class StringTokenizer
    {
        private const string Delimiters = " \t\n\r";
            //The tokenizer uses the default delimiter set: the space character, the tab character, the newline character, and the carriage-return character	

        private readonly string[] _tokens;
        private int _position;

        /// <summary>
        /// Initializes a new class instance with a specified string to process
        /// </summary>
        /// <param name="input">
        /// string to tokenize
        /// </param>
        public StringTokenizer(string input) : this(input, Delimiters.ToCharArray())
        {
        }

        public StringTokenizer(string input, string separators) : this(input, separators.ToCharArray())
        {
        }

        public StringTokenizer(string input, params char[] separators)
        {
            _tokens = input.Split(separators);
            _position = 0;
        }

        public string NextToken()
        {
            while (_position < _tokens.Length)
            {
                if ((_tokens[_position].Length > 0))
                {
                    return _tokens[_position++];
                }
                _position++;
            }
            return null;
        }

    }
}