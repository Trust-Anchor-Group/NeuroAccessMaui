using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using NeuroAccess.Nfc.TravelDocuments.ISO39794;
using Waher.Networking;
using ISO19794EyeColour = NeuroAccess.Nfc.TravelDocuments.ISO19794.EyeColour;
using ISO19794Gender = NeuroAccess.Nfc.TravelDocuments.ISO19794.Gender;
using ISO19794HairColour = NeuroAccess.Nfc.TravelDocuments.ISO19794.HairColour;
using ISO19794ImageDataType = NeuroAccess.Nfc.TravelDocuments.ISO19794.ImageDataType;
using ISO19794RepresentationFace = NeuroAccess.Nfc.TravelDocuments.ISO19794.RepresentationFace;

namespace NeuroAccess.Nfc.Test
{
	/// <summary>
	/// Tests ISO/IEC 39794-5 face image data parsing.
	/// </summary>
	[TestClass]
	public class ISO39794Tests
	{
		/// <summary>
		/// Verifies direct DO'65' face image data block parsing.
		/// </summary>
		[TestMethod]
		public void Test_01_Direct_Face_Image_Data_Block_Parses()
		{
			byte[] ImageData = CreateJpegImage(320, 240);
			byte[] Data = CreateFaceImageDataBlock(ImageData, 2, true, true, true);

			Assert.IsTrue(ISO39794_5.TryParse(Data, out FaceImageDataBlock? DataBlock, out NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord? Record));
			Assert.IsNotNull(DataBlock);
			Assert.IsNotNull(Record);
			Assert.AreEqual(3, DataBlock.VersionBlock.Generation);
			Assert.AreEqual(2019, DataBlock.VersionBlock.Year);
			Assert.HasCount(1, DataBlock.RepresentationBlocks);
			Assert.HasCount(1, Record.Representations);

			ISO19794RepresentationFace Face = (ISO19794RepresentationFace)Record.Representations[0];
			Assert.AreEqual(ISO19794ImageDataType.Jpeg, Face.ImageDataType);
			Assert.AreEqual(320, Face.Width);
			Assert.AreEqual(240, Face.Height);
			Assert.AreEqual(ISO19794Gender.Male, Face.Gender);
			Assert.AreEqual(ISO19794EyeColour.Brown, Face.EyeColour);
			Assert.AreEqual(ISO19794HairColour.Brown, Face.HairColour);
		}

		/// <summary>
		/// Verifies the DO'7F2E' standardized biometric data wrapper around DO'65'.
		/// </summary>
		[TestMethod]
		public void Test_02_Biometric_Data_Block2_Parses_A1_Face_Wrapper()
		{
			byte[] Data = Constructed(0xa1, CreateFaceImageDataBlock(CreateJpegImage(160, 120), 2, true, false, false));
			BiometricDataBlock2 DataBlock = new();
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(DataBlock.TryParse(Data, Client, out IDataObject? Parsed));
			BiometricDataBlock2 ParsedBlock = (BiometricDataBlock2)Parsed;
			Assert.IsNotNull(ParsedBlock.Record);
			Assert.HasCount(1, ParsedBlock.Record.Representations);
		}

		/// <summary>
		/// Verifies JPEG 2000 image data is mapped from ISO/IEC 39794 image data format metadata.
		/// </summary>
		[TestMethod]
		public void Test_03_Jpeg2000_Format_Maps_To_Existing_Record()
		{
			byte[] ImageData = [0x00, 0x00, 0x00, 0x0c, 0x6a, 0x50, 0x20, 0x20, 0x0d, 0x0a, 0x87, 0x0a];
			byte[] Data = CreateFaceImageDataBlock(ImageData, 3, true, false, false);

			Assert.IsTrue(ISO39794_5.TryParse(Data, out NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord? Record));
			Assert.IsNotNull(Record);

			ISO19794RepresentationFace Face = (ISO19794RepresentationFace)Record.Representations[0];
			Assert.AreEqual(ISO19794ImageDataType.Jpeg2000, Face.ImageDataType);
			Assert.AreEqual(320, Face.Width);
			Assert.AreEqual(240, Face.Height);
		}

		/// <summary>
		/// Verifies image size falls back to JPEG headers when the ISO/IEC 39794 image size block is absent.
		/// </summary>
		[TestMethod]
		public void Test_04_Image_Size_Falls_Back_To_Image_Header()
		{
			byte[] Data = CreateFaceImageDataBlock(CreateJpegImage(96, 64), 2, false, false, false);

			Assert.IsTrue(ISO39794_5.TryParse(Data, out NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord? Record));
			Assert.IsNotNull(Record);

			ISO19794RepresentationFace Face = (ISO19794RepresentationFace)Record.Representations[0];
			Assert.AreEqual(96, Face.Width);
			Assert.AreEqual(64, Face.Height);
		}

