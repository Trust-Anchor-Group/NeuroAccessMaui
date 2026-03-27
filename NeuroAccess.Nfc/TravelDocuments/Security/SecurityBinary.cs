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
			if (SecurityInfo.LastElement is byte[] Value)
				this.Value = Value;
			else if (SecurityInfo.LastElement is Vector V &&
				V.Length == 1 &&
				V.FirstElement is byte[] Value2)
			{
				this.Value = Value2;
			}
			else if (SecurityInfo.FirstElement is SecurityBinary Binary)
				this.Value = Binary.Value;
			else
				return false;

			return true;
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
