using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for security objects that represent named binary values.
	/// </summary>
	public abstract class SecurityBinary : SecurityObject
	{
		protected byte[]? value;

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.value is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElement is byte[] Value)
				this.value = Value;
			else if (SecurityInfo.LastElement is Vector V &&
				V.Length == 1 &&
				V.FirstElement is byte[] Value2)
			{
				this.value = Value2;
			}
			else if (SecurityInfo.FirstElement is SecurityBinary Binary)
				this.value = Binary.Value;
			else
				return false;

			return true;
		}

		/// <summary>
		/// Binary Value.
		/// </summary>
		public byte[] Value => this.value!;

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetType().Name + ": " + Hashes.BinaryToString(this.value ?? [], true);
		}
	}
}
