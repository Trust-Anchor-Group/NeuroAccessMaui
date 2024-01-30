using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Notification.Things
{
	/// <summary>
	/// Provisioning Token Type
	/// </summary>
	public enum TokenType
	{
		/// <summary>
		/// User token
		/// </summary>
		User,

		/// <summary>
		/// Device token
		/// </summary>
		Device,

		/// <summary>
		/// Service token
		/// </summary>
		Service
	}

	/// <summary>
	/// A record of a token used in provisioning.
	/// </summary>
	[TypeName(TypeNameSerialization.None)]
	public class ProvisioningToken
	{
		/// <summary>
		/// A record of a token used in provisioning.
		/// </summary>
		public ProvisioningToken()
		{
		}

		/// <summary>
		/// A record of a token used in provisioning.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="Type">Token Type</param>
		public ProvisioningToken(string Token, TokenType Type)
		{
			this.Token = Token;
			this.Type = Type;
		}

		/// <summary>
		/// Token
		/// </summary>
		[DefaultValueStringEmpty]
		public string Token { get; set; } = string.Empty;

		/// <summary>
		/// Token Type
		/// </summary>
		public TokenType Type { get; set; }

		/// <summary>
		/// Friendly Name of certificate
		/// </summary>
		[DefaultValueStringEmpty]
		public string? FriendlyName { get; set; }

		/// <summary>
		/// Binary part of certificate.
		/// </summary>
		[DefaultValueNull]
		public byte[]? Certificate { get; set; }
	}
}
