using System.Xml;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	/// <summary>
	/// Represents the transferable invitation payload (account credentials and optional legal identity).
	/// </summary>
	internal sealed class OnboardingTransferContext
	{
		public OnboardingTransferContext(string accountName, string password, string passwordMethod, XmlElement? legalIdDefinition)
		{
			this.AccountName = accountName;
			this.Password = password;
			this.PasswordMethod = passwordMethod;
			this.LegalIdDefinition = legalIdDefinition;
		}

		/// <summary>
		/// Gets the username provided by the transfer.
		/// </summary>
		public string AccountName { get; }

		/// <summary>
		/// Gets the password provided by the transfer (as supplied in the invitation payload).
		/// </summary>
		public string Password { get; }

		/// <summary>
		/// Gets the password hashing method declared in the invitation payload.
		/// </summary>
		public string PasswordMethod { get; }

		/// <summary>
		/// Gets the optional legal identity definition embedded in the transfer.
		/// </summary>
		public XmlElement? LegalIdDefinition { get; }

		/// <summary>
		/// Gets a value indicating whether the transfer contains an approved legal identity payload.
		/// </summary>
		public bool HasLegalIdentity => this.LegalIdDefinition is not null;
	}
}
