using System;

namespace OpenNLP.PosTagger
{
	/// <summary>
	/// A part of speech tagger that uses a model trained on English data from the
	/// Wall Street Journal and the Brown corpus.  The latest model created
	/// achieved >96% accuracy on unseen data.
	/// </summary>	
	public class EnglishMaximumEntropyPosTagger : MaximumEntropyPosTagger
	{
		
        // Constructors ----------

		public EnglishMaximumEntropyPosTagger(string modelFile, PosLookupList dictionary) : 
            base(GetModel(modelFile), new DefaultPosContextGenerator(), dictionary){ }
		
		public EnglishMaximumEntropyPosTagger(string modelFile, string dictionary) : 
            base(GetModel(modelFile), new DefaultPosContextGenerator(), new PosLookupList(dictionary)){ }

		public EnglishMaximumEntropyPosTagger(string modelFile) : 
            base(GetModel(modelFile), new DefaultPosContextGenerator()){ }
		
        
        // Utilities ---------

		private static SharpEntropy.IMaximumEntropyModel GetModel(string name)
		{
			return new SharpEntropy.GisModel(new SharpEntropy.IO.BinaryGisModelReader(name));
		}
	}

}
