using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;

namespace WordNetApi.Core
{
    public class SynSet
    {
        private WordNetEngine.POS _pos;
        private int _offset;
        private string _gloss;
        private string _uri;
        private List<string> _words;
        private Dictionary<WordNetEngine.SynSetRelation, List<SynSet>> _relationSynSets;
        private Dictionary<WordNetEngine.SynSetRelation, Dictionary<SynSet, Dictionary<int, List<int>>>> _lexicalRelations;
        private SynSet _searchBackPointer;
        private WordNetEngine _wordNetEngine;
        private bool _instantiated;
        private int lex_id;

        // the following must never change...hashing depends on them
        private readonly string _id;
        private readonly int _hashCode;

        #region static members
        /// <summary>
        /// Checks whether two synsets are equal
        /// </summary>
        /// <param name="synset1">First synset</param>
        /// <param name="synset2">Second synset</param>
        /// <returns>True if synsets are equal, false otherwise</returns>
        public static bool operator ==(SynSet synset1, SynSet synset2)
        {
            // check object reference
            if (Object.ReferenceEquals(synset1, synset2))
                return true;

            // check if either (but not both) are null
            if ((object)synset2 == null ^ (object)synset1 == null)
                return false;

            return synset1.Equals(synset2);
        }

        /// <summary>
        /// Checks whether two synsets are unequal
        /// </summary>
        /// <param name="synset1">First synset</param>
        /// <param name="synset2">Second synset</param>
        /// <returns>True if synsets are unequal, false otherwise</returns>
        public static bool operator !=(SynSet synset1, SynSet synset2)
        {
            return !(synset1 == synset2);
        }
        #endregion

        /// <summary>
        /// Gets semantic relations that exist between this synset and other synsets
        /// </summary>
        public IEnumerable<WordNetEngine.SynSetRelation> SemanticRelations
        {
            get { return _relationSynSets.Keys; }
        }

        public string LexGroup
        {
            get { return WordNetEngine.LexicographerFiles[lex_id]; }
        }

        /// <summary>
        /// Gets lexical relations that exist between words in this synset and words in another synset
        /// </summary>
        public IEnumerable<WordNetEngine.SynSetRelation> LexicalRelations
        {
            get { return _lexicalRelations.Keys; }
        }

        /// <summary>
        /// Gets whether or not the current synset has been instantiated
        /// </summary>
        internal bool Instantiated
        {
            get { return _instantiated; }
        }

        /// <summary>
        /// Gets or sets the back-pointer used when searching WordNet
        /// </summary>
        internal SynSet SearchBackPointer
        {
            get { return _searchBackPointer; }
            set { _searchBackPointer = value; }
        }

        /// <summary>
        /// Gets the POS of the current synset
        /// </summary>
        public WordNetEngine.POS POS
        {
            get { return _pos; }
        }

        /// <summary>
        /// Gets the byte offset of synset definition within the data file
        /// </summary>
        public int Offset
        {
            get { return _offset; }
        }

        /// <summary>
        /// Gets or sets the gloss of the current SynSet
        /// </summary>
        public string Gloss
        {
            get { return _gloss; }
            set { this._gloss = value; }
        }

        /// <summary>
        /// Gets or set the URI resource of the current SynSet (WordNet/Resource Identifier)
        /// </summary>
        public string URI
        {
            get { return _uri; }
            set { this._uri = value; }
        }

        /// <summary>
        /// Gets the words in the current SynSet
        /// </summary>
        public List<string> Synonyms
        {
            get { return _words; }
        }

        /// <summary>
        /// Gets the ID of this synset in the form POS:Offset
        /// </summary>
        public string ID
        {
            get { return _id; }
        }

        #region construction
        /// <summary>
        /// Constructor. Creates the shell of a SynSet without any actual information. To gain access to SynSet words, gloss, 
        /// and related SynSets, call SynSet.Instantiate.
        /// </summary>
        /// <param name="pos">POS of SynSet</param>
        /// <param name="offset">Byte location of SynSet definition within data file</param>
        /// <param name="wordNetEngine">WordNet engine used to instantiate this synset. This should be non-null only when constructing
        /// synsets for disk-based WordNet engines.</param>
        internal SynSet(WordNetEngine.POS pos, int offset, WordNetEngine wordNetEngine)
        {
            _pos = pos;
            _offset = offset;
            _wordNetEngine = wordNetEngine;
            _instantiated = false;

            if (_wordNetEngine != null && _wordNetEngine.InMemory)
                throw new Exception("Don't need to pass a non-null WordNetEngine when using in-memory storage");

            // precompute the ID and hash code for efficiency
            _id = _pos + ":" + _offset;
            _hashCode = _id.GetHashCode();
        }

