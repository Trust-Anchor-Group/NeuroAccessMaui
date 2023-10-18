namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class DefinePinView
{
	public static DefinePinView Create()
	{
		return Create<DefinePinView>();
	}

	public DefinePinView(DefinePinViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
	}
}
