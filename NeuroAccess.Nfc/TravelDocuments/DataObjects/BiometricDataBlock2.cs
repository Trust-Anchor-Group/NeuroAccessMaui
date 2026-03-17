using System;
using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.ISO19794;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Data Block, encoded using ISO/IEC 39794 series 
	/// </summary>
	public class BiometricDataBlock2 : BiometricDataBlock
	{
		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 39794 series 
		/// </summary>
		public BiometricDataBlock2()
			: base([], null)
		{
		}

		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 39794 series 
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Record">ISO 39794-5 Biometric Data Interchange Record</param>
		public BiometricDataBlock2(byte[] Value, BiometricDataInterchangeRecord? Record)
			: base(Value, Record)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x7f2e;

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
			if (TravelDocumentsClient.TryParseDataObjects(Value, Client, out IDataObject[]? Inner))
			{
				BiometricHeaderTemplate? Header = null;

				foreach (IDataObject Object in Inner)
				{
					if (Object is BiometricHeaderTemplate Header2)
						Header = Header2;
				}

				Client.Warning("Unable to parse ISO 39794-5 Biomatric Data Interchange Record:\r\n\r\n" +
					Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));

				// TODO: Parse
				Parsed = new BiometricDataBlock2(Value, null);
				return true;
			}
			else
			{
				Parsed = null;
				return false;
			}
		}
	}
}
