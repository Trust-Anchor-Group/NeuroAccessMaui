namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 identity metadata block.
	/// </summary>
	public class IdentityMetadata
	{
		/// <summary>
		/// ISO/IEC 39794-5 identity metadata block.
		/// </summary>
		/// <param name="Gender">Gender, if present.</param>
		/// <param name="EyeColour">Eye colour, if present.</param>
		/// <param name="HairColour">Hair colour, if present.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		public IdentityMetadata(Gender? Gender, EyeColour? EyeColour, HairColour? HairColour,
			byte[][] ExtensionData)
		{
			this.Gender = Gender;
			this.EyeColour = EyeColour;
			this.HairColour = HairColour;
			this.ExtensionData = ExtensionData;
		}

		/// <summary>
		/// Gender, if present.
		/// </summary>
		public Gender? Gender { get; }

		/// <summary>
		/// Eye colour, if present.
		/// </summary>
		public EyeColour? EyeColour { get; }

		/// <summary>
		/// Hair colour, if present.
		/// </summary>
		public HairColour? HairColour { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }
	}
}
