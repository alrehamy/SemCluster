using System;
using System.Collections.Generic;
using System.Text;
using OpenNLP;
using OpenNLP.Chunker;
using OpenNLP.PosTagger;
using OpenNLP.SentenceDetecter;
using OpenNLP.Tokenizer;
using WordNetApi.Core;
using System.Linq;
using System.Reflection;

namespace TextAnalytics
{
    class SemCluster
    {
        // http://bdewilde.github.io/blog/2014/09/23/intro-to-automatic-keyphrase-extraction/
        // http://www.cs.technion.ac.il/~gabr/resources/code/esa/esa.html#references
        // http://people.kmi.open.ac.uk/petr/#projects
        // https://stackoverflow.com/questions/14479074/c-sharp-reflection-load-assembly-and-invoke-a-method-if-it-exists
        // https://stackoverflow.com/questions/29151096/error-in-babelnet-2-5-path-index-configuration

        // SemCluster Configurations
        public double ClosenessToCentroid = 0.7;  // Threshold represents the closeness between cluster seed and other members
        public int ContextWindowSize = 10;          // Word Sense Disambiguation context window size
        public int SelectionWindowSize;             // Threshold represents the window of terms selection
        public bool InMemoryWordNet = false;        // Whether to load WordNet ontology in RAM (faster access) 
        public bool TokenizeHyphen = false;         // Whether to tokenize terms contain hyphen

        // Text Preprocessing Related
        private MaximumEntropySentenceDetector _sentenceDetector;
        private AbstractTokenizer _tokenizer;
        private EnglishMaximumEntropyPosTagger _posTagger;
        private EnglishTreebankChunker _chunker;

        // WordNet Related
        private WordNetEngine _wn;      // WordNet engine instance
        private SynSet RootVirtualNode; // WordNet root node (for similarity computation)
        private WordNetEngine.SynSetRelation[] SynSetRelationTypes; // Semantic relations allowed to scan for LCS computation

        // External KB PlugIns Related
        private int PlugInsNumber;  // Total number of plugins in the PlugIn-Repository
        private List<MethodInfo> KBDriversQueryPointers;      // Instances of the default method Query(WordNetEngine,term)
        private List<object> KBDrivers;          // Instances of each PlugIn as an object
        private object[] KBDriverQueryArgs;    // Array of arguments passed to Query(WordNetEngine,term)

        // Affinity Propagation Implementation Class (C++)
        private AffinityPropagationClustering ap;

        // Main Global Data Structure
        private List<TaggedWord> TextVectors;

        // List of allowed stop-words in entity spans
        private static Dictionary<string, int> AllowedDTList = new Dictionary<string, int>() { { "of", 0 }, { "Of", 0 }, { "the", 0 }, { "The", 0 } };





        // Defualt Constructor
        public SemCluster(string DataFolder)
        {
            try
            {
                Console.WriteLine("\tSemCluster Text Analytics Tool");
                Console.WriteLine("\t------------------------------");
                Console.WriteLine("\t-Wikipedia local server couldn't be found!");
                Console.WriteLine("\t-Seeds SemAve is in manual mode!");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("-> Resources loading ...");
                Console.WriteLine();

                #region Loading External Resources
                _wn = new WordNetEngine(DataFolder + "WordNet", InMemoryWordNet);
                _tokenizer = new EnglishRuleBasedTokenizer(TokenizeHyphen);
                _sentenceDetector = new EnglishMaximumEntropySentenceDetector(DataFolder + "EnglishSD.nbin");
                _posTagger = new EnglishMaximumEntropyPosTagger(DataFolder + "EnglishPOS.nbin", DataFolder + "\\Build\\tagdict");
                _chunker = new EnglishTreebankChunker(DataFolder + "EnglishChunk.nbin");
                #endregion

                PlugInsManager(DataFolder);

                Console.WriteLine("\tResources loaded successfully");
                Console.WriteLine("\t" + PlugInsNumber + " KB plug-ins found in the repository");
                Console.WriteLine("\tPress any key to continue ...");
                Console.ReadKey();
                Console.WriteLine();

                RootVirtualNode = _wn.GetSynSet("Noun:1740");
                ap = new AffinityPropagationClustering();

                SynSetRelationTypes = new WordNetApi.Core.WordNetEngine.SynSetRelation[2];
                SynSetRelationTypes[0] = WordNetApi.Core.WordNetEngine.SynSetRelation.Hypernym;
                SynSetRelationTypes[1] = WordNetApi.Core.WordNetEngine.SynSetRelation.InstanceHypernym;
            }
            catch (Exception ex)
            {
                Dispose();
                throw new Exception(ex.Message);
            }
        }

