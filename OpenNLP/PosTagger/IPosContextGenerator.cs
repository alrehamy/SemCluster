using System;
using System.Collections;

namespace OpenNLP.PosTagger
{
	/// <summary> 
	/// The interface for a context generator for the POS Tagger.
	/// </summary>	
	public interface IPosContextGenerator : Util.IBeamSearchContextGenerator
	{
		new string[] GetContext(int position, string[] tokens, string[] previousTags, object[] additionalContext);
	}
}
