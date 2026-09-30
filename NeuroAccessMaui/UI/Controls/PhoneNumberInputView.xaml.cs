namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Provides the production phone-number entry and validation behaviors as a reusable control.
	/// </summary>
	public partial class PhoneNumberInputView : ContentView
	{
		/// <summary>Identifies the <see cref="PhoneNumber"/> bindable property.</summary>
		public static readonly BindableProperty PhoneNumberProperty = BindableProperty.Create(
			nameof(PhoneNumber), typeof(string), typeof(PhoneNumberInputView), string.Empty, BindingMode.TwoWay);

		/// <summary>Identifies the <see cref="TypeIsValid"/> bindable property.</summary>
		public static readonly BindableProperty TypeIsValidProperty = BindableProperty.Create(
			nameof(TypeIsValid), typeof(bool), typeof(PhoneNumberInputView), true, BindingMode.TwoWay);

		/// <summary>Identifies the <see cref="LengthIsValid"/> bindable property.</summary>
		public static readonly BindableProperty LengthIsValidProperty = BindableProperty.Create(
			nameof(LengthIsValid), typeof(bool), typeof(PhoneNumberInputView), true, BindingMode.TwoWay);

		/// <summary>Identifies the <see cref="IsReadOnly"/> bindable property.</summary>
		public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
			nameof(IsReadOnly), typeof(bool), typeof(PhoneNumberInputView), false);

		/// <summary>Identifies the <see cref="IsInputEnabled"/> bindable property.</summary>
		public static readonly BindableProperty IsInputEnabledProperty = BindableProperty.Create(
			nameof(IsInputEnabled), typeof(bool), typeof(PhoneNumberInputView), true);

		/// <summary>Identifies the <see cref="ShouldFocus"/> bindable property.</summary>
		public static readonly BindableProperty ShouldFocusProperty = BindableProperty.Create(
			nameof(ShouldFocus), typeof(bool), typeof(PhoneNumberInputView), false, propertyChanged: OnShouldFocusChanged);

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneNumberInputView"/> class.
		/// </summary>
		public PhoneNumberInputView()
		{
			this.InitializeComponent();
			this.Loaded += this.OnLoaded;
		}

		/// <summary>Gets or sets the local phone number.</summary>
		public string PhoneNumber
		{
			get => (string)this.GetValue(PhoneNumberProperty);
			set => this.SetValue(PhoneNumberProperty, value);
		}

		/// <summary>Gets or sets whether the phone number contains only accepted characters.</summary>
		public bool TypeIsValid
		{
			get => (bool)this.GetValue(TypeIsValidProperty);
			set => this.SetValue(TypeIsValidProperty, value);
		}

		/// <summary>Gets or sets whether the phone number has an accepted length.</summary>
		public bool LengthIsValid
		{
			get => (bool)this.GetValue(LengthIsValidProperty);
			set => this.SetValue(LengthIsValidProperty, value);
		}

		/// <summary>Gets or sets whether the phone number cannot be edited.</summary>
		public bool IsReadOnly
		{
			get => (bool)this.GetValue(IsReadOnlyProperty);
			set => this.SetValue(IsReadOnlyProperty, value);
		}

		/// <summary>Gets or sets whether the entry accepts interaction.</summary>
		public bool IsInputEnabled
		{
			get => (bool)this.GetValue(IsInputEnabledProperty);
			set => this.SetValue(IsInputEnabledProperty, value);
		}

		/// <summary>Gets or sets whether the entry should request focus when displayed.</summary>
		public bool ShouldFocus
		{
			get => (bool)this.GetValue(ShouldFocusProperty);
			set => this.SetValue(ShouldFocusProperty, value);
		}

		private static void OnShouldFocusChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			PhoneNumberInputView Input = (PhoneNumberInputView)Bindable;
			if (NewValue is true && Input.IsLoaded)
				Input.PhoneEntry.Focus();
		}

		private void OnLoaded(object? Sender, EventArgs E)
		{
			if (this.ShouldFocus)
				this.PhoneEntry.Focus();
		}
	}
}
