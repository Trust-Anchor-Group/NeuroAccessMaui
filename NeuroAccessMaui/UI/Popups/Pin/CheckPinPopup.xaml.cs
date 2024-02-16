namespace NeuroAccessMaui.UI.Popups.Pin
{
	/// <summary>
	/// A Popup letting the user enter a PIN to be verified with the PIN defined by the user earlier.
	/// </summary>
	public partial class CheckPinPopup
	{

		public CheckPinPopup()
			: base(null)
		{
			this.InitializeComponent();
		}

		protected override void OnAppearing()
		{
			this.PinEntry.Focus();
			base.OnAppearing();
		}
	}
}
