using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenNLP.Util;
using SharpEntropy;

namespace OpenNLP.Tokenizer
{
	/// <summary>
	/// A Tokenizer for converting raw text into separated tokens.  It uses
	/// Maximum Entropy to make its decisions.  The features are loosely
	/// based on Jeff Reynar's UPenn thesis "Topic Segmentation:
	/// Algorithms and Applications.", which is available from his
	/// homepage: http://www.cis.upenn.edu/~jcreynar.
	/// </summary>
	public class MaximumEntropyTokenizer : AbstractTokenizer
	{
        internal static Regex AlphaNumeric = new Regex("^[A-Za-z0-9]+$", RegexOptions.Compiled);

		/// <summary>
		/// the maximum entropy model to use to evaluate contexts.
		/// </summary>
		private readonly IMaximumEntropyModel _model;
		
		/// <summary>
		/// The context generator.
		/// </summary>
        private readonly IContextGenerator<Tuple<string, int>> _contextGenerator;
		
		/// <summary>
        /// Optimization flag to skip alpha numeric tokens for further tokenization.
		/// </summary>
		public bool AlphaNumericOptimization { get; set; }
		
		/// <summary>
		/// Class constructor which takes the string locations of the
		/// information which the maxent model needs.
		/// </summary>
		public MaximumEntropyTokenizer(IMaximumEntropyModel model)
		{
			_contextGenerator = new TokenContextGenerator();
			AlphaNumericOptimization = false;
			this._model = model;
		}
		
		/// <summary>Tokenizes the string</summary>
		/// <param name="input">The string to be tokenized</param>
		/// <returns>A span array containing individual tokens as elements</returns>
		public override Span[] TokenizePositions(string input)
		{
            if (string.IsNullOrEmpty(input)) { return new Span[0]; }

			var tokens = SplitOnWhitespaces(input);
			var newTokens = new List<Span>();
			var tokenProbabilities = new List<double>();
			
			for (int currentToken = 0, tokenCount = tokens.Length; currentToken < tokenCount; currentToken++)
			{
				var tokenSpan = tokens[currentToken];
				string token = input.Substring(tokenSpan.Start, (tokenSpan.End) - (tokenSpan.Start));
				// Can't tokenize single characters
				if (token.Length < 2)
				{
					newTokens.Add(tokenSpan);
					tokenProbabilities.Add(1.0);
				}
				else if (AlphaNumericOptimization && AlphaNumeric.IsMatch(token))
				{
					newTokens.Add(tokenSpan);
					tokenProbabilities.Add(1.0);
				}
				else
				{
					int startPosition = tokenSpan.Start;
					int endPosition = tokenSpan.End;
					int originalStart = tokenSpan.Start;
					double tokenProbability = 1.0;
					for (int currentPosition = originalStart + 1; currentPosition < endPosition; currentPosition++)
					{
					    var context = _contextGenerator.GetContext(new Tuple<string, int>(token, currentPosition - originalStart));
						double[] probabilities = _model.Evaluate(context);
						string bestOutcome = _model.GetBestOutcome(probabilities);
						
						tokenProbability *= probabilities[_model.GetOutcomeIndex(bestOutcome)];
						if (bestOutcome == TokenContextGenerator.SplitIndicator)
						{
							newTokens.Add(new Span(startPosition, currentPosition));
							tokenProbabilities.Add(tokenProbability);
							startPosition = currentPosition;
							tokenProbability = 1.0;
						}
					}
					newTokens.Add(new Span(startPosition, endPosition));
					tokenProbabilities.Add(tokenProbability);
				}
			}
			
			return newTokens.ToArray();
		}

	}
}
