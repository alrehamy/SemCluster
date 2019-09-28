using System;
using System.Collections.Generic;
using System.Text;
using WordNetApi.Core;

namespace WordNetApi.Lemmatizer
{
    public class LookupIndexWordOperation : IOperation
    {
        private WordNetEngine mEngine;

        public LookupIndexWordOperation(WordNetEngine engine)
        {
            mEngine = engine;
        }

        #region IOperation Members

        public bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            if (!baseForms.Contains(lemma) && mEngine.GetSynSets(lemma, partOfSpeech).Count>0)
            {
                baseForms.Add(lemma);
                return true;
            }
            return false;
        }

        #endregion
    }
}
