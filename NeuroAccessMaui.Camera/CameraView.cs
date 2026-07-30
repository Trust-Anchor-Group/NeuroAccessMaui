using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Displays a camera preview and exposes a frame stream.
	/// </summary>
	public sealed class CameraView : ContentView, ICameraFrameSource
	{
		private readonly object frameReadySyncObject = new object();
		private EventHandler<CameraFrame>? frameReady;
		private Action<bool>? frameSubscriptionChangedCallback;

		/// <summary>
		/// Identifies the <see cref="SelectedCamera"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SelectedCameraProperty = BindableProperty.Create(
			nameof(SelectedCamera),
			typeof(CameraDescriptor),
			typeof(CameraView),
			default(CameraDescriptor));

		/// <summary>
		/// Identifies the <see cref="Options"/> bindable property.
		/// </summary>
		public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
			nameof(Options),
			typeof(CameraOptions),
			typeof(CameraView),
			new CameraOptions());

		/// <summary>
		/// Identifies the <see cref="IsPreviewRunning"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsPreviewRunningProperty = BindableProperty.Create(
			nameof(IsPreviewRunning),
			typeof(bool),
			typeof(CameraView),
			false);

		/// <summary>
		/// Gets the platform controller for the camera view.
		/// </summary>
		public ICameraController? Controller { get; internal set; }

		/// <summary>
		/// Occurs when a new camera frame is available.
		/// </summary>
		public event EventHandler<CameraFrame>? FrameReady
		{
			add
			{
				bool PreviousHasSubscribers;
				bool CurrentHasSubscribers;

				lock (this.frameReadySyncObject)
				{
					PreviousHasSubscribers = this.frameReady is not null;
					this.frameReady += value;
					CurrentHasSubscribers = this.frameReady is not null;
				}

				if (PreviousHasSubscribers != CurrentHasSubscribers)
					this.NotifyFrameDemandChanged(CurrentHasSubscribers);
			}

			remove
			{
				bool PreviousHasSubscribers;
				bool CurrentHasSubscribers;

				lock (this.frameReadySyncObject)
				{
					PreviousHasSubscribers = this.frameReady is not null;
					this.frameReady -= value;
					CurrentHasSubscribers = this.frameReady is not null;
				}

				if (PreviousHasSubscribers != CurrentHasSubscribers)
					this.NotifyFrameDemandChanged(CurrentHasSubscribers);
			}
		}

		/// <summary>
		/// Gets a value indicating whether any handlers are subscribed to <see cref="FrameReady"/>.
		/// </summary>
		internal bool HasFrameSubscribers
		{
			get
			{
				lock (this.frameReadySyncObject)
				{
					return this.frameReady is not null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the selected camera descriptor.
		/// </summary>
		public CameraDescriptor? SelectedCamera
		{
			get => (CameraDescriptor?)this.GetValue(SelectedCameraProperty);
			set => this.SetValue(SelectedCameraProperty, value);
		}

		/// <summary>
		/// Gets or sets the camera options.
		/// </summary>
		public CameraOptions Options
		{
			get => (CameraOptions)this.GetValue(OptionsProperty);
			set => this.SetValue(OptionsProperty, value ?? new CameraOptions());
		}

		/// <summary>
		/// Gets a value indicating whether the preview is running.
		/// </summary>
		public bool IsPreviewRunning
		{
			get => (bool)this.GetValue(IsPreviewRunningProperty);
			internal set => this.SetValue(IsPreviewRunningProperty, value);
		}

		/// <summary>
		/// Starts the camera preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task StartPreviewAsync(CancellationToken CancellationToken)
		{
			if (this.Controller is null)
				return Task.CompletedTask;

			return this.Controller.StartPreviewAsync(CancellationToken);
		}

		/// <summary>
		/// Stops the camera preview.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task StopPreviewAsync()
		{
			if (this.Controller is null)
				return Task.CompletedTask;

			return this.Controller.StopPreviewAsync();
		}

		/// <summary>
		/// Permanently releases camera resources for the current handler instance.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task ReleaseAsync()
		{
			if (this.Controller is null)
				return Task.CompletedTask;

			return this.Controller.ReleaseAsync();
		}

		/// <summary>
		/// Captures a still image from the active camera preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>JPEG image bytes when supported; otherwise <c>null</c>.</returns>
		public Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken)
		{
			if (this.Controller is null)
				return Task.FromResult<byte[]?>(null);

			return this.Controller.CapturePhotoAsync(CancellationToken);
		}

		/// <summary>
		/// Retrieves the available cameras for the current platform.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A list of available camera descriptors.</returns>
		public static Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken)
		{
			return CameraPlatformService.GetAvailableCamerasAsync(CancellationToken);
		}

		internal void SetController(ICameraController? Controller)
		{
			this.Controller = Controller;
			this.NotifyFrameDemandChanged(this.HasFrameSubscribers);
		}

		internal void SetFrameSubscriptionChangedCallback(Action<bool>? Callback)
		{
			this.frameSubscriptionChangedCallback = Callback;
		}

		internal void RaiseFrameReady(CameraFrame Frame)
		{
			EventHandler<CameraFrame>? FrameReadyHandlers;
			lock (this.frameReadySyncObject)
			{
				FrameReadyHandlers = this.frameReady;
			}

			FrameReadyHandlers?.Invoke(this, Frame);
		}

		private void NotifyFrameDemandChanged(bool HasFrameDemand)
		{
			this.frameSubscriptionChangedCallback?.Invoke(HasFrameDemand);
		}
	}
}
