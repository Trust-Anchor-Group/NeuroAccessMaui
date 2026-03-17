namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Place of Birth
	/// </summary>
	public class PlaceOfBirth : StringObject
	{
		/// <summary>
		/// Place of Birth
		/// </summary>
		public PlaceOfBirth()
			: base()
		{
		}

		/// <summary>
		/// Place of Birth
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		/// <param name="PlaceFields">Fields identifying the place.</param>
		public PlaceOfBirth(byte[] Value, string StringValue, string[] PlaceFields)
			: base(Value, StringValue)
		{
			this.PlaceFields = PlaceFields;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f11;

		/// <summary>
		/// Fields identifying the place.
		/// </summary>
		public string[]? PlaceFields { get; }

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			string[] Fields = StringValue.Split('<', System.StringSplitOptions.RemoveEmptyEntries);
			return new PlaceOfBirth(Value, StringValue, Fields);
		}
	}
}
