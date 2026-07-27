using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;
using Waher.Content.Xml;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Identifies the explicitly selected content format for a token update.
	/// </summary>
	internal enum TokenUpdateContentType
	{
		/// <summary>
		/// Plain text content.
		/// </summary>
		Text,

		/// <summary>
		/// Structured XML content.
		/// </summary>
		Xml
	}

	/// <summary>
	/// Captures a validated manual token update without submitting it.
	/// </summary>
	internal sealed class TokenUpdateDraft
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenUpdateDraft"/> class.
		/// </summary>
		/// <param name="ContentType">Explicitly selected content format.</param>
		/// <param name="Content">Complete update content.</param>
		/// <param name="Personal">Whether the update is private to the current owner.</param>
		public TokenUpdateDraft(
			TokenUpdateContentType ContentType,
			string Content,
			bool Personal)
		{
			this.ContentType = ContentType;
			this.Content = Content;
			this.Personal = Personal;
		}

		/// <summary>
		/// Gets the explicitly selected content format.
		/// </summary>
		public TokenUpdateContentType ContentType { get; }

		/// <summary>
		/// Gets the complete update content.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Gets a value indicating whether the update is private to the current owner.
		/// </summary>
		public bool Personal { get; }
	}

	/// <summary>
	/// Describes the server and refresh outcome of a manual token update.
	/// </summary>
	internal enum TokenUpdateSubmissionStatus
	{
		/// <summary>
		/// The server confirmed the update and current state was refreshed.
		/// </summary>
		Confirmed,

		/// <summary>
		/// The server confirmed the update but current state could not be completely refreshed.
		/// </summary>
		ConfirmedRefreshFailed,

		/// <summary>
		/// The server rejected the update or it failed before an ambiguous send outcome.
		/// </summary>
		Failed,

		/// <summary>
		/// The server outcome could not be confirmed safely.
		/// </summary>
		OutcomeUncertain
	}

	/// <summary>
	/// Returns a safe, user-facing submission outcome to the update composer.
	/// </summary>
	internal sealed class TokenUpdateSubmissionResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenUpdateSubmissionResult"/> class.
		/// </summary>
		/// <param name="Status">Submission outcome.</param>
		/// <param name="Message">Localized user-facing outcome message.</param>
		/// <param name="AllowRetry">Whether the same draft can safely be submitted again.</param>
		public TokenUpdateSubmissionResult(
			TokenUpdateSubmissionStatus Status,
			string Message,
			bool AllowRetry)
		{
			this.Status = Status;
			this.Message = Message;
			this.AllowRetry = AllowRetry;
		}

		/// <summary>
		/// Gets the submission outcome.
		/// </summary>
		public TokenUpdateSubmissionStatus Status { get; }

		/// <summary>
		/// Gets the localized user-facing outcome message.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets a value indicating whether the same draft can safely be submitted again.
		/// </summary>
		public bool AllowRetry { get; }

		/// <summary>
		/// Gets a value indicating whether the composer can close after a confirmed submission.
		/// </summary>
		public bool ShouldClose =>
			this.Status is TokenUpdateSubmissionStatus.Confirmed or
				TokenUpdateSubmissionStatus.ConfirmedRefreshFailed;
	}

	/// <summary>
	/// Manages an explicit text or advanced XML update draft for a token.
	/// </summary>
	public partial class TokenUpdateComposerViewModel : BaseViewModel
	{
		private readonly Func<TokenUpdateDraft, Task<TokenUpdateSubmissionResult>> submitAsync;
		private readonly TaskCompletionSource<TokenUpdateSubmissionResult?> result =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		private bool isClosed;

		/// <summary>
		/// Initializes an update composer with its owning page submission callback.
		/// </summary>
		/// <param name="SubmitAsync">Callback that submits a validated draft and refreshes authoritative state.</param>
		internal TokenUpdateComposerViewModel(
			Func<TokenUpdateDraft, Task<TokenUpdateSubmissionResult>> SubmitAsync)
		{
			ArgumentNullException.ThrowIfNull(SubmitAsync);
			this.submitAsync = SubmitAsync;
		}

		/// <summary>
		/// Gets the confirmed outcome, or null when the composer is dismissed.
		/// </summary>
		internal Task<TokenUpdateSubmissionResult?> Result => this.result.Task;

		/// <summary>
		/// Gets or sets the plain-text draft.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendUpdate))]
		[NotifyCanExecuteChangedFor(nameof(SendUpdateCommand))]
		private string textNote = string.Empty;

		/// <summary>
		/// Gets or sets the XML draft kept separately from the plain-text draft.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendUpdate))]
		[NotifyCanExecuteChangedFor(nameof(SendUpdateCommand))]
		private string xmlNote = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether the update is private to the current owner.
		/// </summary>
		[ObservableProperty]
		private bool personal;

		/// <summary>
		/// Gets or sets a value indicating whether advanced XML mode is explicitly selected.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendUpdate))]
		[NotifyCanExecuteChangedFor(nameof(SendUpdateCommand))]
		private bool isXmlMode;

		/// <summary>
		/// Gets or sets a value indicating whether submission is currently pending.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendUpdate))]
		[NotifyPropertyChangedFor(nameof(CanCancel))]
		[NotifyPropertyChangedFor(nameof(CanChangeMode))]
		[NotifyCanExecuteChangedFor(nameof(SendUpdateCommand))]
		[NotifyCanExecuteChangedFor(nameof(CancelCommand))]
		[NotifyCanExecuteChangedFor(nameof(ShowTextModeCommand))]
		[NotifyCanExecuteChangedFor(nameof(ShowXmlModeCommand))]
		private bool isSubmitting;

		/// <summary>
		/// Gets or sets a value indicating whether validation feedback is visible.
		/// </summary>
		[ObservableProperty]
		private bool hasValidationError;

		/// <summary>
		/// Gets or sets a value indicating whether submission feedback is visible.
		/// </summary>
		[ObservableProperty]
		private bool hasSubmissionFeedback;

		/// <summary>
		/// Gets or sets the localized validation or submission feedback.
		/// </summary>
		[ObservableProperty]
		private string feedbackMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether retry is blocked until activity is refreshed externally.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendUpdate))]
		[NotifyCanExecuteChangedFor(nameof(SendUpdateCommand))]
		private bool isRetryBlocked;

		/// <summary>
		/// Gets a value indicating whether the safe default text mode is selected.
		/// </summary>
		public bool IsTextMode => !this.IsXmlMode;

		/// <summary>
		/// Gets a value indicating whether the active draft can be submitted.
		/// </summary>
		public bool CanSendUpdate =>
			!this.IsSubmitting &&
			!this.IsRetryBlocked &&
			!string.IsNullOrWhiteSpace(this.IsXmlMode ? this.XmlNote : this.TextNote);

		/// <summary>
		/// Gets a value indicating whether the composer can be cancelled.
		/// </summary>
		public bool CanCancel => !this.IsSubmitting;

		/// <summary>
		/// Gets a value indicating whether the content mode can be changed.
		/// </summary>
		public bool CanChangeMode => !this.IsSubmitting;

		partial void OnIsXmlModeChanged(bool Value)
		{
			this.OnPropertyChanged(nameof(this.IsTextMode));
			this.ClearRecoverableFeedback();
		}

		partial void OnTextNoteChanged(string Value)
		{
			this.ClearRecoverableFeedback();
		}

		partial void OnXmlNoteChanged(string Value)
		{
			this.ClearRecoverableFeedback();
		}

		/// <summary>
		/// Selects the safe default plain-text mode without changing either draft buffer.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanChangeMode))]
		private void ShowTextMode()
		{
			this.IsXmlMode = false;
		}

		/// <summary>
		/// Selects advanced XML mode without changing either draft buffer.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanChangeMode))]
		private void ShowXmlMode()
		{
			this.IsXmlMode = true;
		}

		/// <summary>
		/// Validates and submits the explicitly selected update format.
		/// </summary>
		[RelayCommand(
			AllowConcurrentExecutions = false,
			CanExecute = nameof(CanSendUpdate))]
		private async Task SendUpdate()
		{
			string Content = this.IsXmlMode ? this.XmlNote : this.TextNote;
			TokenUpdateContentType ContentType = this.IsXmlMode
				? TokenUpdateContentType.Xml
				: TokenUpdateContentType.Text;

			this.HasValidationError = false;
			this.HasSubmissionFeedback = false;
			this.FeedbackMessage = string.Empty;

			if (ContentType == TokenUpdateContentType.Xml && !XML.IsValidXml(Content))
			{
				this.HasValidationError = true;
				this.FeedbackMessage = ServiceRef.Localizer[nameof(AppResources.InvalidXmlUpdate)];
				return;
			}

			this.IsSubmitting = true;
			try
			{
				TokenUpdateDraft Draft = new(ContentType, Content, this.Personal);
				TokenUpdateSubmissionResult SubmissionResult = await this.submitAsync(Draft);

				if (SubmissionResult.ShouldClose)
				{
					this.result.TrySetResult(SubmissionResult);
					this.TextNote = string.Empty;
					this.XmlNote = string.Empty;
					if (!this.isClosed)
						await ServiceRef.PopupService.PopAsync();
					return;
				}

				this.HasSubmissionFeedback = true;
				this.FeedbackMessage = SubmissionResult.Message;
				this.IsRetryBlocked = !SubmissionResult.AllowRetry;
			}
			catch (Exception ex)
			{
				// The callback contract should return safe outcomes. This guard preserves the draft if it does not.
				ServiceRef.LogService.LogWarning(
					"Token update submission UI failed.",
					new KeyValuePair<string, object?>(
						"FailureType",
						ex.GetType().Name));
				this.HasSubmissionFeedback = true;
				this.FeedbackMessage = ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)];
			}
			finally
			{
				this.IsSubmitting = false;
			}
		}

		/// <summary>
		/// Dismisses the composer without submitting either draft.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCancel))]
		private async Task Cancel()
		{
			this.result.TrySetResult(null);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Completes the result when the popup is dismissed outside its explicit controls.
		/// </summary>
		internal void Close()
		{
			this.isClosed = true;
			this.result.TrySetResult(null);
		}

		private void ClearRecoverableFeedback()
		{
			this.HasValidationError = false;

			if (this.IsRetryBlocked)
				return;

			this.HasSubmissionFeedback = false;
			this.FeedbackMessage = string.Empty;
		}
	}
}
