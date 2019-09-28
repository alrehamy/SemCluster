using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SharpEntropy.IO
{
	/// <summary>
	/// A reader for GIS models stored in a binary format.  This format is not the one
	/// used by the <see cref="SharpEntropy.IO.JavaBinaryGisModelReader">java version of MaxEnt</see>.
	/// It has two main differences, designed for performance when loading the data
	/// from file: first, it uses big endian data values, which is native for C#, and secondly it
	/// encodes the outcome patterns and values in a more efficient manner.
	/// </summary>
	public class BinaryGisModelReader : GisModelReader
	{
		private readonly Stream _input;
		private readonly byte[] _buffer;
		private int _stringLength = 0;
		private readonly Encoding _encoding = Encoding.UTF8;

		/// <summary>
		/// Constructor which directly instantiates the Stream containing
		/// the model contents.
		/// </summary>
		/// <param name="dataInputStream">
		/// The Stream containing the model information.
		/// </param>
		public BinaryGisModelReader(Stream dataInputStream)
		{
			using (_input = dataInputStream)
			{
				_buffer = new byte[256];
				base.ReadModel();
			}
		}
		
		/// <summary>
		/// Constructor which takes a filename and creates a reader for it. 
		/// </summary>
		/// <param name="fileName">
		/// The full path and name of the file in which the model is stored.
		/// </param>
		public BinaryGisModelReader(string fileName)
		{
			using (_input = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				_buffer = new byte[256];
				base.ReadModel();
			}
		}

		/// <summary>
		/// Reads a 32-bit signed integer from the model file.
		/// </summary>
		protected override int ReadInt32()
		{
			_input.Read(_buffer, 0, 4);
			return BitConverter.ToInt32(_buffer, 0);
		}
		
		/// <summary>
		/// Reads a double-precision floating point number from the model file.
		/// </summary>
		protected override double ReadDouble()
		{
			_input.Read(_buffer, 0, 8);
			return BitConverter.ToDouble(_buffer, 0);
		}
		
		/// <summary>
		/// Reads a UTF-8 encoded string from the model file.
		/// </summary>
		protected override string ReadString()
		{
			_stringLength = _input.ReadByte();
			_input.Read(_buffer, 0, _stringLength);
			return _encoding.GetString(_buffer, 0, _stringLength);
		}

		/// <summary>
		/// Reads the predicate data from the file in a more efficient format to that implemented by
		/// GisModelReader.
		/// </summary>
		/// <param name="outcomePatterns">
		/// Jagged 2-dimensional array of integers that will contain the outcome patterns for the model
		/// after this method is called.
		/// </param>
		/// <param name="predicates">
		/// Dictionary that will contain the predicate information for the model
		/// after this method is called.
		/// </param>
        protected override void ReadPredicates(out int[][] outcomePatterns, out Dictionary<string, PatternedPredicate> predicates)
		{
			//read from the model how many outcome patterns there are
			int outcomePatternCount = ReadInt32();
			outcomePatterns = new int[outcomePatternCount][];
		    //read from the model how many predicates there are
            predicates = new Dictionary<string, PatternedPredicate>(ReadInt32());

			//for each outcome pattern in the model
			for (int currentOutcomePattern = 0; currentOutcomePattern < outcomePatternCount; currentOutcomePattern++)
			{
				//read the number of outcomes in this pattern.  This number is 1 greater than the real number of outcomes
				//in the pattern, because the 0th value contains the number of predicates that use this pattern.
				var currentOutcomePatternLength = ReadInt32();
				outcomePatterns[currentOutcomePattern] = new int[currentOutcomePatternLength];
				//read in the outcomes for this pattern
				for (int currentOutcome = 0; currentOutcome <currentOutcomePatternLength; currentOutcome++)
				{
					outcomePatterns[currentOutcomePattern][currentOutcome] = ReadInt32();
				}
				//read in the details of the predicates in this pattern
				for (int currentPredicate = 0; currentPredicate < outcomePatterns[currentOutcomePattern][0]; currentPredicate++)
				{
					string predicateName = ReadString();
					//we know that the number of parameters in this predicate will be the number of outcomes in the pattern
					double[] parameters = new double[currentOutcomePatternLength - 1];
					//read in the parameters for this predicate
					for (int currentParameter = 0; currentParameter < currentOutcomePatternLength - 1; currentParameter++)
					{
						parameters[currentParameter] = ReadDouble();
					}
					predicates.Add(predicateName, new PatternedPredicate(currentOutcomePattern, parameters));
				}
			}

		}

	}
}
