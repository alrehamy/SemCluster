using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OpenNLP.SentenceDetecter
{
	/// <summary>
	/// Scans strings, StringBuilders, and char[] arrays for the offsets of
	/// sentence ending characters.
	/// 
	/// <p>Implementations of this interface can use regular expressions,
	/// hand-coded DFAs, and other scanning techniques to locate end of
	/// sentence offsets.</p>
	/// </summary>
	public interface IEndOfSentenceScanner
	{
		/// <summary>
		/// The receiver scans 'input' for sentence ending characters and
		/// returns their offsets.
		/// </summary>
		/// <param name="input">
		/// a <code>string</code> value
		/// </param>
		/// <returns>
		/// a <code>List</code> of integers.
		/// </returns>
        List<int> GetPositions(string input);

		/// <summary>
		/// The receiver scans 'buffer' for sentence ending characters and
		/// returns their offsets.
		/// </summary>
		/// <param name="buffer">
		/// a <code>StringBuilder</code> value
		/// </param>
		/// <returns>
		/// a <code>List</code> of integers.
		/// </returns>
        List<int> GetPositions(StringBuilder buffer);
			
		/// <summary>
		/// The receiver scans 'characterBuffer' for sentence ending characters and
		/// returns their offsets.
		/// </summary>
		/// <param name="characterBuffer">
		/// a <code>char[]</code> value
		/// </param>
		/// <returns>
		/// a <code>List</code> of integers.
		/// </returns>
        List<int> GetPositions(char[] characterBuffer);

        /// <summary>
        /// Gets the characters for which we are testing a potential 
        /// end of sentence for this scanner.
        /// </summary>
	    List<char> GetPotentialEndOfSentenceCharacters();
	} 
}
