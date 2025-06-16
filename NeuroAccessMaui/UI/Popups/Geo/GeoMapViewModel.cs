using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Geo;
using System;

namespace NeuroAccessMaui.UI.Popups.Password
{
    public partial class GeoMapViewModel : ReturningPopupViewModel<string>
    {
		
        /// <summary>
		/// Optional initial coordinate to center the map on.
		/// </summary>
		public GeoPosition? InitialCoordinate { get; }

        private GeoPosition? _selectedCoordinate;

        /// <summary>
        /// Constructor accepting optional latitude and longitude.
        /// </summary>
        /// <param name="Latitude">Initial latitude, if any.</param>
        /// <param name="Longitude">Initial longitude, if any.</param>
        public GeoMapViewModel(double? Latitude = null, double? Longitude = null)
        {
            if (Latitude.HasValue && Longitude.HasValue)
                InitialCoordinate = new GeoPosition(Latitude.Value, Longitude.Value);
        }

        /// <summary>
        /// Sets the user-selected coordinate.
        /// </summary>
        /// <param name="Latitude">Selected latitude</param>
        /// <param name="Longitude">Selected longitude</param>
        public void SetSelectedCoordinate(double Latitude, double Longitude)
        {

            _selectedCoordinate = new GeoPosition(Latitude, Longitude);
        }

        /// <summary>
        /// Returns the selected coordinate as "lat,lon" when submitting.
        /// </summary>
        [RelayCommand]
        public void Submit()
        {
            if (_selectedCoordinate is GeoPosition coord)
                result.SetResult($"{coord.Latitude},{coord.Longitude}");
            else
                result.SetResult(null);
        }
    }
}