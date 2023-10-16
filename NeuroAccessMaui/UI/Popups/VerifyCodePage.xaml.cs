using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups;

public interface ICodeVerification
{

}

/// <summary>
/// Prompts the user for its PIN
/// </summary>
public partial class VerifyCodePage
{
	private readonly ICodeVerification codeVerification;
	private readonly TaskCompletionSource<string?> result = new();
	private readonly List<Label> innerLabels;

	/// <summary>
	/// Task waiting for result. null means dialog was closed without providing a CODE.
	/// </summary>
	public Task<string?> Result => this.result.Task;

	public string PhoneOrEmail { get; set; }

	public string LocalizedVerifyCodePageDetails
	{
		get
		{
			return ServiceRef.Localizer[nameof(AppResources.OnboardingVerifyCodePageDetails), this.PhoneOrEmail];
		}
	}

	/// <summary>
	/// Prompts the user for its CODE
	/// </summary>
	public VerifyCodePage(ICodeVerification CodeVerification, string PhoneOrEmail, ImageSource? Background = null) : base(Background)
	{
		this.codeVerification = CodeVerification;
		this.PhoneOrEmail = PhoneOrEmail;

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

		this.InnerCodeEntry.Focus();
	}

	protected override void OnDisappearing()
	{
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

	[RelayCommand]
	public async Task Resend()
	{
		//!!! this.result.TrySetResult(Code);
		await MopupService.Instance.PopAsync();
	}

	private void InnerCodeEntry_TextChanged(object Sender, TextChangedEventArgs e)
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
			this.innerLabels[i].Text = (NewLength > i) ? NewText[i..(i+1)] : "0";
		}

		this.VerifyCommand.NotifyCanExecuteChanged();
	}

	private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		this.InnerCodeEntry.Focus();
	}
}
