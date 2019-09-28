using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using WordNetApi.Lemmatizer;

namespace WordNetApi.Core
{
    /// <summary>
    /// Provides access to the WordNet resource via two alternative methods, in-memory and disk-based. The former is blazingly
    /// fast but also hugely inefficient in terms of memory consumption. The latter uses essentially zero memory but is slow
    /// because all searches have to be conducted on-disk.
    /// </summary>
    public class WordNetEngine
    {
        private string _wordNetDirectory;
        private bool _inMemory;
        private Dictionary<POS, BinarySearchTextStream> _posIndexWordSearchStream;  // disk-based search streams to get words from the index files
        private Dictionary<POS, StreamReader> _posSynSetDataFile;                   // disk-based synset data files
        private Dictionary<POS, Dictionary<string, List<SynSet>>> _posWordSynSets;   // in-memory pos-word synsets lookup
        private Dictionary<string, SynSet> _idSynset;                               // in-memory id-synset lookup where id is POS:Offset
        private Dictionary<string, StreamReader> LemmaExcptionsFile;

        #region static members
        /// <summary>
        /// SynSet relations
        /// </summary>
        public enum SynSetRelation
        {
            None,
            AlsoSee,
            Antonym,
            Attribute,
            Cause,
            DerivationallyRelated,
            DerivedFromAdjective,
            Entailment,
            Hypernym,
            Hyponym,
            InstanceHypernym,
            InstanceHyponym,
            MemberHolonym,
            MemberMeronym,
            PartHolonym,
            ParticipleOfVerb,
            PartMeronym,
            Pertainym,
            RegionDomain,
            RegionDomainMember,
            SimilarTo,
            SubstanceHolonym,
            SubstanceMeronym,
            TopicDomain,
            TopicDomainMember,
            UsageDomain,
            UsageDomainMember,
            VerbGroup,
        }

        /// <summary>
        /// WordNet parts-of-speech
        /// </summary>
        public enum POS
        {
            None,
            Noun,
            Verb,
            Adjective,
            Adverb
        }

        /// <summary>
        /// SynSet relation symbols that are available for each POS
        /// </summary>
        private static Dictionary<POS, Dictionary<string, SynSetRelation>> _posSymbolRelation;

        /// <summary>
        /// LexicographerFiles for synsets categorization
        /// </summary>
        public static string[] LexicographerFiles;


