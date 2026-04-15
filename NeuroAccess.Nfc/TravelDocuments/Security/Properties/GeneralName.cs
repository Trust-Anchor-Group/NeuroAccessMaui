using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Net;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties
{
	/// <summary>
	/// Abstract base class for general name properties.
	/// </summary>
	public abstract class GeneralName : SecurityObject
	{
		private object? name;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length != 2)
				return false;

			object? Last = SecurityInfo.LastElementNested;

			if (Last is Vector Context)
				return TryParse(Context, out this.name);
			else if (Last is byte[] BinName)
			{
				this.name = BinName;
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.name is not null;

		/// <summary>
		/// General Name
		/// </summary>
		public object? Name => this.name;

		/// <summary>
		/// Tries to parse a general name from an ASN.1 decoded vector.
		/// </summary>
		/// <param name="ParsedVector">Parsed vector.</param>
		/// <param name="Name">General name, if successful.</param>
		/// <returns>If able to parse the general name from the vector definition.</returns>
		public static bool TryParse(Vector ParsedVector, [NotNullWhen(true)] out object? Name)
		{
			object? First = ParsedVector.FirstElement;
			Name = null;

			if (First is not byte[] GeneralName)
			{
				while (First is Vector v && v.Length == 1)
				{
					ParsedVector = v;
					First = ParsedVector.FirstElement;
				}

				if (First is byte[] GeneralName2)
					GeneralName = GeneralName2;
				else if (First is Vector v &&
					v.Length > 0 &&
					v.FirstElement is byte[] GeneralName3)	// First is absolute name, second a relative name
				{
					GeneralName = GeneralName3;
				}
				else
				{
					Name = ParsedVector;
					return true;
				}
			}

			if (GeneralName.Length == 0)
				return false;

			switch (GeneralName[0])
			{
				case 0x83:	// ORAddress
				case 0x85:	// EDIPartyName
					return ASN1.TryDecodeDerAs(UniversalTagNumber.Sequence, GeneralName, out Name);

				case 0x84:  // Name
					Name = GeneralName;
					return true;

				case 0x81:  // IA5String
				case 0x82:
				case 0x86:
					return ASN1.TryDecodeDerAs(UniversalTagNumber.IA5String, GeneralName, out Name);

				case 0x80:  // OtherName
					return ASN1.TryDecodeDerAs(UniversalTagNumber.OctetString, GeneralName, out Name);

				case 0x87:	// IpAddress
					if (!ASN1.TryDecodeDerAs(UniversalTagNumber.OctetString, GeneralName, out Name))
						return false;

					if (Name is byte[] BinIpAddress)
						Name = new IPAddress(BinIpAddress);
					else if (Name is null)
						return false;

					return true;

				case 0x88:
					return ASN1.TryDecodeDerAs(UniversalTagNumber.ObjectIdentifier, GeneralName, out Name);

				default:
					return false;
			}
		}
	}
}
