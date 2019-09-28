using System;

namespace SharpEntropy
{
	/// <summary> 
	/// Generate contexts for maximum entropy decisions.
	/// </summary>
	public interface IContextGenerator
	{
		/// <summary>
		/// Builds up the list of contextual predicates given an object.
		/// </summary>
		string[] GetContext(object input);
	}

    /// <summary> 
    /// Generate contexts for maximum entropy decisions.
    /// </summary>
    public interface IContextGenerator<T>
    {
        /// <summary>
        /// Builds up the list of contextual predicates given an object of type T.
        /// </summary>
        string[] GetContext(T input);
    }

}
