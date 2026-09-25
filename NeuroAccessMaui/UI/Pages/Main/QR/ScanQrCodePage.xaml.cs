
using CommunityToolkit.Maui.Layouts;
// using CommunityToolkit.Mvvm.Input; // Removed page-level commands; commands now reside in ViewModel
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Services;
using ZXing.Net.Maui;
#if ANDROID
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#endif

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// A page to display for scanning of a QR code, either automatically via the camera, or by entering the code manually.
	/// </summary>
	public partial class ScanQrCodePage
	{
		private bool pageVisible;
#if ANDROID
		private bool cameraNeedsRebinding;
#endif

		public ScanQrCodePage()
		{
			// Create VM (it will PopLatestArgs internally)
			ScanQrCodeViewModel vm = new();
			this.BindingContext = vm; // set before InitializeComponent for compiled bindings

			this.InitializeComponent();

			// Post-XAML control configuration
			this.LinkEntry.Keyboard = Keyboard.Url;
			this.LinkEntry.IsSpellCheckEnabled = false;
			this.LinkEntry.IsTextPredictionEnabled = false;

			StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");

			this.CameraBarcodeReaderView.IsDetecting = false;
			this.CameraBarcodeReaderView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.TwoDimensional,
				AutoRotate = true,
				TryHarder = true,
				TryInverted = true,
				Multiple = false,
			};

			vm.DoSwitchMode(true);
		}


		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			// Base Appearing executes ViewModel lifecycle
			await base.OnAppearingAsync();
			this.pageVisible = true;

			// Sync initial states from VM
			if (this.BindingContext is ScanQrCodeViewModel vm)
			{
				this.ApplyState(vm.IsAutomaticScan, initial: true);
				this.CameraBarcodeReaderView.IsTorchOn = vm.IsTorchOn;
				this.CameraBarcodeReaderView.CameraLocation = vm.CameraLocation;
				vm.PropertyChanged += this.VmOnPropertyChanged;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			this.pageVisible = false;
			if (this.BindingContext is ScanQrCodeViewModel vm)
				vm.PropertyChanged -= this.VmOnPropertyChanged;
			try
			{
				await this.CloseCameraAsync();
			}
			finally
			{
				await base.OnDisappearingAsync();
			}
		}

		private async void VmOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!this.pageVisible || sender is not ScanQrCodeViewModel vm)
				return;

			switch (e.PropertyName)
			{
				case nameof(ScanQrCodeViewModel.IsAutomaticScan):
					await this.Dispatcher.DispatchAsync(() => this.ApplyState(vm.IsAutomaticScan));
					break;
				case nameof(ScanQrCodeViewModel.IsTorchOn):
					this.CameraBarcodeReaderView.IsTorchOn = vm.IsTorchOn;
					break;
				case nameof(ScanQrCodeViewModel.IsDetecting):
					this.CameraBarcodeReaderView.IsDetecting = vm.IsDetecting;
					break;
				case nameof(ScanQrCodeViewModel.CameraLocation):
					this.CameraBarcodeReaderView.CameraLocation = vm.CameraLocation;
					break;
			}
		}

		private async void ApplyState(bool isAutomatic, bool initial = false)
		{
			try
			{
				if (!this.pageVisible)
					return;

				string current = StateContainer.GetCurrentState(this.GridWithAnimation);
				bool currentlyAutomatic = string.Equals(current, "AutomaticScan", StringComparison.OrdinalIgnoreCase);
				if (currentlyAutomatic == isAutomatic && !initial)
					return;

				if (initial)
				{
					// Initial setup: avoid animation & camera flip hack to prevent black screen
					if (isAutomatic)
					{
						if (!currentlyAutomatic)
							StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");
						this.LinkEntry.Unfocus();
						// slight delay to allow native camera initialization before enabling detection
						await Task.Delay(250);
						if (!this.pageVisible)
							return;
#if ANDROID
						if (this.cameraNeedsRebinding && this.CameraBarcodeReaderView.Handler is CameraBarcodeReaderViewHandler Handler)
						{
							Handler.UpdateValue(nameof(this.CameraBarcodeReaderView.CameraLocation));
							Handler.UpdateValue(nameof(this.CameraBarcodeReaderView.IsTorchOn));
							this.cameraNeedsRebinding = false;
						}
#endif
						this.CameraBarcodeReaderView.IsDetecting = true;
					}
					else
					{
						StateContainer.SetCurrentState(this.GridWithAnimation, "ManualScan");
						this.CameraBarcodeReaderView.IsTorchOn = false;
						this.CameraBarcodeReaderView.IsDetecting = false;
						this.LinkEntry.Focus();
					}
					return;
				}

				if (!isAutomatic)
				{
					// Enter manual: stop camera
					this.CameraBarcodeReaderView.IsTorchOn = false;
					this.CameraBarcodeReaderView.IsDetecting = false;
					await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, "ManualScan", CancellationToken.None);
					if (!this.pageVisible)
						return;
					this.LinkEntry.Focus();
				}
				else
				{
					this.LinkEntry.Unfocus();
					// Re-init camera by flipping (runtime toggle only)
					if (this.CameraBarcodeReaderView.CameraLocation == CameraLocation.Rear)
					{
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
					}
					else
					{
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
					}
					await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, "AutomaticScan", CancellationToken.None);
					if (!this.pageVisible)
						return;
					this.CameraBarcodeReaderView.IsDetecting = true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Stops detection and releases the scanner's Android camera use cases before navigation completes.
		/// </summary>
		/// <returns>A task representing camera cleanup on the main thread.</returns>
		private Task CloseCameraAsync()
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.CameraBarcodeReaderView.IsDetecting = false;
				this.CameraBarcodeReaderView.IsTorchOn = false;
#if ANDROID
				this.UnbindAndroidCamera();
#endif
			});
		}
