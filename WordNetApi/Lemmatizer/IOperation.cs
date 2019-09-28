using System;
using System.Collections.Generic;
using System.Text;

namespace WordNetApi.Lemmatizer
{
    public interface IOperation
    {
        /// <summary>
        /// Execute the operation.
        /// </summary>
        /// <param name="lemma">
        /// input lemma to look up
        /// </param>
        ///<param name="partOfSpeech">
        /// part of speech of the lemma to look up
        /// </param>
        /// <param name="baseForms">
        /// List to which all discovered base forms should be added.
        /// </param>
        /// <returns>
        /// True if at least one base form was discovered by the operation and
        /// added to baseForms.
        /// </returns>
        bool Execute(string lemma, string partOfSpeech, List<string> baseForms);
    }
}