		/// <summary>
		/// Verifies invalid DER does not produce a face record.
		/// </summary>
		[TestMethod]
		public void Test_05_Invalid_Der_Returns_Null_Record()
		{
			Assert.IsFalse(ISO39794_5.TryParse([0x65, 0x80, 0x00, 0x00], out NeuroAccess.Nfc.TravelDocuments.ISO19794.BiometricDataInterchangeRecord? Record));
			Assert.IsNull(Record);
		}

		/// <summary>
		/// Verifies unsupported ISO/IEC 39794 biometric tags are recognized without creating a face record.
		/// </summary>
		[TestMethod]
		public void Test_06_Unsupported_Biometric_Tags_Return_Null_Record()
		{
			byte[] Data = Constructed(0xa1, Constructed(0x64, Primitive(0x80, [0x01])));
			BiometricDataBlock2 DataBlock = new();
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(DataBlock.TryParse(Data, Client, out IDataObject? Parsed));
			BiometricDataBlock2 ParsedBlock = (BiometricDataBlock2)Parsed;
			Assert.IsNull(ParsedBlock.Record);
		}

		/// <summary>
		/// Verifies the official ICAO mandatory-fields DG2 silver dataset parses through the normal DG2 TLV path.
		/// </summary>
		[TestMethod]
		public void Test_07_Official_Mandatory_Fields_Dataset_Parses()
		{
			AssertOfficialDatasetParses("DG2 Silver Dataset (Mandatory Fields Only).dat");
		}

		/// <summary>
		/// Verifies the official ICAO all-fields DG2 silver dataset parses through the normal DG2 TLV path.
		/// </summary>
		[TestMethod]
		public void Test_08_Official_All_Fields_Dataset_Parses()
		{
			AssertOfficialDatasetParses("DG2 Silver Dataset (All Fields).dat");
		}

		private static byte[] CreateFaceImageDataBlock(byte[] ImageData, int ImageDataFormat,
			bool IncludeSize, bool IncludeIdentity, bool IncludeUnknownExtension)
		{
			List<byte[]> ImageInformationChildren =
			[
				Primitive(0x80, IntegerBytes(ImageDataFormat)),
				Constructed(0xa1, Constructed(0xa1, Primitive(0x80, IntegerBytes(0))))
			];

			if (IncludeSize)
				ImageInformationChildren.Add(Constructed(0xa7, Primitive(0x80, IntegerBytes(320)), Primitive(0x81, IntegerBytes(240))));

			byte[] ImageInformation = Constructed(0xa1, [.. ImageInformationChildren]);
			byte[] Representation2D = Constructed(0xa0, Primitive(0x80, ImageData), ImageInformation);
			byte[] BaseRepresentation = Constructed(0xa0, Representation2D);
			List<byte[]> RepresentationBlockChildren =
			[
				Primitive(0x80, IntegerBytes(1)),
				Constructed(0xa1, BaseRepresentation)
			];

			if (IncludeIdentity)
			{
				RepresentationBlockChildren.Add(Constructed(0xa8,
					Constructed(0xa0, Constructed(0xa1, Primitive(0x80, IntegerBytes(2)))),
					Constructed(0xa1, Constructed(0xa1, Primitive(0x80, IntegerBytes(4)))),
					Constructed(0xa2, Constructed(0xa1, Primitive(0x80, IntegerBytes(5))))));
			}

			if (IncludeUnknownExtension)
				RepresentationBlockChildren.Add(Primitive(0x85, [0x01, 0x02]));

			byte[] RepresentationBlock = Constructed(0x30, [.. RepresentationBlockChildren]);
			byte[] VersionBlock = Constructed(0xa0,
				Primitive(0x80, IntegerBytes(3)),
				Primitive(0x81, IntegerBytes(2019)));
			byte[] RepresentationBlocks = Constructed(0xa1, RepresentationBlock);

			return Constructed(0x65, VersionBlock, RepresentationBlocks);
		}

		private static void AssertOfficialDatasetParses(string FileName)
		{
			byte[] Data = File.ReadAllBytes(GetTestDataPath(FileName));
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObjects(Data, Client, out IDataObject[]? DataObjects));
			Assert.HasCount(1, DataObjects);
			Assert.IsInstanceOfType(DataObjects[0], typeof(BiometricEncodingFace));

