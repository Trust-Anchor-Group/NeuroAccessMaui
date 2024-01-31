using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Things.CanRead;
using NeuroAccessMaui.UI.Pages.Things.IsFriend;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using IdApp.Services;
using IdApp.Services.Notification;
using IdApp.Services.Notification.Things;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Things;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Things.CanControl
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class CanControlModel : XmppViewModel
	{
		private NotificationEvent @event;

		/// <summary>
		/// Creates an instance of the <see cref="CanControlModel"/> class.
		/// </summary>
		protected internal CanControlModel()
			: base()
		{
			this.ClickCommand = new Command(async P => await this.LabelClicked(P));
			this.AddContactCommand = new Command(async _ => await this.AddContact());
			this.RemoveContactCommand = new Command(async _ => await this.RemoveContact());
			this.AcceptCommand = new Command(_ => this.Accept(), _ => this.CanExecuteAccept());
			this.RejectCommand = new Command(_ => this.Reject(), _ => this.IsConnected);
			this.IgnoreCommand = new Command(async _ => await this.Ignore());
			this.AllParametersCommand = new Command(_ => this.AllParameters());
			this.NoParametersCommand = new Command(_ => this.NoParameters());

			this.Tags = new ObservableCollection<HumanReadableTag>();
			this.CallerTags = new ObservableCollection<HumanReadableTag>();
			this.Parameters = new ObservableCollection<FieldReference>();
			this.RuleRanges = new ObservableCollection<RuleRangeModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out CanControlNavigationArgs args))
			{
				this.@event = args.Event;
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
				this.RemoteJid = args.RemoteJid;
				this.RemoteFriendlyName = args.RemoteFriendlyName;
				this.Key = args.Key;
				this.ProvisioningService = args.ProvisioningService;
				this.NodeId = args.NodeId;
				this.SourceId = args.SourceId;
				this.PartitionId = args.PartitionId;

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

				if ((args.UserTokens?.Length ?? 0) > 0)
				{
					foreach (ProvisioningToken Token in args.UserTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(LocalizationResourceManager.Current["User"], Token.FriendlyName ?? Token.Token)));

					this.HasUser = true;
				}

				if ((args.ServiceTokens?.Length ?? 0) > 0)
				{
					foreach (ProvisioningToken Token in args.ServiceTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(LocalizationResourceManager.Current["Service"], Token.FriendlyName ?? Token.Token)));

					this.HasService = true;
				}

				if ((args.DeviceTokens?.Length ?? 0) > 0)
				{
					foreach (ProvisioningToken Token in args.DeviceTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(LocalizationResourceManager.Current["Device"], Token.FriendlyName ?? Token.Token)));

					this.HasDevice = true;
				}

				this.Parameters.Clear();

				SortedDictionary<string, bool> PermittedParameters = new();
				string[] AllParameters = args.AllParameters;

				if (AllParameters is null)
				{
					AllParameters = await CanControlNotificationEvent.GetAvailableParameterNames(this.BareJid, new ThingReference(this.NodeId, this.SourceId, this.PartitionId), this);

					if (AllParameters is not null)
					{
						args.Event.AllParameters = AllParameters;
						await Database.Update(args.Event);
					}
				}

				bool AllParametersPermitted = args.Parameters is null;

				if (AllParameters is not null)
				{
					foreach (string Parameter in AllParameters)
						PermittedParameters[Parameter] = AllParametersPermitted;
				}

				if (!AllParametersPermitted)
				{
					foreach (string Parameter in args.Parameters)
						PermittedParameters[Parameter] = true;
				}

				foreach (KeyValuePair<string, bool> P in PermittedParameters)
				{
					FieldReference Parameter = new(this, P.Key, P.Value);
					this.Parameters.Add(Parameter);

					Parameter.PropertyChanged += this.Parameter_PropertyChanged;
				}

				this.RuleRanges.Clear();
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Caller, LocalizationResourceManager.Current["CallerOnly"]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("User", LocalizationResourceManager.Current["ToUser"]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("Service", LocalizationResourceManager.Current["ToService"]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("Device", LocalizationResourceManager.Current["ToDevice"]));

				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Domain, string.Format(LocalizationResourceManager.Current["EntireDomain"], XmppClient.GetDomain(this.RemoteJid))));
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.All, LocalizationResourceManager.Current["Everyone"]));

				this.SelectedRuleRangeIndex = 0;
			}

			this.EvaluateAllCommands();
		}

		private void Parameter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			this.EvaluateAllCommands();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClickCommand, this.AddContactCommand, this.RemoveContactCommand, this.AcceptCommand, this.RejectCommand,
				this.IgnoreCommand, this.AllParametersCommand, this.NoParametersCommand);
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
		/// Holds a list of parameters that will be permitted.
		/// </summary>
		public ObservableCollection<FieldReference> Parameters { get; }

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
		/// The command to bind to for selecting all fields
		/// </summary>
		public System.Windows.Input.ICommand AllParametersCommand { get; }

		/// <summary>
		/// The command to bind to for selecting no fields
		/// </summary>
		public System.Windows.Input.ICommand NoParametersCommand { get; }

		/// <summary>
		/// See <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(CanControlModel), default(string));

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
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(CanControlModel), default(string));

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
			BindableProperty.Create(nameof(RemoteJid), typeof(string), typeof(CanControlModel), default(string));

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
			BindableProperty.Create(nameof(RemoteFriendlyName), typeof(string), typeof(CanControlModel), default(string));

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
			BindableProperty.Create(nameof(RemoteFriendlyNameAvailable), typeof(bool), typeof(CanControlModel), default(bool));

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
			BindableProperty.Create(nameof(Key), typeof(string), typeof(CanControlModel), default(string));

		/// <summary>
		/// Provisioning key.
		/// </summary>
		public string Key
		{
			get => (string)this.GetValue(KeyProperty);
			set => this.SetValue(KeyProperty, value);
		}

		/// <summary>
		/// See <see cref="HasUser"/>
		/// </summary>
		public static readonly BindableProperty HasUserProperty =
			BindableProperty.Create(nameof(HasUser), typeof(bool), typeof(CanControlModel), default(bool));

		/// <summary>
		/// If request has user information
		/// </summary>
		public bool HasUser
		{
			get => (bool)this.GetValue(HasUserProperty);
			set => this.SetValue(HasUserProperty, value);
		}

		/// <summary>
		/// See <see cref="HasService"/>
		/// </summary>
		public static readonly BindableProperty HasServiceProperty =
			BindableProperty.Create(nameof(HasService), typeof(bool), typeof(CanControlModel), default(bool));

		/// <summary>
		/// If request has user information
		/// </summary>
		public bool HasService
		{
			get => (bool)this.GetValue(HasServiceProperty);
			set => this.SetValue(HasServiceProperty, value);
		}

		/// <summary>
		/// See <see cref="HasDevice"/>
		/// </summary>
		public static readonly BindableProperty HasDeviceProperty =
			BindableProperty.Create(nameof(HasDevice), typeof(bool), typeof(CanControlModel), default(bool));

		/// <summary>
		/// If request has user information
		/// </summary>
		public bool HasDevice
		{
			get => (bool)this.GetValue(HasDeviceProperty);
			set => this.SetValue(HasDeviceProperty, value);
		}

		/// <summary>
		/// See <see cref="ProvisioningService"/>
		/// </summary>
		public static readonly BindableProperty ProvisioningServiceProperty =
			BindableProperty.Create(nameof(ProvisioningService), typeof(string), typeof(CanControlModel), default(string));

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
			BindableProperty.Create(nameof(CallerInContactsList), typeof(bool), typeof(CanControlModel), default(bool));

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
			BindableProperty.Create(nameof(SelectedRuleRangeIndex), typeof(int), typeof(CanControlModel), -1);

		/// <summary>
		/// The selected rule range index
		/// </summary>
		public int SelectedRuleRangeIndex
		{
			get => (int)this.GetValue(SelectedRuleRangeIndexProperty);
			set => this.SetValue(SelectedRuleRangeIndexProperty, value);
		}

		/// <summary>
		/// See <see cref="NodeId"/>
		/// </summary>
		public static readonly BindableProperty NodeIdProperty =
			BindableProperty.Create(nameof(NodeId), typeof(string), typeof(CanControlModel), default(string));

		/// <summary>
		/// Node ID
		/// </summary>
		public string NodeId
		{
			get => (string)this.GetValue(NodeIdProperty);
			set => this.SetValue(NodeIdProperty, value);
		}

		/// <summary>
		/// See <see cref="SourceId"/>
		/// </summary>
		public static readonly BindableProperty SourceIdProperty =
			BindableProperty.Create(nameof(SourceId), typeof(string), typeof(CanControlModel), default(string));

		/// <summary>
		/// Source ID
		/// </summary>
		public string SourceId
		{
			get => (string)this.GetValue(SourceIdProperty);
			set => this.SetValue(SourceIdProperty, value);
		}

		/// <summary>
		/// See <see cref="PartitionId"/>
		/// </summary>
		public static readonly BindableProperty PartitionIdProperty =
			BindableProperty.Create(nameof(PartitionId), typeof(string), typeof(CanControlModel), default(string));

		/// <summary>
		/// Partition ID
		/// </summary>
		public string PartitionId
		{
			get => (string)this.GetValue(PartitionIdProperty);
			set => this.SetValue(PartitionIdProperty, value);
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

		private void Accept()
		{
			this.Respond(true);
		}

		private bool CanExecuteAccept()
		{
			if (!this.IsConnected)
				return false;

			foreach (FieldReference Parameter in this.Parameters)
			{
				if (Parameter.Permitted)
					return true;
			}

			return false;
		}

		private void Reject()
		{
			this.Respond(false);
		}

		private void Respond(bool Accepts)
		{
			if (this.SelectedRuleRangeIndex >= 0)
			{
				RuleRangeModel Range = this.RuleRanges[this.SelectedRuleRangeIndex];
				ThingReference Thing = new(this.NodeId, this.SourceId, this.PartitionId);

				if (Range.RuleRange is RuleRange RuleRange)
				{
					ControlRequestResolver Resolver = new(this.BareJid, this.RemoteFriendlyName, RuleRange);

					switch (RuleRange)
					{
						case RuleRange.Caller:
						default:
							this.XmppService.CanControlResponseCaller(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

						case RuleRange.Domain:
							this.XmppService.CanControlResponseDomain(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

						case RuleRange.All:
							this.XmppService.CanControlResponseAll(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

					}
				}
				else if (Range.RuleRange is ProvisioningToken Token)
				{
					ControlRequestResolver Resolver = new(this.BareJid, this.RemoteFriendlyName, Token);

					switch (Token.Type)
					{
						case TokenType.User:
							this.XmppService.CanControlResponseUser(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;

						case TokenType.Service:
							this.XmppService.CanControlResponseService(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;

						case TokenType.Device:
							this.XmppService.CanControlResponseDevice(this.ProvisioningService, this.BareJid, this.RemoteJid, this.Key,
								Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;
					}
				}
			}
		}

		private string[] GetParameters()
		{
			List<string> Result = new();
			bool AllPermitted = true;

			foreach (FieldReference Parameter in this.Parameters)
			{
				if (Parameter.Permitted)
					Result.Add(Parameter.Name);
				else
					AllPermitted = false;
			}

			if (AllPermitted)
				return null;
			else
				return Result.ToArray();
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

		private void AllParameters()
		{
			foreach (FieldReference Parameter in this.Parameters)
				Parameter.Permitted = true;
		}

		private void NoParameters()
		{
			foreach (FieldReference Parameter in this.Parameters)
				Parameter.Permitted = false;
		}

	}
}
