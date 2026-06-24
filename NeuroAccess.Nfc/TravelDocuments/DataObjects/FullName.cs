namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Full Name
	/// </summary>
	public class FullName : StringObject
	{
		/// <summary>
		/// Full Name
		/// </summary>
		public FullName()
			: base()
		{
		}

		/// <summary>
		/// Full Name
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		public FullName(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f0e;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new FullName(Value, StringValue);
		}
	}
}
