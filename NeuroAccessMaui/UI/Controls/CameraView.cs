using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;
using SkiaSharp;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A custom wrapper around <see cref="CommunityToolkit.Maui.Views.CameraView"/>.
	/// </summary>
	public partial class CameraView : CommunityToolkit.Maui.Views.CameraView
	{
		/// <summary>
		/// Message sent when an image has been captured.
		/// </summary>
		public sealed class ImageCapturedMessage : ValueChangedMessage<byte[]>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ImageCapturedMessage"/> class.
			/// </summary>
			/// <param name="Value">The captured image bytes.</param>
			public ImageCapturedMessage(byte[] Value)
				: base(Value)
			{
			}
		}

		#region Local Fields

		private PeriodicTimer? captureTimer;
		private CancellationTokenSource? captureTimerTokenSource;
		private bool isAttached;

		#endregion

		#region Properties

		public static readonly BindableProperty CaptureModeProperty =
			BindableProperty.Create(
				nameof(CaptureMode),
				typeof(CaptureMode),
				typeof(CameraView),
				CaptureMode.Manual,
				propertyChanged: OnCaptureSettingsChanged);

		/// <summary>
		/// Gets or sets the capture mode for the camera.
		/// </summary>
		public CaptureMode CaptureMode
		{
			get => (CaptureMode)this.GetValue(CaptureModeProperty);
			set => this.SetValue(CaptureModeProperty, value);
		}

		public static readonly BindableProperty CaptureIntervalProperty =
			BindableProperty.Create(
				nameof(CaptureInterval),
				typeof(TimeSpan),
				typeof(CameraView),
				TimeSpan.FromSeconds(1),
				propertyChanged: OnCaptureSettingsChanged);

		/// <summary>
		/// Gets or sets the interval between automatic captures.
		/// </summary>
		public TimeSpan CaptureInterval
		{
			get => (TimeSpan)this.GetValue(CaptureIntervalProperty);
			set => this.SetValue(CaptureIntervalProperty, value);
		}

		#endregion

		#region Constructor & Setup

		/// <summary>
		/// Initializes a new instance of the <see cref="CameraView"/> class.
		/// </summary>
		public CameraView()
		{
		}

		/// <inheritdoc/>
		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();

			this.isAttached = this.Handler is not null;
			this.ApplyCaptureSettings();
		}

		private static void OnCaptureSettingsChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is CameraView View)
				View.ApplyCaptureSettings();
		}

		private void ApplyCaptureSettings()
		{
			if (!this.isAttached)
			{
				this.StopAutomaticCapture();
				return;
			}

			if (this.CaptureMode == CaptureMode.Automatic)
				this.StartAutomaticCapture();
			else
				this.StopAutomaticCapture();
		}

		private void StartAutomaticCapture()
		{
			if (this.Handler is null)
				return;

			this.StopAutomaticCapture();

			this.captureTimerTokenSource = new CancellationTokenSource();
			this.captureTimer = new PeriodicTimer(this.CaptureInterval);
			CancellationTokenSource? LocalTokenSource = this.captureTimerTokenSource;
			PeriodicTimer? LocalTimer = this.captureTimer;

			CancellationToken Token = LocalTokenSource.Token;
			_ = Task.Run(async () =>
			{
				try
				{
					while (LocalTimer is not null && await LocalTimer.WaitForNextTickAsync(Token))
					{
						await this.TakePhoto();
					}
				}
				catch
				{
					// Not handled
				}

			}, Token);
		}

		private void StopAutomaticCapture()
		{
			CancellationTokenSource? TokenSource = this.captureTimerTokenSource;
			this.captureTimerTokenSource = null;

			try
			{
				TokenSource?.Cancel();
			}
			catch
			{
				// Not handled
			}

			TokenSource?.Dispose();

			PeriodicTimer? Timer = this.captureTimer;
			this.captureTimer = null;
			Timer?.Dispose();
		}

		#endregion

		#region Commands

		[RelayCommand]
		private async Task TakePhoto()
		{
			try
			{
				if (this.Handler is null)
					return;

				CancellationTokenSource captureImageCTS = new CancellationTokenSource(TimeSpan.FromSeconds(3));
				Stream Stream = await this.CaptureImage(captureImageCTS.Token);

				if (Stream is MemoryStream memoryStream)
				{
					byte[] ImageData = memoryStream.ToArray();
					WeakReferenceMessenger.Default.Send(new ImageCapturedMessage(ImageData));
				}
			}
			catch
			{
				// Do nothing for now
			}
		}

		#endregion

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.StopAutomaticCapture();
			}

			base.Dispose(disposing);
		}
	}

	public enum CaptureMode
	{
		Manual,
		Automatic
	}

}