        /// <summary>
        /// Instantiates the current synset. Any related synsets are created as synset shells. These shells only contain
        /// the POS and offset. Other members can be initialized by calling Instantiate on the shells. This should only 
        /// be called when _not_ using in-memory storage. When using in-memory storage, all synsets are instantiated in
        /// the WordNetEngine constructor. 
        /// </summary>
        internal void Instantiate()
        {
            // instantiate with definition line from disk
            Instantiate(_wordNetEngine.GetSynSetDefinition(_pos, _offset), null);
        }

        /// <summary>
        /// Instantiates the current synset. If idSynset is non-null, related synsets references are set to those from 
        /// idSynset; otherwise, related synsets are created as shells.
        /// </summary>
        /// <param name="definition">Definition line of synset from data file</param>
        /// <param name="idSynset">Lookup for related synsets. If null, all related synsets will be created as shells.</param>
        internal void Instantiate(string definition, Dictionary<string, SynSet> idSynset)
        {
            // don't re-instantiate
            if (!_instantiated)
            {
                // get number of words in the synset and the start character of the word list
                int wordStart;
                int numWords = int.Parse(GetField(definition, 3, out wordStart), NumberStyles.HexNumber);
                wordStart = definition.IndexOf(' ', wordStart) + 1;

                _words = new List<string>(numWords);

                // get words in synset
                for (int i = 0; i < numWords; ++i)
                {
                    int wordEnd = definition.IndexOf(' ', wordStart + 1) - 1;
                    int wordLen = wordEnd - wordStart + 1;
                    string word = definition.Substring(wordStart, wordLen);
                    _words.Add(word);

                    // get lex_id
                    lex_id = Convert.ToInt32(definition.Substring(definition.IndexOf(' ') + 1, 2));

                    // skip lex_id field
                    wordStart = definition.IndexOf(' ', wordEnd + 2) + 1;
                }

                // get gloss
                _gloss = definition.Substring(definition.IndexOf('|') + 1).Trim();

                // get number and start of relations
                int relationCountField = 3 + (_words.Count * 2) + 1;
                int relationFieldStart;
                int numRelations = int.Parse(GetField(definition, relationCountField, out relationFieldStart));
                relationFieldStart = definition.IndexOf(' ', relationFieldStart) + 1;

                // grab each related synset
                _relationSynSets = new Dictionary<WordNetEngine.SynSetRelation, List<SynSet>>();
                _lexicalRelations = new Dictionary<WordNetEngine.SynSetRelation, Dictionary<SynSet, Dictionary<int, List<int>>>>();
                for (int relationNum = 0; relationNum < numRelations; ++relationNum)
                {
                    string relationSymbol = null;
                    int relatedSynSetOffset = -1;
                    WordNetEngine.POS relatedSynSetPOS = WordNetEngine.POS.None;
                    int sourceWordIndex = -1;
                    int targetWordIndex = -1;

                    // each relation has four columns
                    for (int relationField = 0; relationField <= 3; ++relationField)
                    {
                        int fieldEnd = definition.IndexOf(' ', relationFieldStart + 1) - 1;
                        int fieldLen = fieldEnd - relationFieldStart + 1;
                        string fieldValue = definition.Substring(relationFieldStart, fieldLen);

                        // relation symbol
                        if (relationField == 0)
                            relationSymbol = fieldValue;
                        // related synset offset
                        else if (relationField == 1)
                            relatedSynSetOffset = int.Parse(fieldValue);
                        // related synset POS
                        else if (relationField == 2)
                            relatedSynSetPOS = GetPOS(fieldValue);
                        // source/target word for lexical relation
                        else if (relationField == 3)
                        {
                            sourceWordIndex = int.Parse(fieldValue.Substring(0, 2), NumberStyles.HexNumber);
                            targetWordIndex = int.Parse(fieldValue.Substring(2), NumberStyles.HexNumber);
                        }
                        else
                            throw new Exception();

                        relationFieldStart = definition.IndexOf(' ', relationFieldStart + 1) + 1;
                    }

                    // get related synset...create shell if we don't have a lookup
                    SynSet relatedSynSet;
                    if (idSynset == null)
                        relatedSynSet = new SynSet(relatedSynSetPOS, relatedSynSetOffset, _wordNetEngine);
                    // look up related synset directly
                    else
                        relatedSynSet = idSynset[relatedSynSetPOS + ":" + relatedSynSetOffset];

                    // get relation
                    WordNetEngine.SynSetRelation relation = WordNetEngine.GetSynSetRelation(_pos, relationSymbol);

                    // add semantic relation if we have neither a source nor a target word index
                    if (sourceWordIndex == 0 && targetWordIndex == 0)
                    {
                        _relationSynSets.EnsureContainsKey(relation, typeof(List<SynSet>));
                        _relationSynSets[relation].Add(relatedSynSet);
                    }
                    // add lexical relation
                    else
                    {
                        _lexicalRelations.EnsureContainsKey(relation, typeof(Dictionary<SynSet, Dictionary<int, List<int>>>));
                        _lexicalRelations[relation].EnsureContainsKey(relatedSynSet, typeof(Dictionary<int, List<int>>));
                        _lexicalRelations[relation][relatedSynSet].EnsureContainsKey(sourceWordIndex, typeof(List<int>));

                        if (!_lexicalRelations[relation][relatedSynSet][sourceWordIndex].Contains(targetWordIndex))
                            _lexicalRelations[relation][relatedSynSet][sourceWordIndex].Add(targetWordIndex);
                    }
                }
                _instantiated = true;
            }

            // release the wordnet engine if we have one...don't need it anymore
            if (_wordNetEngine != null)
                _wordNetEngine = null;
        }

