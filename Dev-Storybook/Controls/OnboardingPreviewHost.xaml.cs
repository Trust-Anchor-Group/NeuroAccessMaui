using DevStorybook.Services;

namespace DevStorybook.Controls
{
	/// <summary>
	/// Supplies the production onboarding background and content margins required by onboarding step views.
	/// </summary>
	public partial class OnboardingPreviewHost : ContentView
	{
		/// <summary>Identifies the <see cref="PreviewContent"/> bindable property.</summary>
		public static readonly BindableProperty PreviewContentProperty = BindableProperty.Create(
			nameof(PreviewContent),
			typeof(View),
			typeof(OnboardingPreviewHost),
			propertyChanged: OnPreviewContentChanged);

		/// <summary>Identifies the <see cref="UseSystemBottomInset"/> bindable property.</summary>
		public static readonly BindableProperty UseSystemBottomInsetProperty = BindableProperty.Create(
			nameof(UseSystemBottomInset),
			typeof(bool),
			typeof(OnboardingPreviewHost),
			true,
			propertyChanged: OnUseSystemBottomInsetChanged);

		/// <summary>Initializes a new instance of the <see cref="OnboardingPreviewHost"/> class.</summary>
		public OnboardingPreviewHost()
		{
			this.InitializeComponent();
			this.Loaded += this.OnHostLoaded;
			this.SizeChanged += this.OnHostSizeChanged;
		}

		/// <summary>Gets or sets the production onboarding step rendered inside its normal visual context.</summary>
		public View? PreviewContent
		{
			get => (View?)this.GetValue(PreviewContentProperty);
			set => this.SetValue(PreviewContentProperty, value);
		}

		/// <summary>
		/// Gets or sets whether this host reserves the Android system navigation area.
		/// </summary>
		public bool UseSystemBottomInset
		{
			get => (bool)this.GetValue(UseSystemBottomInsetProperty);
			set => this.SetValue(UseSystemBottomInsetProperty, value);
		}

		private static void OnPreviewContentChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is OnboardingPreviewHost Host)
				Host.PreviewContentHost.Content = NewValue as View;
		}

		private static void OnUseSystemBottomInsetChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is OnboardingPreviewHost Host)
				Host.ApplySystemBottomInset();
		}

		private void OnHostLoaded(object? Sender, EventArgs Args)
		{
			this.ApplySystemBottomInset();
		}

		private void OnHostSizeChanged(object? Sender, EventArgs Args)
		{
			this.ApplySystemBottomInset();
			double ViewportHeight = Math.Max(0.0, this.PreviewScrollView.Height);
			if (ViewportHeight > 0.0)
				this.PreviewContentHost.MinimumHeightRequest = ViewportHeight;
		}

		private void ApplySystemBottomInset()
		{
			double BottomInset = this.UseSystemBottomInset
				? StorybookSystemInsets.GetBottomInset()
				: 0.0;
			this.PreviewScrollView.Padding = new Thickness(0.0, 0.0, 0.0, BottomInset);
		}
	}
}
