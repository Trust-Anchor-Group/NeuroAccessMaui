using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.Keys;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Certificate List, without signature, as defined in RFC 5280, §5.1
	/// </summary>
	public class ToBeSignedCertificateList
	{
		private readonly Dictionary<System.Numerics.BigInteger, RevokedReason> revokedReasons = [];

		/// <summary>
		/// Certificate List, without signature, as defined in RFC 5280, §5.1
		/// </summary>
		/// <param name="Binary">Binary representation of list to be signed.</param>
		/// <param name="Version">Version of representation.</param>
		/// <param name="SignatureAlgorithm">Signature algorithm used.</param>
		/// <param name="Issuer">Information about Issuer.</param>
		/// <param name="ThisUpdate">Timestamp of this update.</param>
		/// <param name="NextUpdate">Timestamp of next update, if known.</param>
		/// <param name="RevokedCertificates">List of revoked certificates.</param>
		/// <param name="AuthorityKeyIdentifier"></param>Authority Key Identifier, if known.</param>
		/// <param name="Extensions">Extensions, if any.</param>
		private ToBeSignedCertificateList(byte[] Binary, int? Version, ISignatureAlgorithm SignatureAlgorithm,
			Vector Issuer, DateTimeOffset ThisUpdate, DateTimeOffset NextUpdate,
			RevokedCertificate[] RevokedCertificates, byte[]? AuthorityKeyIdentifier,
			Vector? Extensions)
		{
			this.Binary = Binary;
			this.Version = Version;
			this.SignatureAlgorithm = SignatureAlgorithm;
			this.Issuer = Issuer;
			this.ThisUpdate = ThisUpdate;
			this.NextUpdate = NextUpdate;
			this.RevokedCertificates = RevokedCertificates;
			this.AuthorityKeyIdentifier = AuthorityKeyIdentifier;
			this.Extensions = Extensions;

			foreach (RevokedCertificate RevokedCertificate in RevokedCertificates)
			{
				System.Numerics.BigInteger SerialNumber = RevokedCertificate.SerialNumber;

				if (RevokedCertificate.Reason.HasValue)
				{
					if (RevokedCertificate.Reason.Value == RevokedReason.RemoveFromCRL)
						this.revokedReasons.Remove(SerialNumber);
					else
						this.revokedReasons[SerialNumber] = RevokedCertificate.Reason.Value;
				}
				else
					this.revokedReasons[SerialNumber] = RevokedReason.Unspecified;
			}
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

			int c = TbsCertList.Length;
			int i = 0;
			int? Version = null;

			if (i < c && TbsCertList.FirstElement is System.Numerics.BigInteger V)
			{
				if (V < int.MinValue || V > int.MaxValue)
					return false;

				Version = (int)V;
				i++;
			}

			if (i >= c)
				return false;

			object? AlgorithmIdentifier = TbsCertList[i++];
			ISignatureAlgorithm? SignatureAlgorithm;

			if (AlgorithmIdentifier is ISignatureAlgorithm Algorithm)
				SignatureAlgorithm = Algorithm;
			else if (AlgorithmIdentifier is Vector AlgorithmIdentifierVector)
				SignatureAlgorithm = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifierVector);
			else
				return false;

			if (SignatureAlgorithm is null)
				return false;

			if (i >= c || TbsCertList[i++] is not Vector Issuer)
				return false;

			if (i >= c || TbsCertList[i++] is not DateTimeOffset ThisUpdate)
				return false;

			if (i < c && TbsCertList[i] is DateTimeOffset NextUpdate)
				i++;
			else
				NextUpdate = DateTime.MaxValue;

			// ICAO Doc 9303-12, Table 9:
			// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p12_cons_en.pdf#page=43
			// The revokedCertificates sequence is omitted when no certificates are revoked.
			System.Collections.IEnumerable RevokedCertificates = Array.Empty<object>();
			if (i < c && TbsCertList[i] is Sequence RevokedCertificatesSequence)
			{
				i++;
				RevokedCertificates = RevokedCertificatesSequence;
			}

			ChunkedList<RevokedCertificate> RevokedCertificates2 = [];

			foreach (object? Item in RevokedCertificates)
			{
				if (Item is not Vector RevokedCertificate)
					return false;

				int d = RevokedCertificate.Length;

				if (d < 2)
					return false;

				if (RevokedCertificate.FirstElement is not System.Numerics.BigInteger SerialNumber)
					return false;

				if (RevokedCertificate[1] is not DateTimeOffset Timestamp)
					return false;

				Vector? RevokedCertificateExtensions = null;
				RevokedReason? Reason = null;

				if (d > 2 && RevokedCertificate[2] is Vector RevokedCertificateExtensions2)
				{
					RevokedCertificateExtensions = RevokedCertificateExtensions2;

					foreach (object? Extension in RevokedCertificateExtensions2)
					{
						if (Extension is Vector ExtensionSequence &&
							ExtensionSequence.Length >= 2 &&
							ExtensionSequence.FirstElement is string ExtensionOid &&
							ExtensionOid == "2.5.29.21" &&
							ExtensionSequence[1] is byte[] ExtensionBin &&
							ExtensionBin.Length > 0)
						{
							if (ASN1.TryDecodeDerAs(UniversalTagNumber.Integer, ExtensionBin, out object? ParsedExtension) &&
								ParsedExtension is System.Numerics.BigInteger ReasonCode &&
								ReasonCode >= int.MinValue &&
								ReasonCode <= int.MaxValue)
							{
								Reason = (RevokedReason)(int)ReasonCode;
							}
						}
					}
				}

				RevokedCertificates2.Add(new RevokedCertificate(SerialNumber, Timestamp, RevokedCertificateExtensions, Reason));
			}

			Vector? ListExtensions;
			byte[]? AuthorityKeyIdentifier = null;

			if (i < c && TbsCertList[i] is Vector ListExtensions2)
			{
				i++;

				if (ListExtensions2.Length == 1 &&
					ListExtensions2.FirstElementNested is Vector ListExtensions3 &&
					(ListExtensions2.SubSection[0] & 0x80) != 0)
				{
					ListExtensions2 = ListExtensions3;
				}

				ListExtensions = ListExtensions2;

				foreach (object? Extension in ListExtensions2)
				{
					if (Extension is AuthorityKeyIdentifier Aki)
					{
						AuthorityKeyIdentifier = Aki.Identifier;
						break;
					}
				}
			}
			else
				ListExtensions = null;

			if (i < c)
				return false;

			Parsed = new ToBeSignedCertificateList(TbsCertList.SubSection, Version, SignatureAlgorithm,
				Issuer, ThisUpdate, NextUpdate, [.. RevokedCertificates2], AuthorityKeyIdentifier,
				ListExtensions);

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
		/// Signature algorithm.
		/// </summary>
		public ISignatureAlgorithm SignatureAlgorithm { get; }

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
		public RevokedCertificate[] RevokedCertificates { get; }

		/// <summary>
		/// Authority Key Identifier
		/// </summary>
		public byte[]? AuthorityKeyIdentifier { get; }

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions { get; }

		/// <summary>
		/// Checks if a certificate has been revoked.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <param name="Reason">Reason for the certificate being revoked.</param>
		/// <returns>If the certificate has been revoked.</returns>
		public bool HasBeenRevoked(Certificate Certificate, out RevokedReason Reason)
		{
			return this.revokedReasons.TryGetValue(Certificate.SerialNumber, out Reason);
		}
	}
}
