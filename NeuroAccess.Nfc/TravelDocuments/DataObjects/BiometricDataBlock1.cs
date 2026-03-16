using System;
using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.ISO19794;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Data Block, encoded using ISO/IEC 19794 series first edition 
	/// </summary>
	public class BiometricDataBlock1 : BiometricDataBlock2
	{
		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 19794 series first edition 
		/// </summary>
		public BiometricDataBlock1()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 19794 series first edition 
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public BiometricDataBlock1(byte[] Value, BiometricDataInterchangeRecord? Record)
			: base(Value)
		{
			this.Record = Record;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f2e;

		/// <summary>
		/// ISO 19794-5 Biomatric Data Interchange Record
		/// </summary>
		public BiometricDataInterchangeRecord? Record { get; }

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		public override bool TryParse(byte[] Value, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject? Parsed)
		{
			if (!ISO19794_5.TryParse(Value, out BiometricDataInterchangeRecord? Record))
			{
				Client.Warning("Unable to parse ISO 19794-5 Biomatric Data Interchange Record:\r\n\r\n" +
					Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));
			}

			// TODO: Parse
			Parsed = new BiometricDataBlock1(Value, Record);
			return true;
		}
	}
}
