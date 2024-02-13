using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using System.Collections.ObjectModel;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public partial class IsFriendModel : XmppViewModel
	{
		private NotificationEvent? @event;

		/// <summary>
		/// Creates an instance of the <see cref="IsFriendModel"/> class.
		/// </summary>
		protected internal IsFriendModel()
			: base()
		{
			this.Tags = [];
			this.CallerTags = [];
			this.RuleRanges = [];
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out IsFriendNavigationArgs? args))
			{
				this.@event = args.Event;
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
				this.RemoteJid = args.RemoteJid;
				this.RemoteFriendlyName = args.RemoteFriendlyName;
				this.Key = args.Key;
				this.ProvisioningService = args.ProvisioningService;

				if (this.FriendlyName == this.BareJid)
					this.FriendlyName = ServiceRef.Localizer[nameof(AppResources.NotAvailable)];

				this.RemoteFriendlyNameAvailable = this.RemoteFriendlyName != this.RemoteJid;
				if (!this.RemoteFriendlyNameAvailable)
					this.RemoteFriendlyName = ServiceRef.Localizer[nameof(AppResources.NotAvailable)];

				this.Tags.Clear();
				this.CallerTags.Clear();

				ContactInfo Thing = await ContactInfo.FindByBareJid(this.BareJid ?? string.Empty);
				if (Thing?.MetaData is not null)
				{
					foreach (Property Tag in Thing.MetaData)
						this.Tags.Add(new HumanReadableTag(Tag));
				}

				ContactInfo Caller = await ContactInfo.FindByBareJid(this.RemoteJid ?? string.Empty);
				this.CallerInContactsList = Caller is not null;
				if (Caller?.MetaData is not null)
				{
					foreach (Property Tag in Caller.MetaData)
						this.CallerTags.Add(new HumanReadableTag(Tag));
				}

				this.RuleRanges.Clear();
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Caller, ServiceRef.Localizer[nameof(AppResources.CallerOnly)]));
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Domain, ServiceRef.Localizer[nameof(AppResources.EntireDomain), XmppClient.GetDomain(this.RemoteJid)]));
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.All, ServiceRef.Localizer[nameof(AppResources.Everyone)]));

				this.SelectedRuleRangeIndex = 0;
			}
		}

		#region Properties

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// Holds a list of meta-data tags associated with the caller.
		/// </summary>
		public ObservableCollection<HumanReadableTag> CallerTags { get; }

		/// <summary>
		/// Available Rule Ranges
		/// </summary>
		public ObservableCollection<RuleRangeModel> RuleRanges { get; }

		/// <summary>
		/// The Bare JID of the thing.
		/// </summary>
		[ObservableProperty]
		private string? bareJid;

		/// <summary>
		/// The Friendly Name of the thing.
		/// </summary>
		[ObservableProperty]
		private string? friendlyName;

		/// <summary>
		/// The Bare JID of the remote entity trying to connect to the thing.
		/// </summary>
		[ObservableProperty]
		private string? remoteJid;

		/// <summary>
		/// The Friendly Name of the remote entity
		/// </summary>
		[ObservableProperty]
		private string? remoteFriendlyName;

		/// <summary>
		/// If the Friendly Name of the remote entity exists
		/// </summary>
		[ObservableProperty]
		private bool remoteFriendlyNameAvailable;

		/// <summary>
		/// Provisioning key.
		/// </summary>
		[ObservableProperty]
		private string? key;

		/// <summary>
		/// Provisioning key.
		/// </summary>
		[ObservableProperty]
		private string? provisioningService;

		/// <summary>
		/// The Friendly Name of the remote entity
		/// </summary>
		[ObservableProperty]
		private bool callerInContactsList;

		/// <summary>
		/// The selected rule range index
		/// </summary>
		[ObservableProperty]
		private int selectedRuleRangeIndex;

		#endregion

		/// <summary>
		/// The command to bind to for processing a user click on a label
		/// </summary>
		[RelayCommand]
		private static Task Click(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue);
			else if (obj is string s)
				return ViewClaimThingViewModel.LabelClicked(string.Empty, s, s);
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Adds the device to the list of contacts.
		/// </summary>
		[RelayCommand]
		private async Task AddContact()
		{
			if (!this.CallerInContactsList)
			{
				ContactInfo Info = new()
				{
					BareJid = this.RemoteJid,
					FriendlyName = (this.RemoteFriendlyNameAvailable ? this.RemoteFriendlyName : this.RemoteJid) ?? string.Empty
				};

				await Database.Insert(Info);

				this.CallerInContactsList = true;
			}
		}

		/// <summary>
		/// The command to bind to for reemoving a caller from the contact list.
		/// </summary>
		[RelayCommand]
		private async Task RemoveContact()
		{
			if (this.CallerInContactsList)
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.RemoteJid ?? string.Empty);
				if (Info is not null)
					await Database.Delete(Info);

				this.CallerInContactsList = false;
			}
		}

		private RuleRange GetRuleRange()
		{
			return this.SelectedRuleRangeIndex switch
			{
				1 => RuleRange.Domain,
				2 => RuleRange.All,
				_ => RuleRange.Caller,
			};
		}

		/// <summary>
		/// The command to bind to for accepting the request
		/// </summary>
		[RelayCommand]
		private void Accept()
		{
			this.Respond(true);
		}

		/// <summary>
		/// The command to bind to for rejecting the request
		/// </summary>
		[RelayCommand]
		private void Reject()
		{
			this.Respond(false);
		}

		private void Respond(bool Accepts)
		{
			RuleRange Range = this.GetRuleRange();
			FriendshipResolver Resolver = new(this.BareJid ?? string.Empty, this.RemoteJid ?? string.Empty, Range);

			ServiceRef.XmppService.IsFriendResponse(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
				this.BareJid ?? string.Empty, this.RemoteJid ?? string.Empty, this.Key ?? string.Empty, Accepts, Range,
				this.ResponseHandler, Resolver);
		}

		private async Task ResponseHandler(object? Sender, IqResultEventArgs e)
		{
			if (e.Ok)
			{
				if (this.@event is not null)
					await ServiceRef.NotificationService.DeleteEvents(this.@event);

				await ServiceRef.NotificationService.DeleteResolvedEvents((IEventResolver)e.State);

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await ServiceRef.NavigationService.GoBackAsync();
				});
			}
			else
			{
				MainThread.BeginInvokeOnMainThread(async () => await ServiceRef.UiSerializer.DisplayException(e.StanzaError ??
					new Exception(ServiceRef.Localizer[nameof(AppResources.UnableToRespond)])));
			}
		}

		/// <summary>
		/// The command to bind to for ignoring the request
		/// </summary>
		[RelayCommand]
		private async Task Ignore()
		{
			if (this.@event is not null)
				await ServiceRef.NotificationService.DeleteEvents(this.@event);

			await ServiceRef.NavigationService.GoBackAsync();
		}

	}
}
