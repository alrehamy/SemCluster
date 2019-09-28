using System;
using System.IO;

namespace SharpEntropy.IO
{
	/// <summary> 
	/// Model writer that saves models in plain text format.
	/// </summary>
	public class PlainTextGisModelWriter : GisModelWriter
	{
		private StreamWriter mOutput;
		
		/// <summary>
		/// Default constructor.
		/// </summary>
		public PlainTextGisModelWriter()
		{
		}
			
		/// <summary>
		/// Takes a GIS model and a file and writes the model to that file.
		/// </summary>
		/// <param name="model">
		/// The GisModel which is to be persisted.
		/// </param>
		/// <param name="fileName">
		/// The name of the file in which the model is to be persisted.
		/// </param>
		public void Persist(GisModel model, string fileName)
		{
            using (mOutput = new StreamWriter(fileName, false, System.Text.Encoding.UTF7))
			{
				base.Persist(model);
			}
		}

		/// <summary>
		/// Takes a GisModel and a stream and writes the model to that stream.
		/// </summary>
		/// <param name="model">
		/// The GisModel which is to be persisted.
		/// </param>
		/// <param name="writer">
		/// The StreamWriter which will be used to persist the model.
		/// </param>
		public void Persist(GisModel model, StreamWriter writer)
		{
			using (mOutput = writer)
			{
				base.Persist(model);
			}
		}
	
		/// <summary>
		/// Writes a string to the model file.
		/// </summary>
		/// /// <param name="data">
		/// The string data to be persisted.
		/// </param>
		protected override void WriteString(string data)
		{
			mOutput.Write(data);
			mOutput.WriteLine();
		}
		
		/// <summary>
		/// Writes a 32-bit signed integer to the model file.
		/// </summary>
		/// <param name="data">
		/// The integer data to be persisted.
		/// </param>
		protected override void WriteInt32(int data)
		{
			mOutput.Write(data.ToString(System.Globalization.CultureInfo.InvariantCulture));
			mOutput.WriteLine();
		}
		
		/// <summary>
		/// Writes a double-precision floating point number to the model file.
		/// </summary>
		/// <param name="data">
		/// The floating point data to be persisted.
		/// </param>
		protected override void WriteDouble(double data)
		{
			mOutput.Write(data.ToString(System.Globalization.CultureInfo.InvariantCulture));
			mOutput.WriteLine();
		}
	}
}
