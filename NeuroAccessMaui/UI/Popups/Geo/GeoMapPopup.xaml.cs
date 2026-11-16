using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;

namespace NeuroAccessMaui.UI.Popups.Geo
{
	/// <summary>
	/// A map where the user picks a place by moving the map under a pin.
	/// </summary>
	public partial class GeoMapPopup : BasePopup
	{
		/// <summary>
		/// How far in to zoom when the map opens on a place, in map resolution.
		/// </summary>
		private const double placeResolution = 10;

		/// <summary>
		/// Creates the map where the user picks a place.
		/// </summary>
		/// <param name="ViewModel">The view model, which gets the picked place.</param>
		public GeoMapPopup(GeoMapViewModel ViewModel)
		{
			this.InitializeComponent();

			this.GeoMapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
			this.GeoMapControl.Map.Navigator.RotationLock = true;

			ViewModel.GetCenter = () => this.GeoMapControl.Map?.Navigator.Viewport is Mapsui.Viewport Viewport
				? new MPoint(Viewport.CenterX, Viewport.CenterY)
				: null;

			this.BindingContext = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnAppearingAsync()
		{
			if (this.BindingContext is GeoMapViewModel ViewModel && ViewModel.InitialPosition is not null)
			{
				MPoint Point = SphericalMercator.FromLonLat(ViewModel.InitialPosition.Longitude, ViewModel.InitialPosition.Latitude).ToMPoint();
				this.GeoMapControl.Map.Navigator.CenterOnAndZoomTo(Point, placeResolution);
			}

			return base.OnAppearingAsync();
		}
	}
}
