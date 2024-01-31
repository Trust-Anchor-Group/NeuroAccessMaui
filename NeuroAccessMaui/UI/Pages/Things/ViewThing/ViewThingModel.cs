using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Main.XmppForm;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Things.ReadSensor;
using IdApp.Services;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Things;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Things.ViewThing
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ViewThingModel : XmppViewModel, ILinkableView
	{
		private readonly Dictionary<string, PresenceEventArgs> presences = new(StringComparer.InvariantCultureIgnoreCase);
		private ContactInfo thing;

		/// <summary>
		/// Creates an instance of the <see cref="ViewThingModel"/> class.
		/// </summary>
		protected internal ViewThingModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.Clicked(x));
			this.AddToListCommand = new Command(async _ => await this.AddToList(), _ => !this.InContacts);
			this.RemoveFromListCommand = new Command(async _ => await this.RemoveFromList(), _ => this.InContactsAndNotOwner);
			this.DeleteRulesCommand = new Command(async _ => await this.DeleteRules(), _ => this.IsConnected && this.IsOwner);
			this.DisownThingCommand = new Command(async _ => await this.DisownThing(), _ => this.IsConnected && this.IsOwner);
			this.ReadSensorCommand = new Command(async _ => await this.ReadSensor(), _ => this.IsConnected && this.IsSensor);
			this.ControlActuatorCommand = new Command(async _ => await this.ControlActuator(), _ => this.IsConnected && this.IsActuator);
			this.ChatCommand = new Command(async _ => await this.Chat(), _ => this.IsConnected && !this.IsNodeInConcentrator);

			this.Tags = new ObservableCollection<HumanReadableTag>();
			this.Notifications = new ObservableCollection<EventModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out ViewThingNavigationArgs args))
			{
				this.thing = args.Thing;

				if (this.thing.MetaData is not null)
				{
					this.Tags.Clear();

					foreach (Property Tag in this.thing.MetaData)
						this.Tags.Add(new HumanReadableTag(Tag));
				}

				if (args.Events is not null)
				{
					this.Notifications.Clear();

					int c = 0;

					foreach (NotificationEvent Event in args.Events)
					{
						this.Notifications.Add(new EventModel(Event.Received,
							await Event.GetCategoryIcon(this),
							await Event.GetDescription(this),
							Event,
							this));

						if (Event.Button == EventButton.Contacts)
							c++;
					}

					this.NrPendingChatMessages = c;
					this.HasPendingChatMessages = c > 0;
				}

				this.InContacts = !string.IsNullOrEmpty(this.thing.ObjectId);
				this.IsOwner = this.thing.Owner ?? false;
				this.IsSensor = this.thing.IsSensor ?? false;
				this.IsActuator = this.thing.IsActuator ?? false;
				this.IsConcentrator = this.thing.IsConcentrator ?? false;
				this.IsNodeInConcentrator = !string.IsNullOrEmpty(this.thing.NodeId) || !string.IsNullOrEmpty(this.thing.SourceId) || !string.IsNullOrEmpty(this.thing.Partition);
				this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;
				this.HasNotifications = this.Notifications.Count > 0;
			}

			await this.AssignProperties();
			this.EvaluateAllCommands();

			this.XmppService.OnPresence += this.Xmpp_OnPresence;
			this.XmppService.OnRosterItemAdded += this.Xmpp_OnRosterItemAdded;
			this.XmppService.OnRosterItemUpdated += this.Xmpp_OnRosterItemUpdated;
			this.XmppService.OnRosterItemRemoved += this.Xmpp_OnRosterItemRemoved;
			this.TagProfile.Changed += this.TagProfile_Changed;
			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;

			if (this.IsConnected && this.IsThingOnline)
				await this.CheckCapabilities();
		}

		private async Task CheckCapabilities()
		{
			if (this.InContacts &&
				!this.thing.IsSensor.HasValue ||
				!this.thing.IsActuator.HasValue ||
				!this.thing.IsConcentrator.HasValue ||
				!this.thing.SupportsSensorEvents.HasValue)
			{
				string FullJid = this.GetFullJid();

				if (!string.IsNullOrEmpty(FullJid))
				{
					ServiceDiscoveryEventArgs e = await this.XmppService.SendServiceDiscoveryRequest(FullJid);

					if (!this.InContacts)
						return;

					this.thing.IsSensor = e.HasFeature(SensorClient.NamespaceSensorData);
					this.thing.SupportsSensorEvents = e.HasFeature(SensorClient.NamespaceSensorEvents);
					this.thing.IsActuator = e.HasFeature(ControlClient.NamespaceControl);
					this.thing.IsConcentrator = e.HasFeature(ConcentratorServer.NamespaceConcentrator);

					if (this.InContacts && !string.IsNullOrEmpty(this.thing.ObjectId))
						await Database.Update(this.thing);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.IsSensor = this.thing.IsSensor ?? false;
						this.IsActuator = this.thing.IsActuator ?? false;
						this.IsConcentrator = this.thing.IsConcentrator ?? false;
						this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;

						this.EvaluateAllCommands();
					});
				}
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.OnPresence -= this.Xmpp_OnPresence;
			this.XmppService.OnRosterItemAdded -= this.Xmpp_OnRosterItemAdded;
			this.XmppService.OnRosterItemUpdated -= this.Xmpp_OnRosterItemUpdated;
			this.XmppService.OnRosterItemRemoved -= this.Xmpp_OnRosterItemRemoved;
			this.TagProfile.Changed -= this.TagProfile_Changed;
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			await base.OnDispose();
		}

		private async Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			switch (e.Type)
			{
				case PresenceType.Available:
					this.presences[e.FromBareJID] = e;

					if (!this.InContacts && string.Compare(e.FromBareJID, this.thing.BareJid, true) == 0)
					{
						if (string.IsNullOrEmpty(this.thing.ObjectId))
							await Database.Insert(this.thing);

						this.InContacts = true;
					}

					break;

				case PresenceType.Unavailable:
					this.presences.Remove(e.FromBareJID);
					break;
			}

			this.UiSerializer.BeginInvokeOnMainThread(async () => await this.CalcThingIsOnline());
		}

		private async Task CalcThingIsOnline()
		{
			if (this.thing is null)
				this.IsThingOnline = false;
			else
			{
				this.IsThingOnline = this.IsOnline(this.thing.BareJid);

				if (this.IsThingOnline)
					await this.CheckCapabilities();
			}

			this.EvaluateAllCommands();
		}

		private bool IsOnline(string BareJid)
		{
			if (this.presences.TryGetValue(BareJid, out PresenceEventArgs e))
				return e.IsOnline;

			RosterItem Item = this.XmppService?.GetRosterItem(BareJid);
			if (Item is not null && Item.HasLastPresence)
				return Item.LastPresence.IsOnline;

			return false;
		}

		private string GetFullJid()
		{
			if (this.thing is null)
				return null;
			else
			{
				if (this.presences.TryGetValue(this.thing.BareJid, out PresenceEventArgs e))
					return e.IsOnline ? e.From : null;

				RosterItem Item = this.XmppService.GetRosterItem(this.thing.BareJid);

				if (Item is null || !Item.HasLastPresence || !Item.LastPresence.IsOnline)
					return null;
				else
					return Item.LastPresenceFullJid;
			}
		}

		private Task AssignProperties()
		{
			return this.CalcThingIsOnline();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClickCommand, this.AddToListCommand, this.RemoveFromListCommand, this.DeleteRulesCommand,
				this.DisownThingCommand, this.ReadSensorCommand, this.ControlActuatorCommand, this.ChatCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () => await this.AssignProperties());
		}

		#region Properties

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// Holds a list of notifications.
		/// </summary>
		public ObservableCollection<EventModel> Notifications { get; }

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public System.Windows.Input.ICommand ClickCommand { get; }

		/// <summary>
		/// The command to bind to for adding a thing to the list
		/// </summary>
		public System.Windows.Input.ICommand AddToListCommand { get; }

		/// <summary>
		/// The command to bind to for removing a thing from the list
		/// </summary>
		public System.Windows.Input.ICommand RemoveFromListCommand { get; }

		/// <summary>
		/// The command to bind to for clearing rules for the thing.
		/// </summary>
		public System.Windows.Input.ICommand DeleteRulesCommand { get; }

		/// <summary>
		/// The command to bind to for disowning a thing
		/// </summary>
		public System.Windows.Input.ICommand DisownThingCommand { get; }

		/// <summary>
		/// The command to bind to for reading a sensor
		/// </summary>
		public System.Windows.Input.ICommand ReadSensorCommand { get; }

		/// <summary>
		/// The command to bind to for controlling an actuator
		/// </summary>
		public System.Windows.Input.ICommand ControlActuatorCommand { get; }

		/// <summary>
		/// The command to bind to for chatting with a thing
		/// </summary>
		public System.Windows.Input.ICommand ChatCommand { get; }

		/// <summary>
		/// See <see cref="InContacts"/>
		/// </summary>
		public static readonly BindableProperty InContactsProperty =
			BindableProperty.Create(nameof(InContacts), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact list.
		/// </summary>
		public bool InContacts
		{
			get => (bool)this.GetValue(InContactsProperty);
			set
			{
				this.SetValue(InContactsProperty, value);
				this.InContactsAndNotOwner = this.InContacts && !this.IsOwner;
			}
		}

		/// <summary>
		/// See <see cref="IsOwner"/>
		/// </summary>
		public static readonly BindableProperty IsOwnerProperty =
			BindableProperty.Create(nameof(IsOwner), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsOwner
		{
			get => (bool)this.GetValue(IsOwnerProperty);
			set
			{
				this.SetValue(IsOwnerProperty, value);
				this.InContactsAndNotOwner = this.InContacts && !this.IsOwner;
			}
		}

		/// <summary>
		/// See <see cref="InContactsAndNotOwner"/>
		/// </summary>
		public static readonly BindableProperty InContactsAndNotOwnerProperty =
			BindableProperty.Create(nameof(InContactsAndNotOwner), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact list.
		/// </summary>
		public bool InContactsAndNotOwner
		{
			get => (bool)this.GetValue(InContactsAndNotOwnerProperty);
			set => this.SetValue(InContactsAndNotOwnerProperty, value);
		}

		/// <summary>
		/// See <see cref="IsThingOnline"/>
		/// </summary>
		public static readonly BindableProperty IsThingOnlineProperty =
			BindableProperty.Create(nameof(IsThingOnline), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsThingOnline
		{
			get => (bool)this.GetValue(IsThingOnlineProperty);
			set => this.SetValue(IsThingOnlineProperty, value);
		}

		/// <summary>
		/// See <see cref="IsSensor"/>
		/// </summary>
		public static readonly BindableProperty IsSensorProperty =
			BindableProperty.Create(nameof(IsSensor), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool IsSensor
		{
			get => (bool)this.GetValue(IsSensorProperty);
			set => this.SetValue(IsSensorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsActuator"/>
		/// </summary>
		public static readonly BindableProperty IsActuatorProperty =
			BindableProperty.Create(nameof(IsActuator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is an actuator
		/// </summary>
		public bool IsActuator
		{
			get => (bool)this.GetValue(IsActuatorProperty);
			set => this.SetValue(IsActuatorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsConcentrator"/>
		/// </summary>
		public static readonly BindableProperty IsConcentratorProperty =
			BindableProperty.Create(nameof(IsConcentrator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a concentrator
		/// </summary>
		public bool IsConcentrator
		{
			get => (bool)this.GetValue(IsConcentratorProperty);
			set => this.SetValue(IsConcentratorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsNodeInConcentrator"/>
		/// </summary>
		public static readonly BindableProperty IsNodeInConcentratorProperty =
			BindableProperty.Create(nameof(IsNodeInConcentrator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a concentrator
		/// </summary>
		public bool IsNodeInConcentrator
		{
			get => (bool)this.GetValue(IsNodeInConcentratorProperty);
			set => this.SetValue(IsNodeInConcentratorProperty, value);
		}

		/// <summary>
		/// See <see cref="SupportsSensorEvents"/>
		/// </summary>
		public static readonly BindableProperty SupportsSensorEventsProperty =
			BindableProperty.Create(nameof(SupportsSensorEvents), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool SupportsSensorEvents
		{
			get => (bool)this.GetValue(SupportsSensorEventsProperty);
			set => this.SetValue(SupportsSensorEventsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasNotifications"/>
		/// </summary>
		public static readonly BindableProperty HasNotificationsProperty =
			BindableProperty.Create(nameof(HasNotifications), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool HasNotifications
		{
			get => (bool)this.GetValue(HasNotificationsProperty);
			set => this.SetValue(HasNotificationsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasPendingChatMessages"/>
		/// </summary>
		public static readonly BindableProperty HasPendingChatMessagesProperty =
			BindableProperty.Create(nameof(HasPendingChatMessages), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is in the contact.
		/// </summary>
		public bool HasPendingChatMessages
		{
			get => (bool)this.GetValue(HasPendingChatMessagesProperty);
			set => this.SetValue(HasPendingChatMessagesProperty, value);
		}

		/// <summary>
		/// See <see cref="NrPendingChatMessages"/>
		/// </summary>
		public static readonly BindableProperty NrPendingChatMessagesProperty =
			BindableProperty.Create(nameof(NrPendingChatMessages), typeof(int), typeof(ViewThingModel), default(int));

		/// <summary>
		/// Gets or sets whether the identity is in the contact.
		/// </summary>
		public int NrPendingChatMessages
		{
			get => (int)this.GetValue(NrPendingChatMessagesProperty);
			set => this.SetValue(NrPendingChatMessagesProperty, value);
		}

		#endregion

		private Task Clicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThing.ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this);
			else if (obj is string s)
				return ViewClaimThing.ViewClaimThingViewModel.LabelClicked(string.Empty, s, s, this);
			else
				return Task.CompletedTask;
		}

		private async Task DeleteRules()
		{
			try
			{
				if (!await this.UiSerializer.DisplayAlert(
					LocalizationResourceManager.Current["Question"], LocalizationResourceManager.Current["DeleteRulesQuestion"],
					LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["Cancel"]))
				{
					return;
				}

				if (!await App.VerifyPin())
					return;

				TaskCompletionSource<bool> Result = new();

				this.XmppService.DeleteDeviceRules(this.thing.RegistryJid, this.thing.BareJid, this.thing.NodeId,
					this.thing.SourceId, this.thing.Partition, (sender, e) =>
				{
					if (e.Ok)
						Result.TrySetResult(true);
					else if (e.StanzaError is not null)
						Result.TrySetException(e.StanzaError);
					else
						Result.TrySetResult(false);

					return Task.CompletedTask;
				}, null);

				if (!await Result.Task)
					return;

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["RulesDeleted"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task DisownThing()
		{
			try
			{
				if (!await this.UiSerializer.DisplayAlert(
					LocalizationResourceManager.Current["Question"], LocalizationResourceManager.Current["DisownThingQuestion"],
					LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["Cancel"]))
				{
					return;
				}

				if (!await App.VerifyPin())
					return;

				(bool Succeeded, bool Done) = await this.NetworkService.TryRequest(() =>
					this.XmppService.Disown(this.thing.RegistryJid, this.thing.BareJid, this.thing.SourceId, this.thing.Partition, this.thing.NodeId));

				if (!Succeeded)
					return;

				if (this.InContacts)
				{
					if (!string.IsNullOrEmpty(this.thing.ObjectId))
					{
						await Database.Delete(this.thing);
						await Database.Provider.Flush();

						this.thing.ObjectId = null;
					}

					this.thing.ObjectId = null;
					this.InContacts = false;
				}

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["ThingDisowned"]);
				await this.NavigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task AddToList()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				RosterItem Item = this.XmppService.GetRosterItem(this.thing.BareJid);
				if (Item is null || Item.State == SubscriptionState.None || Item.State == SubscriptionState.From)
				{
					string IdXml;

					if (this.TagProfile.LegalIdentity is null)
						IdXml = string.Empty;
					else
					{
						StringBuilder Xml = new();
						this.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						IdXml = Xml.ToString();
					}

					this.XmppService.RequestPresenceSubscription(this.thing.BareJid);
				}
				else
				{
					if (!this.InContacts)
					{
						if (string.IsNullOrEmpty(this.thing.ObjectId))
							await Database.Insert(this.thing);

						this.InContacts = true;
					}

					await this.CalcThingIsOnline();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task RemoveFromList()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				if (this.InContacts)
				{
					if (!string.IsNullOrEmpty(this.thing.ObjectId))
					{
						await Database.Delete(this.thing);
						this.thing.ObjectId = null;
					}

					this.XmppService.RequestPresenceUnsubscription(this.thing.BareJid);

					if (this.XmppService.GetRosterItem(this.thing.BareJid) is not null)
						this.XmppService.RemoveRosterItem(this.thing.BareJid);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.thing.ObjectId = null;
						this.thing.IsActuator = null;
						this.thing.IsSensor = null;
						this.thing.IsConcentrator = null;

						this.IsConcentrator = false;
						this.IsSensor = false;
						this.IsActuator = false;

						this.InContacts = false;

						this.EvaluateAllCommands();
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ReadSensor()
		{
			ViewThingNavigationArgs Args = new(this.thing, MyThingsViewModel.GetNotificationEvents(this, this.thing));

			await this.NavigationService.GoToAsync(nameof(ReadSensorPage), Args, BackMethod.Pop);
		}

		private async Task ControlActuator()
		{
			try
			{
				string SelectedLanguage = App.SelectedLanguage;

				if (string.IsNullOrEmpty(this.thing.NodeId) && string.IsNullOrEmpty(this.thing.SourceId) && string.IsNullOrEmpty(this.thing.Partition))
					this.XmppService.GetControlForm(this.GetFullJid(), SelectedLanguage, this.ControlFormCallback, null);
				else
				{
					ThingReference ThingRef = new(this.thing.NodeId, this.thing.SourceId, this.thing.Partition);
					this.XmppService.GetControlForm(this.GetFullJid(), SelectedLanguage, this.ControlFormCallback, null, ThingRef);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private Task ControlFormCallback(object Sender, DataFormEventArgs e)
		{
			if (e.Ok)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(XmppFormPage), new XmppFormNavigationArgs(e.Form));
				});
			}
			else
				this.UiSerializer.DisplayAlert(e.StanzaError ?? new Exception("Unable to get control form."));

			return Task.CompletedTask;
		}

		private async Task Chat()
		{
			try
			{
				string LegalId = this.thing?.LegalId;
				string FriendlyName = this.thing.FriendlyName;
				ChatNavigationArgs Args = new(LegalId, this.thing.BareJid, FriendlyName);

				await this.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, this.thing.BareJid);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private void NotificationService_OnNotificationsDeleted(object Sender, NotificationEventsArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				bool IsNode = this.IsNodeInConcentrator;
				string Key = this.thing.ThingNotificationCategoryKey;
				int NrChatMessagesRemoved = 0;

				foreach (NotificationEvent Event in e.Events)
				{
					switch (Event.Button)
					{
						case EventButton.Contacts:
							if (IsNode)
								continue;

							if (Event.Category != this.thing.BareJid)
								continue;
							break;

						case EventButton.Things:
							if (Event.Category != Key)
								continue;
							break;

						default:
							continue;
					}

					int i = 0;

					foreach (EventModel Model in this.Notifications)
					{
						if (Model.Event.ObjectId == Event.ObjectId)
						{
							this.Notifications.RemoveAt(i);

							if (Event.Button == EventButton.Contacts)
								NrChatMessagesRemoved++;

							break;
						}

						i++;
					}
				}

				this.NrPendingChatMessages -= NrChatMessagesRemoved;
				this.HasNotifications = this.Notifications.Count > 0;
				this.HasPendingChatMessages = this.NrPendingChatMessages > 0;
			});
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					switch (e.Event.Button)
					{
						case EventButton.Contacts:
							if (this.IsNodeInConcentrator)
								return;

							if (e.Event.Category != this.thing.BareJid)
								return;
							break;

						case EventButton.Things:
							if (e.Event.Category != this.thing.ThingNotificationCategoryKey)
								return;
							break;

						default:
							return;
					}

					this.Notifications.Add(new EventModel(e.Event.Received,
						await e.Event.GetCategoryIcon(this),
						await e.Event.GetDescription(this),
						e.Event, this));

					this.HasNotifications = true;

					if (e.Event.Button == EventButton.Contacts)
					{
						this.NrPendingChatMessages++;
						this.HasPendingChatMessages = true;
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

		private Task Xmpp_OnRosterItemRemoved(object Sender, RosterItem Item)
		{
			this.presences.Remove(Item.BareJid);
			this.UiSerializer.BeginInvokeOnMainThread(async () => await this.CalcThingIsOnline());
			return Task.CompletedTask;
		}

		private Task Xmpp_OnRosterItemUpdated(object Sender, RosterItem Item)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () => await this.CalcThingIsOnline());
			return Task.CompletedTask;
		}

		private Task Xmpp_OnRosterItemAdded(object Sender, RosterItem Item)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () => await this.CalcThingIsOnline());
			return Task.CompletedTask;
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link
		{
			get
			{
				StringBuilder sb = new();
				bool HasJid = false;
				bool HasSourceId = false;
				bool HasPartition = false;
				bool HasNodeId = false;
				bool HasRegistry = false;

				sb.Append("iotdisco:");

				if (this.thing.MetaData is not null)
				{
					foreach (Property P in this.thing.MetaData)
					{
						sb.Append(';');

						switch (P.Name.ToUpper())
						{
							case Constants.XmppProperties.Altitude:
							case Constants.XmppProperties.Latitude:
							case Constants.XmppProperties.Longitude:
							case Constants.XmppProperties.Version:
								sb.Append('#');
								break;

							case Constants.XmppProperties.Jid:
								HasJid = true;
								break;

							case Constants.XmppProperties.SourceId:
								HasSourceId = true;
								break;

							case Constants.XmppProperties.Partition:
								HasPartition = true;
								break;

							case Constants.XmppProperties.NodeId:
								HasNodeId = true;
								break;

							case Constants.XmppProperties.Registry:
								HasRegistry = true;
								break;
						}

						sb.Append(Uri.EscapeDataString(P.Name));
						sb.Append('=');
						sb.Append(Uri.EscapeDataString(P.Value));
					}
				}

				if (!HasJid)
				{
					sb.Append("JID=");
					sb.Append(Uri.EscapeDataString(this.thing.BareJid));
				}

				if (!HasSourceId && !string.IsNullOrEmpty(this.thing.SourceId))
				{
					sb.Append(";SID=");
					sb.Append(Uri.EscapeDataString(this.thing.SourceId));
				}

				if (!HasPartition && !string.IsNullOrEmpty(this.thing.Partition))
				{
					sb.Append(";PT=");
					sb.Append(Uri.EscapeDataString(this.thing.Partition));
				}

				if (!HasNodeId && !string.IsNullOrEmpty(this.thing.NodeId))
				{
					sb.Append(";NID=");
					sb.Append(Uri.EscapeDataString(this.thing.NodeId));
				}

				if (!HasRegistry && !string.IsNullOrEmpty(this.thing.RegistryJid))
				{
					sb.Append(";R=");
					sb.Append(Uri.EscapeDataString(this.thing.RegistryJid));
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => Task.FromResult(this.thing.FriendlyName);

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[] Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string MediaContentType => null;

		#endregion
	}
}
