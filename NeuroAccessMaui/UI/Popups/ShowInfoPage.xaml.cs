using CommunityToolkit.Mvvm.Input;
using Mopups.Services;

namespace NeuroAccessMaui.UI.Popups;

public partial class ShowInfoPage
{
	public double ViewWidth => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);
	//public double ViewHeight => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (1.0 / 2.0);

	public string InfoTitle { get; set; }
	public string InfoText { get; set; }

	public ShowInfoPage(string InfoTitle, string InfoText, ImageSource? Background = null) : base(Background)
	{
		this.InfoTitle = InfoTitle;
		this.InfoText = InfoText;

		this.InitializeComponent();
		this.BindingContext = this;
	}

	[RelayCommand]
	public async Task Close()
	{
		await MopupService.Instance.PopAsync();
	}
}
