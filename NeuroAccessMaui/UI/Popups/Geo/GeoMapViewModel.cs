using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Projections;
using NeuroAccessMaui.Services;
using Waher.Runtime.Geo;

namespace NeuroAccessMaui.UI.Popups.Geo
{
	/// <summary>
	/// View model for the map where the user picks a place. The place is the point under the pin in the middle of the map.
	/// </summary>
	public partial class GeoMapViewModel : ReturningPopupViewModel<GeoPosition>
	{
		/// <summary>
		/// View model for the map where the user picks a place.
		/// </summary>
		/// <param name="InitialPosition">The place to show first, or null to show the allowed area, or the whole world.</param>
		/// <param name="AreaMin">One corner of the area the place must be in, or null.</param>
		/// <param name="AreaMax">The opposite corner of the area the place must be in, or null.</param>
		public GeoMapViewModel(GeoPosition? InitialPosition, GeoPosition? AreaMin = null, GeoPosition? AreaMax = null)
		{
			this.InitialPosition = InitialPosition;
			this.AreaMin = AreaMin;
			this.AreaMax = AreaMax;
		}

		/// <summary>
		/// One corner of the area the place must be in, if the contract limits it.
		/// </summary>
		public GeoPosition? AreaMin { get; }

		/// <summary>
		/// The opposite corner of the area the place must be in, if the contract limits it.
		/// </summary>
		public GeoPosition? AreaMax { get; }

		/// <summary>
		/// The place to show first, if any.
		/// </summary>
		public GeoPosition? InitialPosition { get; }

		/// <summary>
		/// Gets the point in the middle of the map, in world coordinates. Set by the popup.
		/// </summary>
		public Func<MPoint?>? GetCenter { get; set; }

		/// <summary>
		/// Returns the place under the pin, and closes the map.
		/// </summary>
		[RelayCommand]
		private async Task ConfirmAsync()
		{
			MPoint? Center = this.GetCenter?.Invoke();

			if (Center is null)
				this.TrySetResult(null);
			else
			{
				(double Longitude, double Latitude) = SphericalMercator.ToLonLat(Center.X, Center.Y);
				this.TrySetResult(new GeoPosition(Latitude, Longitude));
			}

			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Closes the map without a place.
		/// </summary>
		[RelayCommand]
		private async Task CancelAsync()
		{
			this.TrySetResult(null);
			await ServiceRef.PopupService.PopAsync();
		}
	}
}
