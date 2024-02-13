namespace NeuroAccessMaui.UI.Popups.Pin
{
	/// <summary>
	/// Page letting the user enter a PIN to be verified with the PIN defined by the user earlier.
	/// </summary>
	public partial class CheckPinPopup
	{
		private readonly CheckPinViewModel viewModel;

		/// <summary>
		/// Page letting the user enter a PIN to be verified with the PIN defined by the user earlier.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		/// <param name="Background">Optional background.</param>
		public CheckPinPopup(CheckPinViewModel ViewModel, ImageSource? Background = null)
			: base(Background)
		{
			this.InitializeComponent();
			this.BindingContext = this.viewModel = ViewModel;
		}

		protected override void OnAppearing()
		{
			this.PinEntry.Focus();
			base.OnAppearing();
		}

		/// <inheritdoc/>
		protected override void OnDisappearing()
		{
			this.viewModel.Close();
			base.OnDisappearing();
		}
	}
}
