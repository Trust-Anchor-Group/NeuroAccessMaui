using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using Waher.Runtime.Geo;
using Map = Mapsui.Map;
using MauiColor = Microsoft.Maui.Graphics.Color;
using Polygon = NetTopologySuite.Geometries.Polygon;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A small OpenStreetMap that shows a place with a pin, and the area the contract allows as a dashed outline. It can't
	/// be moved or zoomed; taps go to the view around it.
	/// </summary>
	public class GeoMapPreview : ContentView, IDisposable
	{
		/// <summary>
		/// How far in the map is zoomed on a place, in metres per pixel.
		/// </summary>
		private const double placeResolution = 10;

		/// <summary>
		/// The place to show, or null.
		/// </summary>
		public static readonly BindableProperty PositionProperty =
			BindableProperty.Create(nameof(Position), typeof(GeoPosition), typeof(GeoMapPreview), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((GeoMapPreview)Bindable).UpdateMap());

		/// <summary>
		/// One corner of the allowed area, or null.
		/// </summary>
		public static readonly BindableProperty AreaMinProperty =
			BindableProperty.Create(nameof(AreaMin), typeof(GeoPosition), typeof(GeoMapPreview), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((GeoMapPreview)Bindable).UpdateMap());

		/// <summary>
		/// The opposite corner of the allowed area, or null.
		/// </summary>
		public static readonly BindableProperty AreaMaxProperty =
			BindableProperty.Create(nameof(AreaMax), typeof(GeoPosition), typeof(GeoMapPreview), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((GeoMapPreview)Bindable).UpdateMap());

		/// <summary>
		/// The size of the pin.
		/// </summary>
		public static readonly BindableProperty PinSizeProperty =
			BindableProperty.Create(nameof(PinSize), typeof(double), typeof(GeoMapPreview), 34.0,
				propertyChanged: (Bindable, OldValue, NewValue) => ((GeoMapPreview)Bindable).UpdatePinSize());

		private readonly MapControl mapControl;
		private readonly MemoryLayer areaLayer;
		private readonly SvgView pin;
		private bool isDisposed;

		/// <summary>
		/// Creates the map preview.
		/// </summary>
		public GeoMapPreview()
		{
			Map Map = new();
			Map.Layers.Add(OpenStreetMap.CreateTileLayer());
			Map.Navigator.RotationLock = true;
			Map.Navigator.PanLock = true;
			Map.Navigator.ZoomLock = true;

			this.areaLayer = new MemoryLayer { Name = "Area", Style = null };
			Map.Layers.Add(this.areaLayer);

			this.mapControl = new MapControl
			{
				Map = Map,
				InputTransparent = true
			};
			this.mapControl.SizeChanged += (Sender, Args) => this.UpdateMap();

			// The pin's tip is at the middle of the map.
			this.pin = new SvgView
			{
				Source = "kyc_location.svg",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				InputTransparent = true
			};
			this.pin.SetDynamicResource(SvgView.TintColorProperty, "ButtonAccessPrimarybgWL");
			this.UpdatePinSize();

			this.InputTransparent = true;
			this.Content = new Grid { Children = { this.mapControl, this.pin } };
		}

		/// <summary>
		/// Releases the map.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the map.
		/// </summary>
		/// <param name="Disposing">If called from <see cref="Dispose()"/>.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
			{
				this.mapControl.Dispose();
				this.areaLayer.Dispose();
				this.pin.Dispose();
			}

			this.isDisposed = true;
		}

		/// <summary>
		/// The place to show, or null.
		/// </summary>
		public GeoPosition? Position
		{
			get => (GeoPosition?)this.GetValue(PositionProperty);
			set => this.SetValue(PositionProperty, value);
		}

		/// <summary>
		/// One corner of the allowed area, or null.
		/// </summary>
		public GeoPosition? AreaMin
		{
			get => (GeoPosition?)this.GetValue(AreaMinProperty);
			set => this.SetValue(AreaMinProperty, value);
		}

		/// <summary>
		/// The opposite corner of the allowed area, or null.
		/// </summary>
		public GeoPosition? AreaMax
		{
			get => (GeoPosition?)this.GetValue(AreaMaxProperty);
			set => this.SetValue(AreaMaxProperty, value);
		}

		/// <summary>
		/// The size of the pin.
		/// </summary>
		public double PinSize
		{
			get => (double)this.GetValue(PinSizeProperty);
			set => this.SetValue(PinSizeProperty, value);
		}

		/// <summary>
		/// Sizes the pin so its tip stays at the middle of the map.
		/// </summary>
		private void UpdatePinSize()
		{
			this.pin.WidthRequest = this.PinSize;
			this.pin.HeightRequest = this.PinSize;
			this.pin.TranslationY = -this.PinSize / 2;
		}

		/// <summary>
		/// Draws the allowed area and moves the map to the place, or to the area while there is no place.
		/// </summary>
		private void UpdateMap()
		{
			Map? Map = this.mapControl.Map;
			if (Map is null)
				return;

			MRect? Area = GetArea(this.AreaMin, this.AreaMax);
			this.areaLayer.Features = Area is null ? [] : [CreateAreaFeature(Area)];
			this.areaLayer.DataHasChanged();

			this.pin.IsVisible = this.Position is not null;

			if (this.mapControl.Width <= 0 || this.mapControl.Height <= 0)
				return;

			if (this.Position is GeoPosition Place)
				Map.Navigator.CenterOnAndZoomTo(ToMapPoint(Place), placeResolution);
			else if (Area is not null)
				Map.Navigator.ZoomToBox(Area.Grow(Area.Width * 0.1, Area.Height * 0.1));
		}

		/// <summary>
		/// Creates a map layer with the dashed outline of the allowed area.
		/// </summary>
		/// <param name="AreaMin">One corner of the area, or null.</param>
		/// <param name="AreaMax">The opposite corner of the area, or null.</param>
		/// <returns>The layer, or null when there is no area.</returns>
		public static MemoryLayer? CreateAreaLayer(GeoPosition? AreaMin, GeoPosition? AreaMax)
		{
			if (GetArea(AreaMin, AreaMax) is not MRect Area)
				return null;

			return new MemoryLayer
			{
				Name = "Area",
				Style = null,
				Features = [CreateAreaFeature(Area)]
			};
		}

		/// <summary>
		/// Gets the allowed area in map coordinates.
		/// </summary>
		/// <param name="AreaMin">One corner of the area, or null.</param>
		/// <param name="AreaMax">The opposite corner of the area, or null.</param>
		/// <returns>The area, or null when the place isn't limited.</returns>
		public static MRect? GetArea(GeoPosition? AreaMin, GeoPosition? AreaMax)
		{
			if (AreaMin is not GeoPosition Min || AreaMax is not GeoPosition Max)
				return null;

			MPoint A = ToMapPoint(Min);
			MPoint B = ToMapPoint(Max);

			return new MRect(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y), Math.Max(A.X, B.X), Math.Max(A.Y, B.Y));
		}

		/// <summary>
		/// Converts a place to a point on the map.
		/// </summary>
		/// <param name="Place">The place.</param>
		/// <returns>The point on the map.</returns>
		private static MPoint ToMapPoint(GeoPosition Place)
		{
			return SphericalMercator.FromLonLat(Place.Longitude, Place.Latitude).ToMPoint();
		}

		/// <summary>
		/// Creates the dashed outline of the allowed area.
		/// </summary>
		/// <param name="Area">The area in map coordinates.</param>
		/// <returns>The outline.</returns>
		private static GeometryFeature CreateAreaFeature(MRect Area)
		{
			Coordinate[] Corners =
			[
				new(Area.MinX, Area.MinY),
				new(Area.MaxX, Area.MinY),
				new(Area.MaxX, Area.MaxY),
				new(Area.MinX, Area.MaxY),
				new(Area.MinX, Area.MinY)
			];

			MauiColor Accent = GetColor("ButtonAccessPrimarybgWL", Microsoft.Maui.Graphics.Colors.Teal);
			GeometryFeature Feature = new(new Polygon(new LinearRing(Corners)));

			Feature.Styles.Add(new VectorStyle
			{
				Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(28, ToByte(Accent.Red), ToByte(Accent.Green), ToByte(Accent.Blue))),
				Outline = new Pen(Mapsui.Styles.Color.FromArgb(255, ToByte(Accent.Red), ToByte(Accent.Green), ToByte(Accent.Blue)), 2)
				{
					PenStyle = PenStyle.Dash
				}
			});

			return Feature;
		}

		/// <summary>
		/// Gets a colour from the app's resources.
		/// </summary>
		/// <param name="Key">The resource key.</param>
		/// <param name="Fallback">The colour to use when the resource is missing.</param>
		/// <returns>The colour.</returns>
		private static MauiColor GetColor(string Key, MauiColor Fallback)
		{
			if (Application.Current?.Resources.TryGetValue(Key, out object? Value) == true && Value is MauiColor Color)
				return Color;

			return Fallback;
		}

		/// <summary>
		/// Converts a colour channel from 0–1 to 0–255.
		/// </summary>
		/// <param name="Channel">The channel, from 0 to 1.</param>
		/// <returns>The channel, from 0 to 255.</returns>
		private static int ToByte(float Channel)
		{
			return (int)Math.Round(Math.Clamp(Channel, 0, 1) * 255);
		}
	}
}
