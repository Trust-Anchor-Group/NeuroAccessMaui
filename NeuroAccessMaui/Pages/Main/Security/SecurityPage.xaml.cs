namespace NeuroAccessMaui.Pages.Main.Security;

/// <summary>
/// A page ...
/// </summary>
public partial class SecurityPage
{
	/// <summary>
	/// Creates a new instance of the <see cref="SecurityPage"/> class.
	/// </summary>
	public SecurityPage(SecurityViewModel ViewModel)
	{
		this.InitializeComponent();
		this.BindingContext = ViewModel;
	}
}
