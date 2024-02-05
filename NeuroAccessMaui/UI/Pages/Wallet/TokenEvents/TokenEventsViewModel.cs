using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events;
using NeuroFeatures.Events;
using System.Collections.ObjectModel;
using Waher.Content.Xml;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// The view model to bind to for when displaying the events of a token.
	/// </summary>
	public partial class TokenEventsViewModel : BaseViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="TokenEventsViewModel"/> class.
		/// </summary>
		public TokenEventsViewModel()
			: base()
		{
			this.Events = [];
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out TokenEventsNavigationArgs? args))
			{
				this.TokenId = args.TokenId;

				this.Events.Clear();

				foreach (TokenEvent Event in args.Events)
				{
					EventItem Item = EventItem.Create(Event);
					await Item.DoBind();
					this.Events.Add(Item);
				}
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.Events.Clear();

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Events
		/// </summary>
		public ObservableCollection<EventItem> Events { get; }

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? tokenId;

		#endregion

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		[RelayCommand]
		private async Task AddNote()
		{
			try
			{
				AddTextNotePage AddTextNotePage = new();

				await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(AddTextNotePage);
				bool? Result = await AddTextNotePage.Result;

				if (Result.HasValue && Result.Value)
				{
					NoteItem NewEvent;

					if (XML.IsValidXml(AddTextNotePage.TextNote))
					{
						await ServiceRef.XmppService.AddNeuroFeatureXmlNote(this.TokenId, AddTextNotePage.TextNote, AddTextNotePage.Personal);

						NewEvent = new NoteXmlItem(new NoteXml()
						{
							Note = AddTextNotePage.TextNote,
							Personal = AddTextNotePage.Personal,
							TokenId = this.TokenId,
							Timestamp = DateTime.Now
						});
					}
					else
					{
						await ServiceRef.XmppService.AddNeuroFeatureTextNote(this.TokenId, AddTextNotePage.TextNote, AddTextNotePage.Personal);

						NewEvent = new NoteTextItem(new NoteText()
						{
							Note = AddTextNotePage.TextNote,
							Personal = AddTextNotePage.Personal,
							TokenId = this.TokenId,
							Timestamp = DateTime.Now
						});
					}

					await NewEvent.DoBind();

					this.Events.Insert(0, NewEvent);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		#endregion

	}
}
