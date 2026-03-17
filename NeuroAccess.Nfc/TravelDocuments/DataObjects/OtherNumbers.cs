namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Other Numbers
	/// </summary>
	public class OtherNumbers : StringObject
	{
		/// <summary>
		/// Other Numbers
		/// </summary>
		public OtherNumbers()
			: base()
		{
		}

		/// <summary>
		/// Other Numbers
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		/// <param name="NumberFields">Fields identifying the address.</param>
		public OtherNumbers(byte[] Value, string StringValue, string[] NumberFields)
			: base(Value, StringValue)
		{
			this.NumberFields = NumberFields;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f17;

		/// <summary>
		/// Fields identifying the address.
		/// </summary>
		public string[]? NumberFields { get; }

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			string[] Fields = StringValue.Split('<', System.StringSplitOptions.RemoveEmptyEntries);
			return new OtherNumbers(Value, StringValue, Fields);
		}
	}
}
