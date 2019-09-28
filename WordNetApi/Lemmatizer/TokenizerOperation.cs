using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using WordNetApi.Core;

namespace WordNetApi.Lemmatizer
{
    public class TokenizerOperation : AbstractDelegatingOperation
    {
        /// <summary>
        /// Parameter that determines the operations this operation
        /// will perform on the tokens.
        /// </summary>
        public const string TokenOperations = "token_operations";
        /// <summary>
        /// Parameter that determines the operations this operation
        /// will perform on the phrases.
        /// </summary>
        public const string PhraseOperations = "phrase_operations";
        /// <summary>
        /// Parameter list that determines the delimiters this
        /// operation will use to concatenate tokens.
        /// </summary>
        public const string Delimiters = "delimiters";

        private WordNetEngine mEngine;

        private string[] mDelimiters;

        public TokenizerOperation(WordNetEngine engine)
        {
            mEngine = engine;
        }

        public TokenizerOperation(WordNetEngine engine, string[] delimiters)
        {
            mEngine = engine;
            mDelimiters = delimiters;
        }

        #region IOperation Members

        public override bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            string[] tokens = Util.Split(lemma);
            List<string>[] tokenForms = new List<string>[tokens.Length];

            if (!HasDelegate(TokenOperations))
            {
                AddDelegate(TokenOperations, new IOperation[] { new LookupIndexWordOperation(mEngine) });
            }
            if (!HasDelegate(PhraseOperations))
            {
                AddDelegate(PhraseOperations, new IOperation[] { new LookupIndexWordOperation(mEngine) });
            }

            for (int currentToken = 0; currentToken < tokens.Length; currentToken++)
            {
                tokenForms[currentToken] = new List<string>();
                tokenForms[currentToken].Add(tokens[currentToken]);
                ExecuteDelegate(tokens[currentToken], partOfSpeech, tokenForms[currentToken], TokenOperations);
            }
            bool foundForms = false;
            for (int currentTokenForm = 0; currentTokenForm < tokenForms.Length; currentTokenForm++)
            {
                for (int tokenFormToCompare = tokenForms.Length - 1; tokenFormToCompare >= currentTokenForm; tokenFormToCompare--)
                {
                    if (TryAllCombinations(partOfSpeech, tokenForms, currentTokenForm, tokenFormToCompare, baseForms))
                    {
                        foundForms = true;
                    }
                }
            }
            return foundForms;
        }

        #endregion

        private bool TryAllCombinations(string partOfSpeech, List<string>[] tokenForms, int startIndex, int endIndex, List<string> baseForms)
        {
            int length = endIndex - startIndex + 1;
            int[] indexArray = new int[length];
            int[] endArray = new int[length];
            for (int i = 0; i < indexArray.Length; i++)
            {
                indexArray[i] = 0;
                endArray[i] = tokenForms[startIndex + i].Count - 1;
            }

            bool foundForms = false;
            for (; ; )
            {
                string[] tokens = new string[length];
                for (int i = 0; i < length; i++)
                {
                    tokens[i] = tokenForms[i + startIndex][indexArray[i]];
                }
                for (int i = 0; i < mDelimiters.Length; i++)
                {
                    if (TryAllCombinations(partOfSpeech, tokens, mDelimiters[i], baseForms))
                    {
                        foundForms = true;
                    }
                }

                if (IsArrayEqual(indexArray, endArray))
                {
                    break;
                }

                for (int i = length - 1; i >= 0; i--)
                {
                    if (indexArray[i] == endArray[i])
                    {
                        indexArray[i] = 0;
                    }
                    else
                    {
                        indexArray[i]++;
                        break;
                    }
                }
            }
            return foundForms;
        }

        private bool TryAllCombinations(string partOfSpeech, string[] tokens, string delimiter, List<string> baseForms)
        {
            BitArray bits = new BitArray(64);
            int size = tokens.Length - 1;

            bool foundForms = false;
            do
            {
                string lemma = Util.GetLemma(tokens, bits, delimiter);
                if (ExecuteDelegate(lemma, partOfSpeech, baseForms, PhraseOperations))
                {
                    foundForms = true;
                }
            }
            while (Util.Increment(bits, size));

            return foundForms;
        }

        private bool IsArrayEqual(int[] array1, int[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
