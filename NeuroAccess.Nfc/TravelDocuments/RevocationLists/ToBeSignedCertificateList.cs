using NeuroAccess.Nfc.TravelDocuments.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Certificate List, without signature, as defined in RFC 5280, §5.1
	/// </summary>
	public class ToBeSignedCertificateList
	{
		private readonly Dictionary<string, RevokedReason> revokedReasons = [];

		/// <summary>
		/// Certificate List, without signature, as defined in RFC 5280, §5.1
		/// </summary>
		private ToBeSignedCertificateList(byte[] Binary, int? Version, Vector AlgorithmIdentifier,
			Vector Issuer, DateTimeOffset ThisUpdate, DateTimeOffset NextUpdate,
			RevokedCertificate[] RevokedCertificates, byte[]? AuthorityKeyIdentifier,
			Vector? Extensions)
		{
			this.Binary = Binary;
			this.Version = Version;
			this.AlgorithmIdentifier = AlgorithmIdentifier;
			this.Issuer = Issuer;
			this.ThisUpdate = ThisUpdate;
			this.NextUpdate = NextUpdate;
			this.RevokedCertificates = RevokedCertificates;
			this.AuthorityKeyIdentifier = AuthorityKeyIdentifier;
			this.Extensions = Extensions;

			foreach (RevokedCertificate RevokedCertificate in RevokedCertificates)
			{
				string SerialNumber = RevokedCertificate.SerialNumber.ToString("X", CultureInfo.InvariantCulture);

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

			ChunkedList<RevokedCertificate> RevokedCertificates2 = [];

			foreach (object? Item in RevokedCertificates.Elements)
			{
				if (Item is not Vector RevokedCertificate)
					return false;

				int d = RevokedCertificate.Elements.Length;

				if (d < 2)
					return false;

				if (RevokedCertificate.Elements.GetValue(0) is not System.Numerics.BigInteger SerialNumber)
					return false;

				if (RevokedCertificate.Elements.GetValue(1) is not DateTimeOffset Timestamp)
					return false;

				Vector? RevokedCertificateExtensions = null;
				RevokedReason? Reason = null;

				if (d > 2 && RevokedCertificate.Elements.GetValue(2) is Vector RevokedCertificateExtensions2)
				{
					RevokedCertificateExtensions = RevokedCertificateExtensions2;

					foreach (object? Extension in RevokedCertificateExtensions2.Elements)
					{
						if (Extension is Vector ExtensionSequence &&
							ExtensionSequence.Elements.Length >= 2 &&
							ExtensionSequence.Elements.GetValue(0) is string ExtensionOid &&
							ExtensionOid == "2.5.29.21" &&
							ExtensionSequence.Elements.GetValue(1) is byte[] ExtensionBin &&
							ExtensionBin.Length > 0)
						{
							ExtensionBin[0] = (byte)UniversalTagNumber.Integer;

							if (TravelDocumentsClient.TryDecodeDER(ExtensionBin, out object? ParsedExtension) &&
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

			if (i < c && TbsCertList.Elements.GetValue(i) is Vector ListExtensions2)
			{
				i++;
				ListExtensions = ListExtensions2;

				foreach (object? Extension in ListExtensions2.Elements)
				{
					if (Extension is not Vector ExtensionSequence)
						continue;

					if (ExtensionSequence.Elements.Length < 2)
						continue;

					if (ExtensionSequence.Elements.GetValue(0) is not Vector ListExtensions3)
						continue;

					if (ListExtensions3.Elements.Length < 2)
						continue;

					if (ListExtensions3.Elements.GetValue(0) is not string ExtensionOid)
						continue;

					if (ExtensionOid != "2.5.29.35")
						continue;

					if (ListExtensions3.Elements.GetValue(1) is not Vector ExtensionValue)
						continue;

					if (ExtensionValue.Elements.Length == 0)
						continue;

					if (ExtensionValue.Elements.GetValue(0) is not byte[] ImplicitValue)
						continue;

					ImplicitValue[0] = (byte)UniversalTagNumber.OctetString;

					if (!TravelDocumentsClient.TryDecodeDER(ImplicitValue, out object? ParsedExtension))
						continue;

					if (ParsedExtension is not byte[] Aki)
						continue;

					AuthorityKeyIdentifier = Aki;
					break;
				}
			}
			else
				ListExtensions = null;

			if (i < c)
				return false;

			Parsed = new ToBeSignedCertificateList(TbsCertList.SubSection, Version, AlgorithmIdentifier,
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
		public bool HasBeenRevoked(X509Certificate2 Certificate, out RevokedReason Reason)
		{
			return this.revokedReasons.TryGetValue(Certificate.SerialNumber, out Reason);
		}
	}
}
