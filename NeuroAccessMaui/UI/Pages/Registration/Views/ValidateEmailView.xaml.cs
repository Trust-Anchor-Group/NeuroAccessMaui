namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ValidateEmailView
{
	public static ValidateEmailView? Create()
	{
		return Create<ValidateEmailView>();
	}

	public ValidateEmailView(ValidateEmailViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
	}
}
