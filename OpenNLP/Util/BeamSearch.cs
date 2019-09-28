using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Caching;

namespace OpenNLP.Util
{
    /// <summary>
    /// Performs k-best search over sequence.  This is besed on the description in
    /// Ratnaparkhi (1998), PhD diss, Univ. of Pennsylvania. 
    /// </summary>
    public class BeamSearch
    {
        private const int ZeroLog = -100000;
        private static readonly object[] EmptyAdditionalContext = new object[0];

        internal SharpEntropy.IMaximumEntropyModel Model;
        internal IBeamSearchContextGenerator ContextGenerator;
        internal int Size;

        private readonly MemoryCache contextsCache;


        // Constructors -----------------

        /// <summary>Creates new search object</summary>
        /// <param name="size">The size of the beam (k)</param>
        /// <param name="contextGenerator">the context generator for the model</param>
        /// <param name="model">the model for assigning probabilities to the sequence outcomes</param>
        public BeamSearch(int size, IBeamSearchContextGenerator contextGenerator,
            SharpEntropy.IMaximumEntropyModel model) :
                this(size, contextGenerator, model, 0){}

        /// <summary>Creates new search object</summary>
        /// <param name="size">The size of the beam (k)</param>
        /// <param name="contextGenerator">the context generator for the model</param>
        /// <param name="model">the model for assigning probabilities to the sequence outcomes</param>
        /// <param name="cacheSizeInMegaBytes">size of the cache to use for performance</param>
        public BeamSearch(int size, IBeamSearchContextGenerator contextGenerator,
            SharpEntropy.IMaximumEntropyModel model, int cacheSizeInMegaBytes)
        {
            Size = size;
            ContextGenerator = contextGenerator;
            Model = model;

            if (cacheSizeInMegaBytes > 0)
            {
                var properties = new NameValueCollection
			    {
			        {"cacheMemoryLimitMegabytes", cacheSizeInMegaBytes.ToString()}
			    };
                contextsCache = new MemoryCache("beamSearchContextCache", properties);
            }
        }


        // Methods ------------------------

        /// <summary>
        /// Returns the best sequence of outcomes based on model for this object.
        /// </summary>
        /// <param name="numSequences">The maximum number of sequences to be returned</param>
        /// <param name="sequence">The input sequence</param>
        /// <param name="additionalContext">
        /// An object[] of additional context.
        /// This is passed to the context generator blindly with the assumption that the context are appropiate.
        /// </param>
        /// <returns>An array of the top ranked sequences of outcomes</returns>		
        public Sequence[] BestSequences(int numSequences, string[] sequence, object[] additionalContext)
        {
            return BestSequences(numSequences, sequence, additionalContext, ZeroLog);
        }

