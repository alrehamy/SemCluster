using OpenNLP.PosTagger;
using OpenNLP.SentenceDetecter;
using OpenNLP.Tokenizer;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using WordNetApi.Core;

namespace TextAnalytics
{
    class WordNetBoostrap
    {
        private MaximumEntropySentenceDetector _sentenceDetector;
        private AbstractTokenizer _tokenizer;
        private EnglishMaximumEntropyPosTagger _posTagger;
        WordNetEngine _wn;
        int k = 0;
        string[] SynsetArray = new string[180000];
        string[] Copyrights = new string[29];
        string _wordNetPath;
        public WordNetBoostrap(string nlpModelsPath, string wordNetPath)
        {
            this._wordNetPath = wordNetPath;
            _wn = new WordNetEngine(_wordNetPath, true);
            _tokenizer = new EnglishRuleBasedTokenizer(false);
            _sentenceDetector = new EnglishMaximumEntropySentenceDetector(nlpModelsPath + "EnglishSD.nbin");
            _posTagger = new EnglishMaximumEntropyPosTagger(nlpModelsPath + "EnglishPOS.nbin", nlpModelsPath + @"\Parser\tagdict");
        }

        public bool Configure()
        {
            try
            {
                string line;
                System.IO.StreamReader FileRead = new System.IO.StreamReader(_wordNetPath + "\\data.noun");
                // Skip copyrights paragraph
                for (var i = 0; i < 29; i++)
                    Copyrights[i] = FileRead.ReadLine();

                while ((line = FileRead.ReadLine()) != null)
                {
                    SynsetArray[k] = line;
                    k++;
                }
                FileRead.Close();

                ModifySynSetGloss();

                //System.IO.StreamWriter DataWriter = new System.IO.StreamWriter(_wordNetPath + "\\data1.noun", false);
                //for (var i = 0; i < 29; i++)
                //    DataWriter.Write(Copyrights[i]+"\n");
                //for (int i = 0; i < k; i++)
                //    DataWriter.Write(SynsetArray[i]+"\n");
                //DataWriter.Close();

                ModifySynSetWords();

                return true;
            }
            catch (Exception ex)
            {
                Dispose();
                return false;
            }
        }

        private void ModifySynSetGloss()
        {
            string Gloss, NewSynsetLine;
            int GlossSize = 0;
            List<int> anamol = new List<int>();

            

            for (int i = 0; i < k; i++)
            {
                string[] tmp = SynsetArray[i].Split('|');
                // Gloss is in the format: IDs | string

                Gloss = tmp.Last();

                //Store the original gloss length
                GlossSize = tmp.Last().Length;

                Gloss = Lexicalize(Gloss);

                // Calculate Change in length of Gloss string after editting
                if (Gloss.Length > GlossSize)
                    Console.Write(i+" , ");

                GlossSize = GlossSize - Gloss.Length;

                // Add start-WhiteSpace deleted in the tokenization step
                Gloss = " " + Gloss;

                // For legcy Purpose, we don't change Gloss string length, therefore byte-based index is preserved
                // To do so, we add whitespaces for missing chars' Compensation
                for (int l = 0; l < GlossSize - 1; l++)
                    Gloss = Gloss + " ";

                NewSynsetLine = "";

                // Reconstruct the Synset Line
                for (int j = 0; j < tmp.Length - 1; j++)
                    NewSynsetLine = NewSynsetLine + tmp[j] + "|";

                // Gluing the new Gloss
                NewSynsetLine = NewSynsetLine + Gloss;

                // Saving the updates since we need farther processing
                SynsetArray[i] = NewSynsetLine;
            }
        }

