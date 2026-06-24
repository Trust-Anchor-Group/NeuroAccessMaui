using System.Collections.Generic;
using System.Numerics;

namespace NeuroAccess.Nfc.TravelDocuments.Security.FieldTypes
{
	/// <summary>
	/// Abstract base class for field types.
	/// </summary>
	public class PrimeField : FieldType
	{
		private BigInteger? prime;

		/// <summary>
		/// Abstract base class for field types.
		/// </summary>
		public PrimeField()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class for field types.
		/// </summary>
		/// <param name="Prime">Prime number.</param>
		public PrimeField(BigInteger Prime)
			: base()
		{
			this.prime = Prime;
		}

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.1.1";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.prime is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo[1] is BigInteger Prime)
			{
				this.prime = Prime;
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Prime number used to define the prime field.
		/// </summary>
		public BigInteger Prime => this.prime!.Value;

		/// <summary>
		/// Gets parsed parameters from the field type definition, if available.
		/// </summary>
		/// <param name="Parameters">Dictionary to receive parsed parameters.</param>
		public override void GetParsedParameters(Dictionary<string, object?> Parameters)
		{
			Parameters["p"] = this.prime;
		}
	}
}
