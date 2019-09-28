using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNLP
{
    /// <summary>
    /// A word tagged by a part of speech tagger
    /// ex: will/MD | to/TO | etc.
    /// </summary>
    public class TaggedWord
    {
        public string Tag { get; set; }
        public string Word { get; set; }
        public int Index { get; set; }
        public  WordNetApi.Core.SynSet Sense { get; set; }

        /// <summary>
        /// Constructor for string of format "will/MD"
        /// </summary>
        public TaggedWord(string stringTaggedWord, int indexInGroup)
        {
            if (stringTaggedWord.Contains("/"))
            {
                this.Word = stringTaggedWord.Split('/').First();
                this.Tag = stringTaggedWord.Split('/').Last();
                this.Index = indexInGroup;
                this.Sense = null;
            }
        }

        public TaggedWord(string word, string tag, int index)
        {
            this.Word = word;
            this.Tag = tag;
            this.Index = index;
            this.Sense = null;
        }

        public override string ToString()
        {
            return this.Word+"/"+this.Tag;
        }
    }
}
