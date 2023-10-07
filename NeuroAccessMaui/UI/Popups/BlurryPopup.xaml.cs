namespace NeuroAccessMaui.UI.Popups;

/// <summary>
/// Creates Popup with blurred background
/// </summary>
public partial class BlurryPopup
{
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
