using System;
using NeuroAccess.Nfc.TravelDocuments.Security;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Reference to a revoked certificate.
	/// </summary>
	public class RevokedCertificate
	{
		/// <summary>
		/// Reference to a revoked certificate.
		/// </summary>
		/// <param name="SerialNumber">Serial number.</param>
		/// <param name="Timestamp">Timestamp</param>
		/// <param name="Extensions">Optional extensions</param>
		/// <param name="Reason">Reason for revoking the certificate</param>
		public RevokedCertificate(System.Numerics.BigInteger SerialNumber, DateTimeOffset Timestamp,
			Vector? Extensions, RevokedReason? Reason)
		{
			this.SerialNumber = SerialNumber;
			this.Timestamp = Timestamp;
			this.Extensions = Extensions;
			this.Reason = Reason;
		}

		/// <summary>
		/// Serial number.
		/// </summary>
		public System.Numerics.BigInteger SerialNumber { get; }

		/// <summary>
		/// Timestamp
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Optional extensions
		/// </summary>
		public Vector? Extensions { get; }

		/// <summary>
		/// Reason for revoking the certificate
		/// </summary>
		public RevokedReason? Reason { get; }
	}
}
