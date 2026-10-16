using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// View model for popup page prompting the user for a text note to be added.
	/// </summary>
	public partial class AddTextNoteViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool?> result = new();
		private readonly Func<string, bool, Task> submit;

		/// <summary>
		/// View model for popup page prompting the user for a text note to be added.
		/// </summary>
		/// <param name="Submit">Sends the note, given its text and if it is personal. The popup closes only when this succeeds.</param>
		public AddTextNoteViewModel(Func<string, bool, Task> Submit)
			: base()
		{
			this.submit = Submit;
		}

		/// <summary>
		/// Result will be provided here: true when the note was sent. If dialog is cancelled, false or null is returned.
		/// </summary>
		public Task<bool?> Result => this.result.Task;

		/// <summary>
		/// Text Note
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(AddNoteCommand))]
		private string? textNote;

		/// <summary>
		/// If note is personal or not.
		/// </summary>
		[ObservableProperty]
		private bool personal;

		/// <summary>
		/// If the note is being sent.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(AddNoteCommand))]
		private bool isSending;

		/// <summary>
		/// If the note can be added.
		/// </summary>
		public bool CanAddNote => !string.IsNullOrWhiteSpace(this.TextNote) && !this.IsSending;

		/// <summary>
		/// Sends the note. When sending fails, the reason is shown and the popup stays open with the text kept.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanAddNote))]
		private async Task AddNote()
		{
			string? Note = this.TextNote;
			if (string.IsNullOrWhiteSpace(Note))
				return;

			this.IsSending = true;

			try
			{
				await this.submit(Note, this.Personal);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await TokenErrors.ShowAsync(Ex);
				return;
			}
			finally
			{
				this.IsSending = false;
			}

			this.result.TrySetResult(true);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Does not add note.
		/// </summary>
		[RelayCommand]
		private async Task Cancel()
		{
			this.result.TrySetResult(false);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Closes
		/// </summary>
		internal void Close()
		{
			this.result.TrySetResult(null);
		}
	}
}
