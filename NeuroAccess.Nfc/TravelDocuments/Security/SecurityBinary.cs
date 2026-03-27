using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for security objects that represent named binary values.
	/// </summary>
	public abstract class SecurityBinary : SecurityObject
	{
		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length == 1 || SecurityInfo.Length == 2)
			{
				if (SecurityInfo[SecurityInfo.Length] is byte[] Value)
				{
					this.Value = Value;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Binary Value.
		/// </summary>
		public byte[] Value { get; private set; } = [];

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetType().Name + ": " + Hashes.BinaryToString(this.Value, true);
		}
	}
}
