using CommunityToolkit.Mvvm.Input;
using Mopups.Services;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class ShowInfoPopup
	{
		public string InfoTitle { get; set; }
		public string InfoText { get; set; }

		public ShowInfoPopup(string InfoTitle, string InfoText)
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
