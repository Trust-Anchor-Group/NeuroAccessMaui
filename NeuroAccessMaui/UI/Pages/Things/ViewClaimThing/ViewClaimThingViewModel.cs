using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Text.RegularExpressions;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;

namespace NeuroAccessMaui.UI.Pages.Things.ViewClaimThing
{
	/// <summary>
	/// The view model to bind to for when displaying thing claim information.
	/// </summary>
	public class ViewClaimThingViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ViewClaimThingViewModel"/> class.
		/// </summary>
		public ViewClaimThingViewModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
			this.ClaimThingCommand = new Command(async _ => await this.ClaimThing(), _ => this.CanClaimThing);
			this.Tags = new ObservableCollection<HumanReadableTag>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out ViewClaimThingNavigationArgs args))
			{
				this.Uri = args.Uri;

				if (this.XmppService.TryDecodeIoTDiscoClaimURI(args.Uri, out MetaDataTag[] Tags))
				{
					this.RegistryJid = null;

					foreach (MetaDataTag Tag in Tags)
					{
						this.Tags.Add(new HumanReadableTag(Tag));

						if (Tag.Name.ToUpper() == "R")
							this.RegistryJid = Tag.StringValue;
					}

					if (string.IsNullOrEmpty(this.RegistryJid))
						this.RegistryJid = this.XmppService.RegistryServiceJid;
				}
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;

			await base.OnDispose();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClaimThingCommand);
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
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		#region Properties

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create(nameof(Uri), typeof(string), typeof(ViewClaimThingViewModel), default(string));

		/// <summary>
		/// iotdisco URI to process
		/// </summary>
		public string Uri
		{
			get => (string)this.GetValue(UriProperty);
			set => this.SetValue(UriProperty, value);
		}

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand ClaimThingCommand { get; }

		/// <summary>
		/// See <see cref="CanClaimThing"/>
		/// </summary>
		public static readonly BindableProperty CanClaimThingProperty =
			BindableProperty.Create(nameof(CanClaimThing), typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanClaimThing
		{
			get { return this.IsConnected && this.XmppService.IsOnline; }
		}

		/// <summary>
		/// See <see cref="MakePublic"/>
		/// </summary>
		public static readonly BindableProperty MakePublicProperty =
			BindableProperty.Create(nameof(MakePublic), typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool MakePublic
		{
			get => (bool)this.GetValue(MakePublicProperty);
			set => this.SetValue(MakePublicProperty, value);
		}

		/// <summary>
		/// See <see cref="RegistryJid"/>
		/// </summary>
		public static readonly BindableProperty RegistryJidProperty =
			BindableProperty.Create(nameof(RegistryJid), typeof(string), typeof(ViewClaimThingViewModel), default(string));

		/// <summary>
		/// JID of registry the thing uses.
		/// </summary>
		public string RegistryJid
		{
			get => (string)this.GetValue(RegistryJidProperty);
			set => this.SetValue(RegistryJidProperty, value);
		}

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this);
			else if (obj is string s)
				return LabelClicked(string.Empty, s, s, this);
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Processes the click of a localized meta-data label.
		/// </summary>
		/// <param name="Name">Tag name</param>
		/// <param name="Value">Tag value</param>
		/// <param name="LocalizedValue">Localized tag value</param>
		/// <param name="Services">Service references</param>
		public static async Task LabelClicked(string Name, string Value, string LocalizedValue, IServiceReferences Services)
		{ 
			try
			{
				switch (Name)
				{
					case "MAN":
						if (System.Uri.TryCreate("https://" + Value, UriKind.Absolute, out Uri Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					case "PURL":
						if (System.Uri.TryCreate(Value, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					case "R":
						SRV SRV;

						try
						{
							SRV = await DnsResolver.LookupServiceEndpoint(Value, "xmpp-server", "tcp");
						}
						catch
						{
							break;
						}

						if (System.Uri.TryCreate("https://" + SRV.TargetHost, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					default:
						if ((Value.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) ||
							Value.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase)) &&
							System.Uri.TryCreate(Value, UriKind.Absolute, out Uri) &&
							await Launcher.TryOpenAsync(Uri))
						{
							return;
						}
						else
						{
							Match M = XmppClient.BareJidRegEx.Match(Value);

							if (M.Success && M.Index == 0 && M.Length == Value.Length)
							{
								ContactInfo Info = await ContactInfo.FindByBareJid(Value);
								if (Info is not null)
								{
									await Services.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(Info));
									return;
								}

								int i = Value.IndexOf('@');
								if (i > 0 && Guid.TryParse(Value[..i], out _))
								{
									if (Services.NavigationService.CurrentPage is not ViewIdentityPage)
									{
										Info = await ContactInfo.FindByLegalId(Value);
										if (Info?.LegalIdentity is not null)
										{
											await Services.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Info.LegalIdentity));
											return;
										}
									}
								}
								else
								{
									string FriendlyName = await ContactInfo.GetFriendlyName(Value, Services);
									await Services.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(string.Empty, Value, FriendlyName));
									return;
								}
							}
						}
						break;
				}

				await Clipboard.SetTextAsync(LocalizedValue);
				await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
			}
			catch (Exception ex)
			{
				Services.LogService.LogException(ex);
				await Services.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<HumanReadableTag> Tags)
		{
			return GetFriendlyName(ToProperties(Tags));
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<MetaDataTag> Tags)
		{
			return GetFriendlyName(ToProperties(Tags));
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<Property> Tags)
		{
			return ContactInfo.GetFriendlyName(Tags);
		}

		private async Task ClaimThing()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				(bool Succeeded, NodeResultEventArgs e) = await this.NetworkService.TryRequest(() => this.XmppService.ClaimThing(this.Uri, this.MakePublic));
				if (!Succeeded)
					return;

				if (e.Ok)
				{
					string FriendlyName = GetFriendlyName(this.Tags);
					RosterItem Item = this.XmppService.GetRosterItem(e.JID);
					if (Item is null)
						this.XmppService.AddRosterItem(new RosterItem(e.JID, FriendlyName));

					ContactInfo Info = await ContactInfo.FindByBareJid(e.JID, e.Node.SourceId, e.Node.Partition, e.Node.NodeId);
					if (Info is null)
					{
						Info = new ContactInfo()
						{
							BareJid = e.JID,
							LegalId = string.Empty,
							LegalIdentity = null,
							FriendlyName = FriendlyName,
							IsThing = true,
							Owner = true,
							SourceId = e.Node.SourceId,
							Partition = e.Node.Partition,
							NodeId = e.Node.NodeId,
							MetaData = ToProperties(this.Tags),
							RegistryJid = this.RegistryJid
						};

						await Database.Insert(Info);
					}
					else
					{
						Info.FriendlyName = FriendlyName;

						await Database.Update(Info);
					}

					await Database.Provider.Flush();
					await this.NavigationService.GoBackAsync();
				}
				else
				{
					string Msg = e.ErrorText;
					if (string.IsNullOrEmpty(Msg))
						Msg = LocalizationResourceManager.Current["UnableToClaimThing"];

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], Msg);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Converts an enumerable set of <see cref="HumanReadableTag"/> to an enumerable set of <see cref="Property"/>.
		/// </summary>
		/// <param name="Tags">Enumerable set of <see cref="HumanReadableTag"/></param>
		/// <returns>Enumerable set of <see cref="Property"/></returns>
		public static Property[] ToProperties(IEnumerable<HumanReadableTag> Tags)
		{
			List<Property> Result = new();

			foreach (HumanReadableTag Tag in Tags)
				Result.Add(new Property(Tag.Name, Tag.Value));

			return Result.ToArray();
		}

		/// <summary>
		/// Converts an enumerable set of <see cref="MetaDataTag"/> to an enumerable set of <see cref="Property"/>.
		/// </summary>
		/// <param name="Tags">Enumerable set of <see cref="MetaDataTag"/></param>
		/// <returns>Enumerable set of <see cref="Property"/></returns>
		public static Property[] ToProperties(IEnumerable<MetaDataTag> Tags)
		{
			List<Property> Result = new();

			foreach (MetaDataTag Tag in Tags)
				Result.Add(new Property(Tag.Name, Tag.StringValue));

			return Result.ToArray();
		}

	}
}
