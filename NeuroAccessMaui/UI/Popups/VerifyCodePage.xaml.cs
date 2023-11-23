using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups;

public interface ICodeVerification
{
	int CountDownSeconds { get; set; }
	IDispatcherTimer CountDownTimer { get; }
	IAsyncRelayCommand ResendCodeCommand { get; }
}

/// <summary>
/// Prompts the user for its PIN
/// </summary>
public partial class VerifyCodePage
{
	private readonly TaskCompletionSource<string?> result = new();
	private readonly List<Label> innerLabels;
	private readonly string phoneOrEmail;

	/// <summary>
	/// Task waiting for result. null means dialog was closed without providing a CODE.
	/// </summary>
	public Task<string?> Result => this.result.Task;

	public ICodeVerification CodeVerification { get; }

	public string LocalizedVerifyCodePageDetails
	{
		get
		{
			return ServiceRef.Localizer[nameof(AppResources.OnboardingVerifyCodePageDetails), this.phoneOrEmail];
		}
	}

	/// <summary>
	/// Prompts the user for its CODE
	/// </summary>
	public VerifyCodePage(ICodeVerification CodeVerification, string PhoneOrEmail, ImageSource? Background = null) : base(Background)
	{
		this.phoneOrEmail = PhoneOrEmail;
		this.CodeVerification = CodeVerification;

		this.InitializeComponent();
		this.BindingContext = this;

		this.innerLabels = [
			this.InnerCode1,
			this.InnerCode2,
			this.InnerCode3,
			this.InnerCode4,
			this.InnerCode5,
			this.InnerCode6
			];

		this.InnerCodeEntry.Text = string.Empty;
	}

	/// <inheritdoc/>
	protected override void LayoutChildren(double x, double y, double width, double height)
	{
		base.LayoutChildren(x, y, width, height + 1);
	}

	/// <inheritdoc/>
	protected override void OnAppearing()
	{
		base.OnAppearing();

		this.CodeVerification.CountDownTimer.Tick += this.CountDownEventHandler;
		this.InnerCodeEntry.Focus();
	}

	protected override void OnDisappearing()
	{
		this.CodeVerification.CountDownTimer.Tick -= this.CountDownEventHandler;
		this.result.TrySetResult(null);

		base.OnDisappearing();
	}

	public bool CanVerify => !string.IsNullOrEmpty(this.InnerCodeEntry.Text) && (this.InnerCodeEntry.Text.Length == this.innerLabels.Count);

	[RelayCommand(CanExecute = nameof(CanVerify))]
	public async Task Verify()
	{
		this.result.TrySetResult(this.InnerCodeEntry.Text);
		await MopupService.Instance.PopAsync();
	}

	public string LocalizedResendCodeText
	{
		get
		{
			if (this.CodeVerification.CountDownSeconds > 0)
			{
				return ServiceRef.Localizer[nameof(AppResources.ResendCodeSeconds), this.CodeVerification.CountDownSeconds];
			}

			return ServiceRef.Localizer[nameof(AppResources.ResendCode)];
		}
	}

	private async void InnerCodeEntry_TextChanged(object Sender, TextChangedEventArgs e)
	{
		string NewText = e.NewTextValue;
		int NewLength = NewText.Length;

		bool IsValid = (NewLength <= this.innerLabels.Count) || NewText.ToCharArray().All(ch => !"0123456789".Contains(ch));

		if (!IsValid)
		{
			this.InnerCodeEntry.Text = e.OldTextValue;
			return;
		}

		for (int i = 0; i < this.innerLabels.Count; i++)
		{
			//Label Label = this.innerLabels[i];

			if (NewLength > i)
			{
				this.innerLabels[i].Text = NewText[i..(i + 1)];
				VisualStateManager.GoToState(this.innerLabels[i], VisualStateManager.CommonStates.Normal);
			}
			else
			{
				this.innerLabels[i].Text = "0\u2060"; // Added a "zero width no-break space" to make the disabled state to work right :)
				VisualStateManager.GoToState(this.innerLabels[i], VisualStateManager.CommonStates.Disabled);
			}
		}

		if (NewLength == this.innerLabels.Count)
		{
			await this.InnerCodeEntry.HideKeyboardAsync();
		}

		this.VerifyCommand.NotifyCanExecuteChanged();
	}

	private void CountDownEventHandler(object? sender, EventArgs e)
	{
		this.OnPropertyChanged(nameof(this.LocalizedResendCodeText));
	}

	private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		this.InnerCodeEntry.Focus();
	}

	private async void TheMainGridTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		await MopupService.Instance.PopAsync();
	}
}
