namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 face image data block.
	/// </summary>
	public class FaceImageDataBlock
	{
		/// <summary>
		/// ISO/IEC 39794-5 face image data block.
		/// </summary>
		/// <param name="VersionBlock">Version block.</param>
		/// <param name="RepresentationBlocks">Face representation blocks.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		/// <param name="Warnings">Non-fatal parsing or profile warnings.</param>
		public FaceImageDataBlock(VersionBlock VersionBlock, RepresentationBlock[] RepresentationBlocks,
			byte[][] ExtensionData, string[] Warnings)
		{
			this.VersionBlock = VersionBlock;
			this.RepresentationBlocks = RepresentationBlocks;
			this.ExtensionData = ExtensionData;
			this.Warnings = Warnings;
		}

		/// <summary>
		/// Version block.
		/// </summary>
		public VersionBlock VersionBlock { get; }

		/// <summary>
		/// Face representation blocks.
		/// </summary>
		public RepresentationBlock[] RepresentationBlocks { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }

		/// <summary>
		/// Non-fatal parsing or profile warnings.
		/// </summary>
		public string[] Warnings { get; }
	}
}