        private void ModifySynSetWords()
        {
            #region Load .Index Data
            SortedDictionary<string, string> IndexLine = new SortedDictionary<string, string>();
            System.IO.StreamReader IndexReader = new System.IO.StreamReader(_wordNetPath + "\\index.noun");
            string ReadLine;
            // Skip copyrights paragraph

            for (var i = 0; i < 29; i++)
                IndexReader.ReadLine();

            while ((ReadLine = IndexReader.ReadLine()) != null)
            {

                string FetchIndexKey = "";
                string FetchIndexDetails = "";
                int EndofIndexKey = 0;

                while (ReadLine[EndofIndexKey] != ' ')
                {
                    FetchIndexKey = FetchIndexKey + ReadLine[EndofIndexKey];
                    EndofIndexKey++;
                }
                for (int c = EndofIndexKey; c < ReadLine.Length; c++)
                    FetchIndexDetails = FetchIndexDetails + ReadLine[c];
                IndexLine.Add(FetchIndexKey, FetchIndexDetails);
            };
            IndexReader.Close();
            #endregion

            // Writers for the new data of .data and .index files
            System.IO.StreamWriter IndexWriter = new System.IO.StreamWriter(_wordNetPath + "\\index1.noun", false);
            System.IO.StreamWriter DataWriter = new System.IO.StreamWriter(_wordNetPath + "\\data1.noun", false);

            for (var i = 0; i < 29; i++)
            {
                IndexWriter.Write(Copyrights[i]+"\n");
                DataWriter.Write(Copyrights[i] + "\n");
            }


            // Processing each synset data and index
            for (int i = 0; i < k; i++)
            {
                // The location of first character in the synonyms-list on each Synset line
                int wordStart = 17;
                int SegIndex = 0;
                int WordIndex = 0;
                // Postions 14 and 15 represent the Hex Number of synonyms in the current Synset line
                int numWords = int.Parse(SynsetArray[i][14].ToString() + SynsetArray[i][15].ToString(), System.Globalization.NumberStyles.HexNumber);
                // Array for holding the start and end postion of each synonym in the synonyms-list
                int[] WordSegment = new int[numWords * 2];
                // The synonyms-list
                string[] _words = new string[numWords];

                // Phase-1: Extract the synonyms
                for (int j = 0; j < numWords; ++j)
                {
                    int wordEnd = SynsetArray[i].IndexOf(' ', wordStart + 1) - 1;
                    int wordLen = wordEnd - wordStart + 1;
                    _words[WordIndex] = SynsetArray[i].Substring(wordStart, wordLen);
                    WordIndex++;
                    WordSegment[SegIndex] = wordStart;
                    SegIndex++;
                    WordSegment[SegIndex] = wordLen;
                    SegIndex++;
                    // skip lex_id field
                    wordStart = SynsetArray[i].IndexOf(' ', wordEnd + 2) + 1;
                }

                // Phase-2: Avoid replacing the hyphen with underscore when both versions of the synonym are available in the synonyms-list
                // Without this step, the modified synonym can be duplicated in the synonyms-list

                bool Escape;
                for (int j = 0; j < numWords; j++)
                {
                    string NewSynonym;
                    string temp;
                    Escape = false;
                    if (_words[j].Contains("-"))
                    {
                        NewSynonym = _words[j].Replace("-", "_");
                        for (int d = 0; d < numWords; d++)
                        {
                            if (d != j && NewSynonym == _words[d])
                            {
                                Escape = true;
                                break;
                            }
                        }
                        if (!Escape)
                        {
                            SynsetArray[i] = SwapSubString(SynsetArray[i],WordSegment[j * 2], WordSegment[j * 2 + 1], NewSynonym);
                            if (IndexLine.ContainsKey(_words[j].ToLower()) && !IndexLine.ContainsKey(NewSynonym.ToLower()))
                            {
                                temp = IndexLine[_words[j].ToLower()];
                                IndexLine.Remove(_words[j].ToLower());
                                IndexLine.Add(NewSynonym.ToLower(), temp);
                            }
                        }
                    }
                }
                // write .data data
                DataWriter.Write(SynsetArray[i] + "\n");
            }
            DataWriter.Close();

            // Write .Index data
            foreach (var ID in IndexLine)
            {
                IndexWriter.Write(ID.Key + ID.Value + "\r\n");
            }
            IndexLine = null;
            IndexWriter.Close();
        }

