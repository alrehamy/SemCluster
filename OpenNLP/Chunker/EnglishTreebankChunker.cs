using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OpenNLP.Chunker
{
    /// <summary>
    /// This is a chunker based on the CONLL chunking task which uses Penn Treebank constituents as the basis for the chunks.
    /// See   http://cnts.uia.ac.be/conll2000/chunking/ for data and task definition.
    /// </summary>
    public class EnglishTreebankChunker : MaximumEntropyChunker
    {
        /// <summary>
        /// Creates an English Treebank Chunker which uses the specified model file.
        /// </summary>
        /// <param name="modelFile">
        /// The name of the maxent model to be used.
        /// </param>
        public EnglishTreebankChunker(string modelFile)
            : base(new SharpEntropy.GisModel(new SharpEntropy.IO.BinaryGisModelReader(modelFile)))
        {
        }

        /// <summary>
        /// This method determines whether the outcome is valid for the preceding sequence.  
        /// This can be used to implement constraints on what sequences are valid.  
        /// </summary>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        /// <param name="sequence">
        /// The preceding sequence of outcome assignments. 
        /// </param>
        /// <returns>
        /// true if the outcome is valid for the sequence, false otherwise.
        /// </returns>
        protected internal override bool ValidOutcome(string outcome, Util.Sequence sequence)
        {
            if (outcome.StartsWith("I-"))
            {
                string[] tags = sequence.Outcomes.ToArray();
                int lastTagIndex = tags.Length - 1;
                if (lastTagIndex == -1)
                {
                    return (false);
                }
                else
                {
                    string lastTag = tags[lastTagIndex];
                    if (lastTag == "O")
                    {
                        return false;
                    }
                    if (lastTag.Substring(2) != outcome.Substring(2))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Gets formatted chunk information for a specified sentence.
        /// </summary>
        /// <param name="tokens">
        /// string array of tokens in the sentence
        /// </param>
        /// <param name="tags">
        /// string array of POS tags for the tokens in the sentence
        /// </param>
        /// <returns>
        /// A string containing the formatted chunked sentence
        /// </returns>
        public List<SentenceChunk> GetChunks(string[] tokens, string[] tags)
        {
            var results = new List<SentenceChunk>();

            string[] chunks = Chunk(tokens, tags);
            SentenceChunk currentSentenceChunk = null;
            for (int currentChunk = 0, chunkCount = chunks.Length; currentChunk < chunkCount; currentChunk++)
            {
                if (chunks[currentChunk].StartsWith("B-") || chunks[currentChunk] == "O")
                {
                    if (currentSentenceChunk != null)
                    {
                        results.Add(currentSentenceChunk);
                    }

                    var index = results.Count;
                    if (chunks[currentChunk].Length > 2)
                    {
                        var tag = chunks[currentChunk].Substring(2);
                        currentSentenceChunk = new SentenceChunk(tag, index);
                    }
                    else
                    {
                        currentSentenceChunk = new SentenceChunk(index);
                    }
                }

                var word = tokens[currentChunk];
                var wTag = tags[currentChunk];

                // Filter out specific words from chunking results
                if (wTag[0] == 'N' || wTag[0] == 'J')
                    if (FrequentWordsList.ContainsKey(word.ToLower()))
                        continue;

                if (currentSentenceChunk == null)
                {
                    currentSentenceChunk = new SentenceChunk(0);
                }
                
                var wIndex = currentSentenceChunk.TaggedWords.Count;
                var taggedWord = new TaggedWord(word, wTag, wIndex);
                currentSentenceChunk.TaggedWords.Add(taggedWord);
            }
            // add last chunk
            results.Add(currentSentenceChunk);

            return results;
        }

        /// <summary>
        /// Gets formatted chunk information for a specified sentence.
        /// </summary>
        /// <param name="data">
        /// a string containing a list of tokens and tags, separated by / characters. For example:
        /// Battle-tested/JJ Japanese/NNP industrial/JJ managers/NNS 
        /// </param>
        /// <returns>
        /// A string containing the formatted chunked sentence
        /// </returns>
        public List<SentenceChunk> GetChunks(string data)
        {
            string[] tokenAndTags = data.Split(' ');
            var tokens = new string[tokenAndTags.Length];
            var tags = new string[tokenAndTags.Length];
            for (int currentTokenAndTag = 0, tokenAndTagCount = tokenAndTags.Length; currentTokenAndTag < tokenAndTagCount; currentTokenAndTag++)
            {
                string[] tokenAndTag = tokenAndTags[currentTokenAndTag].Split('/');
                tokens[currentTokenAndTag] = tokenAndTag[0];
                tags[currentTokenAndTag] = tokenAndTag.Length > 1 ? tokenAndTag[1] : PartsOfSpeech.SentenceFinalPunctuation;
            }

            return GetChunks(tokens, tags);
        }

        private static Dictionary<string, int> FrequentWordsList = new Dictionary<string, int>() {
            // Common N*
            { "thing", 0 },
            { "one", 0 },
            { "ones", 0 },
            { "someone", 0 },
            { "somebody", 0 },
            { "everybody", 0 },
            { "everyone", 0 },
            { "today", 0 },
            { "yesterday", 0 },
            { "tomorrow", 0 },
            { "oneday", 0 },
            { "someday", 0 },
            { "somedays", 0 },
            { "month", 0 },
            { "months", 0 },
            { "week", 0 },
            { "year", 0 },
            { "years", 0 },
            { "dear", 0 },
            { "dears", 0 },
            { "regards", 0 },
            { "wishes", 0 },
            { "result", 0 },
            { "results", 0 },
            { "goal", 0 },
            { "goals", 0 },
            // Common J*
            { "certain", 0},
            { "new", 0},
            { "further", 0},
            { "such", 0},
            { "other", 0},
            { "many", 0 },
            { "few", 0 },
            { "simple", 0 },
            { "own", 0 },
            { "final", 0 },
            { "brief", 0 },
            { "baseline", 0 },
            { "previous", 0 },
            // Common Abb
            { "eg", 0 },
            { "e.g", 0 },
            { "e.g.", 0 },
            { "i.e.", 0 },
            { "et.", 0 },
            { "al.", 0 },
            { "w.r.t", 0 },
            { "w.r.t.", 0 },
            { "ex.", 0 },
            { "ex:", 0 },
            { "aka", 0 },
            { "aka.", 0 }
        };
    }
}
