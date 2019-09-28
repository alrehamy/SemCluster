using System;
using System.IO;

namespace SharpEntropy.IO
{
	/// <summary>
	/// A writer for GIS models that saves models in the binary format used by the java 
	/// version of MaxEnt.  This binary format stores data using big-endian values, which means
	/// that the C# version must reverse the byte order of each value in turn, making it
	/// less efficient.  Use only for compatibility with the java MaxEnt library.
	/// </summary>
	public class JavaBinaryGisModelWriter : GisModelWriter
	{
		private Stream mOutput;
		private byte[] mBuffer = new byte[7];
		private System.Text.Encoding mEncoding = System.Text.Encoding.UTF8;
		
		/// <summary>
		/// Default constructor.
		/// </summary>
		public JavaBinaryGisModelWriter()
		{
		}
			
		/// <summary> Takes a GisModel and a File and
		/// writes the model to that file.
		/// </summary>
		/// <param name="model">The GisModel which is to be persisted.
		/// </param>
		/// <param name="fileName">The name of the file in which the model is to be persisted.
		/// </param>
		public void Persist(GisModel model, string fileName)
		{
			using (mOutput = new FileStream(fileName, FileMode.Create))
			{
				base.Persist(model);
			}
		}

		/// <summary>
		/// Takes a GisModel and a Stream and writes the model to that stream.
		/// </summary>
		/// <param name="model">
		/// The GIS model which is to be persisted.
		/// </param>
		/// <param name="dataOutputStream">
		/// The Stream which will be used to persist the model.
		/// </param>
		public void Persist(GisModel model, Stream dataOutputStream)
		{
			using (mOutput = dataOutputStream)
			{
				base.Persist(model);
			}
		}

		/// <summary>
		/// Writes a UTF-8 encoded string to the model file.
		/// </summary>
		/// /// <param name="data">
		/// The string data to be persisted.
		/// </param>
		protected override void WriteString(string data)
		{
			mOutput.WriteByte((byte)(mEncoding.GetByteCount(data) / 256));
			mOutput.WriteByte((byte)(mEncoding.GetByteCount(data) % 256));
			mOutput.Write(mEncoding.GetBytes(data), 0, mEncoding.GetByteCount(data));
		}
		
		/// <summary>
		/// Writes a 32-bit signed integer to the model file.
		/// </summary>
		/// /// <param name="data">
		/// The integer data to be persisted.
		/// </param>
		protected override void WriteInt32(int data)
		{
			mBuffer = BitConverter.GetBytes(data);
			Array.Reverse(mBuffer);
			mOutput.Write(mBuffer, 0, 4);
		}
		
		/// <summary>
		/// Writes a double-precision floating point number to the model file.
		/// </summary>
		/// /// <param name="data">
		/// The floating point data to be persisted.
		/// </param>
		protected override void WriteDouble(double data)
		{
			mBuffer = BitConverter.GetBytes(data);
			Array.Reverse(mBuffer);
			mOutput.Write(mBuffer, 0, 8);
		}
	}
}
