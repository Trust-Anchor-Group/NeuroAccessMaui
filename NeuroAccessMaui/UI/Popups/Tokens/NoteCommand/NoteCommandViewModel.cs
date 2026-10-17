using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using Waher.Networking.XMPP.Contracts;
using Waher.Script;

namespace NeuroAccessMaui.UI.Popups.Tokens.NoteCommand
{
	/// <summary>
	/// View model for the form that asks for the values of a note command's parameters, and then runs the command.
	/// </summary>
	public partial class NoteCommandViewModel : BaseViewModel
	{
		private const int ValidationDelayMs = 300;

		private readonly TaskCompletionSource<bool> result = new();
		private readonly NoteCommandItem item;
		private readonly string tokenId;
		private int validationVersion;

		/// <summary>
		/// View model for the form that asks for the values of a note command's parameters, and then runs the command.
		/// </summary>
		/// <param name="Item">The note command.</param>
		/// <param name="TokenId">ID of the token the note is added to.</param>
		public NoteCommandViewModel(NoteCommandItem Item, string TokenId)
			: base()
		{
			this.item = Item;
			this.tokenId = TokenId;
		}

		/// <summary>
		/// Result will be provided here: true when the note was added. If the form is cancelled, false is returned.
		/// </summary>
		public Task<bool> Result => this.result.Task;

		/// <summary>
		/// The command's title.
		/// </summary>
		public string Title => this.item.Title;

		/// <summary>
		/// What the command does, if the command says.
		/// </summary>
		public string? ToolTip => this.item.ToolTip;

		/// <summary>
		/// If the command says what it does.
		/// </summary>
		public bool HasToolTip => !string.IsNullOrEmpty(this.item.ToolTip);

		/// <summary>
		/// Editors for the command's parameters.
		/// </summary>
		public ObservableCollection<ObservableParameter> Parameters { get; } = [];

		/// <summary>
		/// If every parameter has a valid value.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		private bool areValuesValid;

		/// <summary>
		/// If the command is running.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		private bool isSending;

		/// <summary>
		/// If the command can be run with the values entered.
		/// </summary>
		public bool CanSend => this.AreValuesValid && !this.IsSending;

		/// <summary>
		/// Creates the parameter editors, and checks the values they start with. A yes-or-no value starts as no.
		/// </summary>
		public async Task LoadAsync()
		{
			foreach (Parameter Parameter in this.item.Command.Parameters ?? [])
			{
				ObservableParameter Editor = await ObservableParameter.CreateAsync(Parameter);

				if (Editor is ObservableBooleanParameter && Editor.IsValueMissing)
					Editor.Value = false;

				Editor.PropertyChanged += this.Editor_PropertyChanged;
				this.Parameters.Add(Editor);
			}

			await this.ValidateAsync();
		}

		private void Editor_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ObservableParameter.Value))
				_ = this.ValidateLaterAsync();
		}

		/// <summary>
		/// Checks the values when the user has stopped changing them for a moment.
		/// </summary>
		private async Task ValidateLaterAsync()
		{
			int Version = ++this.validationVersion;

			await Task.Delay(ValidationDelayMs);

			if (Version == this.validationVersion)
				await this.ValidateAsync();
		}

		/// <summary>
		/// Checks every value with the parameter's own rules and validation script. A value nobody has entered isn't shown
		/// as an error, but the command can't run without it.
		/// </summary>
		private async Task ValidateAsync()
		{
			int Version = this.validationVersion;
			bool AllValid = true;

			try
			{
				Variables Variables = NoteCommandRunner.CreateVariables(this.item.Command, this.item.AssignedNames);

				foreach (ObservableParameter Editor in this.Parameters)
				{
					bool Valid = false;
					string ValidationText = string.Empty;

					if (!Editor.IsValueMissing)
					{
						Valid = await NoteCommandRunner.IsParameterValidAsync(Editor.Parameter, Variables);
						if (!Valid)
							ValidationText = Editor.Parameter.ErrorText ?? string.Empty;
					}

					if (Version != this.validationVersion)
						return;

					Editor.IsValid = Valid || Editor.IsValueMissing;
					Editor.ValidationText = ValidationText;
					AllValid &= Valid;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				AllValid = false;
			}

			if (Version == this.validationVersion)
				this.AreValuesValid = AllValid;
		}

		/// <summary>
		/// Runs the command with the values entered. When it doesn't work, the reason is shown and the form stays open
		/// with the values kept.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSend))]
		private async Task Send()
		{
			this.IsSending = true;
			bool Sent;

			try
			{
				Sent = await NoteCommandRunner.RunAsync(this.item.Command, this.tokenId, this.item.AssignedNames);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await TokenErrors.ShowAsync(Ex);
				Sent = false;
			}
			finally
			{
				this.IsSending = false;
			}

			if (!Sent)
				return;

			this.result.TrySetResult(true);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Closes the form without running the command.
		/// </summary>
		[RelayCommand]
		private async Task Cancel()
		{
			this.result.TrySetResult(false);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Called when the form closes.
		/// </summary>
		internal void Close()
		{
			foreach (ObservableParameter Editor in this.Parameters)
				Editor.PropertyChanged -= this.Editor_PropertyChanged;

			this.result.TrySetResult(false);
		}
	}
}
