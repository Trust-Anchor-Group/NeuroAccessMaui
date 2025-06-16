using System.Collections.ObjectModel;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Waher.Runtime.Geo;


using Map = Mapsui.Map;                  // for Mapsui.Map

namespace NeuroAccessMaui.UI.Controls.Geo
{
	public partial class GeoMap : ContentView, IDisposable
	{
		readonly MapControl mapControl;

		public GeoMap()
		{
			mapControl = new MapControl();
			Content = mapControl;

			// 1) Create underlying Map and add OSM raster tile layer
			var map = new Map();
			map.Layers.Add(OpenStreetMap.CreateTileLayer());                   // :contentReference[oaicite:4]{index=4}

			// 2) Register custom style renderer
			mapControl.Renderer.StyleRenderers
					  .Add(typeof(DataUriStyle), new DataUriStyleRenderer()); // :contentReference[oaicite:5]{index=5}

			mapControl.Map = map;

			// 3) React to bindable property changes
			PropertyChanged += OnPropertyChanged;
		}

		Map Map => mapControl.Map!;

		#region CurrentCoordinate

		public static readonly BindableProperty CurrentCoordinateProperty =
			BindableProperty.Create(
				nameof(CurrentCoordinate),
				typeof(GeoPosition),
				typeof(GeoMap),
				default(GeoPosition),
				BindingMode.TwoWay);

		public GeoPosition CurrentCoordinate
		{
			get => (GeoPosition)GetValue(CurrentCoordinateProperty);
			set => SetValue(CurrentCoordinateProperty, value);
		}

		void CenterOn(GeoPosition pos)
		{
			// Convert lon/lat → spherical mercator MPoint
			var merc = SphericalMercator.FromLonLat(pos.Longitude, pos.Latitude)
										.ToMPoint();                     // :contentReference[oaicite:6]{index=6}
			Map.Navigator.CenterOn(merc);                                     // :contentReference[oaicite:7]{index=7}
		}

		#endregion

		#region Items

		public static readonly BindableProperty ItemsProperty =
			BindableProperty.Create(
				nameof(Items),
				typeof(ObservableCollection<MapItem>),
				typeof(GeoMap),
				new ObservableCollection<MapItem>(),
				propertyChanged: OnItemsChanged);

		public ObservableCollection<MapItem> Items
		{
			get => (ObservableCollection<MapItem>)GetValue(ItemsProperty);
			set => SetValue(ItemsProperty, value);
		}

		static void OnItemsChanged(BindableObject b, object oldVal, object newVal)
		{
			if (b is GeoMap gm)
				gm.RebuildItemsLayer();
		}

		void RebuildItemsLayer()
		{
			// A) Remove any existing ItemsLayer
			var old = Map.Layers.FirstOrDefault(l => l.Name == "ItemsLayer");
			if (old != null)
				Map.Layers.Remove(old);                                       

			// B) Build new MemoryLayer with a MemoryProvider
			var features = Items.Select(item =>
			{
				// Convert each GeoPosition → MPoint → PointFeature
				var worldPoint = SphericalMercator
					.FromLonLat(item.Location.Longitude, item.Location.Latitude);
				var pf = new PointFeature(worldPoint.ToMPoint());                     
																		   // Tag your data URI for renderer lookup
				pf["DataUri"] = item.Uri.ToString();
				// Attach placeholder style
				pf.Styles.Add(new DataUriStyle());
				return pf;
			}).ToList();

			var layer = new MemoryLayer
			{
				Name = "ItemsLayer",
				IsMapInfoLayer = true,
				Features = features
			};
			Map.Layers.Add(layer);
		}

		#endregion

		#region Locks

		public static readonly BindableProperty LockRotationProperty =
			BindableProperty.Create(nameof(LockRotation), typeof(bool), typeof(GeoMap), false);
		public static readonly BindableProperty LockPanProperty =
			BindableProperty.Create(nameof(LockPan), typeof(bool), typeof(GeoMap), false);
		public static readonly BindableProperty LockZoomProperty =
			BindableProperty.Create(nameof(LockZoom), typeof(bool), typeof(GeoMap), false);

		public bool LockRotation { get => (bool)GetValue(LockRotationProperty); set => SetValue(LockRotationProperty, value); }
		public bool LockPan { get => (bool)GetValue(LockPanProperty); set => SetValue(LockPanProperty, value); }
		public bool LockZoom { get => (bool)GetValue(LockZoomProperty); set => SetValue(LockZoomProperty, value); }

		void ApplyLocks()
		{
			Map.Navigator.RotationLock = LockRotation;
			Map.Navigator.PanLock = LockPan;
			Map.Navigator.ZoomLock = LockZoom;                         // :contentReference[oaicite:11]{index=11}
		}

		#endregion

		void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(CurrentCoordinate):
					CenterOn(CurrentCoordinate);
					break;
				case nameof(Items):
					RebuildItemsLayer();
					break;
				case nameof(LockRotation):
				case nameof(LockPan):
				case nameof(LockZoom):
					ApplyLocks();
					break;
			}
		}

		private bool _disposed;

		/// <summary>  
		/// Dispose the underlying MapControl when the GeoMap is disposed.  
		/// </summary>  
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>  
		/// Dispose pattern implementation.  
		/// </summary>  
		/// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>  
		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				// Dispose managed resources  
				PropertyChanged -= OnPropertyChanged;
				mapControl.Map = null;
				mapControl.Dispose();
			}

			// Clean up unmanaged resources here if any  

			_disposed = true;
		}

		~GeoMap()
		{
			Dispose(false);
		}
	}

	/// <summary>
	/// Holds the position and a Uri to arbitrary data to render later.
	/// </summary>
	public struct MapItem
	{
		public GeoPosition Location { get; set; }
		public Uri Uri { get; set; }
	}
}
