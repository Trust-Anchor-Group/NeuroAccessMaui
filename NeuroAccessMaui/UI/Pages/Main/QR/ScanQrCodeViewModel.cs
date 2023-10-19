using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// The view model to bind to when scanning a QR code.
/// </summary>
public partial class ScanQrCodeViewModel : BaseViewModel
{
	private ScanQrCodeNavigationArgs? navigationArgs;

	/// <summary>
	/// An event that is fired when the scanning mode changes from automatic scan to manual entry.
	/// </summary>
	public event EventHandler ModeChanged;

	/// <summary>
	/// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
	/// </summary>
	public ScanQrCodeViewModel(ScanQrCodeNavigationArgs? NavigationArgs)
	{
		this.navigationArgs = NavigationArgs;
		//!!! this.SetModeText();
	}

	/// <inheritdoc />
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		if ((this.navigationArgs is null) && ServiceRef.NavigationService.TryGetArgs(out ScanQrCodeNavigationArgs? Args))
		{
			this.navigationArgs = Args;
		}
	}

	/// <summary>
	/// Tries to set the Scan QR Code result and close the scan page.
	/// </summary>
	/// <param name="Url">The URL to set.</param>
	internal async void TrySetResultAndClosePage(string Url)
	{
		if (this.navigationArgs is null)
		{
			try
			{
				//!!!
				/*
				if (this.useShellNavigationService)
					await this.NavigationService.GoBackAsync();
				else
					await App.Current.MainPage.Navigation.PopAsync();
				*/
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
		else
		{
			TaskCompletionSource<string> TaskSource = this.navigationArgs.QrCodeScanned;
			this.navigationArgs.QrCodeScanned = null;

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					//!!!
					/*
					Url = Url?.Trim();

					if (this.useShellNavigationService)
						await this.NavigationService.GoBackAsync();
					else
						await App.Current.MainPage.Navigation.PopAsync();

					if (TaskSource is not null)
					{
						TaskSource?.TrySetResult(Url);
					}
					*/
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}
	}

	/// <inheritdoc />
	protected override async Task OnDispose()
	{
		if (this.navigationArgs?.QrCodeScanned is TaskCompletionSource<string> TaskSource)
		{
			TaskSource.TrySetResult(string.Empty);
		}

		await base.OnDispose();
	}

	#region Properties

	/// <summary>
	/// The link text, i.e. the full qr code including scheme.
	/// </summary>
	[ObservableProperty]
	private string linkText;

	/// <summary>
	/// Gets or sets whether the open command is enabled or not.
	/// </summary>
	[ObservableProperty]
	private bool openIsEnabled; //!!! !string.IsNullOrWhiteSpace((string)LinkText);

	/// <summary>
	/// The raw QR code URL.
	/// </summary>
	[ObservableProperty]
	private string url;

	/// <summary>
	/// The localized, user friendly command name to display in the UI for scanning a QR Code. Typically "Add" or "Open".
	/// </summary>
	public string OpenCommandText
	{
		get
		{
			//!!!
			/*
						this.OpenCommandText = LocalizationResourceManager.Current["Open"];
						this.OpenCommandText = !string.IsNullOrWhiteSpace(this.navigationArgs?.CommandName)
							? this.navigationArgs.CommandName
							: LocalizationResourceManager.Current["Open"];
			*/
			return string.Empty;
		}
	}

	/// <summary>
	/// Gets or sets whether the QR scanning is automatic or manual. <seealso cref="ScanIsManual"/>.
	/// </summary>
	[ObservableProperty]
	private bool scanIsAutomatic;

	/// <summary>
	/// Gets or sets whether the QR scanning is automatic or manual. <seealso cref="ScanIsAutomatic"/>.
	/// </summary>
	[ObservableProperty]
	private bool scanIsManual;

	/// <summary>
	/// The localized mode text to display to the user.
	/// </summary>
	public string ModeText
	{
		get
		{
			//!!! this.ModeText = this.ScanIsAutomatic ? LocalizationResourceManager.Current["QrEnterManually"] : LocalizationResourceManager.Current["QrScanCode"];
			return string.Empty;
		}
	}

	#endregion

	/// <summary>
	/// Action to bind to for switching scan mode from manual to automatic.
	/// </summary>
	[RelayCommand]
	private void SwitchMode()
	{
		this.ScanIsAutomatic = !this.ScanIsAutomatic;
		this.OnModeChanged(EventArgs.Empty);
	}

	/// <summary>
	/// Invoke this method to fire the <see cref="ModeChanged"/> event.
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnModeChanged(EventArgs e)
	{
		this.ModeChanged?.Invoke(this, e);
	}
}