        private void PlugInsManager(string DataFolder)
        {
            // In-Memory instance pointers list
            KBDrivers = new List<object>();
            KBDriversQueryPointers = new List<MethodInfo>();
            // The default signture of Query method
            Type[] KBDriverQuerySignture = new Type[2];
            KBDriverQuerySignture[0] = typeof(WordNetEngine);
            KBDriverQuerySignture[1] = typeof(string);
            // The arguemnts passed to Query method
            KBDriverQueryArgs = new object[2];
            KBDriverQueryArgs[0] = _wn;

            // Read dll files stored in the special folder "PlugIns"
            // Filter only those start with "Plug"
            string[] PlugIns = System.IO.Directory
                .GetFiles(DataFolder + "PlugIns", "*.dll")
                .Select(System.IO.Path.GetFileName)
                .Where(PlugInName => PlugInName.StartsWith("Plug"))
                .ToArray();

            // Create Instance for each PlugIn
            for (int i = 0; i < PlugIns.Length; i++)
            {
                Type PlugInType = Assembly.LoadFrom(DataFolder + "PlugIns\\" + PlugIns[i]).GetType(PlugIns[i].Substring(0, PlugIns[i].Length - 4) + ".Driver");
                if (PlugInType != null)
                {
                    KBDrivers.Add(Activator.CreateInstance(PlugInType));
                    KBDriversQueryPointers.Add(PlugInType.GetMethod("Query", KBDriverQuerySignture));
                }
            }

            PlugInsNumber = KBDrivers.Count;
        }

        public string ShallowParse(string input)
        {
            var output = new StringBuilder();
            string[] sentences = _sentenceDetector.SentenceDetect(input);
            foreach (string sentence in sentences)
            {
                string[] tokens = _tokenizer.Tokenize(sentence);
                string[] tags = _posTagger.Tag(tokens);
                output.Append(string.Join(" ", _chunker.GetChunks(tokens, tags)));
                output.Append("\r\n\r\n");
            }
            return output.ToString();
        }

