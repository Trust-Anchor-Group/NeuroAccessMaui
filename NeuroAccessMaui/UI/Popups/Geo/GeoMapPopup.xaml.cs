using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// A Popup letting the user enter a password to be verified with the password defined by the user earlier.
	/// </summary>
	public partial class GeoMapPopup
	{
        public GeoMapPopup(double? latitude = null, double? longitude = null)
        {
            InitializeComponent();

            // Create and bind ViewModel with optional initial coordinate
            var viewModel = new GeoMapViewModel(latitude, longitude);
            BindingContext = viewModel;

            // Add OSM tile layer
            var tileLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
            GeoMapControl.Map?.Layers.Add(tileLayer);

            // If initial coordinate provided, center map and set zoom
            if (viewModel.InitialCoordinate is { } init)
            {
                GeoMapControl.Map?.Navigator.CenterOn(init.Longitude, init.Latitude);
                GeoMapControl.Map?.Navigator.ZoomTo(10000); // adjust resolution as needed
            }

            // Subscribe to map click
            GeoMapControl. += GeoMapControl_MapClicked;
        }

        private async void GeoMapControl_MapClicked(object sender, MapClickedEventArgs e)
        {
            // Get world position from event
            var world = e.WorldPosition;
            double lat = world.Y;
            double lon = world.X;

            if (BindingContext is GeoMapViewModel viewModel)
            {
                viewModel.SetSelectedCoordinate(lat, lon);
                viewModel.Submit();
            }

        }
    }
}
