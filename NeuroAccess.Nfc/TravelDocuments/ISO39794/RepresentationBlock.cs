namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 representation block for a single biometric representation.
	/// </summary>
	public class RepresentationBlock
	{
		/// <summary>
		/// ISO/IEC 39794-5 representation block for a single biometric representation.
		/// </summary>
		/// <param name="RepresentationId">Representation identifier.</param>
		/// <param name="ImageRepresentation">Image representation.</param>
		/// <param name="IdentityMetadata">Optional identity metadata.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		public RepresentationBlock(int RepresentationId, ImageRepresentation ImageRepresentation,
			IdentityMetadata? IdentityMetadata, byte[][] ExtensionData)
		{
			this.RepresentationId = RepresentationId;
			this.ImageRepresentation = ImageRepresentation;
			this.IdentityMetadata = IdentityMetadata;
			this.ExtensionData = ExtensionData;
		}

		/// <summary>
		/// Representation identifier.
		/// </summary>
		public int RepresentationId { get; }

		/// <summary>
		/// Image representation.
		/// </summary>
		public ImageRepresentation ImageRepresentation { get; }

		/// <summary>
		/// Optional identity metadata.
		/// </summary>
		public IdentityMetadata? IdentityMetadata { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }
	}
}
