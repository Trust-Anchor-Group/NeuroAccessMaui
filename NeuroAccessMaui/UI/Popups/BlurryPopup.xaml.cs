namespace NeuroAccessMaui.UI.Popups;

/// <summary>
/// Creates Popup with blurred background
/// </summary>
public partial class BlurryPopup
{
	public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);
	public virtual double MaximumViewHeightRequest => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

	protected BlurryPopup(ImageSource? Background)
	{
		this.InitializeComponent();

		if (Background is not null)
		{
			this.BackgroundImageSource = Background;
		}
		else
		{
			this.BackgroundColor = Color.FromInt(0x20000000);
		}
	}
}
