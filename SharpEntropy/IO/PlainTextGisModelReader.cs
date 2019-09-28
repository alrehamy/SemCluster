using System;
using System.IO;

namespace SharpEntropy.IO
{
	/// <summary>
	/// A reader for GIS models stored in plain text format.
	/// </summary>
	public class PlainTextGisModelReader : GisModelReader
	{
		private StreamReader mInput;
		
		/// <summary>
		/// Constructor which directly instantiates the StreamReader containing
		/// the model contents.
		/// </summary>
		/// <param name="reader">
		/// The StreamReader containing the model information.
		/// </param>
		public PlainTextGisModelReader(StreamReader reader)
		{
			using (mInput = reader)
			{
				base.ReadModel();
			}
		}
		
		/// <summary>
		/// Constructor which takes a file and creates a reader for it. 
		/// </summary>
		/// <param name="fileName">
		/// The full path and file name in which the model is stored.
		/// </param>
		public PlainTextGisModelReader(string fileName)
		{
			using (mInput = new StreamReader(fileName, System.Text.Encoding.UTF7))
			{
				base.ReadModel();
			}
		}

		/// <summary>
		/// Reads a 32-bit signed integer from the model file.
		/// </summary>
		protected override int ReadInt32()
		{
			return int.Parse(mInput.ReadLine(), System.Globalization.CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads a double-precision floating point number from the model file.
		/// </summary>
		protected override double ReadDouble()
		{
			return double.Parse(mInput.ReadLine(), System.Globalization.CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads a string from the model file.
		/// </summary>
		protected override string ReadString()
		{
			return mInput.ReadLine();
		}

	}
}
