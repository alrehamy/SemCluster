using System;
using System.Collections.Generic;
using System.Text;

namespace WordNetApi.Lemmatizer
{
    /// <summary>
    /// Remove all applicable suffixes from the word(s) and do a look-up.
    /// </summary>
    public class DetachSuffixesOperation : AbstractDelegatingOperation
    {
        public const string Operations = "operations";

        private Dictionary<string, string[][]> mSuffixMap;

        public DetachSuffixesOperation(Dictionary<string, string[][]> suffixMap)
        {
            mSuffixMap = suffixMap;
        }

        #region IOperation Members

        public override bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            if (!mSuffixMap.ContainsKey(partOfSpeech))
            {
                return false;
            }
            string[][] suffixArray = mSuffixMap[partOfSpeech];
            
            bool addedBaseForm = false;
            for (int currentSuffix = 0; currentSuffix < suffixArray.Length; currentSuffix++)
            {
                if (lemma.EndsWith(suffixArray[currentSuffix][0]))
                {
                    string stem = lemma.Substring(0, (lemma.Length - suffixArray[currentSuffix][0].Length) - (0)) + suffixArray[currentSuffix][1];
                    if (ExecuteDelegate(stem, partOfSpeech, baseForms, Operations))
                    {
                        addedBaseForm = true;
                    }
                }
            }
            return addedBaseForm;
        }

        #endregion
    }
}
