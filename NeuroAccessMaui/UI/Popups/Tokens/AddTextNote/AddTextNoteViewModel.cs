using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// View model for popup page prompting the user for a text note to be added.
	/// </summary>
	public partial class AddTextNoteViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool?> result = new();

		/// <summary>
		/// View model for popup page prompting the user for a text note to be added.
		/// </summary>
		public AddTextNoteViewModel()
			: base()
		{
		}

		/// <summary>
		/// Result will be provided here. If dialog is cancelled, null is returned.
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
		/// If the note can be added.
		/// </summary>
		public bool CanAddNote => !string.IsNullOrEmpty(this.TextNote);

		/// <summary>
		/// Adds the note
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanAddNote))]
		private async Task AddNote()
		{
			this.result.TrySetResult(!string.IsNullOrEmpty(this.TextNote));
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
