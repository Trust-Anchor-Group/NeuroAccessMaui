using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Geo;
using System;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Password
{
	public partial class GeoMapViewModel : ReturningPopupViewModel<string>
	{
		// Expose the MapControl so we can read the viewport center
		public MapControl? MapControl { get; set; }

		/// <summary>
		/// Optional initial coordinate to center the map on.
		/// </summary>
		public GeoPosition? InitialCoordinate { get; }

		public GeoMapViewModel(double? Latitude = null, double? Longitude = null)
		{
			if (Latitude.HasValue && Longitude.HasValue)
				InitialCoordinate = new GeoPosition(Latitude.Value, Longitude.Value);
		}

		[RelayCommand]
		public async Task Confirm()
		{
			if (MapControl?.Map?.Navigator.Viewport is null)
			{
				this.TrySetResult(null);
				await ServiceRef.PopupService.PopAsync();
				return;
			}

			var vp = MapControl.Map.Navigator.Viewport;
			// Convert world (Mercator) to lon/lat
			var (lon, lat) = SphericalMercator.ToLonLat(vp.CenterX, vp.CenterY);
			this.TrySetResult($"{lon},{lat}");
			await ServiceRef.PopupService.PopAsync();
		}
	}
}
