using System.Collections.ObjectModel;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NeuroAccessMaui.UI.Controls.Geo;
using Waher.Runtime.Geo;


using Map = Mapsui.Map;                  // for Mapsui.Map

namespace NeuroAccessMaui.UI.Controls
{
	public partial class GeoMap : ContentView, IDisposable
	{
		readonly MapControl mapControl;

		public GeoMap()
		{
			this.mapControl = new MapControl();
			this.Content = this.mapControl;

			// 1) Create underlying Map and add OSM raster tile layer
			Map map = new Map();
			map.Layers.Add(OpenStreetMap.CreateTileLayer());                   // :contentReference[oaicite:4]{index=4}

			// 2) Register custom style renderer
			this.mapControl.Renderer.StyleRenderers
					  .Add(typeof(DataUriStyle), new DataUriStyleRenderer()); // :contentReference[oaicite:5]{index=5}

			this.mapControl.Map = map;

			// 3) React to bindable property changes
			PropertyChanged += this.OnPropertyChanged;
		}

		Map Map => this.mapControl.Map!;

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
			get => (GeoPosition)this.GetValue(CurrentCoordinateProperty);
			set => this.SetValue(CurrentCoordinateProperty, value);
		}

		void CenterOn(GeoPosition pos)
		{
			// Convert lon/lat → spherical mercator MPoint
			Mapsui.MPoint merc = SphericalMercator.FromLonLat(pos.Longitude, pos.Latitude)
										.ToMPoint();
			this.Map.Navigator.CenterOnAndZoomTo(merc, 10, 1000);
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
			get => (ObservableCollection<MapItem>)this.GetValue(ItemsProperty);
			set => this.SetValue(ItemsProperty, value);
		}

		static void OnItemsChanged(BindableObject Bindable, object OldVal, object NewVal)
		{
			if (Bindable is GeoMap GeoMap)
				GeoMap.RebuildItemsLayer();
		}

		void RebuildItemsLayer()
		{
			// A) Remove any existing ItemsLayer
			ILayer? old = this.Map.Layers.FirstOrDefault(l => l.Name == "ItemsLayer");
			if (old is not null)
				this.Map.Layers.Remove(old);

			// B) Build new MemoryLayer with a MemoryProvider
			List<PointFeature> features = this.Items.Select(item =>
			{
				// Convert each GeoPosition → MPoint → PointFeature
				(double x, double y) worldPoint = SphericalMercator
					.FromLonLat(item.Location.Longitude, item.Location.Latitude);
				PointFeature pf = new PointFeature(worldPoint.ToMPoint());
				// Tag your data URI for renderer lookup
				pf["DataUri"] = item.Uri.ToString();
				// Attach placeholder style
				pf.Styles.Add(new DataUriStyle());
				return pf;
			}).ToList();

			MemoryLayer layer = new MemoryLayer
			{
				Name = "ItemsLayer",
				IsMapInfoLayer = true,
				Features = features
			};
			this.Map.Layers.Add(layer);
		}

		#endregion

		#region Locks

		public static readonly BindableProperty LockRotationProperty =
			BindableProperty.Create(nameof(LockRotation), typeof(bool), typeof(GeoMap), false);
		public static readonly BindableProperty LockPanProperty =
			BindableProperty.Create(nameof(LockPan), typeof(bool), typeof(GeoMap), false);
		public static readonly BindableProperty LockZoomProperty =
			BindableProperty.Create(nameof(LockZoom), typeof(bool), typeof(GeoMap), false);

		public bool LockRotation { get => (bool)this.GetValue(LockRotationProperty); set => this.SetValue(LockRotationProperty, value); }
		public bool LockPan { get => (bool)this.GetValue(LockPanProperty); set => this.SetValue(LockPanProperty, value); }
		public bool LockZoom { get => (bool)this.GetValue(LockZoomProperty); set => this.SetValue(LockZoomProperty, value); }

		void ApplyLocks()
		{
			this.Map.Navigator.RotationLock = this.LockRotation;
			this.Map.Navigator.PanLock = this.LockPan;
			this.Map.Navigator.ZoomLock = this.LockZoom;                         // :contentReference[oaicite:11]{index=11}
		}

		#endregion

		void OnPropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs Event)
		{
			switch (Event.PropertyName)
			{
				case nameof(this.CurrentCoordinate):
					this.CenterOn(this.CurrentCoordinate);
					break;
				case nameof(this.Items):
					this.RebuildItemsLayer();
					break;
				case nameof(this.LockRotation):
				case nameof(this.LockPan):
				case nameof(this.LockZoom):
					this.ApplyLocks();
					break;
			}
		}

		private bool disposed;

		/// <summary>  
		/// Dispose the underlying MapControl when the GeoMap is disposed.  
		/// </summary>  
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>  
		/// Dispose pattern implementation.  
		/// </summary>  
		/// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>  
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// Dispose managed resources  
				PropertyChanged -= this.OnPropertyChanged;
				this.mapControl.Dispose();
			}

			// Clean up unmanaged resources here if any  

			this.disposed = true;
		}

		~GeoMap()
		{
			this.Dispose(false);
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
