using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using IdApp.Services;
using IdApp.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class IsFriendModel : XmppViewModel
	{
		private NotificationEvent @event;

		/// <summary>
		/// Creates an instance of the <see cref="IsFriendModel"/> class.
		/// </summary>
		protected internal IsFriendModel()
			: base()
		{
			this.ClickCommand = new Command(async P => await this.LabelClicked(P));
			this.AddContactCommand = new Command(async _ => await this.AddContact());
			this.RemoveContactCommand = new Command(async _ => await this.RemoveContact());
			this.AcceptCommand = new Command(_ => this.Accept());
			this.RejectCommand = new Command(_ => this.Reject());
			this.IgnoreCommand = new Command(async _ => await this.Ignore());

			this.Tags = new ObservableCollection<HumanReadableTag>();
			this.CallerTags = new ObservableCollection<HumanReadableTag>();
			this.RuleRanges = new ObservableCollection<RuleRangeModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out IsFriendNavigationArgs args))
			{
				this.@event = args.Event;
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
				this.RemoteJid = args.RemoteJid;
				this.RemoteFriendlyName = args.RemoteFriendlyName;
				this.Key = args.Key;
				this.ProvisioningService = args.ProvisioningService;

				if (this.FriendlyName == this.BareJid)
					this.FriendlyName = LocalizationResourceManager.Current["NotAvailable"];

				this.RemoteFriendlyNameAvailable = this.RemoteFriendlyName != this.RemoteJid;
				if (!this.RemoteFriendlyNameAvailable)
					this.RemoteFriendlyName = LocalizationResourceManager.Current["NotAvailable"];

				this.Tags.Clear();
				this.CallerTags.Clear();

				ContactInfo Thing = await ContactInfo.FindByBareJid(this.BareJid);
				if (Thing?.MetaData is not null)
				{
					foreach (Property Tag in Thing.MetaData)
						this.Tags.Add(new HumanReadableTag(Tag));
				}

				ContactInfo Caller = await ContactInfo.FindByBareJid(this.RemoteJid);
				this.CallerInContactsList = Caller is not null;
				if (Caller?.MetaData is not null)
				{
					foreach (Property Tag in Caller.MetaData)
						this.CallerTags.Add(new HumanReadableTag(Tag));
				}

				this.RuleRanges.Clear();
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Caller, LocalizationResourceManager.Current["CallerOnly"]));
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Domain, string.Format(LocalizationResourceManager.Current["EntireDomain"], XmppClient.GetDomain(this.RemoteJid))));
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.All, LocalizationResourceManager.Current["Everyone"]));

				this.SelectedRuleRangeIndex = 0;
			}

			this.EvaluateAllCommands();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClickCommand, this.AddContactCommand, this.RemoveContactCommand,
				this.AcceptCommand, this.RejectCommand, this.IgnoreCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
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
		/// The command to bind to for processing a user click on a label
		/// </summary>
		public System.Windows.Input.ICommand ClickCommand { get; }

		/// <summary>
		/// </summary>
		public System.Windows.Input.ICommand AddContactCommand { get; }

		/// <summary>
		/// The command to bind to for reemoving a caller from the contact list.
		/// </summary>
		public System.Windows.Input.ICommand RemoveContactCommand { get; }

		/// <summary>
		/// The command to bind to for accepting the request
		/// </summary>
		public System.Windows.Input.ICommand AcceptCommand { get; }

		/// <summary>
		/// The command to bind to for rejecting the request
		/// </summary>
		public System.Windows.Input.ICommand RejectCommand { get; }

		/// <summary>
		/// The command to bind to for ignoring the request
		/// </summary>
		public System.Windows.Input.ICommand IgnoreCommand { get; }

		/// <summary>
		/// See <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// The Bare JID of the thing.
		/// </summary>
		public string BareJid
		{
			get => (string)this.GetValue(BareJidProperty);
			set => this.SetValue(BareJidProperty, value);
		}

		/// <summary>
		/// See <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// The Friendly Name of the thing.
		/// </summary>
		public string FriendlyName
		{
			get => (string)this.GetValue(FriendlyNameProperty);
			set => this.SetValue(FriendlyNameProperty, value);
		}

		/// <summary>
		/// See <see cref="RemoteJid"/>
		/// </summary>
		public static readonly BindableProperty RemoteJidProperty =
			BindableProperty.Create(nameof(RemoteJid), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// The Bare JID of the remote entity trying to connect to the thing.
		/// </summary>
		public string RemoteJid
		{
			get => (string)this.GetValue(RemoteJidProperty);
			set => this.SetValue(RemoteJidProperty, value);
		}

		/// <summary>
		/// See <see cref="RemoteFriendlyName"/>
		/// </summary>
		public static readonly BindableProperty RemoteFriendlyNameProperty =
			BindableProperty.Create(nameof(RemoteFriendlyName), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// The Friendly Name of the remote entity
		/// </summary>
		public string RemoteFriendlyName
		{
			get => (string)this.GetValue(RemoteFriendlyNameProperty);
			set => this.SetValue(RemoteFriendlyNameProperty, value);
		}

		/// <summary>
		/// See <see cref="RemoteFriendlyNameAvailable"/>
		/// </summary>
		public static readonly BindableProperty RemoteFriendlyNameAvailableProperty =
			BindableProperty.Create(nameof(RemoteFriendlyNameAvailable), typeof(bool), typeof(IsFriendModel), default(bool));

		/// <summary>
		/// If the Friendly Name of the remote entity exists
		/// </summary>
		public bool RemoteFriendlyNameAvailable
		{
			get => (bool)this.GetValue(RemoteFriendlyNameAvailableProperty);
			set => this.SetValue(RemoteFriendlyNameAvailableProperty, value);
		}

		/// <summary>
		/// See <see cref="Key"/>
		/// </summary>
		public static readonly BindableProperty KeyProperty =
			BindableProperty.Create(nameof(Key), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// Provisioning key.
		/// </summary>
		public string Key
		{
			get => (string)this.GetValue(KeyProperty);
			set => this.SetValue(KeyProperty, value);
		}

		/// <summary>
		/// See <see cref="ProvisioningService"/>
		/// </summary>
		public static readonly BindableProperty ProvisioningServiceProperty =
			BindableProperty.Create(nameof(ProvisioningService), typeof(string), typeof(IsFriendModel), default(string));

		/// <summary>
		/// Provisioning key.
		/// </summary>
		public string ProvisioningService
		{
			get => (string)this.GetValue(ProvisioningServiceProperty);
			set => this.SetValue(ProvisioningServiceProperty, value);
		}

		/// <summary>
		/// See <see cref="CallerInContactsList"/>
		/// </summary>
		public static readonly BindableProperty CallerInContactsListProperty =
			BindableProperty.Create(nameof(CallerInContactsList), typeof(bool), typeof(IsFriendModel), default(bool));

		/// <summary>
		/// The Friendly Name of the remote entity
		/// </summary>
		public bool CallerInContactsList
		{
			get => (bool)this.GetValue(CallerInContactsListProperty);
			set => this.SetValue(CallerInContactsListProperty, value);
		}

		/// <summary>
		/// See <see cref="RuleRange"/>
		/// </summary>
		public static readonly BindableProperty SelectedRuleRangeIndexProperty =
			BindableProperty.Create(nameof(SelectedRuleRangeIndex), typeof(int), typeof(IsFriendModel), -1);

		/// <summary>
		/// The selected rule range index
		/// </summary>
		public int SelectedRuleRangeIndex
		{
			get => (int)this.GetValue(SelectedRuleRangeIndexProperty);
			set => this.SetValue(SelectedRuleRangeIndexProperty, value);
		}

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this);
			else if (obj is string s)
				return ViewClaimThingViewModel.LabelClicked(string.Empty, s, s, this);
			else
				return Task.CompletedTask;
		}

		private async Task AddContact()
		{
			if (!this.CallerInContactsList)
			{
				ContactInfo Info = new()
				{
					BareJid = this.RemoteJid,
					FriendlyName = this.RemoteFriendlyNameAvailable ? this.RemoteFriendlyName : this.RemoteJid
				};

				await Database.Insert(Info);

				this.CallerInContactsList = true;
			}
		}

		private async Task RemoveContact()
		{
			if (this.CallerInContactsList)
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.RemoteJid);
				if (Info is not null)
					await Database.Delete(Info);

				this.CallerInContactsList = false;
			}
		}

		private	RuleRange GetRuleRange()
		{
			return this.SelectedRuleRangeIndex switch
			{
				1 => RuleRange.Domain,
				2 => RuleRange.All,
				_ => RuleRange.Caller,
			};
		}

		private void Accept()
		{
			this.Respond(true);
		}

		private void Reject()
		{
			this.Respond(false);
		}

		private void Respond(bool Accepts)
		{
			RuleRange Range = this.GetRuleRange();
			FriendshipResolver Resolver = new(this.BareJid, this.RemoteJid, Range);

			this.XmppService.IsFriendResponse(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
				Accepts, Range, this.ResponseHandler, Resolver);
		}

		private async Task ResponseHandler(object Sender, IqResultEventArgs e)
		{
			if (e.Ok)
			{
				await this.NotificationService.DeleteEvents(this.@event);
				await this.NotificationService.DeleteResolvedEvents((IEventResolver)e.State);

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoBackAsync();
				});
			}
			else
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () => await this.UiSerializer.DisplayAlert(e.StanzaError ??
					new Exception(LocalizationResourceManager.Current["UnableToRespond"])));
			}
		}

		private async Task Ignore()
		{
			await this.NotificationService.DeleteEvents(this.@event);
			await this.NavigationService.GoBackAsync();
		}

	}
}
