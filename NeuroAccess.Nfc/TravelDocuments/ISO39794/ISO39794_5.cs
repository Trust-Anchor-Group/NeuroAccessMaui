using System;
using System.Collections.Generic;
using ISO19794EyeColour = NeuroAccess.Nfc.TravelDocuments.ISO19794.EyeColour;
using ISO19794FaceImageType = NeuroAccess.Nfc.TravelDocuments.ISO19794.FaceImageType;
using ISO19794Gender = NeuroAccess.Nfc.TravelDocuments.ISO19794.Gender;
using ISO19794HairColour = NeuroAccess.Nfc.TravelDocuments.ISO19794.HairColour;
using ISO19794ImageDataType = NeuroAccess.Nfc.TravelDocuments.ISO19794.ImageDataType;
using ISO19794Record = NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord;
using ISO19794Representation = NeuroAccess.Nfc.TravelDocuments.ISO19794.Representation;
using ISO19794RepresentationFace = NeuroAccess.Nfc.TravelDocuments.ISO19794.RepresentationFace;

namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// Parser for ISO/IEC 39794-5 face image data blocks used by ICAO DG2 data.
	/// </summary>
	public static class ISO39794_5
	{
		private const int UniversalClass = 0x00;
		private const int ApplicationClass = 0x40;
		private const int ContextSpecificClass = 0x80;
		private const int SequenceTagNumber = 16;

		/// <summary>
		/// Tries to parse biometric face data from an ISO/IEC 39794-5 DER encoded face image data block.
		/// </summary>
		/// <param name="Data">DER encoded face image data block, including the application tag.</param>
		/// <param name="Record">Parsed biometric data interchange record, if successful.</param>
		/// <returns>If a usable face image record could be parsed.</returns>
		public static bool TryParse(byte[] Data, out ISO19794Record? Record)
		{
			bool Result = TryParse(Data, out FaceImageDataBlock? _, out Record);
			return Result && Record is not null;
		}

		/// <summary>
		/// Tries to parse biometric face data from an ISO/IEC 39794-5 DER encoded face image data block.
		/// </summary>
		/// <param name="Data">DER encoded face image data block, including the application tag.</param>
		/// <param name="DataBlock">Parsed ISO/IEC 39794-5 face image data block, if successful.</param>
		/// <param name="Record">Mapped ISO/IEC 19794-compatible biometric data interchange record, if a usable face image is present.</param>
		/// <returns>If the ISO/IEC 39794-5 face image data block could be parsed.</returns>
		public static bool TryParse(byte[] Data, out FaceImageDataBlock? DataBlock,
			out ISO19794Record? Record)
		{
			ArgumentNullException.ThrowIfNull(Data);

			Parser Parser = new(Data);
			if (!Parser.TryParse(out FaceImageDataBlock? ParsedDataBlock))
			{
				DataBlock = null;
				Record = null;
				return false;
			}

			List<string> Warnings = new(ParsedDataBlock.Warnings);
			Record = TryCreateRecord(ParsedDataBlock, Warnings);
			DataBlock = new FaceImageDataBlock(ParsedDataBlock.VersionBlock,
				ParsedDataBlock.RepresentationBlocks, ParsedDataBlock.ExtensionData, [.. Warnings]);

			return true;
		}

		private static ISO19794Record? TryCreateRecord(FaceImageDataBlock DataBlock, List<string> Warnings)
		{
			List<ISO19794Representation> Representations = [];

			foreach (RepresentationBlock RepresentationBlock in DataBlock.RepresentationBlocks)
			{
				ImageRepresentation2D? Representation2D = RepresentationBlock.ImageRepresentation.Representation2D;
				if (Representation2D is null)
				{
					if (RepresentationBlock.ImageRepresentation.UnsupportedRepresentation)
						Warnings.Add("TODO: ISO 39794-5 non-2D image representations are recognized but not implemented.");

					continue;
				}

				byte[] ImageData = Representation2D.ImageData;
				ImageInformation2D ImageInformation = Representation2D.ImageInformation;
				ISO19794ImageDataType ImageDataType = MapImageDataType(ImageInformation.ImageDataFormat);

				if (TryDetectImageDataType(ImageData, out ISO19794ImageDataType DetectedImageDataType))
				{
					if (ImageDataType != ISO19794ImageDataType.UncompressedRaster &&
						ImageDataType != DetectedImageDataType)
					{
						Warnings.Add("ISO 39794-5 image data format metadata does not match encoded image bytes.");
					}

					ImageDataType = DetectedImageDataType;
				}
				else if (ImageDataType == ISO19794ImageDataType.UncompressedRaster)
					continue;

				if (ImageDataType == ISO19794ImageDataType.Png)
					Warnings.Add("PNG image data is outside the ICAO ISO 39794-5 eMRTD application profile but was decoded as usable image data.");

				int Width = ImageInformation.Width ?? 0;
				int Height = ImageInformation.Height ?? 0;
				if ((Width <= 0 || Height <= 0) &&
					TryReadImageSize(ImageData, out int ImageWidth, out int ImageHeight))
				{
					Width = ImageWidth;
					Height = ImageHeight;
				}

				IdentityMetadata? IdentityMetadata = RepresentationBlock.IdentityMetadata;
				Representations.Add(new ISO19794RepresentationFace(
					MapFaceImageType(ImageInformation.FaceImageKind),
					ImageDataType,
					Width,
					Height,
					ImageData,
					MapGender(IdentityMetadata?.Gender),
					MapEyeColour(IdentityMetadata?.EyeColour),
					MapHairColour(IdentityMetadata?.HairColour)));
			}

			if (Representations.Count == 0)
				return null;

			return new ISO19794Record([.. Representations]);
		}

		private static ISO19794FaceImageType MapFaceImageType(int? FaceImageKind)
		{
			return FaceImageKind switch
			{
				0 => ISO19794FaceImageType.Portrait,
				_ => ISO19794FaceImageType.Portrait
			};
		}

		private static ISO19794ImageDataType MapImageDataType(ImageDataFormat ImageDataFormatCode)
		{
			return ImageDataFormatCode switch
			{
				ImageDataFormat.Jpeg => ISO19794ImageDataType.Jpeg,
				ImageDataFormat.Jpeg2000Lossy or ImageDataFormat.Jpeg2000Lossless => ISO19794ImageDataType.Jpeg2000,
				_ => ISO19794ImageDataType.UncompressedRaster
			};
		}

		private static ISO19794Gender MapGender(Gender? Gender)
		{
			return Gender switch
			{
				NeuroAccess.Nfc.TravelDocuments.ISO39794.Gender.Male => ISO19794Gender.Male,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.Gender.Female => ISO19794Gender.Female,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.Gender.Other => ISO19794Gender.Unknown,
				_ => ISO19794Gender.Unknown
			};
		}

		private static ISO19794EyeColour MapEyeColour(EyeColour? EyeColour)
		{
			return EyeColour switch
			{
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Blue => ISO19794EyeColour.Blue,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Brown => ISO19794EyeColour.Brown,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Grey => ISO19794EyeColour.Gray,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Green => ISO19794EyeColour.Green,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Hazel => ISO19794EyeColour.Hazel,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour.Unknown => ISO19794EyeColour.Unknown,
				_ => ISO19794EyeColour.Other
			};
		}

		private static ISO19794HairColour MapHairColour(HairColour? HairColour)
		{
			return HairColour switch
			{
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Black => ISO19794HairColour.Black,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Blonde => ISO19794HairColour.Blond,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Brown => ISO19794HairColour.Brown,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Grey => ISO19794HairColour.Gray,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.White => ISO19794HairColour.White,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Red => ISO19794HairColour.Red,
				NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour.Unknown => ISO19794HairColour.Unknown,
				_ => ISO19794HairColour.Other
			};
		}

		private static bool TryDetectImageDataType(byte[] Data, out ISO19794ImageDataType ImageDataType)
		{
			ImageDataType = default;
			if (Data.Length >= 3 &&
				Data[0] == 0xff &&
				Data[1] == 0xd8 &&
				Data[2] == 0xff)
			{
				ImageDataType = ISO19794ImageDataType.Jpeg;
				return true;
			}

			if (Data.Length >= 8 &&
				Data[0] == 0x89 &&
				Data[1] == 0x50 &&
				Data[2] == 0x4e &&
				Data[3] == 0x47 &&
				Data[4] == 0x0d &&
				Data[5] == 0x0a &&
				Data[6] == 0x1a &&
				Data[7] == 0x0a)
			{
				ImageDataType = ISO19794ImageDataType.Png;
				return true;
			}

			if ((Data.Length >= 12 &&
				Data[4] == 0x6a &&
				Data[5] == 0x50 &&
				Data[6] == 0x20 &&
				Data[7] == 0x20) ||
				(Data.Length >= 4 &&
				Data[0] == 0xff &&
				Data[1] == 0x4f &&
				Data[2] == 0xff &&
				Data[3] == 0x51))
			{
				ImageDataType = ISO19794ImageDataType.Jpeg2000;
				return true;
			}

			return false;
		}

		private static bool TryReadImageSize(byte[] Data, out int Width, out int Height)
		{
			return TryReadJpegSize(Data, out Width, out Height) ||
				TryReadJpeg2000Size(Data, out Width, out Height);
		}

		private static bool TryReadJpegSize(byte[] Data, out int Width, out int Height)
		{
			Width = 0;
			Height = 0;

			if (Data.Length < 4 || Data[0] != 0xff || Data[1] != 0xd8)
				return false;

			int Offset = 2;
			while (Offset + 3 < Data.Length)
			{
				while (Offset < Data.Length && Data[Offset] == 0xff)
					Offset++;

				if (Offset >= Data.Length)
					return false;

				byte Marker = Data[Offset++];
				if (Marker == 0xd9 || Marker == 0xda)
					return false;

				if (Offset + 1 >= Data.Length)
					return false;

				int SegmentLength = ReadUInt16(Data, Offset);
				if (SegmentLength < 2 || SegmentLength > Data.Length - Offset)
					return false;

				if (IsStartOfFrameMarker(Marker))
				{
					if (SegmentLength < 7)
						return false;

					Height = ReadUInt16(Data, Offset + 3);
					Width = ReadUInt16(Data, Offset + 5);
					return Width > 0 && Height > 0;
				}

				Offset += SegmentLength;
			}

			return false;
		}

		private static bool TryReadJpeg2000Size(byte[] Data, out int Width, out int Height)
		{
			Width = 0;
			Height = 0;

			if (Data.Length >= 4 &&
				Data[0] == 0xff &&
				Data[1] == 0x4f)
			{
				return TryReadJpeg2000CodestreamSize(Data, out Width, out Height);
			}

			if (Data.Length < 12 ||
				Data[4] != 0x6a ||
				Data[5] != 0x50 ||
				Data[6] != 0x20 ||
				Data[7] != 0x20)
			{
				return false;
			}

			return TryReadJpeg2000BoxSize(Data, 0, Data.Length, out Width, out Height);
		}

		private static bool TryReadJpeg2000BoxSize(byte[] Data, int Offset, int EndOffset,
			out int Width, out int Height)
		{
			Width = 0;
			Height = 0;

			while (Offset + 8 <= EndOffset)
			{
				uint RawBoxLength = ReadUInt32(Data, Offset);
				uint BoxType = ReadUInt32(Data, Offset + 4);
				int HeaderLength = 8;
				ulong BoxLength;

				if (RawBoxLength == 1)
				{
					if (Offset + 16 > EndOffset)
						return false;

					HeaderLength = 16;
					BoxLength = ReadUInt64(Data, Offset + 8);
				}
				else if (RawBoxLength == 0)
					BoxLength = (ulong)(EndOffset - Offset);
				else
					BoxLength = RawBoxLength;

				if (BoxLength < (ulong)HeaderLength ||
					BoxLength > (ulong)(EndOffset - Offset) ||
					BoxLength > int.MaxValue)
				{
					return false;
				}

				int ContentOffset = Offset + HeaderLength;
				int ContentLength = (int)BoxLength - HeaderLength;

				if (BoxType == 0x69686472 && ContentLength >= 14)
				{
					Height = ReadInt32(Data, ContentOffset);
					Width = ReadInt32(Data, ContentOffset + 4);
					return Width > 0 && Height > 0;
				}

				if (BoxType == 0x6a703268 &&
					TryReadJpeg2000BoxSize(Data, ContentOffset, ContentOffset + ContentLength, out Width, out Height))
				{
					return true;
				}

				Offset += (int)BoxLength;
			}

			return false;
		}

		private static bool TryReadJpeg2000CodestreamSize(byte[] Data, out int Width, out int Height)
		{
			Width = 0;
			Height = 0;

			int Offset = 0;
			while (Offset + 1 < Data.Length)
			{
				if (Data[Offset++] != 0xff)
					continue;

				byte Marker = Data[Offset++];
				if (Marker == 0x51)
				{
					if (Offset + 38 > Data.Length)
						return false;

					int SegmentLength = ReadUInt16(Data, Offset);
					if (SegmentLength < 38 || SegmentLength > Data.Length - Offset)
						return false;

					uint Xsiz = ReadUInt32(Data, Offset + 4);
					uint Ysiz = ReadUInt32(Data, Offset + 8);
					uint XOsiz = ReadUInt32(Data, Offset + 12);
					uint YOsiz = ReadUInt32(Data, Offset + 16);

					if (Xsiz <= XOsiz || Ysiz <= YOsiz)
						return false;

					uint ImageWidth = Xsiz - XOsiz;
					uint ImageHeight = Ysiz - YOsiz;
					if (ImageWidth > int.MaxValue || ImageHeight > int.MaxValue)
						return false;

					Width = (int)ImageWidth;
					Height = (int)ImageHeight;
					return true;
				}

				if (Marker == 0x4f || Marker == 0xd9)
					continue;

				if (Offset + 1 >= Data.Length)
					return false;

				int Length = ReadUInt16(Data, Offset);
				if (Length < 2 || Length > Data.Length - Offset)
					return false;

				Offset += Length;
			}

			return false;
		}

		private static bool IsStartOfFrameMarker(byte Marker)
		{
			return Marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or 0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf;
		}

		private static int ReadUInt16(byte[] Data, int Offset)
		{
			return (Data[Offset] << 8) | Data[Offset + 1];
		}

		private static int ReadInt32(byte[] Data, int Offset)
		{
			uint Value = ReadUInt32(Data, Offset);
			if (Value > int.MaxValue)
				return -1;

			return (int)Value;
		}

		private static uint ReadUInt32(byte[] Data, int Offset)
		{
			return ((uint)Data[Offset] << 24) |
				((uint)Data[Offset + 1] << 16) |
				((uint)Data[Offset + 2] << 8) |
				Data[Offset + 3];
		}

		private static ulong ReadUInt64(byte[] Data, int Offset)
		{
			return ((ulong)Data[Offset] << 56) |
				((ulong)Data[Offset + 1] << 48) |
				((ulong)Data[Offset + 2] << 40) |
				((ulong)Data[Offset + 3] << 32) |
				((ulong)Data[Offset + 4] << 24) |
				((ulong)Data[Offset + 5] << 16) |
				((ulong)Data[Offset + 6] << 8) |
				Data[Offset + 7];
		}

		private sealed class Parser
		{
			private readonly byte[] data;
			private readonly List<string> warnings = [];

			public Parser(byte[] Data)
			{
				this.data = Data;
			}

			public bool TryParse(out FaceImageDataBlock? DataBlock)
			{
				DataBlock = null;

				if (!TryReadElement(this.data, 0, this.data.Length, out DerElement Root) ||
					Root.EndOffset != this.data.Length ||
					Root.TagClass != ApplicationClass ||
					Root.TagNumber != 5 ||
					!Root.Constructed)
				{
					return false;
				}

				if (!this.TryReadChildren(Root, out DerElement[] Children))
					return false;

				VersionBlock? VersionBlock = null;
				List<RepresentationBlock> RepresentationBlocks = [];
				List<byte[]> ExtensionData = [];

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 0 && Child.Constructed)
					{
						if (!this.TryParseVersionBlock(Child, out VersionBlock))
							return false;
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1 && Child.Constructed)
					{
						if (!this.TryParseRepresentationBlocks(Child, RepresentationBlocks))
							return false;
					}
					else
						ExtensionData.Add(Child.GetEncoded(this.data));
				}

				if (VersionBlock is null || RepresentationBlocks.Count == 0)
					return false;

				DataBlock = new FaceImageDataBlock(VersionBlock, [.. RepresentationBlocks],
					[.. ExtensionData], [.. this.warnings]);
				return true;
			}

			private bool TryParseVersionBlock(DerElement Element, out VersionBlock? VersionBlock)
			{
				VersionBlock = null;
				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				int? Generation = null;
				int? Year = null;
				foreach (DerElement Child in Children)
				{
					if (Child.TagClass != ContextSpecificClass || Child.Constructed)
						continue;

					if (Child.TagNumber == 0 && TryReadInteger(this.data, Child, out int GenerationValue))
						Generation = GenerationValue;
					else if (Child.TagNumber == 1 && TryReadInteger(this.data, Child, out int YearValue))
						Year = YearValue;
				}

				if (!Generation.HasValue || !Year.HasValue)
					return false;

				if (Generation.Value != 3 || Year.Value != 2019)
					this.warnings.Add("ISO 39794-5 version block does not match the ICAO eMRTD application profile baseline.");

				VersionBlock = new VersionBlock(Generation.Value, Year.Value);
				return true;
			}

			private bool TryParseRepresentationBlocks(DerElement Element, List<RepresentationBlock> RepresentationBlocks)
			{
				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass != UniversalClass ||
						Child.TagNumber != SequenceTagNumber ||
						!Child.Constructed)
					{
						this.warnings.Add("Ignoring non-SEQUENCE value in ISO 39794-5 representationBlocks.");
						continue;
					}

					if (!this.TryParseRepresentationBlock(Child, out RepresentationBlock? RepresentationBlock))
						return false;

					RepresentationBlocks.Add(RepresentationBlock);
				}

				return true;
			}

			private bool TryParseRepresentationBlock(DerElement Element, out RepresentationBlock? RepresentationBlock)
			{
				RepresentationBlock = null;
				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				int? RepresentationId = null;
				ImageRepresentation? ImageRepresentation = null;
				IdentityMetadata? IdentityMetadata = null;
				List<byte[]> ExtensionData = [];

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 0 && !Child.Constructed)
					{
						if (!TryReadInteger(this.data, Child, out int RepresentationIdValue))
							return false;

						RepresentationId = RepresentationIdValue;
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1 && Child.Constructed)
					{
						if (!this.TryParseImageRepresentation(Child, out ImageRepresentation))
							return false;
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 8 && Child.Constructed)
					{
						if (!this.TryParseIdentityMetadata(Child, out IdentityMetadata))
							return false;
					}
					else
						ExtensionData.Add(Child.GetEncoded(this.data));
				}

				if (!RepresentationId.HasValue || ImageRepresentation is null)
					return false;

				RepresentationBlock = new RepresentationBlock(RepresentationId.Value, ImageRepresentation,
					IdentityMetadata, [.. ExtensionData]);
				return true;
			}

			private bool TryParseImageRepresentation(DerElement Element, out ImageRepresentation? ImageRepresentation)
			{
				ImageRepresentation = null;
				List<byte[]> ExtensionData = [];

				if (this.TryFindImageRepresentation2D(Element, out ImageRepresentation2D? Representation2D))
				{
					ImageRepresentation = new ImageRepresentation(Representation2D, false, [.. ExtensionData]);
					return true;
				}

				if (this.ContainsConstructedContextTag(Element, 1))
				{
					this.warnings.Add("TODO: ISO 39794-5 extension image representations are recognized but not implemented.");
					ImageRepresentation = new ImageRepresentation(null, true, [.. ExtensionData]);
					return true;
				}

				this.warnings.Add("TODO: ISO 39794-5 non-2D image representations are recognized but not implemented.");
				ImageRepresentation = new ImageRepresentation(null, true, [.. ExtensionData]);
				return true;
			}

			private bool TryFindImageRepresentation2D(DerElement Element, out ImageRepresentation2D? Representation2D)
			{
				if (this.TryParseImageRepresentation2D(Element, out Representation2D))
					return true;

				if (!Element.Constructed || !this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (!Child.Constructed)
						continue;

					if (this.TryFindImageRepresentation2D(Child, out Representation2D))
						return true;
				}

				Representation2D = null;
				return false;
			}

			private bool TryParseImageRepresentation2D(DerElement Element, out ImageRepresentation2D? Representation2D)
			{
				Representation2D = null;
				if (!Element.Constructed || !this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				byte[]? ImageData = null;
				ImageInformation2D? ImageInformation = null;
				List<byte[]> ExtensionData = [];

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 0 && !Child.Constructed)
						ImageData = Child.GetContent(this.data);
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1 && Child.Constructed)
					{
						if (!this.TryParseImageInformation2D(Child, out ImageInformation))
							return false;
					}
					else
						ExtensionData.Add(Child.GetEncoded(this.data));
				}

				if (ImageData is null || ImageInformation is null)
					return false;

				Representation2D = new ImageRepresentation2D(ImageData, ImageInformation, [.. ExtensionData]);
				return true;
			}

			private bool TryParseImageInformation2D(DerElement Element, out ImageInformation2D? ImageInformation)
			{
				ImageInformation = null;
				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				ImageDataFormat ImageDataFormatValue = ImageDataFormat.Unknown;
				int? FaceImageKind = null;
				int? Width = null;
				int? Height = null;
				List<byte[]> ExtensionData = [];

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 0)
					{
						if (!this.TryParseImageDataFormat(Child, out ImageDataFormatValue))
							this.warnings.Add("ISO 39794-5 image data format could not be decoded; image bytes will be used as fallback.");
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1)
					{
						if (this.TryReadExtensibleInteger(Child, out int FaceImageKindValue))
							FaceImageKind = FaceImageKindValue;
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 7 && Child.Constructed)
					{
						if (!this.TryParseImageSize(Child, out Width, out Height))
							return false;
					}
					else
						ExtensionData.Add(Child.GetEncoded(this.data));
				}

				ImageInformation = new ImageInformation2D(ImageDataFormatValue, FaceImageKind,
					Width, Height, [.. ExtensionData]);
				return true;
			}

			private bool TryParseImageDataFormat(DerElement Element, out ImageDataFormat ImageDataFormatValue)
			{
				ImageDataFormatValue = ImageDataFormat.Unknown;

				if (TryReadInteger(this.data, Element, out int DirectValue))
					return TryMapImageDataFormat(DirectValue, out ImageDataFormatValue);

				if (!Element.Constructed || !this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass &&
						Child.TagNumber == 0 &&
						TryReadInteger(this.data, Child, out int Value))
					{
						return TryMapImageDataFormat(Value, out ImageDataFormatValue);
					}

					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1)
					{
						this.warnings.Add("TODO: ISO 39794-5 image data format extension blocks are recognized but not implemented.");
						return false;
					}
				}

				return false;
			}

			private bool TryParseImageSize(DerElement Element, out int? Width, out int? Height)
			{
				Width = null;
				Height = null;

				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass != ContextSpecificClass || Child.Constructed)
						continue;

					if (Child.TagNumber == 0 && TryReadInteger(this.data, Child, out int WidthValue))
						Width = WidthValue;
					else if (Child.TagNumber == 1 && TryReadInteger(this.data, Child, out int HeightValue))
						Height = HeightValue;
				}

				return Width.HasValue && Height.HasValue;
			}

			private bool TryParseIdentityMetadata(DerElement Element, out IdentityMetadata? IdentityMetadata)
			{
				IdentityMetadata = null;
				if (!this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				Gender? Gender = null;
				EyeColour? EyeColour = null;
				HairColour? HairColour = null;
				List<byte[]> ExtensionData = [];

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 0)
					{
						if (this.TryReadExtensibleInteger(Child, out int Value) &&
							Enum.IsDefined(typeof(NeuroAccess.Nfc.TravelDocuments.ISO39794.Gender), Value))
						{
							Gender = (NeuroAccess.Nfc.TravelDocuments.ISO39794.Gender)Value;
						}
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 1)
					{
						if (this.TryReadExtensibleInteger(Child, out int Value) &&
							Enum.IsDefined(typeof(NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour), Value))
						{
							EyeColour = (NeuroAccess.Nfc.TravelDocuments.ISO39794.EyeColour)Value;
						}
					}
					else if (Child.TagClass == ContextSpecificClass && Child.TagNumber == 2)
					{
						if (this.TryReadExtensibleInteger(Child, out int Value) &&
							Enum.IsDefined(typeof(NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour), Value))
						{
							HairColour = (NeuroAccess.Nfc.TravelDocuments.ISO39794.HairColour)Value;
						}
					}
					else
						ExtensionData.Add(Child.GetEncoded(this.data));
				}

				IdentityMetadata = new IdentityMetadata(Gender, EyeColour, HairColour, [.. ExtensionData]);
				return true;
			}

			private bool TryReadExtensibleInteger(DerElement Element, out int Value)
			{
				if (TryReadInteger(this.data, Element, out Value))
					return true;

				if (!Element.Constructed || !this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass &&
						Child.TagNumber == 0 &&
						TryReadInteger(this.data, Child, out Value))
					{
						return true;
					}

					if (Child.Constructed && this.TryReadExtensibleInteger(Child, out Value))
						return true;
				}

				Value = 0;
				return false;
			}

			private bool ContainsConstructedContextTag(DerElement Element, int TagNumber)
			{
				if (!Element.Constructed || !this.TryReadChildren(Element, out DerElement[] Children))
					return false;

				foreach (DerElement Child in Children)
				{
					if (Child.TagClass == ContextSpecificClass && Child.TagNumber == TagNumber && Child.Constructed)
						return true;

					if (this.ContainsConstructedContextTag(Child, TagNumber))
						return true;
				}

				return false;
			}

			private bool TryReadChildren(DerElement Element, out DerElement[] Children)
			{
				Children = [];

				if (!Element.Constructed)
					return false;

				List<DerElement> Result = [];
				int Offset = Element.ContentOffset;
				while (Offset < Element.EndOffset)
				{
					if (!TryReadElement(this.data, Offset, Element.EndOffset, out DerElement Child))
						return false;

					Result.Add(Child);
					Offset = Child.EndOffset;
				}

				Children = [.. Result];
				return true;
			}
		}

		private static bool TryMapImageDataFormat(int Value, out ImageDataFormat ImageDataFormatValue)
		{
			ImageDataFormatValue = Value switch
			{
				2 => ImageDataFormat.Jpeg,
				3 => ImageDataFormat.Jpeg2000Lossy,
				4 => ImageDataFormat.Jpeg2000Lossless,
				_ => ImageDataFormat.Unknown
			};

			return ImageDataFormatValue != ImageDataFormat.Unknown;
		}

		private static bool TryReadElement(byte[] Data, int Offset, int EndOffset, out DerElement Element)
		{
			Element = default;

			if (Offset >= EndOffset)
				return false;

			int StartOffset = Offset;
			byte First = Data[Offset++];
			int TagClass = First & 0xc0;
			bool Constructed = (First & 0x20) != 0;
			int TagNumber = First & 0x1f;

			if (TagNumber == 0x1f)
			{
				TagNumber = 0;
				byte TagByte;
				do
				{
					if (Offset >= EndOffset)
						return false;

					TagByte = Data[Offset++];
					if (TagNumber > (int.MaxValue >> 7))
						return false;

					TagNumber = (TagNumber << 7) | (TagByte & 0x7f);
				}
				while ((TagByte & 0x80) != 0);
			}

			if (Offset >= EndOffset)
				return false;

			byte LengthByte = Data[Offset++];
			int Length;
			if ((LengthByte & 0x80) == 0)
				Length = LengthByte;
			else
			{
				int LengthBytes = LengthByte & 0x7f;
				if (LengthBytes == 0 || LengthBytes > EndOffset - Offset)
					return false;

				Length = 0;
				for (int i = 0; i < LengthBytes; i++)
				{
					byte Value = Data[Offset++];
					if (Length > ((int.MaxValue - Value) >> 8))
						return false;

					Length = (Length << 8) | Value;
				}
			}

			if (Length > EndOffset - Offset)
				return false;

			Element = new DerElement(TagClass, TagNumber, Constructed, Offset,
				Length, Offset + Length, StartOffset);
			return true;
		}

		private static bool TryReadInteger(byte[] Data, DerElement Element, out int Value)
		{
			Value = 0;
			if (Element.Constructed || Element.Length == 0 || Element.Length > 4)
				return false;

			int Offset = Element.ContentOffset;
			for (int i = 0; i < Element.Length; i++)
				Value = (Value << 8) | Data[Offset + i];

			return true;
		}

		private readonly struct DerElement
		{
			public DerElement(
				int TagClass,
				int TagNumber,
				bool Constructed,
				int ContentOffset,
				int Length,
				int EndOffset,
				int StartOffset)
			{
				this.TagClass = TagClass;
				this.TagNumber = TagNumber;
				this.Constructed = Constructed;
				this.ContentOffset = ContentOffset;
				this.Length = Length;
				this.EndOffset = EndOffset;
				this.StartOffset = StartOffset;
			}

			public int TagClass { get; }

			public int TagNumber { get; }

			public bool Constructed { get; }

			public int ContentOffset { get; }

			public int Length { get; }

			public int EndOffset { get; }

			public int StartOffset { get; }

			public byte[] GetContent(byte[] Data)
			{
				byte[] Result = new byte[this.Length];
				Buffer.BlockCopy(Data, this.ContentOffset, Result, 0, Result.Length);
				return Result;
			}

			public byte[] GetEncoded(byte[] Data)
			{
				byte[] Result = new byte[this.EndOffset - this.StartOffset];
				Buffer.BlockCopy(Data, this.StartOffset, Result, 0, Result.Length);
				return Result;
			}
		}
	}
}
