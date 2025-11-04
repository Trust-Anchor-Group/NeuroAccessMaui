using System;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Default implementation of <see cref="IKeyboardInsetsService"/> that normalizes platform keyboard measurements.
	/// </summary>
	public sealed class KeyboardInsetsService : IKeyboardInsetsService, IDisposable
	{
		private readonly IPlatformSpecific platformSpecific;
		private double keyboardHeight;
		private bool isKeyboardVisible;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="KeyboardInsetsService"/> class.
		/// </summary>
		/// <param name="platformSpecific">The platform-specific provider to monitor keyboard changes.</param>
		public KeyboardInsetsService(IPlatformSpecific platformSpecific)
		{
			this.platformSpecific = platformSpecific ?? throw new ArgumentNullException(nameof(platformSpecific));
			this.platformSpecific.KeyboardSizeChanged += this.OnKeyboardSizeChanged;
			this.platformSpecific.KeyboardHidden += this.OnKeyboardHidden;
		}

		/// <inheritdoc/>
		public event EventHandler<KeyboardInsetChangedEventArgs>? KeyboardInsetChanged;

		/// <inheritdoc/>
		public double KeyboardHeight => this.keyboardHeight;

		/// <inheritdoc/>
		public bool IsKeyboardVisible => this.isKeyboardVisible;

		/// <summary>
		/// Disposes the instance and detaches event handlers.
		/// </summary>
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			this.platformSpecific.KeyboardSizeChanged -= this.OnKeyboardSizeChanged;
			this.platformSpecific.KeyboardHidden -= this.OnKeyboardHidden;
			this.isDisposed = true;
		}

		private void OnKeyboardSizeChanged(object? sender, KeyboardSizeMessage message)
		{
			double newHeight = Math.Max(0, message.KeyboardSize);
			bool newVisibility = newHeight > 0.5;

			if (Math.Abs(newHeight - this.keyboardHeight) < 0.5 && newVisibility == this.isKeyboardVisible)
				return;

			this.keyboardHeight = newHeight;
			this.isKeyboardVisible = newVisibility;
			this.RaiseChanged();
		}

		private void OnKeyboardHidden(object? sender, KeyboardSizeMessage message)
		{
			if (this.keyboardHeight <= 0 && !this.isKeyboardVisible)
				return;

			this.keyboardHeight = 0;
			this.isKeyboardVisible = false;
			this.RaiseChanged();
		}

		private void RaiseChanged()
		{
			void Raise()
			{
				this.KeyboardInsetChanged?.Invoke(this, new KeyboardInsetChangedEventArgs(this.keyboardHeight, this.isKeyboardVisible));
			}

			if (MainThread.IsMainThread)
				Raise();
			else
				MainThread.BeginInvokeOnMainThread(Raise);
		}
	}
}
