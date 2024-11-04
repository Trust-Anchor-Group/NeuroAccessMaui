namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class NameEntryView
	{
		public static NameEntryView Create()
		{
			return Create<NameEntryView>();
		}

		public NameEntryView(NameEntryViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
