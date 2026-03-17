namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Personal Summary
	/// </summary>
	public class PersonalSummary : StringObject
	{
		/// <summary>
		/// Personal Summary
		/// </summary>
		public PersonalSummary()
			: base()
		{
		}

		/// <summary>
		/// Personal Summary
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public PersonalSummary(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f15;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new PersonalSummary(Value, StringValue);
		}
	}
}
