using System.Threading.Tasks;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using NeuroAccessMaui.Services;
using Waher.Runtime.Geo;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// Displays an interactive map to pick a geographic coordinate.
	/// </summary>
	public partial class GeoMapPopup : SimplePopup
	{
		public GeoMapPopup(GeoMapViewModel viewModel)
		{
			InitializeComponent();
			viewModel.MapControl = this.GeoMapControl;
			this.BindingContext = viewModel;
			this.GeoMapControl.Map.Navigator.RotationLock = true;
			this.GeoMapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
		}

		public override Task OnAppearingAsync()
		{
			if (this.BindingContext is GeoMapViewModel viewModel && viewModel.InitialCoordinate is not null)
			{
				MPoint mercator = SphericalMercator.FromLonLat(viewModel.InitialCoordinate.Longitude, viewModel.InitialCoordinate.Latitude).ToMPoint();
				this.GeoMapControl.Map?.Navigator.CenterOn(mercator);
				this.GeoMapControl.Map?.Navigator.ZoomTo(1000);
			}
			return base.OnAppearingAsync();
		}
	}
}
