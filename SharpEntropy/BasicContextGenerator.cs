using System;

namespace SharpEntropy
{
	/// <summary>
	/// Generate contexts for maxent decisions, assuming that the input
	/// given to the GetContext() method is a string containing contextual
	/// predicates separated by spaces, e.g:
	/// <p>
	/// cp_1 cp_2 ... cp_n
	/// </p>
	/// </summary>
	public class BasicContextGenerator : IContextGenerator<string>
	{
		/// <summary>
		/// Builds up the list of contextual predicates given a string.
		/// </summary>
		/// <param name="input">
		/// string with contextual predicates separated by spaces.
		/// </param>
		/// <returns>string array of contextual predicates.</returns>
		public virtual string[] GetContext(string input)
		{
			return input.Split(' ');
		}
	}
}