        /// <summary>
        /// Gets a space-delimited field from a synset definition line
        /// </summary>
        /// <param name="line">SynSet definition line</param>
        /// <param name="fieldNum">Number of field to get</param>
        /// <returns>Field value</returns>
        private string GetField(string line, int fieldNum)
        {
            int dummy;
            return GetField(line, fieldNum, out dummy);
        }

        /// <summary>
        /// Gets a space-delimited field from a synset definition line
        /// </summary>
        /// <param name="line">SynSet definition line</param>
        /// <param name="fieldNum">Number of field to get</param>
        /// <param name="startIndex">Start index of field within the line</param>
        /// <returns>Field value</returns>
        private string GetField(string line, int fieldNum, out int startIndex)
        {
            if (fieldNum < 0)
                throw new Exception("Error 500. Invalid field number:  " + fieldNum);

            // scan fields until we hit the one we want
            int currField = 0;
            startIndex = 0;
            while (true)
            {
                if (currField == fieldNum)
                {
                    // get the end of the field
                    int endIndex = line.IndexOf(' ', startIndex + 1) - 1;

                    // watch out for end of line
                    if (endIndex < 0)
                        endIndex = line.Length - 1;

                    // get length of field
                    int fieldLen = endIndex - startIndex + 1;

                    // return field value
                    return line.Substring(startIndex, fieldLen);
                }

                // move to start of next field (one beyond next space)
                startIndex = line.IndexOf(' ', startIndex) + 1;

                // if there are no more spaces and we haven't found the field, the caller requested an invalid field
                if (startIndex == 0)
                    throw new Exception("Error 500. Failed to get field number:  " + fieldNum);

                ++currField;
            }
        }

        /// <summary>
        /// Gets the POS from its code
        /// </summary>
        /// <param name="pos">POS code</param>
        /// <returns>POS</returns>
        private WordNetEngine.POS GetPOS(string pos)
        {
            WordNetEngine.POS relatedPOS;
            if (pos == "n")
                relatedPOS = WordNetEngine.POS.Noun;
            else if (pos == "v")
                relatedPOS = WordNetEngine.POS.Verb;
            else if (pos == "a" || pos == "s")
                relatedPOS = WordNetEngine.POS.Adjective;
            else if (pos == "r")
                relatedPOS = WordNetEngine.POS.Adverb;
            else
                throw new Exception("Error 500. Unexpected POS:  " + pos);

            return relatedPOS;
        }
        #endregion

