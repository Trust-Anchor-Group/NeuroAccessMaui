using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using BruTile.Wms;
using CommunityToolkit.Mvvm.ComponentModel;
using Waher.Security.TOTP;

namespace NeuroAccessMaui.UI.Pages.Main.Authenticator
{
	/// <summary>
	/// View model for the <see cref="AuthenticatorPage"/>.
	/// </summary>
	public partial class AuthenticatorViewModel : BaseViewModel
	{
		[ObservableProperty]
		private ObservableCollection<CredentialModel> credentialModels = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticatorViewModel"/> class.
		/// </summary>
		public AuthenticatorViewModel() : base()
		{
		}

		/// <summary>
		/// OnInitialize
		/// </summary>
		public override async Task OnInitializeAsync()
		{
			foreach (ExternalCredential Credential in await ExternalCredential.GetCredentials())
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.CredentialModels.Add(new CredentialModel(Credential));
				});
			}
		}
	}

	public class CredentialModel
	{
		private readonly ExternalCredential credential;

		public string Issuer => this.credential.Issuer;
		public string Label => this.credential.Label;

		public CredentialModel(ExternalCredential Credential)
		{
			this.credential = Credential;
		}	
	}
}
