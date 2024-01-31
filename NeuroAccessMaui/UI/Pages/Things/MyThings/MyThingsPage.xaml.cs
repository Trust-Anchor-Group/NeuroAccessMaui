using IdApp.Services.Navigation;
using Xamarin.Forms.Xaml;

namespace NeuroAccessMaui.UI.Pages.Things.MyThings
{
	/// <summary>
	/// A page that displays a list of the current user's things.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyThingsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyThingsPage"/> class.
		/// </summary>
		public MyThingsPage()
		{
			this.ViewModel = new MyThingsViewModel();

			this.InitializeComponent();
		}
	}
}
