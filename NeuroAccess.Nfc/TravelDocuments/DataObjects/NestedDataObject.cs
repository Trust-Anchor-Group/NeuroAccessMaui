using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Abstract base class for data objects that contain nested data objects.
	/// </summary>
	public abstract class NestedDataObject : DataObject
	{
		/// <summary>
		/// Abstract base class for data objects that contain nested data objects.
		/// </summary>
		public NestedDataObject()
			: base([])
		{
		}

		/// <summary>
		/// Abstract base class for data objects that contain nested data objects.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public NestedDataObject(byte[] Value)
			: base(Value)
		{
		}

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
				Parsed = this.Create(Value, Inner, Client);
				return true;
			}
			else
			{
				Parsed = null;
				return false;
			}

		}

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <returns>New data object instance.</returns>
		public abstract IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client);
	}
}
