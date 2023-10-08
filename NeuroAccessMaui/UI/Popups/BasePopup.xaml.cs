namespace NeuroAccessMaui.UI.Popups;

/// <summary>
/// Creates a base popup 
/// </summary>
public partial class BasePopup
{
	public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);
	public virtual double MaximumViewHeightRequest => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

	protected BasePopup(ImageSource? Background)
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
