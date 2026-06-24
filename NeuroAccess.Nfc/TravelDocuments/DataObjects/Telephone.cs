namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Telephone
	/// </summary>
	public class Telephone : StringObject
	{
		/// <summary>
		/// Telephone
		/// </summary>
		public Telephone()
			: base()
		{
		}

		/// <summary>
		/// Telephone
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public Telephone(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f12;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new Telephone(Value, StringValue);
		}
	}
}
