using System;

namespace OpenNLP.SentenceDetecter
{
	/// <summary>
	/// A sentence detector which uses a model trained on English data 
	/// (Wall Street Journal text).
	/// </summary>
	public class EnglishMaximumEntropySentenceDetector : MaximumEntropySentenceDetector
	{
		/// <summary>
		/// Constructor which loads the English sentence detection model
		/// transparently.
		/// </summary>
		public EnglishMaximumEntropySentenceDetector(string name): 
            base(new SharpEntropy.GisModel(new SharpEntropy.IO.BinaryGisModelReader(name))){}

        public EnglishMaximumEntropySentenceDetector(string name, IEndOfSentenceScanner scanner):
            base(new SharpEntropy.GisModel(new SharpEntropy.IO.BinaryGisModelReader(name)), scanner) { }
	}
}
