using System.Diagnostics.CodeAnalysis;
using Waher.Content;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Abstract base class for string-valued objects.
	/// </summary>
	public abstract class StringObject : DataObject
	{
		/// <summary>
		/// Abstract base class for string-valued objects.
		/// </summary>
		public StringObject()
			: base([])
		{
		}

		/// <summary>
		/// Abstract base class for string-valued objects.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public StringObject(byte[] Value, string StringValue)
			: base(Value)
		{
			this.StringValue = StringValue;
		}

		/// <summary>
		/// String value.
		/// </summary>
		public string? StringValue { get; }

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
			string StringValue = InternetContent.ISO_8859_1.GetString(Value);

			Client.Information("String Value: " + StringValue);

			Parsed = this.Create(Value, StringValue);
			return true;
		}

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public abstract StringObject Create(byte[] Value, string StringValue);
	}
}