        private string Lexicalize(string input)
        {
            var output = new StringBuilder();
            string[] sentences = _sentenceDetector.SentenceDetect(input);
            foreach (string sentence in sentences)
            {
                string[] tokens = _tokenizer.Tokenize(sentence);
                string[] tags = _posTagger.Tag(tokens);
                for (int currentTag = 0; currentTag < tags.Length; currentTag++)
                {
                    if (!IsStopWord.ContainsKey(tokens[currentTag]))
                    {
                        if (tags[currentTag].StartsWith("NN"))
                        {
                            if (tags[currentTag] == "NNS")
                                output.Append(_wn.Lemmatize(tokens[currentTag], "noun")).Append(" ");
                            else
                                output.Append(tokens[currentTag]).Append(" ");
                        }
                        else
                            if (tags[currentTag].StartsWith("VB"))
                            {
                                if (tags[currentTag]!="VBP")
                                output.Append(_wn.Lemmatize(tokens[currentTag], "verb")).Append(" ");
                            }
                            else
                                if (tags[currentTag].StartsWith("JJ"))
                                    output.Append(tokens[currentTag]).Append(" ");
                    }
                }
            }
            return output.ToString();
        }

        public void Dispose()
        {
            _tokenizer = null;
            _sentenceDetector = null;
            _posTagger = null;

            SynsetArray = null;
            IsStopWord = null;

            _wn.Dispose();
        }