        /// <summary>
        /// Gets the number of synsets related to the current one by the given relation
        /// </summary>
        /// <param name="relation">Relation to check</param>
        /// <returns>Number of synset related to the current one by the given relation</returns>
        public int GetRelatedSynSetCount(WordNetEngine.SynSetRelation relation)
        {
            if (!_relationSynSets.ContainsKey(relation))
                return 0;

            return _relationSynSets[relation].Count;
        }

        /// <summary>
        /// Gets synsets related to the current synset
        /// </summary>
        /// <param name="relation">Synset relation to follow</param>
        /// <param name="recursive">Whether or not to follow the relation recursively for all related synsets</param>
        /// <returns>Synsets related to the given one by the given relation</returns>
        public List<SynSet> GetRelatedSynSets(WordNetEngine.SynSetRelation relation, bool recursive)
        {
            return GetRelatedSynSets(new WordNetEngine.SynSetRelation[] { relation }, recursive);
        }

        /// <summary>
        /// Gets synsets related to the current synset
        /// </summary>
        /// <param name="relations">Synset relations to follow</param>
        /// <param name="recursive">Whether or not to follow the relations recursively for all related synsets</param>
        /// <returns>Synsets related to the given one by the given relations</returns>
        public List<SynSet> GetRelatedSynSets(IEnumerable<WordNetEngine.SynSetRelation> relations, bool recursive)
        {
            List<SynSet> synsets = new List<SynSet>();

            GetRelatedSynSets(relations, recursive, synsets);

            return synsets;
        }

        /// <summary>
        /// Private version of GetRelatedSynSets that avoids cyclic paths in WordNet. The current synset must itself be instantiated.
        /// </summary>
        /// <param name="relations">Synset relations to get</param>
        /// <param name="recursive">Whether or not to follow the relation recursively for all related synsets</param>
        /// <param name="currSynSets">Current collection of synsets, which we'll add to.</param>
        private void GetRelatedSynSets(IEnumerable<WordNetEngine.SynSetRelation> relations, bool recursive, List<SynSet> currSynSets)
        {
            // try each relation
            foreach (WordNetEngine.SynSetRelation relation in relations)
                if (_relationSynSets.ContainsKey(relation))
                    foreach (SynSet relatedSynset in _relationSynSets[relation])
                        // only add synset if it isn't already present (wordnet contains cycles)
                        if (!currSynSets.Contains(relatedSynset))
                        {
                            // instantiate synset if it isn't already (for disk-based storage)
                            if (!relatedSynset.Instantiated)
                                relatedSynset.Instantiate();

                            currSynSets.Add(relatedSynset);

                            if (recursive)
                                relatedSynset.GetRelatedSynSets(relations, recursive, currSynSets);
                        }
        }

        /// <summary>
        /// Gets the shortest path from the current synset to another, following the given synset relations.
        /// </summary>
        /// <param name="destination">Destination synset</param>
        /// <param name="relations">Relations to follow, or null for all relations.</param>
        /// <returns>Synset path, or null if none exists.</returns>
        public List<SynSet> GetShortestPathTo(SynSet destination, IEnumerable<WordNetEngine.SynSetRelation> relations)
        {
            if (relations == null)
                relations = Enum.GetValues(typeof(WordNetEngine.SynSetRelation)) as WordNetEngine.SynSetRelation[];

            // make sure the backpointer on the current synset is null - can't predict what other functions might do
            _searchBackPointer = null;

            // avoid cycles
            List<SynSet> synsetsEncountered = new List<SynSet>();
            synsetsEncountered.Add(this);

            // start search queue
            Queue<SynSet> searchQueue = new Queue<SynSet>();
            searchQueue.Enqueue(this);

            // run search
            List<SynSet> path = null;
            while (searchQueue.Count > 0 && path == null)
            {
                SynSet currSynSet = searchQueue.Dequeue();

                // see if we've finished the search
                if (currSynSet == destination)
                {
                    // gather synsets along path
                    path = new List<SynSet>();
                    while (currSynSet != null)
                    {
                        path.Add(currSynSet);
                        currSynSet = currSynSet.SearchBackPointer;
                    }

                    // reverse for the correct order
                    path.Reverse();
                }
                // expand the search one level
                else
                    foreach (SynSet synset in currSynSet.GetRelatedSynSets(relations, false))
                        if (!synsetsEncountered.Contains(synset))
                        {
                            synset.SearchBackPointer = currSynSet;
                            searchQueue.Enqueue(synset);

                            synsetsEncountered.Add(synset);
                        }
            }

            // null-out all search backpointers
            foreach (SynSet synset in synsetsEncountered)
                synset.SearchBackPointer = null;
            return path;
        }