        public void Extract(string text_segment)
        {
            if (!string.IsNullOrEmpty(text_segment))
            {
                #region Local Variables

                int i = 0;
                int j;
                int k;
                int d;
                int l;
                int chunkLength;
                int chunksLength;
                string curToken;
                List<SynSet> Senses, tmpSenses;
                SynSet tmpSense;

                List<SentenceChunk> Chunks = new List<SentenceChunk>(); // This list of all chunks
                List<SentenceChunk> tmpChunks = new List<SentenceChunk>(); // This list of all chunks
                Dictionary<string, SynSet> CachedConcepts = new Dictionary<string, SynSet>();
                TextVectors = new List<TaggedWord>();                   // The list that will hold all mappable terms with thier information
                List<string> MiscTerms = new List<string>();            // The list of unmapped terms in the text
                string[] tokens;
                string[] sentences = _sentenceDetector.SentenceDetect(text_segment);

                #endregion

                #region Section 3.1.

                // Extract all chunks from the given text segment
                for (k = 0; k < sentences.Length; k++)
                {
                    tokens = _tokenizer.Tokenize(sentences[k]);
                    tmpChunks = _chunker.GetChunks(tokens, _posTagger.Tag(tokens));
                    tmpChunks.RemoveAll(predicate => predicate.TaggedWords.Count == 0);
                    Chunks.AddRange(tmpChunks);
                }

                tmpChunks = null;
                tokens = null;
                sentences = null;

                // Extract elements that will be used for Similarity Matrix Generation as the input of clustering
                chunksLength = Chunks.Count;
                while (i < chunksLength)
                {
                    // Look only inside NP chunks
                    if (Chunks[i].Tag == "NP")
                    {
                        #region Rectify NP Chunks
                        if (i + 1 < chunksLength)
                        {
                            if (Chunks[i + 1].Tag == "NP")
                            {
                                if (Chunks[i + 1].TaggedWords[0].Tag.StartsWith("NNP") || AllowedDTList.ContainsKey(Chunks[i + 1].TaggedWords[0].Word))
                                {
                                    int length = Chunks[i].TaggedWords.Count;
                                    foreach (TaggedWord w in Chunks[i + 1].TaggedWords)
                                    {
                                        w.Index = length;
                                        Chunks[i].TaggedWords.Add(w);
                                        length++;
                                    }

                                    Chunks.RemoveRange(i + 1, 1);
                                    chunksLength = chunksLength - 1;
                                }
                            }
                            else
                                if (Chunks[i + 1].Tag == "PP" && i + 2 < chunksLength)
                                {
                                    if (Chunks[i + 2].TaggedWords[0].Tag.StartsWith("NNP") || AllowedDTList.ContainsKey(Chunks[i + 1].TaggedWords[0].Word))
                                    {
                                        int length = Chunks[i].TaggedWords.Count;
                                        Chunks[i + 1].TaggedWords[0].Index = length;
                                        Chunks[i].TaggedWords.Add(Chunks[i + 1].TaggedWords[0]);
                                        length++;
                                        foreach (TaggedWord w in Chunks[i + 2].TaggedWords)
                                        {
                                            w.Index = length;
                                            length++;
                                            Chunks[i].TaggedWords.Add(w);
                                        }

                                        Chunks.RemoveRange(i + 1, 2);
                                        chunksLength = chunksLength - 2;
                                    }
                                }
                        }
                        #endregion

                        #region Find N-Gram NNPs

                        // This part is very important:
                        // 1- Rectify any linguistic errors generated as side effect of the previous step (such as "Belly the")
                        // 2- Eliminate any syntactic errors such as Texas Rangers (sports) --> Texas Ranger (Police)
                        // since we don't alter the value of a NNP(s)

                        chunkLength = Chunks[i].TaggedWords.Count;
                        j = 0;
                        // Loop through all chunk words
                        while (j < chunkLength)
                        {
                            if (Chunks[i].TaggedWords[j].Tag[0] == 'N')
                            {
                                // Needed for fast access to the last element in SemanticElements
                                d = TextVectors.Count() - 1;

                                // Check the probability of merging N-gram Named Entities (NNP(S)* || NNP(S)*|DT*|NNP(S)*)
                                if (Chunks[i].TaggedWords[j].Tag.StartsWith("NNP"))
                                {
                                    k = 0;

                                    // First scan to see if the pattern is satisfied
                                    for (l = j + 1; l < chunkLength; l++)
                                        // Here to define any patterns the user may wish to apply
                                        if (
                                            Chunks[i].TaggedWords[l].Tag.StartsWith("NNP") // allow N-Gram NNP
                                            || AllowedDTList.ContainsKey(Chunks[i].TaggedWords[l].Word) // allow adding stop words inside the NNP
                                            || Chunks[i].TaggedWords[l].Tag == "CD" // allow adding numbers inside NNP
                                            )
                                            k++;
                                        else
                                            break;
                                    // k-value changing means a pattern has been found
                                    // if k is changed and the scanned pattern does not end with a stop word
                                    if (k > 0 && !AllowedDTList.ContainsKey(Chunks[i].TaggedWords[j + k].Word))
                                    {
                                        // Concatenate all the pattern parts ans store them in temp variable
                                        curToken = Chunks[i].TaggedWords[j].Word;
                                        for (l = j + 1; l <= j + k; l++)
                                        {
                                            curToken = curToken + " " + Chunks[i].TaggedWords[l].Word;
                                        }

                                        // Delete all the parts added in temp
                                        Chunks[i].TaggedWords.RemoveRange(j + 1, k);

                                        // rectify the sequence length after deletion
                                        chunkLength = chunkLength - k;


                                        // Check if the perv token is a capitalized JJ
                                        if (d > -1 && j > 0 && TextVectors[d].Tag == "JJ" && char.IsUpper(TextVectors[d].Word[0]))
                                        {
                                            // Replace current j with its previous j-1 word, and allocate special tag NNP*J
                                            Chunks[i].TaggedWords[j - 1].Tag = Chunks[i].TaggedWords[j].Tag + "J";
                                            Chunks[i].TaggedWords[j - 1].Word = TextVectors[d].Word + " " + curToken;
                                            // Remove the previous word from all lists
                                            TextVectors.RemoveAt(d);
                                            Chunks[i].TaggedWords.RemoveRange(j, 1);
                                            chunkLength--;
                                            j--;
                                        }
                                        else
                                            // Only update the current word
                                            Chunks[i].TaggedWords[j].Word = curToken;

                                        TextVectors.Add(Chunks[i].TaggedWords[j]);
                                        // Skip the loop by k steps
                                        j = j + k;
                                    }
                                    else
                                    {
                                        // If there is no pattern match --> add singular NNP(S)
                                        // Before addition check JJ pattern
                                        if (d > -1 && j > 0 && TextVectors[d].Tag == "JJ" && char.IsUpper(TextVectors[d].Word[0]))
                                        {
                                            // Replace current j with its previous j-1 word, and allocate special tag NNP*J
                                            Chunks[i].TaggedWords[j - 1].Tag = Chunks[i].TaggedWords[j].Tag + "J";
                                            Chunks[i].TaggedWords[j - 1].Word = TextVectors[d].Word + " " + Chunks[i].TaggedWords[j].Word;
                                            // Remove the previous word from all lists
                                            TextVectors.RemoveAt(d);
                                            Chunks[i].TaggedWords.RemoveRange(j, 1);
                                            chunkLength--;
                                            j--;

                                        }

                                        TextVectors.Add(Chunks[i].TaggedWords[j]);
                                        j++;
                                    }
                                }
                                else
                                {
                                    // If the current word is NN(S)
                                    if (Chunks[i].TaggedWords[j].Tag == "NNS")
                                        Chunks[i].TaggedWords[j].Word = _wn.Lemmatize(Chunks[i].TaggedWords[j].Word, "noun");

                                    // Find if the current token forms bigram WordNet concept with the previous token
                                    if (j > 0)
                                        if (Chunks[i].TaggedWords[j - 1].Tag == "NN" || Chunks[i].TaggedWords[j - 1].Tag == "NNS" || Chunks[i].TaggedWords[j - 1].Tag == "JJ")
                                            if (_wn.GetSynSets(Chunks[i].TaggedWords[j - 1].Word + "_" + Chunks[i].TaggedWords[j].Word, "noun").Count > 0)
                                            {
                                                Chunks[i].TaggedWords[j].Word = Chunks[i].TaggedWords[j - 1].Word + "_" + Chunks[i].TaggedWords[j].Word;
                                                Chunks[i].TaggedWords[j].Index = Chunks[i].TaggedWords[j - 1].Index;
                                                Chunks[i].TaggedWords.RemoveRange(j - 1, 1);
                                                TextVectors.RemoveAt(d);
                                                j--;
                                                chunkLength--;
                                            }

                                    TextVectors.Add(Chunks[i].TaggedWords[j]);
                                    j++;
                                }
                            }
                            else
                            {
                                if (Chunks[i].TaggedWords[j].Tag[0] == 'J')
                                    // We add adjectives to increase the disambiguation accuracy 
                                    TextVectors.Add(Chunks[i].TaggedWords[j]);
                                // Skip any chunk element that is not NNP(S),NN(S), or JJ(*)
                                j++;
                            }
                        }
                        #endregion

                        i++;
                    }
                    else
                    {
                        // Remove the current Chunk since it was checked during rectification phase of the previous step
                        // Keeping only NPs is for efficiency reason during the last step of the algorithm
                        Chunks.RemoveRange(i, 1);
                        chunksLength--;
                    }
                }

                #region Disambiguatation

                d = TextVectors.Count;
                // Normalize NNP* vectors before the actual disambiguatation
                // Performing normalization after disambiguatation may affects caching the concepts since the keys may change
                for (i = 0; i < d; i++)
                    if (TextVectors[i].Tag.StartsWith("NNP"))
                        for (j = 0; j < d; j++)
                            if (TextVectors[j].Tag.StartsWith("NNP"))
                                if (TextVectors[i].Word.Contains(TextVectors[j].Word))
                                {
                                    TextVectors[j].Word = TextVectors[i].Word;
                                    TextVectors[j].Tag = TextVectors[i].Tag;

                                }
                                else
                                    if (TextVectors[j].Word.Contains(TextVectors[i].Word))
                                    {
                                        TextVectors[i].Word = TextVectors[j].Word;
                                        TextVectors[i].Tag = TextVectors[j].Tag;
                                    }



                for (i = 0; i < d; i++)
                {
                    // For limiting access to the list -- Efficiency 
                    curToken = TextVectors[i].Word;
                    if (TextVectors[i].Tag == "NN" || TextVectors[i].Tag == "NNS")
                    {
                        if (CachedConcepts.ContainsKey(curToken))
                            TextVectors[i].Sense = CachedConcepts[curToken];
                        else
                        {
                            // Check availability in WordNet
                            Senses = _wn.GetSynSets(curToken, false, WordNetEngine.POS.Noun);
                            if (Senses.Count > 0)
                            {
                                tmpSense = Disambiguate(Senses, GenerateContextWindow(i, d));
                                CachedConcepts.Add(curToken, tmpSense);
                                TextVectors[i].Sense = CachedConcepts[curToken];
                            }
                        }
                    }
                    else
                        if (TextVectors[i].Tag.StartsWith("NNP"))
                        {
                            if (CachedConcepts.ContainsKey(curToken))
                            {
                                TextVectors[i].Sense = CachedConcepts[curToken];
                                continue;
                            }

                            Senses = _wn.GetSynSets(curToken.Replace(" ", "_"), false, WordNetEngine.POS.Noun);
                            if (Senses.Count > 0)
                            {
                                tmpSense = Disambiguate(Senses, GenerateContextWindow(i, d));
                                CachedConcepts.Add(curToken, tmpSense);
                                TextVectors[i].Sense = CachedConcepts[curToken];
                                continue;
                            }

                            if (PlugInsNumber > 0)
                            {
                                Senses.Clear();
                                for (l = 0; l < PlugInsNumber; l++)
                                {
                                    KBDriverQueryArgs[1] = curToken;
                                    tmpSenses = KBDriversQueryPointers[l].Invoke(KBDrivers[l], KBDriverQueryArgs) as List<SynSet>;
                                    if (tmpSenses != null)
                                        Senses.AddRange(tmpSenses);
                                }

                                if (Senses.Count > 0)
                                {
                                    tmpSense = Disambiguate(Senses, GenerateContextWindow(i, d));
                                    CachedConcepts.Add(curToken, tmpSense);
                                    TextVectors[i].Sense = CachedConcepts[curToken];
                                    continue;
                                }
                            }

                            if (TextVectors[i].Tag.EndsWith("J"))
                            {
                                TextVectors[i].Word = curToken.Substring(curToken.IndexOf(" ") + 1);
                                TextVectors[i].Tag = TextVectors[i].Tag.Substring(0, TextVectors[i].Tag.Length - 1);
                                i--;
                                continue;
                            }
                        }
                }


                // Prepare the vectors for semantic similarity measurement
                // hence, any vector does not hold valid sense must be excluded from the list in temp list
                i = 0;
                while (i < d)
                {
                    if (TextVectors[i].Sense == null)
                    {
                        if (TextVectors[i].Tag.StartsWith("NNP") && !MiscTerms.Contains(TextVectors[i].Word))
                            MiscTerms.Add(TextVectors[i].Word);
                        TextVectors.RemoveAt(i);
                        d--;
                    }
                    else
                        i++;
                }
                #endregion
                // [Implicit-Dispose]
                tmpSense = null;
                tmpSenses = null;
                Senses = null;

                #endregion

                #region Section 3.2.

                // Row * Col - Diagonal / 2 (above or under the Diagonal)
                double[] S = new double[((d * d) - d) / 2];
                // Dummy counter
                k = 0;
                for (i = 0; i < d; i++)
                    for (j = i + 1; j < d; j++)
                    {
                        S[k] = Math.Round(wupMeasure(TextVectors[i].Sense, TextVectors[j].Sense), 4);
                        k++;
                    }

                // Perform clustering on S
                int[] res = ap.Run(S, d, 1, 0.9, 1000, 50);

                // Optimized Clustering information collection
                // We collect clustering information and at the same time filter out all terms that are not close to their exemplars
                Dictionary<int, List<int>> ClusRes = new Dictionary<int, List<int>>();
                // ===================================
                for (i = 0; i < res.Length; i++)
                {
                    if (!ClusRes.ContainsKey(res[i]))
                        ClusRes.Add(res[i], new List<int>());

                    if (i == res[i])
                    {
                        ClusRes[res[i]].Add(i);
                        continue;
                    }

                    if (Math.Round(wupMeasure(TextVectors[res[i]].Sense, TextVectors[i].Sense), 4) >= ClosenessToCentroid)
                        ClusRes[res[i]].Add(i);
                }

                Console.WriteLine("-> Clustering Information:\n");
                foreach (KeyValuePair<int, List<int>> kv in ClusRes)
                {
                    Console.Write("\t[" + TextVectors[kv.Key].Word + "] " + TextVectors[kv.Key].Sense.ID + " : ");
                    foreach (var item in kv.Value)
                    {
                        Console.Write(TextVectors[item].Word + ",");
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }

                // Manual averaging of exemplars (Sec. 3.2)
                Console.WriteLine("-> Remove unimportant clusters:");
                bool delFlag;
                while (true)
                {
                    delFlag = false;
                    Console.Write("\tEnter Seed:");
                    curToken = Console.ReadLine();
                    if (curToken == "$")
                        break;

                    foreach (var key in ClusRes.Keys)
                        if (TextVectors[key].Word == curToken)
                        {
                            delFlag = ClusRes.Remove(key);
                            break;
                        }
                    if (delFlag)
                        Console.WriteLine("\tCluster deleted");
                    else
                        Console.WriteLine("\tSeed is not found");
                    Console.WriteLine();
                }

                // ESA-Based averaging of exemplars
                // Insert here local server API

                #endregion

                #region Section 3.3.

                // Flatten ClusRes into List
                List<int> Seeds = ClusRes.Values
                     .SelectMany(x => x)  // Flatten
                     .ToList();

                // Final seeds list must be sorted in case of using candidate phrase selection from a window
                //Seeds.Sort();

                List<string> CandidatePhrases = new List<string>();
                List<string> CandidatePhraseSeed = new List<string>();

                SelectionWindowSize = Chunks.Count;
                for (i = 0; i < Chunks.Count; i++)
                    if (Chunks[i].Tag == "NP")
                    {
                        d = Chunks[i].TaggedWords.Count;
                        for (l = 0; l < Seeds.Count; l++)
                        {
                            for (j = 0; j < d; j++)
                            {
                                if (Chunks[i].TaggedWords[j].Word == TextVectors[Seeds[l]].Word)
                                {
                                    if (TextVectors[Seeds[l]].Tag.StartsWith("NNP") && !CandidatePhrases.Contains(TextVectors[Seeds[l]].Word) && i < SelectionWindowSize)
                                    {
                                        CandidatePhrases.Add(TextVectors[Seeds[l]].Word);
                                        if (TextVectors[Seeds[l]].Sense.URI != null)
                                            CandidatePhraseSeed.Add(TextVectors[Seeds[l]].Sense.URI);
                                        else
                                            CandidatePhraseSeed.Add("http://www.pdl.io/core_onto/" + TextVectors[Seeds[l]].Sense.ID);
                                    }
                                    else
                                        if (TextVectors[Seeds[l]].Tag == "NN" || TextVectors[Seeds[l]].Tag == "NNS")
                                        {
                                            curToken = TextVectors[Seeds[l]].Word;
                                            if (j > 0 && Chunks[i].TaggedWords[j - 1].Tag == "JJ")
                                            {
                                                curToken = Chunks[i].TaggedWords[j - 1].Word + " " + curToken;
                                            }

                                            for (k = j + 1; k < d; k++)
                                            {
                                                if (Chunks[i].TaggedWords[k].Tag != "NN")
                                                    break;
                                                else
                                                    curToken = curToken + " " + Chunks[i].TaggedWords[k].Word;
                                            }

                                            if (curToken.Contains(" ") || curToken.Contains("_"))
                                                if (!CandidatePhrases.Contains(curToken))
                                                {
                                                    CandidatePhrases.Add(curToken);
                                                    if (TextVectors[Seeds[l]].Sense.URI != null)
                                                        CandidatePhraseSeed.Add(TextVectors[Seeds[l]].Sense.URI);
                                                    else
                                                        CandidatePhraseSeed.Add("http://www.pdl.io/core_onto/" + TextVectors[Seeds[l]].Sense.ID);
                                                }
                                        }
                                }
                            }
                        }
                    }

                #endregion

                // Print results
                Console.WriteLine("\n-> Candidate Keyphrases:\n");
                for (i = 0; i < CandidatePhrases.Count; i++)
                    Console.WriteLine("\t" + CandidatePhrases[i].Replace("_", " ") + " , URI:" + CandidatePhraseSeed[i]);

                Console.WriteLine("\n-> MISC Entities:\n");
                for (i = 0; i < MiscTerms.Count; i++)
                    Console.WriteLine("\t" + MiscTerms[i]);

            }
        }

        private string[] GenerateContextWindow(int i, int vocabulary)
        {
            StringBuilder context = new StringBuilder();

            if (ContextWindowSize > vocabulary)
                ContextWindowSize = vocabulary;

            int lk = 0;
            int l = i;
            while (l > 0 && lk < ContextWindowSize)
            {
                l--;
                if (TextVectors[l].Tag[0] == 'N')
                {
                    if (TextVectors[l].Sense != null)
                    {
                        context.Append(string.Join(" ", TextVectors[l].Sense.Synonyms)).Append(" ");
                        context.Append(TextVectors[l].Sense.Gloss).Append(" ");
                    }
                    else
                        context.Append(TextVectors[l].Word).Append(" ");
                    lk++;
                }
                else
                    context.Append(TextVectors[l].Word).Append(" ");
            }

            int r = i + 1;
            int lr = 0;
            while (r < vocabulary && lr < ContextWindowSize)
            {
                context.Append(TextVectors[r].Word).Append(" ");
                r++;
                lr++;
            }
            return (
                    _tokenizer.Tokenize(
                                        context
                                        .ToString()
                                        .ToLower()
                                        .Replace("-", " ")));
        }

        private int Intersect(string[] t1, string[] t2)
        {
            return t1.Intersect(t2, StringComparer.OrdinalIgnoreCase).Count();
        }

        private SynSet Disambiguate(List<SynSet> Senses, string[] context)
        {
            int synSize = Senses.Count;
            List<SynSet> RelatedSenses;
            SynSet tmpSense = null;

            // Temp variable
            string senseData;
            // Traking the sense that maximizes the overlap score
            int overlap;
            int score = 0;

            //Rada Recommendation
            //if (synSize > 3)
            //    synSize = 3;

            if (synSize > 0)
            {
                for (int k = 0; k < synSize; k++)
                {
                    senseData = string.Join(" ", Senses[k].Synonyms) + " " + Senses[k].Gloss;
                    senseData = senseData.Replace("_", " ").Replace("-", " ");
                    overlap = Intersect(_tokenizer.Tokenize(senseData), context) * 60;


                    // strong relations
                    senseData = "";
                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.Hypernym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;
                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.Hyponym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;

                    overlap = overlap + Intersect(_tokenizer.Tokenize(senseData), context) * 20;



                    // weak relations
                    senseData = "";
                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.PartHolonym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;
                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.PartMeronym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;
                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.InstanceHypernym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;

                    RelatedSenses = Senses[k].GetRelatedSynSets(WordNetApi.Core.WordNetEngine.SynSetRelation.InstanceHyponym, false);
                    if (RelatedSenses.Count > 0)
                        foreach (SynSet syn in RelatedSenses)
                            senseData = senseData + string.Join(" ", syn.Synonyms) + " " + syn.Gloss;

                    overlap = overlap + Intersect(_tokenizer.Tokenize(senseData), context) * 5;

                    if (overlap > score)
                    {
                        score = overlap;
                        tmpSense = Senses[k];
                    }
                }
            }
            if (tmpSense == null)
            {
                tmpSense = Senses[0];
            }

            return tmpSense;
        }

        private double wupMeasure(SynSet Sense1, SynSet Sense2)
        {
            string str1, str2;
            if (Sense1 == Sense2)
                return 1;

            double similarity = 0;
            SynSet LCS = Sense1.GetClosestMutuallyReachableSynset(Sense2, SynSetRelationTypes);
            SynSet LCSAlternative = Sense2.GetClosestMutuallyReachableSynset(Sense1, SynSetRelationTypes);
            if (LCS == null || LCSAlternative == null)
                return 0;
            else
            {

                str1 = string.Join(" ", Sense1.Synonyms) + " " + Sense1.Gloss;
                str2 = string.Join(" ", Sense2.Synonyms) + " " + Sense2.Gloss;
                str1 = str1.Replace("_", " ").Replace("-", " ");
                str2 = str2.Replace("_", " ").Replace("-", " ");
                double overlap = 10 * Math.Log(1 + Intersect(_tokenizer.Tokenize(str1), _tokenizer.Tokenize(str2)));

                double LCS_Depth = LCS.GetShortestPathTo(RootVirtualNode, SynSetRelationTypes).Count + 1;
                double LCS_DepthAlter = LCSAlternative.GetShortestPathTo(RootVirtualNode, SynSetRelationTypes).Count + 1;

                if (LCS_Depth < LCS_DepthAlter)
                {
                    LCS = LCSAlternative;
                    LCS_Depth = LCS_DepthAlter;
                }

                similarity = ((2 * LCS_Depth) + overlap) /
                            (
                                (Sense1.GetShortestPathTo(LCS, SynSetRelationTypes).Count - 1 + LCS_Depth) +
                                (Sense2.GetShortestPathTo(LCS, SynSetRelationTypes).Count - 1 + LCS_Depth) +
                                overlap
                            );
            }

            return similarity;
        }

        // [Explicit-Dispose]
        public void Dispose()
        {
            if (_wn != null)
                _wn.Dispose();

            _tokenizer = null;
            _sentenceDetector = null;
            _posTagger = null;
            _chunker = null;

            // Dispose CLI/C++ Dll
            ap = null;

            // Dispose all KB plugins
            if (PlugInsNumber > 0)
                for (int i = 0; i < PlugInsNumber; i++)
                {
                    KBDrivers[i] = null;
                    KBDriversQueryPointers[i] = null;
                }
        }
    }
}