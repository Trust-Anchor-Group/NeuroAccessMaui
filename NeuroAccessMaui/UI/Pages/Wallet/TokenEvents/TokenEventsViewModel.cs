using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events;
using NeuroAccessMaui.UI.Popups.Tokens.AddTextNote;
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

				if (args.Events is not null)
				{
					foreach (TokenEvent Event in args.Events)
					{
						EventItem Item = EventItem.Create(Event);
						await Item.DoBind();
						this.Events.Add(Item);
					}
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
				AddTextNoteViewModel AddTextNoteViewModel = new();
				AddTextNotePopup AddTextNotePopup = new(AddTextNoteViewModel);

				await MopupService.Instance.PushAsync(AddTextNotePopup);
				bool? Result = await AddTextNoteViewModel.Result;

				if (Result.HasValue && Result.Value)
				{
					NoteItem NewEvent;

					if (XML.IsValidXml(AddTextNoteViewModel.TextNote))
					{
						await ServiceRef.XmppService.AddNeuroFeatureXmlNote(this.TokenId!, AddTextNoteViewModel.TextNote!, AddTextNoteViewModel.Personal);

						NewEvent = new NoteXmlItem(new NoteXml()
						{
							Note = AddTextNoteViewModel.TextNote,
							Personal = AddTextNoteViewModel.Personal,
							TokenId = this.TokenId,
							Timestamp = DateTime.Now
						});
					}
					else
					{
						await ServiceRef.XmppService.AddNeuroFeatureTextNote(this.TokenId!, AddTextNoteViewModel.TextNote!, AddTextNoteViewModel.Personal);

						NewEvent = new NoteTextItem(new NoteText()
						{
							Note = AddTextNoteViewModel.TextNote,
							Personal = AddTextNoteViewModel.Personal,
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
