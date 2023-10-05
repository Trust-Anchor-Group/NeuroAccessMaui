namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class CreateAccountView
{
	public static CreateAccountView Create()
	{
		return Create<CreateAccountView>();
	}

	public CreateAccountView(CreateAccountViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
	}
}