        /// <summary>
        /// Gets the closest synset that is reachable from the current and another synset along the given relations. For example, 
        /// given two synsets and the Hypernym relation, this will return the lowest synset that is a hypernym of both synsets. If 
        /// the hypernym hierarchy forms a tree, this will be the lowest common ancestor.
        /// </summary>
        /// <param name="synset">Other synset</param>
        /// <param name="relations">Relations to follow</param>
        /// <returns>Closest mutually reachable synset</returns>
        public SynSet GetClosestMutuallyReachableSynset(SynSet synset, IEnumerable<WordNetEngine.SynSetRelation> relations)
        {
            // avoid cycles
            List<SynSet> synsetsEncountered = new List<SynSet>();
            synsetsEncountered.Add(this);

            // start search queue
            Queue<SynSet> searchQueue = new Queue<SynSet>();
            searchQueue.Enqueue(this);

            // run search
            SynSet closest = null;
            while (searchQueue.Count > 0 && closest == null)
            {
                SynSet currSynSet = searchQueue.Dequeue();

                /* check for a path between the given synset and the current one. if such a path exists, the current
                 * synset is the closest mutually reachable synset. */
                if (synset.GetShortestPathTo(currSynSet, relations) != null)
                    closest = currSynSet;
                // otherwise, expand the search along the given relations
                else
                    foreach (SynSet relatedSynset in currSynSet.GetRelatedSynSets(relations, false))
                        if (!synsetsEncountered.Contains(relatedSynset))
                        {
                            searchQueue.Enqueue(relatedSynset);
                            synsetsEncountered.Add(relatedSynset);
                        }
            }

            return closest;
        }

        /// <summary>
        /// Gets lexically related words for the current synset. Many of the relations in WordNet are lexical instead of semantic. Whereas
        /// the latter indicate relations between entire synsets (e.g., hypernym), the former indicate relations between specific 
        /// words in synsets. This method retrieves all lexical relations and the words related thereby.
        /// </summary>
        /// <returns>Mapping from relations to mappings from words in the current synset to related words in the related synsets</returns>
        public Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, List<string>>> GetLexicallyRelatedWords()
        {
            Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, List<string>>> relatedWords = new Dictionary<WordNetEngine.SynSetRelation, Dictionary<string, List<string>>>();
            foreach (WordNetEngine.SynSetRelation relation in _lexicalRelations.Keys)
            {
                relatedWords.EnsureContainsKey(relation, typeof(Dictionary<string, List<string>>));

                foreach (SynSet relatedSynSet in _lexicalRelations[relation].Keys)
                {
                    // make sure related synset is initialized
                    if (!relatedSynSet.Instantiated)
                        relatedSynSet.Instantiate();

                    foreach (int sourceWordIndex in _lexicalRelations[relation][relatedSynSet].Keys)
                    {
                        string sourceWord = _words[sourceWordIndex - 1];

                        relatedWords[relation].EnsureContainsKey(sourceWord, typeof(List<string>), false);

                        foreach (int targetWordIndex in _lexicalRelations[relation][relatedSynSet][sourceWordIndex])
                        {
                            string targetWord = relatedSynSet.Synonyms[targetWordIndex - 1];
                            relatedWords[relation][sourceWord].Add(targetWord);
                        }
                    }
                }
            }

            return relatedWords;
        }

        /// <summary>
        /// Gets hash code for this synset
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Checks whether the current synset equals another
        /// </summary>
        /// <param name="obj">Other synset</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is SynSet))
                return false;

            SynSet synSet = obj as SynSet;

            return _pos == synSet.POS && _offset == synSet.Offset;
        }
    }

}
