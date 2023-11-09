using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// The view model to bind to when scanning a QR code.
/// </summary>
public partial class ScanQrCodeViewModel : BaseViewModel
{
	private ScanQrCodeNavigationArgs? navigationArgs;

	/// <summary>
	/// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
	/// </summary>
	public ScanQrCodeViewModel(ScanQrCodeNavigationArgs? NavigationArgs)
	{
		this.navigationArgs = NavigationArgs;
	}

	/// <inheritdoc />
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		LocalizationManager.Current.PropertyChanged += this.PropertyChangedEventHandler;

		if ((this.navigationArgs is null) && ServiceRef.NavigationService.TryGetArgs(out ScanQrCodeNavigationArgs? Args))
		{
			this.navigationArgs = Args;
			this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
		}
	}

	/// <inheritdoc/>
	protected override async Task OnDispose()
	{
		LocalizationManager.Current.PropertyChanged -= this.PropertyChangedEventHandler;

		if (this.navigationArgs?.QrCodeScanned is TaskCompletionSource<string> TaskSource)
		{
			TaskSource.TrySetResult(string.Empty);
		}

		await base.OnDispose();
	}

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
	}

	public Task DoSwitchMode(bool IsAutomaticScan)
	{
		this.IsAutomaticScan = IsAutomaticScan;
		return Task.CompletedTask;
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

	#region Properties

	/// <summary>
	/// The manually typed QR text
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(OpenUrlCommand))]
	private string? urlText;

	/// <summary>
	/// The scanned QR text
	/// </summary>
	[ObservableProperty]
	private string? qrText;

	/// <summary>
	/// Gets or sets whether the QR scanning is automatic or manual.
	/// </summary>
	[ObservableProperty]
	private bool isAutomaticScan = true;

	/// <summary>
	/// The localized page title text to display.
	/// </summary>
	public string LocalizedQrPageTitle
	{
		get
		{
			if (this.navigationArgs?.QrTitle is not null)
			{
				return ServiceRef.Localizer[this.navigationArgs.QrTitle];
			}

			return string.Empty;
		}
	}

	public bool CanOpenUrl => false;

	#endregion

	[RelayCommand(CanExecute = nameof(CanOpenUrl))]
	private async Task OpenUrl()
	{
		await Task.CompletedTask;
	}
}
