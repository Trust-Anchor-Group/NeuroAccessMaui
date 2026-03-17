using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for security objects that represent named string values.
	/// </summary>
	public abstract class SecurityString : SecurityObject
	{
		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
		{
			if (SecurityInfo.Length == 2 &&
				SecurityInfo.GetValue(1) is string Value)
			{
				this.Value = Value;
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// String Value.
		/// </summary>
		public string Value { get; private set; } = string.Empty;

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetType().Name + ": " + this.Value;
		}
	}
}
