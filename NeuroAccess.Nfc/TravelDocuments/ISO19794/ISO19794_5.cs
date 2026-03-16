using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Waher.Runtime.IO;
using Waher.Script.Exceptions;

namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// ISO 19794-5: Biometric Data interchange formats - Part 5: Face image data.
	/// </summary>
	public static class ISO19794_5
	{
		/// <summary>
		/// Tries to parse biometric data from ISO 19794-5 compliant data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <param name="Record">Parsed record, if successful.</param>
		/// <returns>If able to parse the data.</returns>
		public static bool TryParse(byte[] Data,
			[NotNullWhen(true)] out BiometricDataInterchangeRecord? Record)
		{
			using MemoryStream StreamData = new(Data);
			return TryParse(StreamData, out Record);
		}

		/// <summary>
		/// Tries to parse biometric data from ISO 19794-5 compliant data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <param name="Record">Parsed record, if successful.</param>
		/// <returns>If able to parse the data.</returns>
		public static bool TryParse(Stream Data,
			[NotNullWhen(true)] out BiometricDataInterchangeRecord? Record)
		{
			long Start = Data.Position;
			long MaxRecordLength = Data.Length - Start;

			Record = null;

			// General Header

			if (!Data.TryRead(out string? s) || s != "FAC")
				return false;

			if (!Data.TryRead(out s) || !int.TryParse(s, out int i))
				return false;

			int Version = i / 10;
			int Revision = i % 10;

			if (!Data.TryRead(out uint RecordLength) ||
				RecordLength < 68 || RecordLength > MaxRecordLength)
			{
				return false;
			}

			if (!Data.TryRead(out ushort NrRepresentations) || NrRepresentations == 0)
				return false;

			if (Version >= 3)
			{
				if (!Data.TryRead(out bool Certification))
					return false;

				if (!Data.TryRead(out ushort TemporalSemantics))
					return false;
			}

			// Representations

			for (int Representation = 0; Representation < NrRepresentations; Representation++)
			{
				long RepresentationStart = Data.Position;
				long MaxRepresentationLength = Data.Length - RepresentationStart;

				// Representation Header

				if (!Data.TryRead(out uint RepresentationLength) ||
					RepresentationLength < 51 || RepresentationLength > MaxRepresentationLength)
				{
					return false;
				}

				if (Version >= 3)
				{
					if (!Data.TryRead(out DateTime CaptureDateAndTime))
						return false;

					return false;   // TODO: Parse version 3 records
				}

				if (!Data.TryRead(out ushort NrLandmarkPoints))
					return false;

				if (!Data.TryRead(out byte Gender) || (Gender > 2 && Gender < 0xff) ||
					!Data.TryRead(out byte EyeColour) ||
					!Data.TryRead(out byte HairColour) ||
					!Data.TryRead(out byte SubjectHeight) ||
					!Data.TryRead(3, out uint PropertyMask) ||
					!Data.TryRead(out ushort ExpressionMask) ||
					!Data.TryRead(3, out uint PoseAngle) ||
					!Data.TryRead(3, out uint PoseAngleUncertainty))
				{
					return false;
				}

				if (NrLandmarkPoints > 0)
					return false;               // TODO: Parse landmark points


				if (!Data.TryRead(out byte FaceImageType) ||
					!Data.TryRead(out byte ImageDataType) ||
					!Data.TryRead(out ushort Width) ||
					!Data.TryRead(out ushort Height))
				{
					return false;
				}

				if (Version >= 3)
				{
					if (!Data.TryRead(out byte SpatialSamplingRateLevel) ||
						!Data.TryRead(out byte PostAcquisitionProcessing) ||
						!Data.TryRead(out byte CrossReference))
					{
						return false;
					}
				}

				if (!Data.TryRead(out byte ImageColourSpace))
					return false;


				if (Version < 3)
				{
					if (!Data.TryRead(out byte SourceType) ||
						!Data.TryRead(out ushort DeviceType) ||
						!Data.TryRead(out ushort Quality))
					{
						return false;
					}
				}

				int BytesLeft = (int)RepresentationLength - (int)(Data.Position - RepresentationStart);
				if (BytesLeft < 0)
					return false;

				if (!Data.TryRead(BytesLeft, out byte[] ImageData))
					return false;
			}

			Record = new BiometricDataInterchangeRecord();
			return true;
		}

		private static bool TryRead(this Stream Data, int N, out byte[] Binary)
		{
			Binary = new byte[N];
			return Data.TryReadAll(Binary, 0, N) == N;
		}

		private static bool TryRead(this Stream Data, out bool Result)
		{
			if (!Data.TryRead(out byte b) || b > 1)
			{
				Result = false;
				return false;
			}
			else
			{
				Result = b != 0;
				return true;
			}
		}

		private static bool TryRead(this Stream Data, out byte Result)
		{
			int i = Data.ReadByte();

			if (i < 0)
			{
				Result = 0;
				return false;
			}
			else
			{
				Result = (byte)i;
				return true;
			}
		}

		private static bool TryRead(this Stream Data, out ushort Result)
		{
			Result = 0;

			for (int i = 0; i < 2; i++)
			{
				int j = Data.ReadByte();
				if (j < 0)
					return false;

				Result <<= 8;
				Result |= (byte)j;
			}

			return true;
		}

		private static bool TryRead(this Stream Data, out uint Result)
		{
			return Data.TryRead(4, out Result);
		}

		private static bool TryRead(this Stream Data, int N, out uint Result)
		{
			Result = 0;

			for (int i = 0; i < N; i++)
			{
				int j = Data.ReadByte();
				if (j < 0)
					return false;

				Result <<= 8;
				Result |= (byte)j;
			}

			return true;
		}

		private static bool TryRead(this Stream Data, [NotNullWhen(true)] out string? Result)
		{
			return Data.TryRead(Encoding.ASCII, out Result);
		}

		private static bool TryRead(this Stream Data, Encoding Encoding,
			[NotNullWhen(true)] out string? Result)
		{
			using MemoryStream Temp = new();

			while (true)
			{
				int i = Data.ReadByte();
				if (i < 0)
				{
					Result = null;
					return false;
				}

				if (i == 0)
				{
					Result = Encoding.GetString(Temp.ToArray());
					return true;
				}

				Temp.WriteByte((byte)i);
			}
		}

		private static bool TryRead(this Stream Data, out DateTime Result)
		{
			if (!Data.TryRead(out ushort Year) || Year < 1800 || Year > 9999 ||
				!Data.TryRead(out byte Month) || Month < 1 || Month > 12 ||
				!Data.TryRead(out byte Day) || Day < 1 || Day > DateTime.DaysInMonth(Year, Month) ||
				!Data.TryRead(out byte Hour) || Hour > 59 ||
				!Data.TryRead(out byte Minute) || Minute > 59 ||
				!Data.TryRead(out byte Second) || Second > 59 ||
				!Data.TryRead(out ushort Millisecond) || Millisecond > 1000)
			{
				Result = DateTime.MinValue;
				return false;
			}
			else
			{
				Result = new DateTime(Year, Month, Day, Hour, Minute, Second, Millisecond, DateTimeKind.Utc);
				return true;
			}
		}
	}
}
