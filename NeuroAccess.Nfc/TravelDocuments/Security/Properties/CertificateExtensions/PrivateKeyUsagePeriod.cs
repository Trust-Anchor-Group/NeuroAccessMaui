using System;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Text;
using Waher.Content.Xml;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Private Key Usage Period
	/// </summary>
	public class PrivateKeyUsagePeriod : SecurityObject
	{
		private DateTimeOffset? notBefore = null;
		private DateTimeOffset? notAfter = null;
		protected bool configured = false;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.16";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configured;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElementNested is not Vector UsagePeriod)
				return false;

			int c = UsagePeriod.Length;

			if (c > 0)
			{
				if (!TryParseGeneralizedTime(UsagePeriod[0], out DateTimeOffset? NotBefore))
					return false;

				this.notBefore = NotBefore;

				if (c > 1)
				{
					if (!TryParseGeneralizedTime(UsagePeriod[1], out DateTimeOffset? NotAfter))
						return false;

					this.notAfter = NotAfter;
				}
			}

			this.configured = true;

			return true;
		}

		public static bool TryParseGeneralizedTime(object? Data,
			[NotNullWhen(true)] out DateTimeOffset? Result)
		{
			if (Data is DateTimeOffset Timestamp)
			{
				Result = Timestamp;
				return true;
			}

			if (Data is byte[] Bin && Bin.Length > 0 && (Bin[0] & 0x80) != 0 &&
				Bin.Length == Bin[1] + 2)
			{
				Bin = (byte[])Bin.Clone();
				Bin[0] = (byte)UniversalTagNumber.IA5String;
				AsnReader Reader = new(Bin, AsnEncodingRules.DER);
				string s = Reader.ReadCharacterString((UniversalTagNumber)Bin[0]);

				if (s.Length > 12)
				{
					StringBuilder sb = new();

					sb.Append(s[..4]);
					sb.Append('-');
					sb.Append(s.AsSpan(4, 2));
					sb.Append('-');
					sb.Append(s.AsSpan(6, 2));
					sb.Append('T');
					sb.Append(s.AsSpan(8, 2));
					sb.Append(':');
					sb.Append(s.AsSpan(10, 2));
					sb.Append(':');
					sb.Append(s[12..]);

					if (XML.TryParse(sb.ToString(), out Timestamp))
					{
						Result = Timestamp;
						return true;
					}
				}
			}

			Result = null;
			return false;
		}

		/// <summary>
		/// Not Before
		/// </summary>
		public DateTimeOffset? NotBefore => this.notBefore;

		/// <summary>
		/// Not After
		/// </summary>
		public DateTimeOffset? NotAfter => this.notAfter;
	}
}
