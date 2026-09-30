using System.Windows.Input;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Displays the selected phone country calling code and exposes the country selection command.
	/// </summary>
	public partial class PhoneCountryCodePickerView : ContentView
	{
		/// <summary>
		/// Identifies the <see cref="DialCode"/> bindable property.
		/// </summary>
		public static readonly BindableProperty DialCodeProperty = BindableProperty.Create(
			nameof(DialCode),
			typeof(string),
			typeof(PhoneCountryCodePickerView),
			string.Empty);

		/// <summary>
		/// Identifies the <see cref="SelectCommand"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
			nameof(SelectCommand),
			typeof(ICommand),
			typeof(PhoneCountryCodePickerView));

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneCountryCodePickerView"/> class.
		/// </summary>
		public PhoneCountryCodePickerView()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the selected country's calling code without the leading plus sign.
		/// </summary>
		public string DialCode
		{
			get => (string)this.GetValue(DialCodeProperty);
			set => this.SetValue(DialCodeProperty, value);
		}

		/// <summary>
		/// Gets or sets the command that opens country selection.
		/// </summary>
		public ICommand? SelectCommand
		{
			get => (ICommand?)this.GetValue(SelectCommandProperty);
			set => this.SetValue(SelectCommandProperty, value);
		}
	}
}
