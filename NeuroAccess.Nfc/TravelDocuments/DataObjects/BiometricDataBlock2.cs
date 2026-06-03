using System;
using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.ISO39794;
using ISO19794Record = NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Data Block, encoded using ISO/IEC 39794 series.
	/// </summary>
	public class BiometricDataBlock2 : BiometricDataBlock
	{
		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 39794 series.
		/// </summary>
		public BiometricDataBlock2()
			: base([], null)
		{
		}

		/// <summary>
		/// Biometric Data Block, encoded using ISO/IEC 39794 series.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Record">Mapped biometric data interchange record.</param>
		public BiometricDataBlock2(byte[] Value, ISO19794Record? Record)
			: base(Value, Record)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x7f2e;

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation.</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		public override bool TryParse(byte[] Value, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject? Parsed)
		{
			if (!TryExtractBiometricDataBlock(Value, out int Tag, out byte[]? BiometricDataBlock))
			{
				Client.Warning("Unable to find ISO 39794 biometric data block in DO'7F2E':\r\n\r\n" +
					Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));

				Parsed = new BiometricDataBlock2(Value, null);
				return true;
			}

			if (Tag == 0x64 || Tag == 0x66)
			{
				Client.Warning("TODO: ISO 39794 biometric data block DO'" +
					Tag.ToString("X2") + "' is recognized but not implemented.");

				Parsed = new BiometricDataBlock2(Value, null);
				return true;
			}

			if (Tag != 0x65)
			{
				Client.Warning("Unsupported ISO 39794 biometric data block DO'" +
					Tag.ToString("X2") + "' in DO'7F2E'.");

				Parsed = new BiometricDataBlock2(Value, null);
				return true;
			}

			if (!ISO39794_5.TryParse(BiometricDataBlock, out FaceImageDataBlock? FaceImageDataBlock,
				out ISO19794Record? Record))
			{
				Client.Warning("Unable to parse ISO 39794-5 Biometric Data Interchange Record:\r\n\r\n" +
					Convert.ToBase64String(BiometricDataBlock, Base64FormattingOptions.InsertLineBreaks));
			}
			else if (FaceImageDataBlock is not null)
			{
				foreach (string Warning in FaceImageDataBlock.Warnings)
					Client.Warning(Warning);
			}

			Parsed = new BiometricDataBlock2(Value, Record);
			return true;
		}

		private static bool TryExtractBiometricDataBlock(byte[] Value, out int Tag,
			[NotNullWhen(true)] out byte[]? BiometricDataBlock)
		{
			Tag = 0;
			BiometricDataBlock = null;

			if (TryReadTlv(Value, 0, Value.Length, out TlvElement DirectElement) &&
				IsBiometricDataBlockTag(DirectElement.Tag) &&
				DirectElement.StartOffset == 0 &&
				DirectElement.EndOffset == Value.Length)
			{
				Tag = DirectElement.Tag;
				BiometricDataBlock = DirectElement.GetEncoded(Value);
				return true;
			}

			int Offset = 0;
			while (Offset < Value.Length)
			{
				if (!TryReadTlv(Value, Offset, Value.Length, out TlvElement OuterElement))
					return false;

				if (OuterElement.Tag == 0xa1)
				{
					int InnerOffset = OuterElement.ContentOffset;
					while (InnerOffset < OuterElement.EndOffset)
					{
						if (!TryReadTlv(Value, InnerOffset, OuterElement.EndOffset, out TlvElement InnerElement))
							return false;

						if (IsBiometricDataBlockTag(InnerElement.Tag))
						{
							Tag = InnerElement.Tag;
							BiometricDataBlock = InnerElement.GetEncoded(Value);
							return true;
						}

						InnerOffset = InnerElement.EndOffset;
					}
				}
				else if (IsBiometricDataBlockTag(OuterElement.Tag))
				{
					Tag = OuterElement.Tag;
					BiometricDataBlock = OuterElement.GetEncoded(Value);
					return true;
				}

				Offset = OuterElement.EndOffset;
			}

			return false;
		}

		private static bool IsBiometricDataBlockTag(int Tag)
		{
			return Tag is 0x64 or 0x65 or 0x66;
		}

		private static bool TryReadTlv(byte[] Data, int Offset, int EndOffset, out TlvElement Element)
		{
			Element = default;

			if (Offset >= EndOffset)
				return false;

			int StartOffset = Offset;
			int Tag = Data[Offset++];
			if ((Tag & 31) == 31)
			{
				Tag = 0;
				byte TagByte;
				do
				{
					if (Offset >= EndOffset)
						return false;

					TagByte = Data[Offset++];
					if (Tag > (int.MaxValue >> 7))
						return false;

					Tag = (Tag << 7) | (TagByte & 0x7f);
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
				if (LengthBytes == 0 || LengthBytes > 4 || Offset + LengthBytes > EndOffset)
					return false;

				Length = 0;
				for (int i = 0; i < LengthBytes; i++)
				{
					if (Length > (int.MaxValue >> 8))
						return false;

					Length = (Length << 8) | Data[Offset++];
				}
			}

			if (Length < 0 || Length > EndOffset - Offset)
				return false;

			Element = new TlvElement(Tag, StartOffset, Offset, Length, Offset + Length);
			return true;
		}

		private readonly struct TlvElement
		{
			public TlvElement(int Tag, int StartOffset, int ContentOffset, int Length, int EndOffset)
			{
				this.Tag = Tag;
				this.StartOffset = StartOffset;
				this.ContentOffset = ContentOffset;
				this.Length = Length;
				this.EndOffset = EndOffset;
			}

			public int Tag { get; }

			public int StartOffset { get; }

			public int ContentOffset { get; }

			public int Length { get; }

			public int EndOffset { get; }

			public byte[] GetEncoded(byte[] Data)
			{
				byte[] Result = new byte[this.EndOffset - this.StartOffset];
				Buffer.BlockCopy(Data, this.StartOffset, Result, 0, Result.Length);
				return Result;
			}
		}
	}
}
