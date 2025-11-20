using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Things.ViewThing;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor
{
	/// <summary>
	/// A page that displays sensor data from a sensor.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ReadSensorPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ReadSensorPage"/> class.
		/// </summary>
		public ReadSensorPage()
		{
			this.ContentPageModel = new ReadSensorViewModel(ServiceRef.NavigationService.PopLatestArgs<ViewThingNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
