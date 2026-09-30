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

		/// <summary>Initializes a new instance of the <see cref="OnboardingPreviewHost"/> class.</summary>
		public OnboardingPreviewHost()
		{
			this.InitializeComponent();
		}

		/// <summary>Gets or sets the production onboarding step rendered inside its normal visual context.</summary>
		public View? PreviewContent
		{
			get => (View?)this.GetValue(PreviewContentProperty);
			set => this.SetValue(PreviewContentProperty, value);
		}

		private static void OnPreviewContentChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is OnboardingPreviewHost Host)
				Host.PreviewContentHost.Content = NewValue as View;
		}
	}
}
