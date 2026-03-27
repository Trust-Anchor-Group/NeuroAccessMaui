namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Basic Constraints
	/// </summary>
	public class BasicConstraints : SecurityObject
	{
		private bool certificateAuthority = false;
		private int pathLengthConstraint = int.MaxValue;
		private KeyUsage? keyUsage = null;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.19";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElement is not Vector BasicConstraints)
				return false;

			int c = BasicConstraints.Length;
			int i = 0;

			if (i < c && BasicConstraints[i] is KeyUsage KeyUsage)
			{
				i++;
				this.keyUsage = KeyUsage;
			}

			if (i < c && BasicConstraints[i] is bool CertificateAuthority)
			{
				i++;
				this.certificateAuthority = CertificateAuthority;
			}

			if (i < c && BasicConstraints[i] is System.Numerics.BigInteger PathLengthConstraint &&
				PathLengthConstraint >= int.MinValue &&
				PathLengthConstraint <= int.MaxValue)
			{
				i++;
				this.pathLengthConstraint = (int)PathLengthConstraint;
			}

			return i == c;
		}

		/// <summary>
		/// Certificate Authority
		/// </summary>
		public bool CertificateAuthority => this.certificateAuthority;

		/// <summary>
		/// Path Length Constraint
		/// </summary>
		public int PathLengthConstraint => this.pathLengthConstraint;

		/// <summary>
		/// Key usage.
		/// </summary>
		public KeyUsage? KeyUsage => this.keyUsage;
	}
}
