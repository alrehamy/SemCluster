using System;
using System.Collections.Generic;
using System.Text;

namespace WordNetApi.Lemmatizer
{
    public abstract class AbstractDelegatingOperation : IOperation
    {
        private Dictionary<string, IOperation[]> mOperationSets;

        public virtual void AddDelegate(string key, IOperation[] operations)
        {
            if (!mOperationSets.ContainsKey(key))
            {
                mOperationSets.Add(key, operations);
            }
            else
            {
                mOperationSets[key] = operations;
            }
        }

        protected internal AbstractDelegatingOperation()
        {
            mOperationSets = new Dictionary<string, IOperation[]>();
        }

        //protected internal abstract AbstractDelegatingOperation getInstance(System.Collections.IDictionary params_Renamed);

        protected internal virtual bool HasDelegate(string key)
        {
            return mOperationSets.ContainsKey(key);
        }

        protected internal virtual bool ExecuteDelegate(string lemma, string partOfSpeech, List<string>baseForms, string key)
        {
            IOperation[] operations = mOperationSets[key];
            bool result = false;
            for (int currentOperation = 0; currentOperation < operations.Length; currentOperation++)
            {
                if (operations[currentOperation].Execute(lemma, partOfSpeech, baseForms))
                {
                    result = true;
                }
            }
            return result;
        }

        #region IOperation Members

        public abstract bool Execute(string lemma, string partOfSpeech, List<string> baseForms);

        #endregion
    }
}
