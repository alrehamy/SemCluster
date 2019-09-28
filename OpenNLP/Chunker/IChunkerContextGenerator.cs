using System;
using System.Collections;

namespace OpenNLP.Chunker
{
	/// <summary>
	/// Context generator interface for chunkers.
	/// </summary>
	public interface IChunkerContextGenerator : Util.IBeamSearchContextGenerator
	{
		/// <summary>
		/// Returns the contexts for chunking of the specified index.
		/// </summary>
		/// <param name="tokenIndex">
		/// The index of the token in the specified toks array for which the context should be constructed. 
		/// </param>
		/// <param name="tokens">
		/// The tokens of the sentence.  The <code>ToString</code> methods of these objects should return the token text.
		/// </param>
		/// <param name="tags">
		/// The POS tags for the the specified tokens.
		/// </param>
		/// /// <param name="previousDecisions">
		/// The previous decisions made in the tagging of this sequence.  Only indices less than tokenIndex will be examined.
		/// </param>
		/// <returns>
		/// An array of predictive contexts on which a model basis its decisions.
		/// </returns>
		string[] GetContext(int tokenIndex, string[] tokens, string[] tags, string[] previousDecisions);
	}
}
