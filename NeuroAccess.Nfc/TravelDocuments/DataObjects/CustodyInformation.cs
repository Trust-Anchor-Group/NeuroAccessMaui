namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Custody Information
	/// </summary>
	public class CustodyInformation : StringObject
	{
		/// <summary>
		/// Custody Information
		/// </summary>
		public CustodyInformation()
			: base()
		{
		}

		/// <summary>
		/// Custody Information
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public CustodyInformation(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f18;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new CustodyInformation(Value, StringValue);
		}
	}
}
