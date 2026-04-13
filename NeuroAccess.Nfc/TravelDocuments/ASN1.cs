using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.Reflection;
using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Events;
using Waher.Networking;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Static class for parsing and decoding security objects encoded using
	/// Abstract Syntax Notation 1 (ASN.1).
	/// </summary>
	public static class ASN1
	{
		private static readonly SortedDictionary<string, int> oidsNotRecognized = [];
		private static readonly SortedDictionary<string, int> ellipticCurvesUsed = [];
		private static readonly SortedDictionary<string, int> unrecognizedCurves = [];
		private static Dictionary<string, ConstructorInfo>? objectConstructors = null;

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="TagNumber">Try to decode the ASN.1 encoded data, as if it was made using
		/// a specific Tag Number.</param>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDerAs(UniversalTagNumber TagNumber, byte[] Data, out object? Value)
		{
			return TryDecodeDerAs(null, TagNumber, Data, out Value);
		}

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="TagNumber">Try to decode the ASN.1 encoded data, as if it was made using
		/// a specific Tag Number.</param>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDerAs(ICommunicationLayer? Client, UniversalTagNumber TagNumber,
			byte[] Data, out object? Value)
		{
			if (Data.Length == 0)
			{
				Value = null;
				return false;
			}

			Data = (byte[])Data.Clone();
			Data[0] = (byte)TagNumber;

			AsnReader Reader = new(Data, AsnEncodingRules.DER);
			return TryDecodeAsn1(Client, Reader, out Value);
		}

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDer(byte[] Data, out object? Value)
		{
			return TryDecodeDer(null, Data, out Value);
		}

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDer(ICommunicationLayer? Client, byte[] Data, out object? Value)
		{
			AsnReader Reader = new(Data, AsnEncodingRules.DER);
			return TryDecodeAsn1(Client, Reader, out Value);
		}

		/// <summary>
		/// Decodes the next ASN.1-encoded object.
		/// </summary>
		/// <param name="Reader">ASN.1 reader</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeAsn1(AsnReader Reader, out object? Value)
		{
			return TryDecodeAsn1(null, Reader, out Value);
		}

		/// <summary>
		/// Decodes the next ASN.1-encoded object.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Reader">ASN.1 reader</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeAsn1(ICommunicationLayer? Client, AsnReader Reader, out object? Value)
		{
			if (!Reader.HasData)
			{
				Value = null;
				return false;
			}

			Asn1Tag Tag = Reader.PeekTag();

			if (Tag.TagClass == TagClass.Universal)
			{
				switch (Tag.TagValue)
				{
					case (int)UniversalTagNumber.EndOfContents:
						Value = null;
						return false;

					case (int)UniversalTagNumber.Boolean:
						Value = Reader.ReadBoolean();
						return true;

					case (int)UniversalTagNumber.Integer:
					case (int)UniversalTagNumber.Enumerated:
						Value = Reader.ReadInteger();
						return true;

					case (int)UniversalTagNumber.BitString:
						Value = Reader.ReadBitString(out _);
						return true;

					case (int)UniversalTagNumber.OctetString:
						byte[] Bin = Reader.ReadOctetString();

						try
						{
							if (TryDecodeDer(Client, Bin, out object? Embedded))
								Value = Embedded;
							else
								Value = Bin;
						}
						catch (Exception)
						{
							Value = Bin;
						}

						return true;

					case (int)UniversalTagNumber.Null:
						Reader.ReadNull();
						Value = null;
						return true;

					case (int)UniversalTagNumber.ObjectIdentifier:
						string Oid = Reader.ReadObjectIdentifier();

						if (TryInstantiate(Oid, out ISecurityObject? SecurityObject))
							Value = SecurityObject;
						else
						{
							Client?.Warning("OID not recognized: " + Oid);
							ReportOidNotRecognized(Oid);
							Value = Oid;
						}
						return true;

					case (int)UniversalTagNumber.ObjectDescriptor:  // Obsolete
					case (int)UniversalTagNumber.UTF8String:
					case (int)UniversalTagNumber.NumericString:
					case (int)UniversalTagNumber.PrintableString:
					case (int)UniversalTagNumber.TeletexString:     // Same as UniversalTagNumber.T61String:
					case (int)UniversalTagNumber.VideotexString:
					case (int)UniversalTagNumber.IA5String:
					case (int)UniversalTagNumber.GraphicString:
					case (int)UniversalTagNumber.VisibleString:     // Same as UniversalTagNumber.ISO646String:
					case (int)UniversalTagNumber.GeneralString:
					case (int)UniversalTagNumber.UniversalString:
					case (int)UniversalTagNumber.UnrestrictedCharacterString:
					case (int)UniversalTagNumber.BMPString:
						Value = Reader.ReadCharacterString((UniversalTagNumber)Tag.TagValue);
						return true;

					case (int)UniversalTagNumber.Real:
					case (int)UniversalTagNumber.RelativeObjectIdentifier:
					case (int)UniversalTagNumber.Time:
					case (int)UniversalTagNumber.Date:
					case (int)UniversalTagNumber.TimeOfDay:
					case (int)UniversalTagNumber.DateTime:
					case (int)UniversalTagNumber.Duration:
					case (int)UniversalTagNumber.ObjectIdentifierIRI:
					case (int)UniversalTagNumber.RelativeObjectIdentifierIRI:
						Value = Reader.ReadEncodedValue();
						return true;

					case (int)UniversalTagNumber.Sequence:          // Same as UniversalTagNumber.SequenceOf:
					case (int)UniversalTagNumber.External:          // Same as UniversalTagNumber.InstanceOf:
					case (int)UniversalTagNumber.Set:               // Same as UniversalTagNumber.SetOf:
					case (int)UniversalTagNumber.Embedded:

						ReadOnlyMemory<byte> Section = Reader.ReadEncodedValue();
						AsnReader Inner = new(Section, Reader.RuleSet);

						if (Tag.TagValue == (int)UniversalTagNumber.Sequence)
							Inner = Inner.ReadSequence();
						else
							Inner = Inner.ReadSetOf(Tag);

						if (!TryDecodeAsn1(Client, Inner, out object? FirstElement))
						{
							Value = Array.Empty<object?>();
							return true;
						}

						if (!TryDecodeAsn1(Client, Inner, out object? Element))
						{
							if (Tag.TagValue == (int)UniversalTagNumber.Sequence)
								Value = new Sequence(new object?[] { FirstElement }, Section.ToArray());
							else
								Value = new Set(new object?[] { FirstElement }, Section.ToArray());

							return true;
						}

						ChunkedList<object?> Elements = [FirstElement, Element];

						while (TryDecodeAsn1(Client, Inner, out Element))
							Elements.Add(Element);

						object?[] Elements2 = [.. Elements];
						byte[] SubSection = Section.ToArray();

						if (FirstElement is ISecurityObject SecurityObject2 &&
							!SecurityObject2.IsConfigured &&
							SecurityObject2.Configure(new Vector(Elements2, SubSection)))
						{
							Value = SecurityObject2;
							return true;
						}

						if (Tag.TagValue == (int)UniversalTagNumber.Sequence)
							Value = new Sequence(Elements2, SubSection);
						else
							Value = new Set(Elements2, SubSection);

						return true;

					case (int)UniversalTagNumber.UtcTime:
						Value = Reader.ReadUtcTime();
						return true;

					case (int)UniversalTagNumber.GeneralizedTime:
						Value = Reader.ReadGeneralizedTime();
						return true;

					default:
						Value = null;
						return false;
				}
			}
			else if (Tag.TagClass == TagClass.ContextSpecific)
			{
				ReadOnlyMemory<byte> Section = Reader.ReadEncodedValue();

				if (Tag.IsConstructed)
				{
					AsnReader Inner = new(Section, Reader.RuleSet);

					try
					{
						Inner = Inner.ReadSequence(Tag);

						if (!TryDecodeAsn1(Client, Inner, out object? FirstElement))
						{
							Value = Array.Empty<object?>();
							return true;
						}

						if (!TryDecodeAsn1(Client, Inner, out object? Element))
						{
							Value = new ContextSpecific(Tag.TagValue, new object?[] { FirstElement }, Section.ToArray());
							return true;
						}

						ChunkedList<object?> Elements = [FirstElement, Element];

						while (TryDecodeAsn1(Client, Inner, out Element))
							Elements.Add(Element);

						object?[] Elements2 = [.. Elements];
						byte[] SubSection = Section.ToArray();

						if (FirstElement is ISecurityObject SecurityObject2 &&
							!SecurityObject2.IsConfigured &&
							SecurityObject2.Configure(new Vector(Elements2, SubSection)))
						{
							Value = new ContextSpecific(Tag.TagValue,
								new object[] { SecurityObject2 }, SubSection);

							return true;
						}

						Value = new ContextSpecific(Tag.TagValue, Elements2, SubSection);
						return true;
					}
					catch (Exception)
					{
						Value = Section.ToArray();
						return true;
					}
				}
				else
				{
					Value = Section.ToArray();
					return true;
				}
			}
			else
			{
				Value = null;
				return false;
			}
		}

		/// <summary>
		/// Records an OID as not recognized.
		/// </summary>
		/// <param name="Oid">OID not recognized.</param>
		/// <returns>Number of times the OID has not been recognized.</returns>
		public static int ReportOidNotRecognized(string Oid)
		{
			lock (oidsNotRecognized)
			{
				if (!oidsNotRecognized.TryGetValue(Oid, out int i))
				{
					oidsNotRecognized[Oid] = 1;
					return 1;
				}
				else
				{
					if (i < int.MaxValue)
						oidsNotRecognized[Oid] = ++i;

					return i;
				}
			}
		}

		/// <summary>
		/// Gets an array of OIDs that has not been recognized.
		/// </summary>
		/// <param name="Clear">If the statistics should be cleared after compiling the list.</param>
		/// <returns>Array of OIDs not recognized together with the number of times each has not
		/// been recognized.</returns>
		public static KeyValuePair<string, int>[] GetOidsNotRecognized(bool Clear)
		{
			lock (oidsNotRecognized)
			{
				KeyValuePair<string, int>[] Result = new KeyValuePair<string, int>[oidsNotRecognized.Count];
				oidsNotRecognized.CopyTo(Result, 0);

				if (Clear)
					oidsNotRecognized.Clear();

				return Result;
			}
		}

		/// <summary>
		/// Records an Elliptic Curve has been used.
		/// </summary>
		/// <param name="Curve">Elliptic Curve</param>
		/// <returns>Number of times the Elliptic Curve has been used.</returns>
		public static int ReportEllipticCurveUse(EllipticCurve Curve)
		{
			string Name = Curve.CurveName;

			lock (ellipticCurvesUsed)
			{
				if (!ellipticCurvesUsed.TryGetValue(Name, out int i))
					ellipticCurvesUsed[Name] = i = 1;
				else
				{
					if (i < int.MaxValue)
						ellipticCurvesUsed[Name] = ++i;
				}

				if (Name == "Custom")
				{
					StringBuilder sb = new StringBuilder();

					sb.Append("Order: ");
					sb.AppendLine(Curve.Order.ToString(CultureInfo.InvariantCulture));
					sb.Append("Cofactor: ");
					sb.AppendLine(Curve.Cofactor.ToString(CultureInfo.InvariantCulture));
					sb.Append("BasePoint.X: ");
					sb.AppendLine(Curve.BasePoint.X.ToString(CultureInfo.InvariantCulture));
					sb.Append("BasePoint.Y: ");
					sb.AppendLine(Curve.BasePoint.Y.ToString(CultureInfo.InvariantCulture));

					if (Curve is PrimeFieldCurve PrimeFieldCurve)
					{
						sb.Append("Prime: ");
						sb.AppendLine(PrimeFieldCurve.Prime.ToString(CultureInfo.InvariantCulture));


						if (PrimeFieldCurve is WeierstrassCurve WeierstrassCurve)
						{
							sb.Append("A: ");
							sb.AppendLine(WeierstrassCurve.A.ToString(CultureInfo.InvariantCulture));
							sb.Append("B: ");
							sb.AppendLine(WeierstrassCurve.B.ToString(CultureInfo.InvariantCulture));
						}
						else if (PrimeFieldCurve is MontgomeryCurve MontgomeryCurve)
						{
							sb.Append("A: ");
							sb.AppendLine(MontgomeryCurve.A.ToString(CultureInfo.InvariantCulture));
						}
						else if (PrimeFieldCurve is EdwardsCurve EdwardsCurve)
						{
							sb.Append("D: ");
							sb.AppendLine(EdwardsCurve.D.ToString(CultureInfo.InvariantCulture));
						}
						else if (PrimeFieldCurve is EdwardsTwistedCurve EdwardsTwistedCurve)
						{
							sb.Append("D: ");
							sb.AppendLine(EdwardsTwistedCurve.D.ToString(CultureInfo.InvariantCulture));
						}
						else
						{
							sb.Append("Type: ");
							sb.AppendLine(Curve.GetType().FullName);
						}
					}

					Name = sb.ToString();
				}

				if (!unrecognizedCurves.TryGetValue(Name, out int j))
					unrecognizedCurves[Name] = 1;
				else
				{
					if (j < int.MaxValue)
						unrecognizedCurves[Name] = ++j;
				}

				return i;
			}
		}

		/// <summary>
		/// Gets an array of Elliptic Curves that has been used.
		/// </summary>
		/// <param name="Clear">If the statistics should be cleared after compiling the list.</param>
		/// <returns>Array of Elliptic Curves used together with the number of times each has been used.</returns>
		public static KeyValuePair<string, int>[] GetEllipticCurvesUsed(bool Clear)
		{
			lock (ellipticCurvesUsed)
			{
				KeyValuePair<string, int>[] Result = new KeyValuePair<string, int>[ellipticCurvesUsed.Count];
				ellipticCurvesUsed.CopyTo(Result, 0);

				if (Clear)
					ellipticCurvesUsed.Clear();

				return Result;
			}
		}

		/// <summary>
		/// Gets an array of unrecognized Elliptic Curves that has been used.
		/// </summary>
		/// <param name="Clear">If the statistics should be cleared after compiling the list.</param>
		/// <returns>Array of unrecognized Elliptic Curves used together with the number of times each has been used.</returns>
		public static KeyValuePair<string, int>[] GetUnrecognizedEllipticCurvesUsed(bool Clear)
		{
			lock (unrecognizedCurves)
			{
				KeyValuePair<string, int>[] Result = new KeyValuePair<string, int>[unrecognizedCurves.Count];
				unrecognizedCurves.CopyTo(Result, 0);

				if (Clear)
					unrecognizedCurves.Clear();

				return Result;
			}
		}

		/// <summary>
		/// Tries to instantiate a new object of a given OID.
		/// </summary>
		/// <param name="Oid">OID of object type.</param>
		/// <param name="Object">Newly created object, if successful.</param>
		/// <returns>If able to create a new object instance of the given OID.</returns>
		public static bool TryInstantiate(string Oid, [NotNullWhen(true)] out ISecurityObject? Object)
		{
			if (objectConstructors is null)
			{
				Dictionary<string, ConstructorInfo> Constructors = [];

				foreach (Type T in Types.GetTypesImplementingInterface(typeof(ISecurityObject)))
				{
					try
					{
						ConstructorInfo? CI = Types.GetDefaultConstructor(T);
						if (CI is null)
							continue;

						ISecurityObject Obj = (ISecurityObject)CI.Invoke(Types.NoParameters);

						Constructors[Obj.Oid] = CI;
					}
					catch (Exception ex)
					{
						Log.Exception(ex);
					}
				}

				objectConstructors = Constructors;
			}

			if (objectConstructors.TryGetValue(Oid, out ConstructorInfo? CI2))
			{
				Object = (ISecurityObject)CI2.Invoke(Types.NoParameters);
				return true;
			}
			else
			{
				Object = null;
				return false;
			}
		}
	}
}
