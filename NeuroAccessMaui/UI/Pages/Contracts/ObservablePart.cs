using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Identities;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	public partial class ObservablePart : ObservableObject, IDisposable
	{
		public ObservablePart(Part part)
		{
			this.Part = part;
			ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.XmppService_OnPetitionedIdentityResponseReceived;
		}
		/// <summary>
		/// Initializes the part, setting properties which needs to be set asynchronosly
		/// </summary>
		/// <param name="contract"></param>
		public async Task InitializeAsync()
		{
			try
			{
				// Set Friendly name before anything can go wrong
				this.FriendlyName = await this.GetFriendlyNameAsync();


				this.identity = await ServiceRef.ContractOrchestratorService.TryGetLegalIdentity(this.LegalId,
									ServiceRef.Localizer[nameof(AppResources.ForInclusionInContract)]);


				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.OnPropertyChanged(nameof(this.HasIdentity));
				});

				// Update the friendly name after the identity might be set
				this.FriendlyName = await this.GetFriendlyNameAsync();
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
		}

		private async Task XmppService_OnPetitionedIdentityResponseReceived(object? Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			Console.WriteLine(e.Response + " : " + e.RequestedIdentity is not null);
			if (e.RequestedIdentity is not null && e.RequestedIdentity.Id == this.LegalId)
			{
				this.identity = e.RequestedIdentity;
				string FriendlyName = await this.GetFriendlyNameAsync();

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.FriendlyName = FriendlyName;
					this.OnPropertyChanged(nameof(this.HasIdentity));
				});
			}
		}

		private async Task<string> GetFriendlyNameAsync()
		{
			try
			{
				if (this.IsMe)
					return ServiceRef.Localizer[nameof(AppResources.Me)];

				if (this.Part.LegalId == ServiceRef.TagProfile.TrustProviderId && !string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
					return ServiceRef.TagProfile.Domain;

				ContactInfo info = await Database.FindFirstIgnoreRest<ContactInfo>(new FilterFieldEqualTo("LegalId", this.Part.LegalId));
				if (info is not null && !string.IsNullOrEmpty(info.FriendlyName))
					return info.FriendlyName;

				if (this.identity is not null)
					return ContactInfo.GetFriendlyName(this.identity);

				if (info is not null && !string.IsNullOrEmpty(info.BareJid))
					return info.BareJid;
			}
			catch (Exception e)
			{
				//Ignore
			}

			return this.LegalId;
		}
		#region Properties
		private LegalIdentity? identity;

		/// <summary>
		/// The wrapped Part object
		/// </summary>
		public Part Part { get; }

		public string LegalId => this.Part.LegalId;


		public string Role => this.Part.Role;

		/// <summary>
		/// The friendly name for the part
		/// Has to be initialized with <see cref="InitializeAsync"/>
		/// </summary>
		public string? FriendlyName
		{
			get => this.friendlyName;
			private set
			{
				this.SetProperty(ref this.friendlyName, value);
			}
		}
		private string? friendlyName = string.Empty;

		public bool IsMe => this.Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id;
		public bool IsThirdParty => !this.IsMe;

		public bool HasIdentity => this.identity is not null;

		public bool HasSigned
		{
			get => this.hasSigned;
			private set
			{
				this.SetProperty(ref this.hasSigned, value);
			}
		}
		private bool hasSigned = false;

		#endregion


		#region Commands
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenIdentityAsync()
		{
			if (this.identity is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Petition)], ServiceRef.Localizer[nameof(AppResources.PetitionIdentitySent)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.InitializeAsync();
			}
			else
			{
				await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(this.identity));
			}
		}
		#endregion

		private bool disposed = false;

		#region IDisposable Support
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// Unsubscribe from the event to prevent memory leaks
				ServiceRef.XmppService.PetitionedIdentityResponseReceived -= this.XmppService_OnPetitionedIdentityResponseReceived;
			}

			this.disposed = true;
		}
		#endregion
	}
}