			BiometricEncodingFace FaceEncoding = (BiometricEncodingFace)DataObjects[0];
			Assert.IsNotNull(FaceEncoding.Templates);
			Assert.IsNotNull(FaceEncoding.Templates.Templates);
			Assert.HasCount(1, FaceEncoding.Templates.Templates);

			BiometricInformationTemplate Template = FaceEncoding.Templates.Templates[0];
			Assert.IsNotNull(Template.BiometricDataBlock);
			Assert.IsInstanceOfType(Template.BiometricDataBlock, typeof(BiometricDataBlock2));
			Assert.IsNotNull(Template.BiometricDataBlock.Record);
			Assert.HasCount(1, Template.BiometricDataBlock.Record.Representations);

			ISO19794RepresentationFace Face = (ISO19794RepresentationFace)Template.BiometricDataBlock.Record.Representations[0];
			Assert.IsTrue(Face.ImageData.Length > 0);
			Assert.IsTrue(Face.Width > 0);
			Assert.IsTrue(Face.Height > 0);
			Assert.IsTrue(Face.ImageDataType is ISO19794ImageDataType.Jpeg or ISO19794ImageDataType.Jpeg2000);
		}

		private static string GetTestDataPath(string FileName)
		{
			string Candidate = Path.Combine(AppContext.BaseDirectory, "TestData", "ISO39794", FileName);
			if (File.Exists(Candidate))
				return Candidate;

			string CurrentDirectory = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(CurrentDirectory))
			{
				Candidate = Path.Combine(CurrentDirectory, "NeuroAccess.Nfc.Test", "TestData", "ISO39794", FileName);
				if (File.Exists(Candidate))
					return Candidate;

				DirectoryInfo? Parent = Directory.GetParent(CurrentDirectory);
				if (Parent is null)
					break;

				CurrentDirectory = Parent.FullName;
			}

			Assert.Fail("Unable to find ISO 39794 test data file: " + FileName);
			return string.Empty;
		}

		private static byte[] CreateJpegImage(int Width, int Height)
		{
			return
			[
				0xff, 0xd8,
				0xff, 0xc0,
				0x00, 0x11,
				0x08,
				(byte)(Height >> 8), (byte)Height,
				(byte)(Width >> 8), (byte)Width,
				0x03,
				0x01, 0x11, 0x00,
				0x02, 0x11, 0x00,
				0x03, 0x11, 0x00,
				0xff, 0xd9
			];
		}

		private static byte[] Primitive(int Tag, byte[] Content)
		{
			return Encode(Tag, Content);
		}

		private static byte[] Constructed(int Tag, params byte[][] Children)
		{
			using MemoryStream Content = new();
			foreach (byte[] Child in Children)
				Content.Write(Child, 0, Child.Length);

			return Encode(Tag, Content.ToArray());
		}

		private static byte[] Encode(int Tag, byte[] Content)
		{
			using MemoryStream Result = new();
			Result.WriteByte((byte)Tag);
			WriteLength(Result, Content.Length);
			Result.Write(Content, 0, Content.Length);
			return Result.ToArray();
		}

		private static void WriteLength(Stream Output, int Length)
		{
			if (Length < 0x80)
			{
				Output.WriteByte((byte)Length);
				return;
			}

			byte[] Buffer = IntegerBytes(Length);
			Output.WriteByte((byte)(0x80 | Buffer.Length));
			Output.Write(Buffer, 0, Buffer.Length);
		}

		private static byte[] IntegerBytes(int Value)
		{
			if (Value == 0)
				return [0];

			byte[] Buffer = new byte[4];
			int Offset = Buffer.Length;
			int Temp = Value;
			while (Temp > 0)
			{
				Buffer[--Offset] = (byte)Temp;
				Temp >>= 8;
			}

			byte[] Result = new byte[Buffer.Length - Offset];
			System.Buffer.BlockCopy(Buffer, Offset, Result, 0, Result.Length);
			return Result;
		}

		private static DocumentInformation CreateDocumentInformation()
		{
			return new DocumentInformation()
			{
				MRZ_Information = "L898902C<369080619406236"
			};
		}

		private sealed class EmptyIsoDepInterface : IIsoDepInterface
		{
			public INfcTag? Tag => null;

			public Task OpenIfClosed()
			{
				return Task.CompletedTask;
			}

			public void CloseIfOpen()
			{
			}

			public Task<byte[]> GetHighLayerResponse()
			{
				return Task.FromResult<byte[]>([]);
			}

			public Task<byte[]> GetHistoricalBytes()
			{
				return Task.FromResult<byte[]>([]);
			}

			public void SetTimeout(int Timeout)
			{
			}

			public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
			{
				throw new NotSupportedException();
			}

			public void Dispose()
			{
			}
		}
	}
}
