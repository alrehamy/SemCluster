# SemCluster
Full implementation of SemCluster Algorithm


SemCluster is a text analytics tool for scalable big data projects, and it aims to create analytic value from large volumes of textual contents by linking them on the metadata level to facilitate various semantic-based tasks such as semantic search, relationship discovery, graphical representation, structural information retrieval and so forth. This tool is fundamentally based on the following prize winning research papers:

1-	Alrehamy, H.H. and Walker, C., 2017, September. SemCluster: unsupervised automatic keyphrase extraction using affinity propagation. In UK Workshop on Computational Intelligence (pp. 222-235). Springer, Cham.

2-	Alrehamy, H. and Walker, C., 2018. Exploiting extensible background knowledge for clustering-based automatic keyphrase extraction. Soft Computing, 22(21), pp.7041-7057.

A use case of SemCluster is available in the following PhD thesis: 

Alrehamy, H., 2018. Extensible metadata management framework for personal data lake (Doctoral dissertation, Cardiff Uuniversity).

SemCluster requires naked text as input which then passes the processing pipeline construed in the above papers. 
SemCluster is unsupervised and exploits extensible background knowledge, which means, it requires no training of whatsoever but it demands the availability of knowledgebase(s). It relies on WordNet by default, however, it allows incorporating information from any other knowledgebase through extensibility.

# Code Details
The current implementation of SemCluster consists of 6 core modules which are:
(1)	Data repository
(2)	OpenNLP
(3)	Entropy
(4)	WordNetAPI
(5)	Affinity Propagation
(6)	SemCluster logic

# Data repository
This is the data module, and it is just a structural folder that contains the following subfolders:
Build: a folder to store trained machine learning models for text tokenization, POS tagging, and chunking.
PlugIns: a folder to store knowledgebase plugins in the form of dynamic linking libraries.
WordNet: a folder to store WordNet files.

# OpenNLP
This module represents the famous OpenNLP library. Since long time, I couldn’t find an efficient C# translation of OpenNLP, therefore it was decided that SemCluster needs a state of the art OpenNLPSharp which was fundamentally developed over the available code repositories but with advanced optimizations to enhance the overall functional complexity of SemCluster, in terms of computation resources, and execution time. The benchmarking of the current OpenNLPSharp implementation will be available soon in a separate Git.

OpenNLP is used as follows (See SemCluster.cs in TextAnalytics project)

private MaximumEntropySentenceDetector _sentenceDetector;
private AbstractTokenizer _tokenizer;
private EnglishMaximumEntropyPosTagger _posTagger;
private EnglishTreebankChunker _chunker; 

# Entropy
This module is a helper for OpenNLP similar to the original coding and it offers multiple entropy-based NLP operations, for instance:

_sentenceDetector = new EnglishMaximumEntropySentenceDetector(DataFolder + "EnglishSD.nbin");

# WordNetAPI
This module offers a C# interface for querying WordNet. It also offers capabilities to support semantic similarity measurement, as well as WordNet based lemmatization (an important intermediate task in NLP, e.g. children -> child). The modules loads WordNet and then offers information retrieval through Synset. There are two loading options: Hard drive based, and RAM based. The second option is more efficient since WordNet contents are mapped into Hash table and stored in the physical memory, which makes querying complexity O(1). 

WordNet is utilized as follows:

private WordNetEngine _wn;      // WordNet engine instance
private SynSet RootVirtualNode; // WordNet root node (for similarity computation)
private WordNetEngine.SynSetRelation[] SynSetRelationTypes; // Semantic relations allowed to scan for LCS computation

And the engine can be initialized as follows:

_wn = new WordNetEngine(DataFolder + "WordNet", InMemoryWordNet);

Where DataFolder is explained above, and InMemoryWordNet is a flag to determine the loading option, in our case it is set to True.

To query WordNet for the input “dummy”:

List<SynSet> Senses = _wn.GetSynSets(“dummy”, false, WordNetEngine.POS.Noun);

To specify relationships:

SynSetRelationTypes = new WordNetApi.Core.WordNetEngine.SynSetRelation[2];
SynSetRelationTypes[0] = WordNetApi.Core.WordNetEngine.SynSetRelation.Hypernym;
SynSetRelationTypes[1] = WordNetApi.Core.WordNetEngine.SynSetRelation.InstanceHypernym;

# Affinity Propagation
This module represents the implementation of a state of the art clustering algorithm called “Affinity Propagation”, which is described in the following paper:

Frey, B.J. and Dueck, D., 2007. Clustering by passing messages between data points. science, 315(5814), pp.972-976.

For computational efficiency reasons, it was decided that AP should be implemented in unmanaged code style, accordingly, it is written in C++ and linked to SemCluster implementation through CLI, therefore, the algorithm can be initialized in the following style (See SemCluster.cs in TextAnalytics project):

AffinityPropagationClustering ap=new AffinityPropagationClustering();
int[] res = ap.Run(S, d, 1, 0.9, 1000, 50);

Where S is a full similarity matrix and d is its dimension. Other values can be understood by reading our papers. 

# SemCluster logic
This module represents the actual implementation of SemCluster algorithm and its six-step workflow to identify the keyphrases in an input text and subsequently tagging each phrase with WordNet-based ontology information as machine-readable metadata. SemClsuter.cs is the backbone of the module. The class WordNetBoostrap.cs preporcesses aims to preporcess WordNet files for one time only, and the preprocessing involves converting the definition of each synset into machine readable by removing stop words and then lemmatizing each word to its base form. Therefore this class must be called only in the first run of SemClsuter tool and then deleted.
Due to copyright issues, certain parts of the code in SemClsuter.cs have been replaced with other code, however the overall tool is still executable and can be used both in production and research, for more information regarding this issue please contact me (alrehamyhh@cardiff.ac.uk).

# Plugging
The Git contains a DBPedia interface that is built around the official API of DBPedia. This project aims to demonstrate SemCluster’s tractability towards knowledge extensibility through software pluggability. For instance, SemCluster retrieves the semantic information of the word “tweet” from WordNet (which is an old definition) and exploits the DBPedia interface plugin to retrieve extra semantic information for this word from DBPedia graph (which is a newer definition) and depending on the internal WSD algorithm, it decides which information is more appropriate in a given context.

The software plugging mechanism is implemented as a function in the class SemCluster.cs and has the following signature:

private void PlugInsManager(string DataFolder)

Where PlugInsManager loads all the plugins in the folder PlugIns, and plugs them in SemCluster's workflow architecture in the following style:

for (int i = 0; i < PlugIns.Length; i++)
{
Type PlugInType = Assembly.LoadFrom(DataFolder + "PlugIns\\" + PlugIns[i]).GetType(PlugIns[i].Substring(0, PlugIns[i].Length - 4) + ".Driver");
if (PlugInType != null)
{
KBDrivers.Add(Activator.CreateInstance(PlugInType));
 KBDriversQueryPointers.Add(PlugInType.GetMethod("Query", KBDriverQuerySignture));
}
}

Where KBDrivers is a list of various instances.

As of the time of writing this description, SemCluster is cited in 10 research papers, and is used in 5 Big Data projects whether for benchmarking or value creation. For more information please follow my scholar account (Hassan H Alrehamy)

