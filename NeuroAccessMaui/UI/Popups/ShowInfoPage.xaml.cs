using CommunityToolkit.Mvvm.Input;
using Mopups.Services;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class ShowInfoPage
	{
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
}
