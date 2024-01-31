using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdApp.Extensions;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Things.ViewThing;
using IdApp.Services;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Things.MyThings
{
	/// <summary>
	/// The view model to bind to when displaying the list of things.
	/// </summary>
	public class MyThingsViewModel : BaseViewModel
	{
		private readonly Dictionary<CaseInsensitiveString, List<ContactInfoModel>> byBareJid = new();
		private TaskCompletionSource<ContactInfoModel> selectedThing;

		/// <summary>
		/// Creates an instance of the <see cref="MyThingsViewModel"/> class.
		/// </summary>
		protected internal MyThingsViewModel()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out MyThingsNavigationArgs Args))
				this.selectedThing = Args.ThingToShare;
			else
				this.selectedThing = null;

			this.XmppService.OnPresence += this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			if (this.selectedThing is not null && this.selectedThing.Task.IsCompleted)
			{
				await this.NavigationService.GoBackAsync();
			}
			else
			{
				this.SelectedThing = null;

				await this.LoadThings();
			}
		}


		private async Task LoadThings()
		{
			SortedDictionary<string, ContactInfo> SortedByName = new();
			SortedDictionary<string, ContactInfo> SortedByAddress = new();
			string Name;
			string Suffix;
			string Key;
			int i;

			foreach (ContactInfo Info in await Database.Find<ContactInfo>(new FilterFieldEqualTo("IsThing", true)))
			{
				Name = Info.FriendlyName;
				if (SortedByName.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (SortedByName.ContainsKey(Name + Suffix));

					SortedByName[Name + Suffix] = Info;
				}
				else
					SortedByName[Name] = Info;

				Key = Info.BareJid + ", " + Info.SourceId + ", " + Info.Partition + ", " + Info.NodeId;
				SortedByAddress[Key] = Info;
			}

			SearchResultThing[] MyDevices = await this.XmppService.GetAllMyDevices();
			foreach (SearchResultThing Thing in MyDevices)
			{
				Property[] MetaData = ViewClaimThing.ViewClaimThingViewModel.ToProperties(Thing.Tags);

				Key = Thing.Jid + ", " + Thing.Node.SourceId + ", " + Thing.Node.Partition + ", " + Thing.Node.NodeId;
				if (SortedByAddress.TryGetValue(Key, out ContactInfo Info))
				{
					if (!Info.Owner.HasValue || !Info.Owner.Value || !AreSame(Info.MetaData, MetaData))
					{
						Info.Owner = true;
						Info.MetaData = MetaData;
						Info.FriendlyName = ViewClaimThing.ViewClaimThingViewModel.GetFriendlyName(MetaData);

						await Database.Update(Info);
					}

					continue;
				}

				Info = new ContactInfo()
				{
					BareJid = Thing.Jid,
					LegalId = string.Empty,
					LegalIdentity = null,
					FriendlyName = ViewClaimThing.ViewClaimThingViewModel.GetFriendlyName(Thing.Tags),
					IsThing = true,
					SourceId = Thing.Node.SourceId,
					Partition = Thing.Node.Partition,
					NodeId = Thing.Node.NodeId,
					Owner = true,
					MetaData = MetaData,
					RegistryJid = this.XmppService.RegistryServiceJid
				};

				foreach (MetaDataTag Tag in Thing.Tags)
				{
					if (Tag.Name.ToUpper() == "R")
						Info.RegistryJid = Tag.StringValue;
				}

				await Database.Insert(Info);

				Name = Info.FriendlyName;
				if (SortedByName.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (SortedByName.ContainsKey(Name + Suffix));

					SortedByName[Name + Suffix] = Info;
				}
				else
					SortedByName[Name] = Info;

				SortedByAddress[Key] = Info;
			}

			await Database.Provider.Flush();

			this.byBareJid.Clear();

			ObservableItemGroup<IUniqueItem> NewThings = new(nameof(this.Things), new());

			foreach (ContactInfo Info in SortedByName.Values)
			{
				NotificationEvent[] Events = GetNotificationEvents(this, Info);

				ContactInfoModel InfoModel = new(this, Info, Events);
				NewThings.Add(InfoModel);

				if (!this.byBareJid.TryGetValue(Info.BareJid, out List<ContactInfoModel> Contacts))
				{
					Contacts = new List<ContactInfoModel>();
					this.byBareJid[Info.BareJid] = Contacts;
				}

				Contacts.Add(InfoModel);
			}

			this.ShowThingsMissing = SortedByName.Count == 0;

			Device.BeginInvokeOnMainThread(() => ObservableItemGroup<IUniqueItem>.UpdateGroupsItems(this.Things, NewThings));
		}


		/// <summary>
		/// Gets available notification events related to a thing.
		/// </summary>
		/// <param name="References">Service references.</param>
		/// <param name="Thing">Thing reference</param>
		/// <returns>Array of events, null if none.</returns>
		public static NotificationEvent[] GetNotificationEvents(IServiceReferences References, ContactInfo Thing)
		{
			if (!string.IsNullOrEmpty(Thing.SourceId) ||
				!string.IsNullOrEmpty(Thing.Partition) ||
				!string.IsNullOrEmpty(Thing.NodeId) ||
				!References.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Thing.BareJid, out NotificationEvent[] ContactEvents))
			{
				ContactEvents = null;
			}

			if (!References.NotificationService.TryGetNotificationEvents(EventButton.Things, Thing.ThingNotificationCategoryKey, out NotificationEvent[] ThingEvents))
				ThingEvents = null;

			return ContactEvents.Join(ThingEvents);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.OnPresence -= this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			this.ShowThingsMissing = false;
			this.Things.Clear();

			this.selectedThing?.TrySetResult(null);

			await base.OnDispose();
		}

		/// <summary>
		/// Checks to sets of meta-data about a thing, to see if they match.
		/// </summary>
		/// <param name="MetaData1">First set of meta-data.</param>
		/// <param name="MetaData2">Second set of meta-data.</param>
		/// <returns>If they are the same.</returns>
		public static bool AreSame(Property[] MetaData1, Property[] MetaData2)
		{
			if ((MetaData1 is null) ^ (MetaData2 is null))
				return false;

			if (MetaData1 is null)
				return true;

			int i, c = MetaData1.Length;
			if (MetaData2.Length != c)
				return false;

			for (i = 0; i < c; i++)
			{
				if ((MetaData1[i].Name != MetaData2[i].Name) || (MetaData1[i].Value != MetaData2[i].Value))
					return false;
			}

			return true;
		}

		/// <summary>
		/// See <see cref="ShowThingsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowThingsMissingProperty =
			BindableProperty.Create(nameof(ShowThingsMissing), typeof(bool), typeof(MyThingsViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowThingsMissing
		{
			get => (bool)this.GetValue(ShowThingsMissingProperty);
			set => this.SetValue(ShowThingsMissingProperty, value);
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableItemGroup<IUniqueItem> Things { get; } = new(nameof(Things), new());

		/// <summary>
		/// See <see cref="SelectedThing"/>
		/// </summary>
		public static readonly BindableProperty SelectedThingProperty =
			BindableProperty.Create(nameof(SelectedThing), typeof(ContactInfoModel), typeof(MyThingsViewModel), default(ContactInfoModel));

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfoModel SelectedThing
		{
			get => (ContactInfoModel)this.GetValue(SelectedThingProperty);
			set
			{
				this.SetValue(SelectedThingProperty, value);

				if (value is not null)
					this.OnSelected(value);
			}
		}

		private void OnSelected(ContactInfoModel Thing)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if (this.selectedThing is null)
				{
					ViewThingNavigationArgs Args = new(Thing.Contact, Thing.Events);
					// Inherit the back method here from the parrent
					await this.NavigationService.GoToAsync(nameof(ViewThingPage), Args, BackMethod.Inherited);
				}
				else
				{
					this.selectedThing.TrySetResult(Thing);
					await this.NavigationService.GoBackAsync();
				}
			});
		}

		private Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			if (this.byBareJid.TryGetValue(e.FromBareJID, out List<ContactInfoModel> Contacts))
			{
				foreach (ContactInfoModel Contact in Contacts)
					Contact.PresenceUpdated();
			}

			return Task.CompletedTask;
		}

		private void NotificationService_OnNotificationsDeleted(object Sender, NotificationEventsArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				foreach (NotificationEvent Event in e.Events)
				{
					switch (Event.Button)
					{
						case EventButton.Contacts:
							foreach (IUniqueItem Item in this.Things)
							{
								if (Item is ContactInfoModel Thing)
								{

									if (!string.IsNullOrEmpty(Thing.NodeId) ||
										!string.IsNullOrEmpty(Thing.SourceId) ||
										!string.IsNullOrEmpty(Thing.Partition))
									{
										continue;
									}

									if (Event.Category != Thing.BareJid)
										continue;

									Thing.RemoveEvent(Event);
								}
							}
							break;

						case EventButton.Things:
							foreach (IUniqueItem Item in this.Things)
							{
								if (Item is ContactInfoModel Thing)
								{

									if (Event.Category != ContactInfo.GetThingNotificationCategoryKey(Thing.BareJid, Thing.NodeId, Thing.SourceId, Thing.Partition))
										continue;

									Thing.RemoveEvent(Event);
								}
							}
							break;

						default:
							return;
					}
				}
			});
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				try
				{
					switch (e.Event.Button)
					{
						case EventButton.Contacts:
							foreach (IUniqueItem Item in this.Things)
							{
								if (Item is ContactInfoModel Thing)
								{
									if (!string.IsNullOrEmpty(Thing.NodeId) ||
									!string.IsNullOrEmpty(Thing.SourceId) ||
									!string.IsNullOrEmpty(Thing.Partition))
									{
										continue;
									}

									if (e.Event.Category != Thing.BareJid)
										continue;

									Thing.AddEvent(e.Event);
								}
							}
							break;

						case EventButton.Things:
							foreach (IUniqueItem Item in this.Things)
							{
								if (Item is ContactInfoModel Thing)
								{
									if (e.Event.Category != ContactInfo.GetThingNotificationCategoryKey(Thing.BareJid, Thing.NodeId, Thing.SourceId, Thing.Partition))
										continue;

									Thing.AddEvent(e.Event);
								}
							}
							break;

						default:
							return;
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

	}
}
