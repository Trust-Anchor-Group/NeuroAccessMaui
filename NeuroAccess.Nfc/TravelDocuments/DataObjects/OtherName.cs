namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Other Name
	/// </summary>
	public class OtherName : StringObject
	{
		/// <summary>
		/// Other Name
		/// </summary>
		public OtherName()
			: base()
		{
		}

		/// <summary>
		/// Other Name
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public OtherName(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f0f;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new OtherName(Value, StringValue);
		}
	}
}
