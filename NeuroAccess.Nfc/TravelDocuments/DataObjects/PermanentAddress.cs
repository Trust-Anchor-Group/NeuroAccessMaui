namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Permanent Address
	/// </summary>
	public class PermanentAddress : StringObject
	{
		/// <summary>
		/// Permanent Address
		/// </summary>
		public PermanentAddress()
			: base()
		{
		}

		/// <summary>
		/// Permanent Address
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		/// <param name="AddressFields">Fields identifying the address.</param>
		public PermanentAddress(byte[] Value, string StringValue, string[] AddressFields)
			: base(Value, StringValue)
		{
			this.AddressFields = AddressFields;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f42;

		/// <summary>
		/// Fields identifying the address.
		/// </summary>
		public string[]? AddressFields { get; }

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			string[] Fields = StringValue.Split('<', System.StringSplitOptions.RemoveEmptyEntries);
			return new PermanentAddress(Value, StringValue, Fields);
		}
	}
}
