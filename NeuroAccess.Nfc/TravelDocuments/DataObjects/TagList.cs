using System.Diagnostics.CodeAnalysis;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Tag List
	/// </summary>
	public class TagList : DataObject
	{
		/// <summary>
		/// Tag List
		/// </summary>
		public TagList()
			: base([])
		{
		}

		/// <summary>
		/// Unicode Version Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Tags">Tags</param>
		public TagList(byte[] Value, byte[] Tags)
			: base(Value)
		{
			this.Tags = Tags;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5c;

		/// <summary>
		/// Tags
		/// </summary>
		public byte[]? Tags { get; }

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
			Client.Information("Tag list: " + Hashes.BinaryToString(Value));
			Parsed = new TagList(Value, Value);
			return true;
		}
	}
}
