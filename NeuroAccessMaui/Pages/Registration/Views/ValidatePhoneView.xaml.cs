namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ValidatePhoneView
{
	public static ValidatePhoneView? Create()
	{
		return Create<ValidatePhoneView>();
	}

	public ValidatePhoneView(ValidatePhoneViewModel ViewModel)
	{
		this.InitializeComponent();
		this.InitializeObject(ViewModel);
	}
}