        private string SwapSubString(string input, int startPostion, int stringLength, string subString)
        {
            if (input == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder(input);
            builder.Remove(startPostion, stringLength);
            builder.Insert(startPostion, subString);
            return builder.ToString();
        }

        private Dictionary<string, int> IsStopWord = new Dictionary<string, int>() {
                                            {"a",0},
                                            {"'s",0},
                                            {"able",0},
                                            {"about",0},
                                            {"above",0},
                                            {"according",0},
                                            {"accordingly",0},
                                            {"across",0},
                                            {"actually",0},
                                            {"after",0},
                                            {"afterward",0},
                                            {"afterwards",0},
                                            {"again",0},
                                            {"against",0},
                                            {"ain't",0},
                                            {"all",0},
                                            {"almost",0},
                                            {"alone",0},
                                            {"along",0},
                                            {"already",0},
                                            {"also",0},
                                            {"although",0},
                                            {"always",0},
                                            {"am",0},
                                            {"among",0},
                                            {"amongst",0},
                                            {"an",0},
                                            {"and",0},
                                            {"another",0},
                                            {"any",0},
                                            {"anybody",0},
                                            {"anyhow",0},
                                            {"anyone",0},
                                            {"anything",0},
                                            {"anyway",0},
                                            {"anyways",0},
                                            {"anywhere",0},
                                            {"apart",0},
                                            {"appear",0},
                                            {"appreciate",0},
                                            {"appropriate",0},
                                            {"around",0},
                                            {"as",0},
                                            {"aside",0},
                                            {"at",0},
                                            {"available",0},
                                            {"away",0},
                                            {"awfully",0},
                                            {"b",0},
                                            {"because",0},
                                            {"before",0},
                                            {"beforehand",0},
                                            {"behind",0},
                                            {"below",0},
                                            {"beside",0},
                                            {"besides",0},
                                            {"best",0},
                                            {"better",0},
                                            {"between",0},
                                            {"beyond",0},
                                            {"both",0},
                                            {"brief",0},
                                            {"but",0},
                                            {"by",0},
                                            {"c",0},
                                            {"c'mon",0},
                                            {"c's",0},
                                            {"came",0},
                                            {"can",0},
                                            {"can't",0},
                                            {"cannot",0},
                                            {"cant",0},
                                            {"certain",0},
                                            {"certainly",0},
                                            {"clearly",0},
                                            {"com",0},
                                            {"consequently",0},
                                            {"could",0},
                                            {"couldn't",0},
                                            {"course",0},
                                            {"currently",0},
                                            {"definitely",0},
                                            {"despite",0},
                                            {"did",0},
                                            {"didn't",0},
                                            {"different",0},
                                            {"d",0},
                                            {"do",0},
                                            {"does",0},
                                            {"doesn't",0},
                                            {"doing",0},
                                            {"don't",0},
                                            {"done",0},
                                            {"down",0},
                                            {"downwards",0},
                                            {"during",0},
                                            {"e",0},
                                            {"each",0},
                                            {"eg",0},
                                            {"eight",0},
                                            {"either",0},
                                            {"else",0},
                                            {"elsewhere",0},
                                            {"enough",0},
                                            {"entirely",0},
                                            {"especially",0},
                                            {"et",0},
                                            {"etc",0},
                                            {"even",0},
                                            {"ever",0},
                                            {"every",0},
                                            {"everybody",0},
                                            {"everyone",0},
                                            {"everything",0},
                                            {"everywhere",0},
                                            {"ex",0},
                                            {"exactly",0},
                                            {"example",0},
                                            {"except",0},
                                            {"f",0},
                                            {"far",0},
                                            {"few",0},
                                            {"for",0},
                                            {"former",0},
                                            {"formerly",0},
                                            {"from",0},
                                            {"further",0},
                                            {"furthermore",0},
                                            {"g",0},
                                            {"given",0},
                                            {"gone",0},
                                            {"got",0},
                                            {"gotten",0},
                                            {"greetings",0},
                                            {"h",0},
                                            {"had",0},
                                            {"hadn't",0},
                                            {"happens",0},
                                            {"hardly",0},
                                            {"hasn't",0},
                                            {"haven't",0},
                                            {"having",0},
                                            {"he",0},
                                            {"he's",0},
                                            {"hello",0},
                                            {"hence",0},
                                            {"her",0},
                                            {"here",0},
                                            {"here's",0},
                                            {"hereafter",0},
                                            {"hereby",0},
                                            {"herein",0},
                                            {"hereupon",0},
                                            {"hers",0},
                                            {"herself",0},
                                            {"hi",0},
                                            {"him",0},
                                            {"himself",0},
                                            {"his",0},
                                            {"hither",0},
                                            {"hopefully",0},
                                            {"how",0},
                                            {"howbeit",0},
                                            {"however",0},
                                            {"i",0},
                                            {"i'd",0},
                                            {"i'll",0},
                                            {"i'm",0},
                                            {"i've",0},
                                            {"ie",0},
                                            {"if",0},
                                            {"immediate",0},
                                            {"in",0},
                                            {"inasmuch",0},
                                            {"inc",0},
                                            {"indeed",0},
                                            {"inner",0},
                                            {"insofar",0},
                                            {"instead",0},
                                            {"into",0},
                                            {"inward",0},
                                            {"it",0},
                                            {"it'd",0},
                                            {"it'll",0},
                                            {"it's",0},
                                            {"its",0},
                                            {"itself",0},
                                            {"j",0},
                                            {"just",0},
                                            {"k",0},
                                            {"l",0},
                                            {"last",0},
                                            {"lately",0},
                                            {"later",0},
                                            {"latter",0},
                                            {"latterly",0},
                                            {"least",0},
                                            {"less",0},
                                            {"lest",0},
                                            {"let",0},
                                            {"let's",0},
                                            {"likely",0},
                                            {"little",0},
                                            {"ltd",0},
                                            {"m",0},
                                            {"mainly",0},
                                            {"many",0},
                                            {"me",0},
                                            {"meanwhile",0},
                                            {"merely",0},
                                            {"more",0},
                                            {"moreover",0},
                                            {"most",0},
                                            {"mostly",0},
                                            {"much",0},
                                            {"my",0},
                                            {"myself",0},
                                            {"n",0},
                                            {"namely",0},
                                            {"nd",0},
                                            {"near",0},
                                            {"nearly",0},
                                            {"necessary",0},
                                            {"neither",0},
                                            {"nevertheless",0},
                                            {"new",0},
                                            {"next",0},
                                            {"no",0},
                                            {"nobody",0},
                                            {"non",0},
                                            {"none",0},
                                            {"noone",0},
                                            {"nor",0},
                                            {"normally",0},
                                            {"not",0},
                                            {"nothing",0},
                                            {"novel",0},
                                            {"now",0},
                                            {"nowhere",0},
                                            {"o",0},
                                            {"of",0},
                                            {"off",0},
                                            {"oh",0},
                                            {"ok",0},
                                            {"okay",0},
                                            {"old",0},
                                            {"once",0},
                                            {"ones",0},
                                            {"onto",0},
                                            {"or",0},
                                            {"other",0},
                                            {"others",0},
                                            {"otherwise",0},
                                            {"our",0},
                                            {"ours",0},
                                            {"ourselves",0},
                                            {"out",0},
                                            {"outside",0},
                                            {"over",0},
                                            {"overall",0},
                                            {"p",0},
                                            {"particular",0},
                                            {"per",0},
                                            {"please",0},
                                            {"plus",0},
                                            {"possible",0},
                                            {"presumably",0},
                                            {"q",0},
                                            {"que",0},
                                            {"quite",0},
                                            {"qv",0},
                                            {"r",0},
                                            {"rather",0},
                                            {"rd",0},
                                            {"re",0},
                                            {"reasonably",0},
                                            {"regarding",0},
                                            {"regardless",0},
                                            {"regards",0},
                                            {"relatively",0},
                                            {"respectively",0},
                                            {"right",0},
                                            {"s",0},
                                            {"same",0},
                                            {"secondly",0},
                                            {"seen",0},
                                            {"self",0},
                                            {"selves",0},
                                            {"sensible",0},
                                            {"serious",0},
                                            {"seriously",0},
                                            {"seven",0},
                                            {"several",0},
                                            {"she",0},
                                            {"since",0},
                                            {"so",0},
                                            {"some",0},
                                            {"somebody",0},
                                            {"somehow",0},
                                            {"someone",0},
                                            {"something",0},
                                            {"somewhat",0},
                                            {"somewhere",0},
                                            {"soon",0},
                                            {"sorry",0},
                                            {"sub",0},
                                            {"such",0},
                                            {"sup",0},
                                            {"sure",0},
                                            {"t's",0},
                                            {"th",0},
                                            {"than",0},
                                            {"thanx",0},
                                            {"that",0},
                                            {"that's",0},
                                            {"thats",0},
                                            {"the",0},
                                            {"their",0},
                                            {"theirs",0},
                                            {"them",0},
                                            {"themselves",0},
                                            {"then",0},
                                            {"thence",0},
                                            {"there",0},
                                            {"there's",0},
                                            {"thereafter",0},
                                            {"thereby",0},
                                            {"therefore",0},
                                            {"therein",0},
                                            {"theres",0},
                                            {"thereupon",0},
                                            {"these",0},
                                            {"they",0},
                                            {"they'd",0},
                                            {"they'll",0},
                                            {"they're",0},
                                            {"they've",0},
                                            {"this",0},
                                            {"thorough",0},
                                            {"thoroughly",0},
                                            {"those",0},
                                            {"though",0},
                                            {"through",0},
                                            {"throughout",0},
                                            {"thru",0},
                                            {"thus",0},
                                            {"to",0},
                                            {"together",0},
                                            {"too",0},
                                            {"toward",0},
                                            {"towards",0},
                                            {"truly",0},
                                            {"twice",0},
                                            {"u",0},
                                            {"un",0},
                                            {"under",0},
                                            {"unfortunately",0},
                                            {"unless",0},
                                            {"unlikely",0},
                                            {"until",0},
                                            {"unto",0},
                                            {"up",0},
                                            {"upon",0},
                                            {"us",0},
                                            {"useful",0},
                                            {"usually",0},
                                            {"uucp",0},
                                            {"v",0},
                                            {"value",0},
                                            {"various",0},
                                            {"very",0},
                                            {"via",0},
                                            {"viz",0},
                                            {"vs",0},
                                            {"w",0},
                                            {"way",0},
                                            {"we",0},
                                            {"we'd",0},
                                            {"we'll",0},
                                            {"we're",0},
                                            {"we've",0},
                                            {"will",0},
                                            {"well",0},
                                            {"what",0},
                                            {"what's",0},
                                            {"whatever",0},
                                            {"when",0},
                                            {"whence",0},
                                            {"whenever",0},
                                            {"where",0},
                                            {"where's",0},
                                            {"whereafter",0},
                                            {"whereas",0},
                                            {"whereby",0},
                                            {"wherein",0},
                                            {"whereupon",0},
                                            {"wherever",0},
                                            {"whether",0},
                                            {"which",0},
                                            {"while",0},
                                            {"whither",0},
                                            {"who",0},
                                            {"who's",0},
                                            {"whoever",0},
                                            {"whole",0},
                                            {"whom",0},
                                            {"whose",0},
                                            {"why",0},
                                            {"willing",0},
                                            {"with",0},
                                            {"within",0},
                                            {"without",0},
                                            {"won't",0},
                                            {"wonder",0},
                                            {"would",0},
                                            {"wouldn't",0},
                                            {"x",0},
                                            {"y",0},
                                            {"yes",0},
                                            {"yet",0},
                                            {"you",0},
                                            {"you'd",0},
                                            {"you'll",0},
                                            {"you're",0},
                                            {"you've",0},
                                            {"your",0},
                                            {"yours",0},
                                            {"yourself",0},
                                            {"yourselves",0},
                                            {"z",0},
                                            {"zero",0},
                                            {"!",0},
                                            {"#",0},
                                            {"\"",0},
                                            {"%",0},
                                            {"$",0},
                                            {"'",0},
                                            {"&",0},
                                            {")",0},
                                            {"(",0},
                                            {"+",0},
                                            {"*",0},
                                            {"-",0},
                                            {",",0},
                                            {"/",0},
                                            {".",0},
                                            {";",0},
                                            {":",0},
                                            {"=",0},
                                            {"<",0},
                                            {"?",0},
                                            {">",0},
                                            {"@",0},
                                            {"[",0},
                                            {"]",0},
                                            {"_",0},
                                            {"^",0},
                                            {"`",0},
                                            {"{",0},
                                            {"}",0},
                                            {"|",0},
                                            {"~",0},
                                            {"one",0},
                                            {"two",0},
                                            {"three",0},
                                            {"four",0},
                                            {"five",0},
                                            {"six",0},
                                            {"seve",0},
                                            {"nine",0},
                                            {"ten",0},
                                            {"eleven",0},
                                            {"twelve",0},
                                            {"thirdteen",0},
                                            {"fourteen",0},
                                            {"fifteen",0},
                                            {"sixteen",0},
                                            {"seventeen",0},
                                            {"eighteen",0},
                                            {"nineteen",0},
                                            {"twenty",0},
                                            {"http",0},
                                            {"https",0},
                                            {"ww",0},
                                            {"www",0},
                                            {"org",0},
                                            {"net",0},
                                            {"co",0},
                                            {"om",0},
                                            {"on",0},
                                            {"c0m",0},
                                            {"c0",0},
                                            {"0m",0},
                                            {"gov",0},
                                            {"edu",0},
                                            {"cn",0},
                                            {"cm",0},
                                            {"con",0},
                                            {"mm",0},
                                            {"site",0},
                                            {"website",0},
                                            {"sdn",0},
                                            {"bhd",0},
                                            {"was",0},
                                            {"were",0},
                                            {"is",0},
                                            {"are",0},
                                            {"be",0},
                                            {"has",0},
                                            {"have",0},
                                            {"been",0},
                                            {"num",0},
                                            {"should",0},
                                            {"may",0},
                                            {"might",0},
                                            {"ppv",0}
        };
    }
}

