using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events;
using NeuroAccessMaui.UI.Popups.Tokens.AddTextNote;
using NeuroFeatures.Events;
using System.Collections.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// The view model to bind to for when displaying the events of a token.
	/// </summary>
	public partial class TokenEventsViewModel : BaseViewModel
	{
		private readonly TokenEventsNavigationArgs? navigationArguments;

		/// <summary>
		/// Creates an instance of the <see cref="TokenEventsViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public TokenEventsViewModel(TokenEventsNavigationArgs? Args)
			: base()
		{
			this.navigationArguments = Args;
			this.TokenId = Args?.TokenId;
			this.IsMyToken = Args?.IsMyToken ?? false;

			this.Events = [];
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.navigationArguments is not null)
			{
				this.Events.Clear();

				if (this.navigationArguments.Events is not null)
				{
					foreach (TokenEvent Event in this.navigationArguments.Events)
					{
						EventItem Item = EventItem.Create(Event);
						await Item.DoBind();
						this.Events.Add(Item);
					}
				}
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.Events.Clear();

			await base.OnDisposeAsync();
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

		/// <summary>
		/// If the token belongs to the user, who can then add notes.
		/// </summary>
		[ObservableProperty]
		private bool isMyToken;

		#endregion

		#region Commands

		/// <summary>
		/// Command to add a text note to the token. Notes are always sent as text, as written.
		/// </summary>
		[RelayCommand]
		private async Task AddNote()
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			try
			{
				AddTextNoteViewModel AddTextNoteViewModel = new((Note, Personal) =>
					ServiceRef.XmppService.AddNeuroFeatureTextNote(TokenId, Note, Personal));
				AddTextNotePopup AddTextNotePopup = new(AddTextNoteViewModel);

				await ServiceRef.PopupService.PushAsync(AddTextNotePopup);
				bool? Result = await AddTextNoteViewModel.Result;

				if (Result.HasValue && Result.Value)
				{
					NoteTextItem NewEvent = new(new NoteText()
					{
						Note = AddTextNoteViewModel.TextNote,
						Personal = AddTextNoteViewModel.Personal,
						TokenId = TokenId,
						Timestamp = DateTime.Now
					});

					await NewEvent.DoBind();

					this.Events.Insert(0, NewEvent);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		#endregion

	}
}
