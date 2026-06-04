namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 image representation choice.
	/// </summary>
	public class ImageRepresentation
	{
		/// <summary>
		/// ISO/IEC 39794-5 image representation choice.
		/// </summary>
		/// <param name="Representation2D">Decoded 2D image representation, if present.</param>
		/// <param name="UnsupportedRepresentation">If the representation was recognized but is unsupported.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		public ImageRepresentation(ImageRepresentation2D? Representation2D,
			bool UnsupportedRepresentation, byte[][] ExtensionData)
		{
			this.Representation2D = Representation2D;
			this.UnsupportedRepresentation = UnsupportedRepresentation;
			this.ExtensionData = ExtensionData;
		}

		/// <summary>
		/// Decoded 2D image representation, if present.
		/// </summary>
		public ImageRepresentation2D? Representation2D { get; }

		/// <summary>
		/// If the representation was recognized but is unsupported.
		/// </summary>
		public bool UnsupportedRepresentation { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }
	}
}
