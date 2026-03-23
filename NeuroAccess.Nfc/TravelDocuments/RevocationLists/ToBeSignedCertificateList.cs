using System;
using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.Security;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Certificate List, without signature, as defined in RFC 5280, §5.1
	/// </summary>
	public class ToBeSignedCertificateList
	{
		/// <summary>
		/// Certificate List, without signature, as defined in RFC 5280, §5.1
		/// </summary>
		private ToBeSignedCertificateList(byte[] Binary, int? Version, Vector AlgorithmIdentifier,
			Vector Issuer, DateTimeOffset ThisUpdate, DateTimeOffset NextUpdate,
			Vector RevokedCertificates, Vector? Extensions)
		{
			this.Binary = Binary;
			this.Version = Version;
			this.AlgorithmIdentifier = AlgorithmIdentifier;
			this.Issuer = Issuer;
			this.ThisUpdate = ThisUpdate;
			this.NextUpdate = NextUpdate;
			this.RevokedCertificates = RevokedCertificates;
			this.Extensions = Extensions;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate List, as defined in
		/// RFC 5280, §5.1
		/// </summary>
		/// <param name="TbsCertList">Decoded elements of the object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector TbsCertList, [NotNullWhen(true)] out ToBeSignedCertificateList? Parsed)
		{
			Parsed = null;

			int c = TbsCertList.Elements.Length;
			int i = 0;
			int? Version = null;

			if (i < c && TbsCertList.Elements.GetValue(0) is System.Numerics.BigInteger V)
			{
				if (V < int.MinValue || V > int.MaxValue)
					return false;

				Version = (int)V;
				i++;
			}

			if (i >= c || TbsCertList.Elements.GetValue(i++) is not Vector AlgorithmIdentifier)
				return false;

			if (i >= c || TbsCertList.Elements.GetValue(i++) is not Vector Issuer)
				return false;

			if (i >= c || TbsCertList.Elements.GetValue(i++) is not DateTimeOffset ThisUpdate)
				return false;

			if (i < c && TbsCertList.Elements.GetValue(i) is DateTimeOffset NextUpdate)
				i++;
			else
				NextUpdate = DateTime.MaxValue;

			if (i >= c || TbsCertList.Elements.GetValue(i++) is not Vector RevokedCertificates)
				return false;

			Vector? Extensions;

			if (i < c && TbsCertList.Elements.GetValue(i) is Vector Extensions2)
			{
				i++;
				Extensions = Extensions2;
			}
			else
				Extensions = null;

			if (i < c)
				return false;

			Parsed = new ToBeSignedCertificateList(TbsCertList.SubSection, Version, AlgorithmIdentifier,
				Issuer, ThisUpdate, NextUpdate, RevokedCertificates, Extensions);

			return true;
		}

		/// <summary>
		/// Binary representation of list to be signed.
		/// </summary>
		public byte[] Binary { get; }

		/// <summary>
		/// Version of document.
		/// </summary>
		public int? Version { get; }

		/// <summary>
		/// Algorithm identifier.
		/// </summary>
		public Vector AlgorithmIdentifier { get; }

		/// <summary>
		/// Issuer
		/// </summary>
		public Vector Issuer { get; }

		/// <summary>
		/// This update
		/// </summary>
		public DateTimeOffset ThisUpdate { get; }

		/// <summary>
		/// Next update, or <see cref="DateTime.MaxValue"/> if not specified.
		/// </summary>
		public DateTimeOffset NextUpdate { get; }

		/// <summary>
		/// List of revoked certificates.
		/// </summary>
		public Vector RevokedCertificates { get; }

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions { get; }
	}
}
