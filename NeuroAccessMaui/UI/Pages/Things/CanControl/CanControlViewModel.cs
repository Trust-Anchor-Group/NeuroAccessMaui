using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Things;
using NeuroAccessMaui.UI.Pages.Things.CanRead;
using NeuroAccessMaui.UI.Pages.Things.IsFriend;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Things;

namespace NeuroAccessMaui.UI.Pages.Things.CanControl
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public partial class CanControlViewModel : XmppViewModel
	{
		private NotificationEvent? @event;

		/// <summary>
		/// Creates an instance of the <see cref="CanControlViewModel"/> class.
		/// </summary>
		protected internal CanControlViewModel()
			: base()
		{
			this.Tags = [];
			this.CallerTags = [];
			this.Parameters = [];
			this.RuleRanges = [];
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out CanControlNavigationArgs? args))
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

				if (args.UserTokens is not null && args.UserTokens.Length > 0)
				{
					foreach (ProvisioningToken Token in args.UserTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(ServiceRef.Localizer[nameof(AppResources.User)], Token.FriendlyName ?? Token.Token)));

					this.HasUser = true;
				}

				if (args.ServiceTokens is not null && args.ServiceTokens.Length > 0)
				{
					foreach (ProvisioningToken Token in args.ServiceTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(ServiceRef.Localizer[nameof(AppResources.Service)], Token.FriendlyName ?? Token.Token)));

					this.HasService = true;
				}

				if (args.DeviceTokens is not null && args.DeviceTokens.Length > 0)
				{
					foreach (ProvisioningToken Token in args.DeviceTokens)
						this.CallerTags.Add(new HumanReadableTag(new Property(ServiceRef.Localizer[nameof(AppResources.Device)], Token.FriendlyName ?? Token.Token)));

					this.HasDevice = true;
				}

				this.Parameters.Clear();

				SortedDictionary<string, bool> PermittedParameters = [];
				string[]? AllParameters = args.AllParameters;

				if (AllParameters is null && !string.IsNullOrEmpty(this.BareJid))
				{
					AllParameters = await CanControlNotificationEvent.GetAvailableParameterNames(this.BareJid,
						new ThingReference(this.NodeId, this.SourceId, this.PartitionId));

					if (AllParameters is not null && args?.Event is not null)
					{
						args.Event.AllParameters = AllParameters;
						await Database.Update(args.Event);
					}
				}

				bool AllParametersPermitted = args?.Parameters is null;

				if (AllParameters is not null)
				{
					foreach (string Parameter in AllParameters)
						PermittedParameters[Parameter] = AllParametersPermitted;
				}

				if (!AllParametersPermitted && args?.Parameters is not null)
				{
					foreach (string Parameter in args.Parameters)
						PermittedParameters[Parameter] = true;
				}

				foreach (KeyValuePair<string, bool> P in PermittedParameters)
				{
					FieldReference Parameter = new(P.Key, P.Value);
					this.Parameters.Add(Parameter);
				}

				this.RuleRanges.Clear();
				this.RuleRanges.Add(new RuleRangeModel(RuleRange.Caller, ServiceRef.Localizer[nameof(AppResources.CallerOnly)]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("User", ServiceRef.Localizer[nameof(AppResources.ToUser)]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("Service", ServiceRef.Localizer[nameof(AppResources.ToService)]));

				if (this.HasUser)
					this.RuleRanges.Add(new RuleRangeModel("Device", ServiceRef.Localizer[nameof(AppResources.ToDevice)]));

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
		/// Holds a list of parameters that will be permitted.
		/// </summary>
		public ObservableCollection<FieldReference> Parameters { get; }

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
		/// If request has user information
		/// </summary>
		[ObservableProperty]
		private bool hasUser;

		/// <summary>
		/// If request has user information
		/// </summary>
		[ObservableProperty]
		private bool hasService;

		/// <summary>
		/// If request has user information
		/// </summary>
		[ObservableProperty]
		private bool hasDevice;

		/// <summary>
		/// Provisioning service.
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

		/// <summary>
		/// Node ID
		/// </summary>
		[ObservableProperty]
		private string? nodeId;

		/// <summary>
		/// Source ID
		/// </summary>
		[ObservableProperty]
		private string? sourceId;

		/// <summary>
		/// Partition ID
		/// </summary>
		[ObservableProperty]
		private string? partitionId;

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
		/// Adds the device to the contact list.
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
			if (this.CallerInContactsList && !string.IsNullOrEmpty(this.RemoteJid))
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.RemoteJid);
				if (Info is not null)
					await Database.Delete(Info);

				this.CallerInContactsList = false;
			}
		}

		/// <summary>
		/// The command to bind to for accepting the request
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteAccept))]
		private void Accept()
		{
			this.Respond(true);
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					this.AcceptCommand.NotifyCanExecuteChanged();
					break;
			}
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

		/// <summary>
		/// The command to bind to for rejecting the request
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
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
					ControlRequestResolver Resolver = new(this.BareJid!, this.RemoteFriendlyName ?? string.Empty, RuleRange);

					switch (RuleRange)
					{
						case RuleRange.Caller:
						default:
							ServiceRef.XmppService.CanControlResponseCaller(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

						case RuleRange.Domain:
							ServiceRef.XmppService.CanControlResponseDomain(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

						case RuleRange.All:
							ServiceRef.XmppService.CanControlResponseAll(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Thing, this.ResponseHandler, Resolver);
							break;

					}
				}
				else if (Range.RuleRange is ProvisioningToken Token)
				{
					ControlRequestResolver Resolver = new(this.BareJid!, this.RemoteFriendlyName ?? string.Empty, Token);

					switch (Token.Type)
					{
						case TokenType.User:
							ServiceRef.XmppService.CanControlResponseUser(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;

						case TokenType.Service:
							ServiceRef.XmppService.CanControlResponseService(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;

						case TokenType.Device:
							ServiceRef.XmppService.CanControlResponseDevice(this.ProvisioningService ?? ServiceRef.TagProfile.ProvisioningJid ?? string.Empty,
								this.BareJid!, this.RemoteJid!, this.Key!, Accepts, this.GetParameters(), Token.Token, Thing, this.ResponseHandler, Resolver);
							break;
					}
				}
			}
		}

		private string[]? GetParameters()
		{
			List<string> Result = [];
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
				return [.. Result];
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
					await this.GoBack();
				});
			}
			else
			{
				MainThread.BeginInvokeOnMainThread(async () => await ServiceRef.UiService.DisplayException(e.StanzaError ??
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

			await this.GoBack();
		}

		/// <summary>
		/// The command to bind to for selecting all control parameters
		/// </summary>
		[RelayCommand]
		private void AllParameters()
		{
			foreach (FieldReference Parameter in this.Parameters)
				Parameter.Permitted = true;
		}

		/// <summary>
		/// The command to bind to for selecting no control parameters
		/// </summary>
		[RelayCommand]
		private void NoParameters()
		{
			foreach (FieldReference Parameter in this.Parameters)
				Parameter.Permitted = false;
		}

	}
}
