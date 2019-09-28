using System;
using System.Collections.Generic;

namespace OpenNLP.Tokenizer
{
	/// <summary> 
	/// The interface for tokenizers, which turn messy text into nicely segmented
	/// text tokens.
	/// </summary>
	public interface ITokenizer
	{
		/// <summary> 
		/// Tokenize a string.
		/// </summary>
		/// <param name="input">
		/// The string to be tokenized.
		/// </param>
		/// <returns>
		/// The string[] with the individual tokens as the array
		/// elements.
		/// </returns>
		string[] Tokenize(string input);
			
		/// <summary>Tokenize a string</summary>
		/// <param name="input">The string to be tokenized</param>
		/// <returns>
		/// The Span[] with the spans (offsets into input) for each
		/// token as the individuals array elements.
		/// </returns>
		Util.Span[] TokenizePositions(string input);
	}
}
