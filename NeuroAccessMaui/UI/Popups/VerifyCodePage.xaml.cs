using Mopups.Pages;
using Mopups.Services;

namespace NeuroAccessMaui.UI.Popups;

/// <summary>
/// Prompts the user for its PIN
/// </summary>
public partial class VerifyCodePage : PopupPage
{
	private readonly TaskCompletionSource<string?> result = new();

	/// <summary>
	/// Task waiting for result. null means dialog was closed without providing a CODE.
	/// </summary>
	public Task<string?> Result => this.result.Task;

	/// <summary>
	/// Prompts the user for its CODE
	/// </summary>
	public VerifyCodePage(string Text)
	{
		this.InitializeComponent();
		this.BindingContext = this;
		//this.TextLabel.Text = Text;
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
		//!!! this.Code.Focus();
	}

	private async void OnEnter(object Sender, EventArgs e)
	{
		//!!! await PopupNavigation.Instance.PopAsync();
		//!!! this.result.TrySetResult(this.Code.Text);
	}

	private void Code_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		//!!! this.EnterButton.IsEnabled = !string.IsNullOrEmpty(this.Code.Text);
	}

	private void OnCloseButtonTapped(object Sender, EventArgs e)
	{
		this.Close();
	}

	private async void Close()
	{
		//!!! await PopupNavigation.Instance.PopAsync();
		this.result.TrySetResult(null);
	}

	private void PopupPage_BackgroundClicked(object sender, EventArgs e)
	{

    }
}
