using System;
using System.Collections.Generic;
using System.Text;
using WordNetApi.Core;

namespace WordNetApi.Lemmatizer
{
    /// <summary>Lookup the word in the exceptions file of the given part-of-speech. </summary>
    public class LookupExceptionsOperation : IOperation
    {
        private WordNetEngine mEngine;

        public LookupExceptionsOperation(WordNetEngine engine)
        {
            mEngine = engine;
        }

        #region IOperation Members

        public bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            bool addedBaseForm = false;
            string[] exceptionForms = mEngine.GetExceptionForms(lemma, partOfSpeech);

            foreach (string exceptionForm in exceptionForms)
            {
                if (!baseForms.Contains(exceptionForm))
                {
                    baseForms.Add(exceptionForm);
                    addedBaseForm = true;
                }
            }

            return addedBaseForm;
        }

        #endregion
    }
}
