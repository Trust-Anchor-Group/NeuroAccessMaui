using NeuroAccessMaui.Animations;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A list card with placeholder rows, shown in place of a list while it loads. The rows pulse softly and a line runs
	/// along the card's top edge. When reduced motion is enabled, nothing moves.
	/// </summary>
	public partial class ListCardPlaceholder : ContentView
	{
		private const string pulseAnimationName = "ListCardPlaceholderPulse";
		private const uint pulseDurationMs = 1800;
		private const double dimmedOpacity = 0.55;

		/// <summary>
		/// Identifies the <see cref="ShowsValue"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ShowsValueProperty = BindableProperty.Create(
			nameof(ShowsValue),
			typeof(bool),
			typeof(ListCardPlaceholder),
			false);

		/// <summary>
		/// Initializes a new instance of the <see cref="ListCardPlaceholder"/> class.
		/// </summary>
		public ListCardPlaceholder()
		{
			this.InitializeComponent();

			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;
			this.PropertyChanged += this.Placeholder_PropertyChanged;
		}

		/// <summary>
		/// If the placeholder rows have a value at their end, as token rows do.
		/// </summary>
		public bool ShowsValue
		{
			get => (bool)this.GetValue(ShowsValueProperty);
			set => this.SetValue(ShowsValueProperty, value);
		}

		private void OnLoaded(object? Sender, EventArgs e)
		{
			this.UpdatePulse();
		}

		private void OnUnloaded(object? Sender, EventArgs e)
		{
			this.StopPulse();
		}

		private void Placeholder_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == IsVisibleProperty.PropertyName)
				this.UpdatePulse();
		}

		private void UpdatePulse()
		{
			if (!this.IsVisible || this.Handler is null || MotionPreferences.ReduceMotion)
			{
				this.StopPulse();
				return;
			}

			if (this.Rows.AnimationIsRunning(pulseAnimationName))
				return;

			Animation Pulse = new()
			{
				{ 0, 0.5, new Animation(Value => this.Rows.Opacity = Value, 1, dimmedOpacity, Easing.SinInOut) },
				{ 0.5, 1, new Animation(Value => this.Rows.Opacity = Value, dimmedOpacity, 1, Easing.SinInOut) }
			};

			Pulse.Commit(this.Rows, pulseAnimationName, 16, pulseDurationMs, repeat: () => this.IsVisible && !MotionPreferences.ReduceMotion);
		}

		private void StopPulse()
		{
			this.Rows.AbortAnimation(pulseAnimationName);
			this.Rows.Opacity = 1;
		}
	}
}