        /// <summary>
        /// Returns the best sequence of outcomes based on model for this object.</summary>
        /// <param name="numSequences">The maximum number of sequences to be returned</param>
        /// <param name="sequence">The input sequence</param>
        /// <param name="additionalContext">
        /// An object[] of additional context.  This is passed to the context generator blindly with the assumption that the context are appropiate.
        /// </param>
        /// <param name="minSequenceScore">A lower bound on the score of a returned sequence</param>
        /// <returns>An array of the top ranked sequences of outcomes</returns>		
        public virtual Sequence[] BestSequences(int numSequences, string[] sequence, object[] additionalContext,
            double minSequenceScore)
        {
            int sequenceCount = sequence.Length;
            var previousHeap = new ListHeap<Sequence>(Size);
            var nextHeap = new ListHeap<Sequence>(Size);
            var probabilities = new double[Model.OutcomeCount];//not used at the moment

            previousHeap.Add(new Sequence());
            if (additionalContext == null)
            {
                additionalContext = EmptyAdditionalContext;
            }
            for (int currentSequence = 0; currentSequence < sequenceCount; currentSequence++)
            {
                int sz = Math.Min(Size, previousHeap.Size);
                int sc = 0;
                for (; previousHeap.Size > 0 && sc < sz; sc++)
                {
                    Sequence topSequence = previousHeap.Extract();
                    string[] outcomes = topSequence.Outcomes.ToArray();
                    string[] contexts = ContextGenerator.GetContext(currentSequence, sequence, outcomes,
                        additionalContext);
                    double[] scores;
                    if (contextsCache != null)
                    {
                        var contextKey = string.Join("|", contexts);
                        scores = (double[]) contextsCache[contextKey];
                        if (scores == null)
                        {
                            scores = Model.Evaluate(contexts, probabilities);
                            contextsCache[contextKey] = scores;
                        }
                    }
                    else
                    {
                        scores = Model.Evaluate(contexts, probabilities);
                    }

                    var tempScores = new double[scores.Length];
                    Array.Copy(scores, tempScores, scores.Length);

                    Array.Sort(tempScores);
                    double minimum = tempScores[Math.Max(0, scores.Length - Size)];

                    for (int currentScore = 0; currentScore < scores.Length; currentScore++)
                    {
                        if (scores[currentScore] < minimum)
                        {
                                continue; //only advance first "size" outcomes
                        }

                        string outcomeName = Model.GetOutcomeName(currentScore);
                        if (ValidSequence(currentSequence, sequence, outcomes, outcomeName))
                        {
                            var newSequence = new Sequence(topSequence, outcomeName, scores[currentScore]);
                            if (newSequence.Score > minSequenceScore)
                            {
                                nextHeap.Add(newSequence);
                            }
                        }
                    }
                    if (nextHeap.Size == 0)
                    {
                        //if no advanced sequences, advance all valid
                        for (int currentScore = 0; currentScore < scores.Length; currentScore++)
                        {
                            string outcomeName = Model.GetOutcomeName(currentScore);
                            if (ValidSequence(currentSequence, sequence, outcomes, outcomeName))
                            {
                                var newSequence = new Sequence(topSequence, outcomeName, scores[currentScore]);
                                if (newSequence.Score > minSequenceScore)
                                {
                                    nextHeap.Add(newSequence);
                                }
                            }
                        }
                    }
                    //nextHeap.Sort();
                }
                //    make prev = next; and re-init next (we reuse existing prev set once we clear it)
                previousHeap.Clear();
                ListHeap<Sequence> tempHeap = previousHeap;
                previousHeap = nextHeap;
                nextHeap = tempHeap;
            }
            int topSequenceCount = Math.Min(numSequences, previousHeap.Size);
            var topSequences = new Sequence[topSequenceCount];
            int sequenceIndex = 0;
            for (; sequenceIndex < topSequenceCount; sequenceIndex++)
            {
                topSequences[sequenceIndex] = (Sequence) previousHeap.Extract();
            }
            return topSequences;
        }

        /// <summary>
        /// Returns the best sequence of outcomes based on model for this object.
        /// </summary>
        /// <param name="sequence">The input sequence</param>
        /// <param name="additionalContext">
        /// An object[] of additional context.
        /// This is passed to the context generator blindly with the assumption that the context are appropiate.
        /// </param>
        /// <returns>The top ranked sequence of outcomes</returns>
        public Sequence BestSequence(string[] sequence, object[] additionalContext)
        {
            return BestSequences(1, sequence, additionalContext, ZeroLog)[0];
        }

        /// <summary>
        /// Determines whether a particular continuation of a sequence is valid.  
        /// This is used to restrict invalid sequences such as thoses used in start/continue tag-based chunking 
        /// or could be used to implement tag dictionary restrictions.
        /// </summary>
        /// <param name="index">The index in the input sequence for which the new outcome is being proposed</param>
        /// <param name="inputSequence">The input sequnce</param>
        /// <param name="outcomesSequence">The outcomes so far in this sequence</param>
        /// <param name="outcome">The next proposed outcome for the outcomes sequence</param>
        /// <returns>true if the sequence would still be valid with the new outcome, false otherwise</returns>
        protected internal virtual bool ValidSequence(int index, ArrayList inputSequence, Sequence outcomesSequence,
            string outcome)
        {
            return true;
        }

        /// <summary>
        /// Determines whether a particular continuation of a sequence is valid.  
        /// This is used to restrict invalid sequences such as thoses used in start/continure tag-based chunking 
        /// or could be used to implement tag dictionary restrictions.
        /// </summary>
        /// <param name="index">The index in the input sequence for which the new outcome is being proposed</param>
        /// <param name="inputSequence">The input sequnce</param>
        /// <param name="outcomesSequence">The outcomes so far in this sequence</param>
        /// <param name="outcome">The next proposed outcome for the outcomes sequence</param>
        /// <returns>true if the sequence would still be valid with the new outcome, false otherwise</returns>
        protected internal virtual bool ValidSequence(int index, object[] inputSequence, string[] outcomesSequence,
            string outcome)
        {
            return true;
        }
    }
}