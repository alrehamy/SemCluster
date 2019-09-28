using System;

namespace OpenNLP.SentenceDetecter
{
	/// <summary> 
	/// The interface for sentence detectors, 
	/// which find the sentence boundaries in a text.
	/// </summary>
	public interface ISentenceDetector
	{
		/// <summary> 
		/// Sentence detect a string
		/// </summary>
		/// <param name="input">
		/// The string to be sentence detected.
		/// </param>
		/// <returns>
		/// The string[] with the individual sentences as the array
		/// elements.
		/// </returns>
		string[] SentenceDetect(string input);
			
		/// <summary> 
		/// Sentence detect a string.
		/// </summary>
		/// <param name="input">
		/// The string to be sentence detected.
		/// </param>
		/// <returns>
		/// An int[] with the starting offset positions of each
		/// detected sentence. 
		/// </returns>
		int[] SentencePositionDetect(string input);
	}
}
