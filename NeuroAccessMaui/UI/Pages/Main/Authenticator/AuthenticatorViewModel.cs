using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BruTile.Wms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

	public partial class CredentialModel : ObservableObject
	{
		private readonly ExternalCredential credential;
		private readonly TotpCalculator calculator;
		private Timer timer;

		public string Issuer => this.credential.Issuer;
		public string Label => this.credential.Label;
		public string Account => this.credential.Account;
		public string ImageUri => this.credential.Image;

		[ObservableProperty]
		private int code;

		[ObservableProperty]
		private bool hasCode;

		[ObservableProperty]
		private int secondsRemaining;

		public CredentialModel(ExternalCredential Credential)
		{
			this.credential = Credential;
			this.calculator = new TotpCalculator(this.credential.NrDigits, this.credential.Secret);

			// Start timer to update code every second
			this.timer = new Timer(_ =>
			{
				this.UpdateTimerSecondsRemaining();
			}, null, 0, 1000);
		}

		private void UpdateTimerSecondsRemaining()
		{
			DateTime End = this.calculator.NextStep;
			TimeSpan Remaining = End - DateTime.UtcNow;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.SecondsRemaining = Remaining.Seconds;
				this.UpdateCode();
			});
		}

		[RelayCommand]
		public void UpdateCode()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.Code = this.calculator.Compute();
				this.HasCode = true;
			});
		}

		[RelayCommand]
		public void CopyCode()
		{
			// Copy to clipboard
			MainThread.BeginInvokeOnMainThread(() =>
			{
				Clipboard.Default.SetTextAsync(this.Code.ToString());
			});
		}
	}
}
