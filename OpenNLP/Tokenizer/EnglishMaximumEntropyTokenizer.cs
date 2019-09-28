using System;

namespace OpenNLP.Tokenizer
{
	/// <summary> 
	/// A tokenizer which uses default English data for the maximum entropy model.
	/// </summary>
	public class EnglishMaximumEntropyTokenizer : MaximumEntropyTokenizer
	{
		public EnglishMaximumEntropyTokenizer(string name) : base(new SharpEntropy.GisModel(new SharpEntropy.IO.BinaryGisModelReader(name)))
		{
			AlphaNumericOptimization = true;
		}
	}
}
