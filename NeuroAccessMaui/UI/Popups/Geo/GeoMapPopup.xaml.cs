using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using NeuroAccessMaui.Services;
using Waher.Runtime.Geo;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// A Popup letting the user enter a password to be verified with the password defined by the user earlier.
	/// </summary>
	public partial class GeoMapPopup
	{
		public GeoMapPopup(GeoMapViewModel ViewModel)
		{
			InitializeComponent();

		
			ViewModel.MapControl = GeoMapControl;
			BindingContext = ViewModel;
			GeoMapControl.Map.Navigator.RotationLock = true;
			// Add OpenStreetMap tile layer
			GeoMapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());

		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (this.BindingContext is GeoMapViewModel Vm && Vm.InitialCoordinate is not null)
			{
				var mercator = SphericalMercator.FromLonLat(Vm.InitialCoordinate.Longitude, Vm.InitialCoordinate.Latitude).ToMPoint();
				GeoMapControl.Map?.Navigator.CenterOn(mercator);
				GeoMapControl.Map?.Navigator.ZoomTo(1000);
			}
		}

	}
}
