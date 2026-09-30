namespace DevStorybook.Controls
{
	/// <summary>
	/// Hosts an unmodified production preview and provides an invisible fullscreen exit gesture.
	/// </summary>
	public partial class FullscreenPreviewHost : ContentView
	{
		private static readonly TimeSpan ExitHoldDuration = TimeSpan.FromSeconds(2);
		private static readonly TimeSpan ExitTapInterval = TimeSpan.FromSeconds(2);
		private CancellationTokenSource? exitHoldCancellation;
		private DateTimeOffset lastExitTap;
		private int exitTapCount;

		/// <summary>Identifies the <see cref="PreviewContent"/> bindable property.</summary>
		public static readonly BindableProperty PreviewContentProperty = BindableProperty.Create(
			nameof(PreviewContent),
			typeof(View),
			typeof(FullscreenPreviewHost),
			propertyChanged: OnPreviewContentChanged);

		/// <summary>Initializes a new instance of the <see cref="FullscreenPreviewHost"/> class.</summary>
		public FullscreenPreviewHost()
		{
			this.InitializeComponent();
			this.ExitGestureArea.Clicked += this.OnExitGestureClicked;
			this.ExitGestureArea.Pressed += this.OnExitHoldPressed;
			this.ExitGestureArea.Released += this.OnExitHoldReleased;
		}

		/// <summary>Occurs when a hidden top-left exit gesture is recognized.</summary>
		public event EventHandler? ExitRequested;

		/// <summary>Gets or sets the production content rendered without Storybook layout styling.</summary>
		public View? PreviewContent
		{
			get => (View?)this.GetValue(PreviewContentProperty);
			set => this.SetValue(PreviewContentProperty, value);
		}

		private static void OnPreviewContentChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is FullscreenPreviewHost Host)
				Host.PreviewContentHost.Content = NewValue as View;
		}

		private void OnExitGestureClicked(object? Sender, EventArgs Args)
		{
			DateTimeOffset CurrentTap = DateTimeOffset.UtcNow;
			this.exitTapCount = CurrentTap - this.lastExitTap <= ExitTapInterval
				? this.exitTapCount + 1
				: 1;
			this.lastExitTap = CurrentTap;

			if (this.exitTapCount < 3)
				return;

			this.exitTapCount = 0;
			this.RequestExit();
		}

		private async void OnExitHoldPressed(object? Sender, EventArgs Args)
		{
			this.CancelExitHold();
			CancellationTokenSource HoldCancellation = new CancellationTokenSource();
			this.exitHoldCancellation = HoldCancellation;

			try
			{
				await Task.Delay(ExitHoldDuration, HoldCancellation.Token);
				if (ReferenceEquals(this.exitHoldCancellation, HoldCancellation))
				{
					this.exitHoldCancellation = null;
					this.RequestExit();
				}
			}
			catch (OperationCanceledException)
			{
				// Releasing or leaving the hidden gesture area cancels the hold.
			}
			finally
			{
				HoldCancellation.Dispose();
			}
		}

		private void OnExitHoldReleased(object? Sender, EventArgs Args)
		{
			this.CancelExitHold();
		}

		private void CancelExitHold()
		{
			CancellationTokenSource? HoldCancellation = this.exitHoldCancellation;
			this.exitHoldCancellation = null;
			HoldCancellation?.Cancel();
		}

		private void RequestExit()
		{
			this.CancelExitHold();
			this.ExitRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}