#if ANDROID
		/// <summary>
		/// Unbinds ZXing's preview and analysis while retaining them for reuse when the page returns.
		/// </summary>
		[DynamicDependency("cameraManager", typeof(CameraBarcodeReaderViewHandler))]
		[DynamicDependency("_cameraProvider", "ZXing.Net.Maui.CameraManager", "ZXing.Net.MAUI")]
		[DynamicDependency("_cameraPreview", "ZXing.Net.Maui.CameraManager", "ZXing.Net.MAUI")]
		[DynamicDependency("_imageAnalyzer", "ZXing.Net.Maui.CameraManager", "ZXing.Net.MAUI")]
		private void UnbindAndroidCamera()
		{
			if (this.CameraBarcodeReaderView.Handler is not CameraBarcodeReaderViewHandler Handler)
				return;

			BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
			FieldInfo ManagerField = typeof(CameraBarcodeReaderViewHandler).GetField("cameraManager", Flags)
				?? throw new MissingFieldException(typeof(CameraBarcodeReaderViewHandler).FullName, "cameraManager");
			object? Manager = ManagerField.GetValue(Handler);
			if (Manager is null)
				return;

			// ZXing exposes no public stop-preview operation. Closing the native camera alone
			// leaves its use cases bound to the activity and competing with the next camera page.
			Type ManagerType = Manager.GetType();
			FieldInfo ProviderField = ManagerType.GetField("_cameraProvider", Flags)
				?? throw new MissingFieldException(ManagerType.FullName, "_cameraProvider");
			FieldInfo PreviewField = ManagerType.GetField("_cameraPreview", Flags)
				?? throw new MissingFieldException(ManagerType.FullName, "_cameraPreview");
			FieldInfo AnalysisField = ManagerType.GetField("_imageAnalyzer", Flags)
				?? throw new MissingFieldException(ManagerType.FullName, "_imageAnalyzer");
			if (ProviderField.GetValue(Manager) is not ProcessCameraProvider Provider)
				return;

			List<UseCase> UseCases = new List<UseCase>();
			if (PreviewField.GetValue(Manager) is Preview Preview)
				UseCases.Add(Preview);
			if (AnalysisField.GetValue(Manager) is ImageAnalysis Analysis)
				UseCases.Add(Analysis);
			if (UseCases.Count == 0)
				return;

			Provider.Unbind(UseCases.ToArray());
			this.cameraNeedsRebinding = true;
		}
#endif


		private async void CameraBarcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			string? Result = (e.Results.Length > 0) ? e.Results[0].Value : null;

			await this.Dispatcher.DispatchAsync(async () =>
			{
				ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
				await ViewModel.SetScannedText(Result);
			});
		}
	}
}
