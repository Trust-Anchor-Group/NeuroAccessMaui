using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Networking;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Static class for parsing and decoding security objects encoded using
	/// Abstract Syntax Notation 1 (ASN.1).
	/// </summary>
	public static class ASN1
	{
		private static readonly SortedDictionary<string, int> oidsNotRecognized = [];

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDER(byte[] Data, out object? Value)
		{
			return TryDecodeDER(null, Data, out Value);
		}

		/// <summary>
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDER(ICommunicationLayer? Client, byte[] Data, out object? Value)
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
							if (TryDecodeDER(Client, Bin, out object? Embedded))
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

						ISecurityObject SecurityObject = Types.FindBest<ISecurityObject, string>(Oid);
						if (SecurityObject is null)
						{
							Client?.Warning("OID not recognized: " + Oid);
							ReportOidNotRecognized(Oid);
							Value = Oid;
						}
						else
							Value = SecurityObject;

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

						if (FirstElement is ISecurityObject SecurityObject2)
						{
							if (SecurityObject2.Configure(new Vector(Elements2, SubSection)))
							{
								Value = SecurityObject2;
								return true;
							}
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

						if (FirstElement is ISecurityObject SecurityObject2)
						{
							if (SecurityObject2.Configure(new Vector(Elements2, SubSection)))
							{
								Value = new ContextSpecific(Tag.TagValue,
									new object[] { SecurityObject2 }, SubSection);

								return true;
							}
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
	}
}
