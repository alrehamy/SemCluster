using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenNLP.PosTagger
{
	/// <summary>
	/// A part-of-speech tagger that uses maximum entropy.  Trys to predict whether
	/// words are nouns, verbs, or any of 70 other POS tags depending on their
	/// surrounding context.
	/// </summary>
	public class MaximumEntropyPosTagger : IPosTagger
	{
        private const int DefaultBeamSize = 3;

		public virtual string NegativeOutcome
		{
			get
			{
				return "";
			}
		}

		/// <summary>
		/// Returns the number of different tags predicted by this model.
		/// </summary>
		/// <returns>
		/// the number of different tags predicted by this model.
		/// </returns>
		public virtual int NumTags
		{
			get
			{
				return this.PosModel.OutcomeCount;
			}
		}
		
        /// <summary>
        /// The maximum entropy model to use to evaluate contexts.
        /// </summary>
		protected internal SharpEntropy.IMaximumEntropyModel PosModel { get; set; }

        /// <summary>
        /// The feature context generator.
        /// </summary>
		protected internal IPosContextGenerator ContextGenerator { get; set; }

        /// <summary>
        ///Tag dictionary used for restricting words to a fixed set of tags.
        ///</summary>
		protected internal PosLookupList TagDictionary { get; set; }

	    /// <summary>
	    /// Says whether a filter should be used to check whether a tag assignment
	    /// is to a word outside of a closed class.
	    /// </summary>
	    protected internal bool UseClosedClassTagsFilter { get; set; }

        /// <summary>
        /// The size of the beam to be used in determining the best sequence of pos tags.
        /// </summary>
	    protected internal int BeamSize { get; set; }

		/// <summary>
		/// The search object used for search multiple sequences of tags.
		/// </summary>
		internal Util.BeamSearch Beam;
		
        
        // Constructors ------------------------

		public MaximumEntropyPosTagger(SharpEntropy.IMaximumEntropyModel model) : 
            this(model, new DefaultPosContextGenerator()){ }
		
		public MaximumEntropyPosTagger(SharpEntropy.IMaximumEntropyModel model, PosLookupList dictionary) : 
            this(DefaultBeamSize, model, new DefaultPosContextGenerator(), dictionary){ }
		
		public MaximumEntropyPosTagger(SharpEntropy.IMaximumEntropyModel model, IPosContextGenerator contextGenerator) : 
            this(DefaultBeamSize, model, contextGenerator, null){ }
		
		public MaximumEntropyPosTagger(SharpEntropy.IMaximumEntropyModel model, IPosContextGenerator contextGenerator, PosLookupList dictionary):
            this(DefaultBeamSize, model, contextGenerator, dictionary){ }
		
        public MaximumEntropyPosTagger(int beamSize, SharpEntropy.IMaximumEntropyModel model, IPosContextGenerator contextGenerator, PosLookupList dictionary)
		{
		    UseClosedClassTagsFilter = false;
		    this.BeamSize = beamSize;
			this.PosModel = model;
			this.ContextGenerator = contextGenerator;
			Beam = new PosBeamSearch(this, this.BeamSize, contextGenerator, model);
			this.TagDictionary = dictionary;
		}


        // Methods ----------------------------

        /// <summary>
        /// Returns a list of all the possible POS tags predicted by this model.
        /// </summary>
        /// <returns>string array of the possible POS tags</returns>
        public virtual string[] AllTags()
        {
            var tags = new string[this.PosModel.OutcomeCount];
            for (int currentTag = 0; currentTag < this.PosModel.OutcomeCount; currentTag++)
            {
                tags[currentTag] = this.PosModel.GetOutcomeName(currentTag);
            }
            return tags;
        }

        /// <summary>
        /// Associates tags to a collection of tokens.
        /// This collection of tokens should represent a sentence.
        /// </summary>
        /// <param name="tokens">The collection of tokens as strings</param>
        /// <returns>The collection of tags as strings</returns>
		public virtual string[] Tag(string[] tokens)
		{
            var bestSequence = Beam.BestSequence(tokens, null);
            return bestSequence.Outcomes.ToArray();
		}
		
        /// <summary>
        /// Tags words in a given sentence
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
		public virtual List<TaggedWord> TagSentence(string sentence)
		{
			var tokens = sentence.Split();
			var tags = Tag(tokens);
		    var taggedWords = Enumerable.Range(0, tags.Length)
		        .Select(i => new TaggedWord(tokens[i], tags[i], i))
		        .ToList();

		    return taggedWords;
		}
		
		public virtual string[] GetOrderedTags(List<string> words, List<string> tags, int index)
		{
			return GetOrderedTags(words, tags, index, null);
		}
		
		public virtual string[] GetOrderedTags(List<string> words, List<string> tags, int index, double[] tagProbabilities)
		{
			double[] probabilities = this.PosModel.Evaluate(this.ContextGenerator.GetContext(index, words.ToArray(), tags.ToArray(), null));
			var orderedTags = new string[probabilities.Length];
			for (int currentProbability = 0; currentProbability < probabilities.Length; currentProbability++)
			{
				int max = 0;
				for (int tagIndex = 1; tagIndex < probabilities.Length; tagIndex++)
				{
					if (probabilities[tagIndex] > probabilities[max])
					{
						max = tagIndex;
					}
				}
				orderedTags[currentProbability] = this.PosModel.GetOutcomeName(max);
				if (tagProbabilities != null)
				{
					tagProbabilities[currentProbability] = probabilities[max];
				}
				probabilities[max] = 0;
			}
			return orderedTags;
		}
		

        // Utilities ---------------------------------

        // Inner classes ---------------------------
        private class PosBeamSearch : Util.BeamSearch
        {
            private readonly MaximumEntropyPosTagger _maxentPosTagger;


            // Constructors ---------------------

            public PosBeamSearch(MaximumEntropyPosTagger posTagger, int size, IPosContextGenerator contextGenerator, SharpEntropy.IMaximumEntropyModel model) :
                base(size, contextGenerator, model)
            {
                _maxentPosTagger = posTagger;
            }

            public PosBeamSearch(MaximumEntropyPosTagger posTagger, int size, IPosContextGenerator contextGenerator, SharpEntropy.IMaximumEntropyModel model, int cacheSize) :
                base(size, contextGenerator, model, cacheSize)
            {
                _maxentPosTagger = posTagger;
            }


            // Methods ---------------------------

            protected internal override bool ValidSequence(int index, object[] inputSequence, string[] outcomesSequence, string outcome)
            {
                if (_maxentPosTagger.TagDictionary == null)
                {
                    return true;
                }
                else
                {
                    string[] tags = _maxentPosTagger.TagDictionary.GetTags(inputSequence[index].ToString());
                    if (tags == null)
                    {
                        return true;
                    }
                    else
                    {
                        return new ArrayList(tags).Contains(outcome);
                    }
                }
            }
            protected internal override bool ValidSequence(int index, ArrayList inputSequence, Util.Sequence outcomesSequence, string outcome)
            {
                if (_maxentPosTagger.TagDictionary == null)
                {
                    return true;
                }
                else
                {
                    string[] tags = _maxentPosTagger.TagDictionary.GetTags(inputSequence[index].ToString());
                    if (tags == null)
                    {
                        return true;
                    }
                    else
                    {
                        return new ArrayList(tags).Contains(outcome);
                    }
                }
            }
        }
	}
}
