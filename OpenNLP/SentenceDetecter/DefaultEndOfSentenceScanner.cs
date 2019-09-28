using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNLP.SentenceDetecter
{
	/// <summary>
	/// The default end of sentence scanner implements all of the
	/// EndOfSentenceScanner methods in terms of the GetPositions(char[])
	/// method.
	/// It scans for '.', '?', '!', '"'
	/// </summary>
	public class DefaultEndOfSentenceScanner : IEndOfSentenceScanner
	{
        private static readonly List<char> defaultEndOfSentenceCharacters = new List<char>(){'?','!','.'};
        
	    /// <summary> 
	    /// Creates a new <code>DefaultEndOfSentenceScanner</code> instance.
	    /// </summary>
	    public DefaultEndOfSentenceScanner(){}


        public List<char> GetPotentialEndOfSentenceCharacters()
        {
            return defaultEndOfSentenceCharacters;
        }

        public virtual List<int> GetPositions(string input)
		{
			return GetPositions(input.ToCharArray());
		}

        public virtual List<int> GetPositions(System.Text.StringBuilder buffer)
		{
			return GetPositions(buffer.ToString().ToCharArray());
		}
		
		public virtual List<int> GetPositions(char[] charBuffer)
		{
            var positionList = new List<int>();
			
			for (int currentChar = 0; currentChar < charBuffer.Length; currentChar++)
			{
			    if (this.GetPotentialEndOfSentenceCharacters().Contains(charBuffer[currentChar]))
			    {
			        positionList.Add(currentChar);
			    }
				/*switch (charBuffer[currentChar])
				{	
					case '.': 
					case '?': 
					case '!': 
						positionList.Add(currentChar);
						break;
					default: 
						break;
				}*/
			}
			return positionList;
		}


	    public static List<char> GetDefaultEndOfSentenceCharacters()
	    {
	        return defaultEndOfSentenceCharacters;
	    }
	}
}