        /// <summary>
        /// Static constructor
        /// </summary>
        static WordNetEngine()
        {
            _posSymbolRelation = new Dictionary<POS, Dictionary<string, SynSetRelation>>();

            // noun relations
            Dictionary<string, SynSetRelation> nounSymbolRelation = new Dictionary<string, SynSetRelation>();
            nounSymbolRelation.Add("!", SynSetRelation.Antonym);
            nounSymbolRelation.Add("@", SynSetRelation.Hypernym);
            nounSymbolRelation.Add("@i", SynSetRelation.InstanceHypernym);
            nounSymbolRelation.Add("~", SynSetRelation.Hyponym);
            nounSymbolRelation.Add("~i", SynSetRelation.InstanceHyponym);
            nounSymbolRelation.Add("#m", SynSetRelation.MemberHolonym);
            nounSymbolRelation.Add("#s", SynSetRelation.SubstanceHolonym);
            nounSymbolRelation.Add("#p", SynSetRelation.PartHolonym);
            nounSymbolRelation.Add("%m", SynSetRelation.MemberMeronym);
            nounSymbolRelation.Add("%s", SynSetRelation.SubstanceMeronym);
            nounSymbolRelation.Add("%p", SynSetRelation.PartMeronym);
            nounSymbolRelation.Add("=", SynSetRelation.Attribute);
            nounSymbolRelation.Add("+", SynSetRelation.DerivationallyRelated);
            nounSymbolRelation.Add(";c", SynSetRelation.TopicDomain);
            nounSymbolRelation.Add("-c", SynSetRelation.TopicDomainMember);
            nounSymbolRelation.Add(";r", SynSetRelation.RegionDomain);
            nounSymbolRelation.Add("-r", SynSetRelation.RegionDomainMember);
            nounSymbolRelation.Add(";u", SynSetRelation.UsageDomain);
            nounSymbolRelation.Add("-u", SynSetRelation.UsageDomainMember);
            nounSymbolRelation.Add(@"\", SynSetRelation.DerivedFromAdjective);  // appears in WordNet 3.1
            _posSymbolRelation.Add(POS.Noun, nounSymbolRelation);

            // verb relations
            Dictionary<string, SynSetRelation> verbSymbolRelation = new Dictionary<string, SynSetRelation>();
            verbSymbolRelation.Add("!", SynSetRelation.Antonym);
            verbSymbolRelation.Add("@", SynSetRelation.Hypernym);
            verbSymbolRelation.Add("~", SynSetRelation.Hyponym);
            verbSymbolRelation.Add("*", SynSetRelation.Entailment);
            verbSymbolRelation.Add(">", SynSetRelation.Cause);
            verbSymbolRelation.Add("^", SynSetRelation.AlsoSee);
            verbSymbolRelation.Add("$", SynSetRelation.VerbGroup);
            verbSymbolRelation.Add("+", SynSetRelation.DerivationallyRelated);
            verbSymbolRelation.Add(";c", SynSetRelation.TopicDomain);
            verbSymbolRelation.Add(";r", SynSetRelation.RegionDomain);
            verbSymbolRelation.Add(";u", SynSetRelation.UsageDomain);
            _posSymbolRelation.Add(POS.Verb, verbSymbolRelation);

            // adjective relations
            Dictionary<string, SynSetRelation> adjectiveSymbolRelation = new Dictionary<string, SynSetRelation>();
            adjectiveSymbolRelation.Add("!", SynSetRelation.Antonym);
            adjectiveSymbolRelation.Add("&", SynSetRelation.SimilarTo);
            adjectiveSymbolRelation.Add("<", SynSetRelation.ParticipleOfVerb);
            adjectiveSymbolRelation.Add(@"\", SynSetRelation.Pertainym);
            adjectiveSymbolRelation.Add("=", SynSetRelation.Attribute);
            adjectiveSymbolRelation.Add("^", SynSetRelation.AlsoSee);
            adjectiveSymbolRelation.Add(";c", SynSetRelation.TopicDomain);
            adjectiveSymbolRelation.Add(";r", SynSetRelation.RegionDomain);
            adjectiveSymbolRelation.Add(";u", SynSetRelation.UsageDomain);
            adjectiveSymbolRelation.Add("+", SynSetRelation.DerivationallyRelated);  // not in documentation
            _posSymbolRelation.Add(POS.Adjective, adjectiveSymbolRelation);

            // adverb relations
            Dictionary<string, SynSetRelation> adverbSymbolRelation = new Dictionary<string, SynSetRelation>();
            adverbSymbolRelation.Add("!", SynSetRelation.Antonym);
            adverbSymbolRelation.Add(@"\", SynSetRelation.DerivedFromAdjective);
            adverbSymbolRelation.Add(";c", SynSetRelation.TopicDomain);
            adverbSymbolRelation.Add(";r", SynSetRelation.RegionDomain);
            adverbSymbolRelation.Add(";u", SynSetRelation.UsageDomain);
            adverbSymbolRelation.Add("+", SynSetRelation.DerivationallyRelated);  // not in documentation
            _posSymbolRelation.Add(POS.Adverb, adverbSymbolRelation);

            LexicographerFiles = new string[45];

            LexicographerFiles[0] = "adj.all - all adjective clusters";
            LexicographerFiles[1] = "adj.pert - relational adjectives (pertainyms)";
            LexicographerFiles[2] = "adv.all - all adverbs";
            LexicographerFiles[3] = "noun.Tops - unique beginners for nouns";
            LexicographerFiles[4] = "noun.act - nouns denoting acts or actions";
            LexicographerFiles[5] = "noun.animal - nouns denoting animals";
            LexicographerFiles[6] = "noun.artifact - nouns denoting man-made objects";
            LexicographerFiles[7] = "noun.attribute - nouns denoting attributes of people and objects";
            LexicographerFiles[8] = "noun.body - nouns denoting body parts";
            LexicographerFiles[9] = "noun.cognition - nouns denoting cognitive processes and contents";
            LexicographerFiles[10] = "noun.communication - nouns denoting communicative processes and contents";
            LexicographerFiles[11] = "noun.event - nouns denoting natural events";
            LexicographerFiles[12] = "noun.feeling - nouns denoting feelings and emotions";
            LexicographerFiles[13] = "noun.food - nouns denoting foods and drinks";
            LexicographerFiles[14] = "noun.group - nouns denoting groupings of people or objects";
            LexicographerFiles[15] = "noun.location - nouns denoting spatial position";
            LexicographerFiles[16] = "noun.motive - nouns denoting goals";
            LexicographerFiles[17] = "noun.object - nouns denoting natural objects (not man-made)";
            LexicographerFiles[18] = "noun.person - nouns denoting people";
            LexicographerFiles[19] = "noun.phenomenon - nouns denoting natural phenomena";
            LexicographerFiles[20] = "noun.plant - nouns denoting plants";
            LexicographerFiles[21] = "noun.possession - nouns denoting possession and transfer of possession";
            LexicographerFiles[22] = "noun.process - nouns denoting natural processes";
            LexicographerFiles[23] = "noun.quantity - nouns denoting quantities and units of measure";
            LexicographerFiles[24] = "noun.relation - nouns denoting relations between people or things or ideas";
            LexicographerFiles[25] = "noun.shape - nouns denoting two and three dimensional shapes";
            LexicographerFiles[26] = "noun.state - nouns denoting stable states of affairs";
            LexicographerFiles[27] = "noun.substance - nouns denoting substances";
            LexicographerFiles[28] = "noun.time - nouns denoting time and temporal relations";
            LexicographerFiles[29] = "verb.body - verbs of grooming, dressing and bodily care";
            LexicographerFiles[30] = "verb.change - verbs of size, temperature change, intensifying, etc.";
            LexicographerFiles[31] = "verb.cognition - verbs of thinking, judging, analyzing, doubting";
            LexicographerFiles[32] = "verb.communication - verbs of telling, asking, ordering, singing";
            LexicographerFiles[33] = "verb.competition - verbs of fighting, athletic activities";
            LexicographerFiles[34] = "verb.consumption - verbs of eating and drinking";
            LexicographerFiles[35] = "verb.contact - verbs of touching, hitting, tying, digging";
            LexicographerFiles[36] = "verb.creation - verbs of sewing, baking, painting, performing";
            LexicographerFiles[37] = "verb.emotion - verbs of feeling";
            LexicographerFiles[38] = "verb.motion - verbs of walking, flying, swimming";
            LexicographerFiles[39] = "verb.perception - verbs of seeing, hearing, feeling";
            LexicographerFiles[40] = "verb.possession - verbs of buying, selling, owning";
            LexicographerFiles[41] = "verb.social - verbs of political and social activities and events";
            LexicographerFiles[42] = "verb.stative - verbs of being, having, spatial relations";
            LexicographerFiles[43] = "verb.weather - verbs of raining, snowing, thawing, thundering";
            LexicographerFiles[44] = "adj.ppl - participial adjectives";
        }

        /// <summary>
        /// Gets the relation for a given POS and symbol
        /// </summary>
        /// <param name="pos">POS to get relation for</param>
        /// <param name="symbol">Symbol to get relation for</param>
        /// <returns>SynSet relation</returns>
        internal static SynSetRelation GetSynSetRelation(POS pos, string symbol)
        {
            return _posSymbolRelation[pos][symbol];
        }

        /// <summary>
        /// Gets the part-of-speech associated with a file path
        /// </summary>
        /// <param name="path">Path to get POS for</param>
        /// <returns>POS</returns>
        private static POS GetFilePOS(string path)
        {
            POS pos;
            string extension = System.IO.Path.GetExtension(path).Trim('.');
            if (extension == "adj")
                pos = POS.Adjective;
            else if (extension == "adv")
                pos = POS.Adverb;
            else if (extension == "noun")
                pos = POS.Noun;
            else if (extension == "verb")
                pos = POS.Verb;
            else
                throw new Exception("Error 501");

            return pos;
        }

        /// <summary>
        /// Gets synset shells from a word index line. A synset shell is an instance of SynSet with only the POS and Offset
        /// members initialized. These members are enough to look up the full synset within the corresponding data file. This
        /// method is static to prevent inadvertent references to a current WordNetEngine, which should be passed via the 
        /// corresponding parameter.
        /// </summary>
        /// <param name="wordIndexLine">Word index line from which to get synset shells</param>
        /// <param name="pos">POS of the given index line</param>
        /// <param name="wordNetEngine">WordNetEngine to pass to the constructor of each synset shell</param>
        /// <returns>Synset shells for the given index line</returns>
        private static List<SynSet> GetSynSetShells(string wordIndexLine, POS pos, WordNetEngine wordNetEngine)
        {
            List<SynSet> synsets = new List<SynSet>();

            // get number of synsets
            string[] parts = wordIndexLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int numSynSets = int.Parse(parts[2]);

            // grab each synset shell, from last to first
            int firstOffsetIndex = parts.Length - numSynSets;

            for (int i = parts.Length - 1; i >= firstOffsetIndex; --i)
            {
                // create synset
                int offset = int.Parse(parts[i]);

                // add synset to collection                        
                SynSet synset = new SynSet(pos, offset, wordNetEngine);
                synsets.Add(synset);
            }

            return synsets;
        }
        #endregion

        #region public members
        /// <summary>
        /// Gets whether or not the data in this WordNetEngine is stored in memory
        /// </summary>
        public bool InMemory
        {
            get { return _inMemory; }
        }

        /// <summary>
        /// Gets the WordNet data distribution directory
        /// </summary>
        public string WordNetDirectory
        {
            get { return _wordNetDirectory; }
        }

        /// <summary>
        /// Gets all words in WordNet, organized by POS.
        /// </summary>
        public Dictionary<POS, List<string>> AllWords
        {
            get
            {
                Dictionary<POS, List<string>> posWords = new Dictionary<POS, List<string>>();

                if (_inMemory)
                    // grab words from in-memory index
                    foreach (POS pos in _posWordSynSets.Keys)
                        posWords.Add(pos, new List<string>(_posWordSynSets[pos].Keys));
                else
                    // read index file for each pos
                    foreach (POS pos in _posIndexWordSearchStream.Keys)
                    {
                        // reset index file to start
                        StreamReader indexFile = _posIndexWordSearchStream[pos].Stream;
                        indexFile.SetPosition(0);

                        // read words, skipping header lines
                        List<string> words = new List<string>();
                        string line;
                        while ((line = indexFile.ReadLine()) != null)
                            if (!line.StartsWith(" "))
                                words.Add(line.Substring(0, line.IndexOf(' ')));

                        posWords.Add(pos, words);
                    }

                return posWords;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wordNetDirectory">Path to WorNet directory (the one with the data and index files in it)</param>
        /// <param name="inMemory">Whether or not to store all data in memory. In-memory storage requires quite a bit of space
        /// but it is also very quick. The alternative (false) will cause the data to be searched on-disk with an efficient
        /// binary search algorithm.</param>
        public WordNetEngine(string wordNetDirectory, bool inMemory)
        {
            _wordNetDirectory = wordNetDirectory;
            _inMemory = inMemory;
            _posIndexWordSearchStream = null;
            _posSynSetDataFile = null;

            if (!System.IO.Directory.Exists(_wordNetDirectory))
                throw new DirectoryNotFoundException("Error 502");

            // get data and index paths
            string[] dataPaths = new string[]
            {
                Path.Combine(_wordNetDirectory, "data.adj"),
                Path.Combine(_wordNetDirectory, "data.adv"),
                Path.Combine(_wordNetDirectory, "data.noun"),
                Path.Combine(_wordNetDirectory, "data.verb")
            };

            string[] indexPaths = new string[]
            {
                Path.Combine(_wordNetDirectory, "index.adj"),
                Path.Combine(_wordNetDirectory, "index.adv"),
                Path.Combine(_wordNetDirectory, "index.noun"),
                Path.Combine(_wordNetDirectory, "index.verb")
            };

            // make sure all files exist
            foreach (string path in dataPaths.Union(indexPaths))
                if (!System.IO.File.Exists(path))
                    throw new FileNotFoundException("Error 502");

            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
            // *                                                               *
            // *   UPDATE [HASSAN:11/03/2017]: The lemmatizer requires except- *
            // *   tion dictionary for each POS to be loaded as stream         *
            // *                                                               *
            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

            LemmaExcptionsFile = new Dictionary<string, StreamReader>(4);
            LemmaExcptionsFile.Add("noun", new StreamReader(wordNetDirectory + "\\noun.exc"));
            LemmaExcptionsFile.Add("verb", new StreamReader(wordNetDirectory + "\\verb.exc"));

            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
            // *                                                               *
            // *   UPDATE [HASSAN:11/07/2017]: The lemmatizer requires except- *
            // *   tion dictionary for noun only in the context of SemCluster  *
            // *    tool. In order to implement lemmatizer for all 4-POS tags  *
            // *    you will need the following:                               *
            // *    1) Uncomment the following lines.                          *
            // *    2) Uncomment the lines in suffixMap variable.              *
            // *    3) Uncomment the GetSynsets Switch section                 *
            // *    4) Add Exception files for each POS in the data folder     *
            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

            //LemmaExcptionsFile.Add("adjective", new StreamReader(wordNetDirectory + "\\adj.exc"));
            //LemmaExcptionsFile.Add("adverb", new StreamReader(wordNetDirectory + "\\adv.exc"));

            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
            // *                                                               *
            // *   UPDATE [HASSAN:28/01/2016]: The #region index file sorting  *
            // *   has been removed here,since its required to run only for    *
            // *   first program execution                                     *
            // *                                                               *
            // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

            #region engine init
            if (inMemory)
            {
                // pass 1:  get total number of synsets
                int totalSynsets = 0;
                foreach (string dataPath in dataPaths)
                {
                    // scan synset data file for lines that don't start with a space...these are synset definition lines
                    StreamReader dataFile = new StreamReader(dataPath);
                    string line;
                    while (dataFile.TryReadLine(out line))
                    {
                        int firstSpace = line.IndexOf(' ');
                        if (firstSpace > 0)
                            ++totalSynsets;
                    }
                }

                // pass 2:  create synset shells (pos and offset only)
                _idSynset = new Dictionary<string, SynSet>(totalSynsets);
                foreach (string dataPath in dataPaths)
                {
                    POS pos = GetFilePOS(dataPath);

                    // scan synset data file
                    StreamReader dataFile = new StreamReader(dataPath);
                    string line;
                    while (dataFile.TryReadLine(out line))
                    {
                        int firstSpace = line.IndexOf(' ');
                        if (firstSpace > 0)
                        {
                            // get offset and create synset shell
                            int offset = int.Parse(line.Substring(0, firstSpace));
                            SynSet synset = new SynSet(pos, offset, null);

                            _idSynset.Add(synset.ID, synset);
                        }
                    }
                }

                // pass 3:  instantiate synsets (hooks up relations, set glosses, etc.)
                foreach (string dataPath in dataPaths)
                {
                    POS pos = GetFilePOS(dataPath);

                    // scan synset data file
                    StreamReader dataFile = new StreamReader(dataPath);
                    string line;
                    while (dataFile.TryReadLine(out line))
                    {
                        int firstSpace = line.IndexOf(' ');
                        if (firstSpace > 0)
                            // instantiate synset defined on current line, using the instantiated synsets for all references
                            _idSynset[pos + ":" + int.Parse(line.Substring(0, firstSpace))].Instantiate(line, _idSynset);
                    }
                }

                // organize synsets by pos and words
                _posWordSynSets = new Dictionary<POS, Dictionary<string, List<SynSet>>>();
                foreach (string indexPath in indexPaths)
                {
                    POS pos = GetFilePOS(indexPath);

                    _posWordSynSets.EnsureContainsKey(pos, typeof(Dictionary<string, List<SynSet>>));

                    // scan word index file, skipping header lines
                    StreamReader indexFile = new StreamReader(indexPath);
                    string line;
                    while (indexFile.TryReadLine(out line))
                    {
                        int firstSpace = line.IndexOf(' ');
                        if (firstSpace > 0)
                        {
                            // grab word and synset shells
                            string word = line.Substring(0, firstSpace);
                            List<SynSet> synsets = GetSynSetShells(line, pos, null);

                            // use reference to the synsets that we instantiated in our three-pass routine above
                            _posWordSynSets[pos].Add(word, new List<SynSet>(synsets.Count));
                            foreach (SynSet synset in synsets)
                                _posWordSynSets[pos][word].Add(_idSynset[synset.ID]);
                        }
                    }
                }
            }
            else
            {
                // open binary search streams for index files
                _posIndexWordSearchStream = new Dictionary<POS, BinarySearchTextStream>();
                foreach (string indexPath in indexPaths)
                {
                    // create binary search stream for index file
                    BinarySearchTextStream searchStream = new BinarySearchTextStream(indexPath, new BinarySearchTextStream.SearchComparisonDelegate(

                        delegate(string searchWord, string currentLine)
                        {
                            // if we landed on the header text, search further down
                            if (currentLine[0] == ' ')
                                return 1;

                            // get word on current line
                            string currentWord = currentLine.Substring(0, currentLine.IndexOf(' '));

                            // compare searched-for word to the current word
                            return ((string)searchWord).CompareTo(currentWord);
                        }

                        ));

                    // add search stream for current POS
                    _posIndexWordSearchStream.Add(GetFilePOS(indexPath), searchStream);
                }
                // open readers for synset data files
                _posSynSetDataFile = new Dictionary<POS, StreamReader>();
                foreach (string dataPath in dataPaths)
                    _posSynSetDataFile.Add(GetFilePOS(dataPath), new StreamReader(dataPath));
            }
            #endregion
        }
        #endregion

        #region synset retrieval
        /// <summary>
        /// Gets a synset
        /// </summary>
        /// <param name="synsetID">ID of synset in the format returned by SynSet.ID (i.e., POS:Offset)</param>
        /// <returns>SynSet</returns>
        public SynSet GetSynSet(string synsetID)
        {
            SynSet synset;
            if (_inMemory)
                synset = _idSynset[synsetID];
            else
            {
                // get POS and offset
                int colonLoc = synsetID.IndexOf(':');
                POS pos = (POS)Enum.Parse(typeof(POS), synsetID.Substring(0, colonLoc));
                int offset = int.Parse(synsetID.Substring(colonLoc + 1));

                // create shell and then instantiate
                synset = new SynSet(pos, offset, this);
                synset.Instantiate();
            }

            return synset;
        }

        /// <summary>
        /// Gets all synsets for a word, optionally restricting the returned synsets to one or more parts of speech. This
        /// method does not perform any morphological analysis to match up the given word. It does, however, replace all 
        /// spaces with underscores and call String.ToLower to normalize case.
        /// </summary>
        /// <param name="word">Word to get SynSets for. This method will replace all spaces with underscores and
        /// call ToLower() to normalize the word's case.</param>
        /// <param name="backward">Shell initializaton direction forward/backward.</param>
        /// <param name="posRestriction">POSs to search. Cannot contain POS.None. Will search all POSs if no restriction is given.</param>
        /// <returns>Set of SynSets that contain word</returns>
        public List<SynSet> GetSynSets(string word, bool backward, POS posRestriction)
        {
            word = word.ToLower();
            List<SynSet> FoundSynsets = new List<SynSet>();
            if (_inMemory)
            {
                // read instantiated synsets from memory
                List<SynSet> synsets;
                if (_posWordSynSets[posRestriction].TryGetValue(word, out synsets))
                {
                    FoundSynsets = synsets;
                }
            }
            else
            {
                // get index line for word
                string indexLine = _posIndexWordSearchStream[posRestriction].Search(word);

                // if index line exists, get synset shells and instantiate them
                if (indexLine != null)
                {
                    // get synset shells and instantiate them
                    List<SynSet> synsets;
                    synsets = GetSynSetShells(indexLine, posRestriction, this);
                    foreach (SynSet synset in synsets)
                    {
                        synset.Instantiate();
                        FoundSynsets.Add(synset);
                    }
                }
            }

            if (!backward)
            {
                FoundSynsets.Reverse();
                return FoundSynsets;
            }
            else
                return FoundSynsets;
        }
        public List<SynSet> GetSynSets(string word, string partOfSpeech)
        {
            switch (partOfSpeech)
            {
                case "noun":
                    return GetSynSets(word, true, POS.Noun);
                case "verb":
                    return GetSynSets(word, true, POS.Verb);
                //case "adjective":
                //    return GetSynSets(word, true, POS.Adjective);
                //case "adverb":
                //    return GetSynSets(word, true, POS.Adverb);
                default:
                    return GetSynSets(word, true, POS.None);
            }
        }
        /// <summary>
        /// Gets definition line for synset from data file
        /// </summary>
        /// <param name="pos">POS to get definition for</param>
        /// <param name="offset">Offset into data file</param>
        internal string GetSynSetDefinition(POS pos, int offset)
        {
            // set data file to synset location
            StreamReader dataFile = _posSynSetDataFile[pos];
            dataFile.DiscardBufferedData();
            dataFile.BaseStream.Position = offset;

            // read synset definition
            string synSetDefinition = dataFile.ReadLine();

            // make sure file positions line up
            if (int.Parse(synSetDefinition.Substring(0, synSetDefinition.IndexOf(' '))) != offset)
                throw new Exception("Error 503");

            return synSetDefinition;
        }
        #endregion

        #region WordNet-Based lemmatization
        private IOperation[] mDefaultOperations;
        protected string[] mEmpty = new string[0];
        public delegate void MorphologicalProcessOperation(string lemma, string partOfSpeech, List<string> baseForms);
        public string Lemmatize(string lemma, string partOfSpeech, IOperation[] operations)
        {
            var baseForms = new List<string>();
            foreach (IOperation operation in operations)
            {
                operation.Execute(lemma, partOfSpeech, baseForms);
            }

            if (baseForms.Count > 0)
            {
                if (baseForms.ToArray()[0][0]==lemma[0])
                    return baseForms.ToArray()[0];
                else
                    return lemma;
            }
            else
                return lemma;
        }
        public string Lemmatize(string lemma, string partOfSpeech)
        {
            if (mDefaultOperations == null)
            {
                var suffixMap = new Dictionary<string, string[][]>
                {
                    {
                        "noun", new string[][]
                        {
                            new string[] {"s", ""}, new string[] {"ses", "s"}, new string[] {"xes", "x"},
                            new string[] {"zes", "z"}, new string[] {"ches", "ch"}, new string[] {"shes", "sh"},
                            new string[] {"men", "man"}, new string[] {"ies", "y"}
                        }

                    }
                    ,
                    {
                        "verb", new string[][]
                        {
                            new string[] {"s", ""}, new string[] {"ies", "y"}, new string[] {"es", "e"},
                            new string[] {"es", ""}, new string[] {"ed", "e"}, new string[] {"ed", ""},
                            new string[] {"ing", "e"}, new string[] {"ing", ""}
                        }
                    }
                    //,
                    //{
                    //    "adjective", new string[][]
                    //    {
                    //        new string[] {"er", ""}, new string[] {"est", ""}, new string[] {"er", "e"},
                    //        new string[] {"est", "e"}
                    //    }
                    //}
                };

                var tokDso = new DetachSuffixesOperation(suffixMap);
                tokDso.AddDelegate(DetachSuffixesOperation.Operations, new IOperation[] { new LookupIndexWordOperation(this), new LookupExceptionsOperation(this) });
                var tokOp = new TokenizerOperation(this, new string[] { " ", "-" });
                tokOp.AddDelegate(TokenizerOperation.TokenOperations, new IOperation[] { new LookupIndexWordOperation(this), new LookupExceptionsOperation(this), tokDso });
                var morphDso = new DetachSuffixesOperation(suffixMap);
                morphDso.AddDelegate(DetachSuffixesOperation.Operations, new IOperation[] { new LookupIndexWordOperation(this), new LookupExceptionsOperation(this) });
                mDefaultOperations = new IOperation[] { new LookupExceptionsOperation(this), morphDso, tokOp };
            }
            return Lemmatize(lemma, partOfSpeech, mDefaultOperations);
        }
        public MorphologicalProcessOperation LookupExceptionsOperation
        {
            get
            {
                return delegate(string lemma, string partOfSpeech, List<string> baseForms)
                {
                    string[] exceptionForms = GetExceptionForms(lemma, partOfSpeech);
                    foreach (string exceptionForm in exceptionForms)
                    {
                        if (!baseForms.Contains(exceptionForm))
                        {
                            baseForms.Add(exceptionForm);
                        }
                    }
                };
            }
        }
        public MorphologicalProcessOperation LookupIndexWordOperation
        {
            get
            {
                return delegate(string lemma, string partOfSpeech, List<string> baseForms)
                {
                    if (!baseForms.Contains(lemma) && GetSynSets(lemma, partOfSpeech).Count > 0)
                    {
                        baseForms.Add(lemma);
                    }
                };
            }
        }
        public string[] GetExceptionForms(string lemma, string partOfSpeech)
        {
            string line = BinarySearch(lemma, LemmaExcptionsFile[partOfSpeech]);
            if (line != null)
            {
                var exceptionForms = new List<string>();
                var tokenizer = new Tokenizer(line);
                string skipWord = tokenizer.NextToken();
                string word = tokenizer.NextToken();
                while (word != null)
                {
                    exceptionForms.Add(word);
                    word = tokenizer.NextToken();
                }
                return exceptionForms.ToArray();
            }
            return mEmpty;
        }
        private string BinarySearch(string searchKey, StreamReader searchFile)
        {
            if (searchKey.Length == 0)
            {
                return null;
            }

            int c, n;
            long top, bot, mid, diff;
            string line, key;
            diff = 666;
            line = "";
            bot = searchFile.BaseStream.Seek(0, SeekOrigin.End);
            top = 0;
            mid = (bot - top) / 2;

            do
            {
                searchFile.DiscardBufferedData();
                searchFile.BaseStream.Position = mid - 1;
                if (mid != 1)
                {
                    while ((c = searchFile.Read()) != '\n' && c != -1) { }
                }
                line = searchFile.ReadLine();
                if (line == null)
                {
                    return null;
                }
                n = line.IndexOf(' ');
                key = line.Substring(0, n);
                key = key.Replace("-", " ").Replace("_", " ");
                if (string.CompareOrdinal(key, searchKey) < 0)
                {
                    top = mid;
                    diff = (bot - top) / 2;
                    mid = top + diff;
                }
                if (string.CompareOrdinal(key, searchKey) > 0)
                {
                    bot = mid;
                    diff = (bot - top) / 2;
                    mid = top + diff;
                }
            } while (key != searchKey && diff != 0);

            if (key == searchKey)
            {
                return line;
            }
            return null;
        }
        #endregion
        
        /// <summary>
        /// Disposal. Closes all files and releases any resources associated with this WordNet engine
        /// </summary>
        public void Dispose()
        {
            if (_inMemory)
            {
                // release all in-memory resources
                _posWordSynSets = null;
                _idSynset = null;
            }
            else
            {
                // close all index files
                foreach (BinarySearchTextStream stream in _posIndexWordSearchStream.Values)
                    stream.Close();  //  || stream.Dispose();
                _posIndexWordSearchStream = null;
                // close all data files
                foreach (StreamReader dataFile in _posSynSetDataFile.Values)
                    dataFile.Close();
                _posSynSetDataFile = null;
            }

            // release lemmatization exception files
            foreach (var kv in LemmaExcptionsFile)
                kv.Value.Close();
            LemmaExcptionsFile = null;

        }
    }
}
