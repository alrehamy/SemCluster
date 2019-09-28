using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordNetApi.Lemmatizer
{
    public class Tokenizer
    {
        private readonly string[] _tokens;
        int _position;

        public Tokenizer(string input, params char[] separators)
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
