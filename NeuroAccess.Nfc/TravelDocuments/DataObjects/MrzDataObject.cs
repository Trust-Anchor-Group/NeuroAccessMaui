using System.Diagnostics.CodeAnalysis;
using Waher.Content;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// MRZ Data Object
	/// </summary>
	public class MrzDataObject : DataObject
	{
		/// <summary>
		/// MRZ Data Object
		/// </summary>
		public MrzDataObject()
			: base([])
		{
		}

		/// <summary>
		/// MRZ Data Object
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Mrz">MRZ information</param>
		public MrzDataObject(byte[] Value, string Mrz)
			: base(Value)
		{
			this.Mrz = Mrz;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f1f;

		/// <summary>
		/// MRZ information.
		/// </summary>
		public string? Mrz { get; }

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
			string Mrz = InternetContent.ISO_8859_1.GetString(Value);

			Client.Information("MRZ: " + Mrz);

			Parsed = new MrzDataObject(Value, Mrz);
			return true;
		}
	}
}
