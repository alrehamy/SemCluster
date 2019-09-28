using System;
using System.Collections.Generic;

namespace OpenNLP.PosTagger
{
	/// <summary>
	/// Provides a means of determining which tags are valid for a particular word based on a tag dictionary read from a file.
	/// </summary>
	public class PosLookupList
	{
		private Dictionary<string, string[]> mDictionary;
		private bool mIsCaseSensitive;
		
		public PosLookupList(string file) : this(file, true)
		{
		}
		
		/// <summary>
		/// Create tag dictionary object with contents of specified file and using specified case to determine how to access entries in the tag dictionary.
		/// </summary>
		/// <param name="file">
		/// The file name for the tag dictionary.
		/// </param>
		/// <param name="caseSensitive">
		/// Specifies whether the tag dictionary is case sensitive or not.
		/// </param>
		public PosLookupList(string file, bool caseSensitive) : this(new System.IO.StreamReader(file, System.Text.Encoding.UTF7), caseSensitive)
		{
		}
		
		/// <summary>
		/// Create tag dictionary object with contents of specified file and using specified case to determine how to access entries in the tag dictionary.
		/// </summary>
		/// <param name="reader">
		/// A reader for the tag dictionary.
		/// </param>
		/// <param name="caseSensitive">
		/// Specifies whether the tag dictionary is case sensitive or not.
		/// </param>
		public PosLookupList(System.IO.StreamReader reader, bool caseSensitive)
		{
            mDictionary = new Dictionary<string, string[]>();
			mIsCaseSensitive = caseSensitive;
			for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
			{
				string[] parts = line.Split(' ');
				string[] tags = new string[parts.Length - 1];
				for (int currentTag = 0, tagCount = parts.Length - 1; currentTag < tagCount; currentTag++)
				{
					tags[currentTag] = parts[currentTag + 1];
				}
				mDictionary[parts[0]] = tags;
			}
		}
		
		/// <summary>
		/// Returns a list of valid tags for the specified word. </summary>
		/// <param name="word">
		/// The word.
		/// </param>
		/// <returns>
		/// A list of valid tags for the specified word or null if no information is available for that word.
		/// </returns>
		public virtual string[] GetTags(string word)
		{
			if (!mIsCaseSensitive)
			{
                word = word.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (mDictionary.ContainsKey(word))
            {
			    return mDictionary[word];
            }
            else
            {
                return null;
            }
		}
	}
}
