namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// MRZ Data Object
	/// </summary>
	public class MrzDataObject : StringObject
	{
		/// <summary>
		/// MRZ Data Object
		/// </summary>
		public MrzDataObject()
			: base()
		{
		}

		/// <summary>
		/// MRZ Data Object
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value</param>
		public MrzDataObject(byte[] Value, string StringValue)
			: base(Value, StringValue)
		{
			if (MrzExtensions.ParseMrz(StringValue, out DocumentInformation? Info))
				this.DocumentInformation = Info;
		}

		public DocumentInformation? DocumentInformation { get; }

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f1f;

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			return new MrzDataObject(Value, StringValue);
		}
	}
}
