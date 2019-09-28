using System;
using System.Collections;

namespace OpenNLP.Chunker
{
	/// <summary>
	/// The interface for chunkers which provide chunk tags for a sequence of tokens.
	/// </summary>
	public interface IChunker
	{
		/// <summary>
		/// Generates chunk tags for the given sequence returning the result in an array.
		/// </summary>
		/// <param name="tokens">
		/// an array of the tokens or words of the sequence.
		/// </param>
		/// <param name="tags">
		/// an array of the pos tags of the sequence.
		/// </param>
		/// <returns>
		/// an array of chunk tags for each token in the sequence.
		/// </returns>
		string[] Chunk(string[] tokens, string[] tags);
	}
}
