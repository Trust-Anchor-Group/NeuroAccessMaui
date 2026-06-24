namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Personal Number
	/// </summary>
	public class PersonalNumber : StringObject
	{
		/// <summary>
		/// Personal Number
		/// </summary>
		public PersonalNumber()
			: base()
		{
		}

		/// <summary>
		/// Personal Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public PersonalNumber(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f10;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new PersonalNumber(Value, StringValue);
		}
	}
}
