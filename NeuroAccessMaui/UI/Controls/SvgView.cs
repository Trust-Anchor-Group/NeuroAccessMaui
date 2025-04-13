using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui.Handlers;
using Svg.Skia;

namespace NeuroAccessMaui.UI.Controls
{
	public class AutoHeightSKCanvasView : SKCanvasView
	{
		public virtual Size CustomMeasuredSize(double widthConstraint, double heightConstraint)
		{
			return Size.Zero;
		}
	}
    internal class AutoHeightSKCanvasViewHandler : SKCanvasViewHandler
    {
        public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
			AutoHeightSKCanvasView? AutoHeightSKCanvasView = this.VirtualView as AutoHeightSKCanvasView;
            if (AutoHeightSKCanvasView is not null)
            {
                SizeRequest Custom = AutoHeightSKCanvasView.Measure(widthConstraint, heightConstraint);
                if (Custom.Request == Size.Zero)
                    return base.GetDesiredSize(widthConstraint, heightConstraint);
                else
                    return Custom.Request;
            }
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }
    }
	/// <summary>
	/// A custom .NET MAUI control for rendering Scalable Vector Graphics (SVG) files using SkiaSharp and Svg.Skia.
	/// <para>
	/// This control, derived from <see cref="SKCanvasView"/>, is designed for high-performance vector image rendering in MAUI applications.
	/// It supports asynchronous loading of SVG assets from the application package and provides flexible scaling options via
	/// the <see cref="Aspect"/> property. Additionally, it allows applying a tint via the <see cref="TintColor"/> property,
	/// enabling dynamic color customization without altering the SVG source.
	/// </para>
	/// <para>
	/// The control defines three bindable properties:
	/// <list type="bullet">
	///     <item>
	///         <description>
	///             <see cref="Source"/>: Specifies the file name of the SVG asset (e.g., "icon.svg"). When updated,
	///             the control asynchronously loads and parses the file.
	///         </description>
	///     </item>
	///     <item>
	///         <description>
	///             <see cref="Aspect"/>: Controls how the SVG image is scaled to fit the view. This property supports
	///             three modes:
	///             <list type="bullet">
	///                 <item><description><c>AspectFit</c> (default): Uniform scaling to ensure the entire SVG fits within the view bounds.</description></item>
	///                 <item><description><c>AspectFill</c>: Uniform scaling that fills the view, which may result in clipping.</description></item>
	///                 <item><description><c>Fill</c>: Non-uniform scaling that stretches the SVG to cover the view entirely, potentially distorting the image.</description></item>
	///             </list>
	///         </description>
	///     </item>
	///     <item>
	///         <description>
	///             <see cref="TintColor"/>: Provides an optional tint overlay. If a color is specified, the control
	///             applies a color filter to render the SVG with this tint, overriding its original colors.
	///         </description>
	///     </item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>Example Usage (XAML):</strong>
	/// <code>
	/// &lt;ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
	///              xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	///              xmlns:controls="clr-namespace:NeuroAccessMaui.UI.Controls"
	///              x:Class="YourApp.Views.SamplePage"&gt;
	///     &lt;ContentPage.Content&gt;
	///         &lt;controls:SvgView Source="sample.svg"
	///                           Aspect="AspectFit"
	///                           TintColor="RoyalBlue"
	///                           HorizontalOptions="Center"
	///                           VerticalOptions="Center" /&gt;
	///     &lt;/ContentPage.Content&gt;
	/// &lt;/ContentPage&gt;
	/// </code>
	/// </para>
	/// <para>
	/// <strong>Example Usage (C#):</strong>
	/// <code>
	/// var svgView = new SvgView
	/// {
	///     Source = "sample.svg",
	///     Aspect = Aspect.AspectFill,
	///     TintColor = Colors.DarkRed
	/// };
	/// 
	/// Content = new StackLayout
	/// {
	///     Children = { svgView }
	/// };
	/// </code>
	/// </para>
	/// <para>
	/// <strong>Important Implementation Details:</strong>
	/// <list type="bullet">
	///     <item>
	///         <description>
	///             The control asynchronously loads the SVG asset using <see cref="Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync"/>.
	///             In case of an error during SVG loading, a warning is logged via a logging service and the control fails gracefully.
	///         </description>
	///     </item>
	/// </list>
	/// </para>
	/// </summary>
	public class SvgView : AutoHeightSKCanvasView, IDisposable
	{
		/// <summary>
		/// Identifies the <see cref="Source"/> bindable property.
		/// This property represents the SVG file name bundled as a MAUI asset (e.g., "foo.svg").
		/// </summary>
		public static readonly BindableProperty SourceProperty =
			BindableProperty.Create(
				nameof(Source),
				typeof(string),
				typeof(SvgView),
				default(string),
				propertyChanged: OnSourceChanged);

		/// <summary>
		/// Identifies the <see cref="Aspect"/> bindable property.
		/// Determines how the SVG is scaled to fill the view.
		/// </summary>
		public static readonly BindableProperty AspectProperty =
			BindableProperty.Create(
				nameof(Aspect),
				typeof(Aspect),
				typeof(SvgView),
				Aspect.AspectFit,
				propertyChanged: OnAspectChanged);

		/// <summary>
		/// Identifies the <see cref="TintColor"/> bindable property.
		/// When provided, the SVG will be rendered using this tint color.
		/// </summary>
		public static readonly BindableProperty TintColorProperty =
			BindableProperty.Create(
				nameof(TintColor),
				typeof(Color),
				typeof(SvgView),
				null,
				propertyChanged: OnTintColorChanged);

		/// <summary>
		/// Gets or sets the SVG asset file name. For example: "foo.svg".
		/// </summary>
		public string Source
		{
			get => (string)this.GetValue(SourceProperty);
			set => this.SetValue(SourceProperty, value);
		}

		/// <summary>
		/// Gets or sets the scaling mode used to display the SVG.
		/// The default is <see cref="Aspect.AspectFit"/>.
		/// </summary>
		public Aspect Aspect
		{
			get => (Aspect)this.GetValue(AspectProperty);
			set => this.SetValue(AspectProperty, value);
		}

		/// <summary>
		/// Gets or sets the tint color to override the SVG’s original colors.
		/// When set to a non-null value, the SVG is rendered with this tint.
		/// </summary>
		public Color? TintColor
		{
			get => (Color?)this.GetValue(TintColorProperty);
			set => this.SetValue(TintColorProperty, value);
		}

		/// <summary>
		/// The internal SkiaSharp SVG instance used to hold the loaded SVG content.
		/// </summary>
		private SKSvg? svg;

		/// <summary>
		/// Flag indicating if the object has been disposed.
		/// </summary>
		private bool disposedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="SvgView"/> class.
		/// Registers the paint surface event handler and disables touch events.
		/// </summary>
		public SvgView()
		{
			// Disable touch events if not needed.
			this.EnableTouchEvents = false;
			PaintSurface += this.OnPaintSurface;
		}

		/// <summary>
		/// Callback method that is invoked when the <see cref="Source"/> property changes.
		/// Triggers an asynchronous load of the SVG file.
		/// </summary>
		/// <param name="bindable">The bindable object instance.</param>
		/// <param name="oldValue">The previous value of the Source property.</param>
		/// <param name="newValue">The new value of the Source property.</param>
		private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
		{
			SvgView SvgView = (SvgView)bindable;
			SvgView.LoadSvg();
		}

		/// <summary>
		/// Callback method that is invoked when the <see cref="Aspect"/> property changes.
		/// Forces a redraw of the view.
		/// </summary>
		/// <param name="bindable">The bindable object instance.</param>
		/// <param name="oldValue">The previous value of the Aspect property.</param>
		/// <param name="newValue">The new value of the Aspect property.</param>
		private static void OnAspectChanged(BindableObject bindable, object oldValue, object newValue)
		{
			SvgView SvgView = (SvgView)bindable;
			SvgView.InvalidateSurface();
		}

		/// <summary>
		/// Callback method that is invoked when the <see cref="TintColor"/> property changes.
		/// Forces a redraw of the view.
		/// </summary>
		/// <param name="bindable">The bindable object instance.</param>
		/// <param name="oldValue">The previous value of the TintColor property.</param>
		/// <param name="newValue">The new value of the TintColor property.</param>
		private static void OnTintColorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			SvgView SvgView = (SvgView)bindable;
			SvgView.InvalidateSurface();
		}

		/// <summary>
		/// Asynchronously loads the SVG file from the app package assets.
		/// This method handles errors and ensures proper disposal of previous SVG instances.
		/// </summary>
		private async void LoadSvg()
		{
			if (string.IsNullOrEmpty(this.Source))
				return;

			// Dispose of any previous instance.
			this.svg?.Dispose();
			this.svg = null;

			try
			{
				using Stream Stream = await FileSystem.OpenAppPackageFileAsync(this.Source);
				this.svg = new SKSvg();
				this.svg.Load(Stream);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogWarning($"Error loading SVG asset '{this.Source}': {Ex}", this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				this.svg = null;
			}
			finally
			{
				// Request a redraw regardless of success or failure.
				this.InvalidateSurface();

				this.InvalidateMeasure();

				

			}
		}

		/// <summary>
		/// Handles the <see cref="SKCanvasView.PaintSurface"/> event to render the SVG image on the canvas.
		/// Applies scaling and centering transformations based on the specified <see cref="Aspect"/> property.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event arguments containing paint surface information.</param>
		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			SKCanvas Canvas = e.Surface.Canvas;
			Canvas.Clear();

			if (this.svg?.Picture is null)
				return;

			SKImageInfo Info = e.Info;
			SKRect SvgRect = this.svg.Picture.CullRect;

			// Calculate scaling factors.
			float ScaleX = Info.Width / SvgRect.Width;
			float ScaleY = Info.Height / SvgRect.Height;
			float Scale;
			float TranslateX = 0;
			float TranslateY = 0;

			switch (this.Aspect)
			{
				case Aspect.Fill:
					// Non-uniform scaling to completely fill the view (might distort the image).
					Canvas.Scale(ScaleX, ScaleY);
					this.DrawPictureWithOptionalTint(Canvas, this.svg.Picture);
					return;

				case Aspect.AspectFill:
					// Uniform scaling that covers the view (parts may be clipped).
					Scale = Math.Max(ScaleX, ScaleY);
					break;

				case Aspect.AspectFit:
				default:
					// Uniform scaling to fit entirely within the view.
					Scale = Math.Min(ScaleX, ScaleY);
					break;
			}

			// Calculate dimensions after scaling.
			float ScaledWidth = SvgRect.Width * Scale;
			float ScaledHeight = SvgRect.Height * Scale;

			// Center the SVG.
			TranslateX = (Info.Width - ScaledWidth) / 2;
			TranslateY = (Info.Height - ScaledHeight) / 2;

			Canvas.Translate(TranslateX, TranslateY);
			Canvas.Scale(Scale);

			this.DrawPictureWithOptionalTint(Canvas, this.svg.Picture);
		}

		/// <summary>
		/// Renders the specified <see cref="SKPicture"/> to the canvas, applying a tint if specified by the <see cref="TintColor"/> property.
		/// </summary>
		/// <param name="canvas">The drawing canvas.</param>
		/// <param name="picture">The SVG picture to draw.</param>
		private void DrawPictureWithOptionalTint(SKCanvas canvas, SKPicture picture)
		{
			if (this.TintColor is null)
			{
				canvas.DrawPicture(picture);
			}
			else
			{
				// Apply tint using an SKPaint with a color filter.
				using SKPaint Paint = new()
				{
					ColorFilter = SKColorFilter.CreateBlendMode(this.TintColor.ToSKColor(), SKBlendMode.SrcIn)
				};
				canvas.DrawPicture(picture, Paint);
			}
		}


		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			// If the SVG is not loaded yet, fall back to the default measurement.
			if (this.svg?.Picture == null)
			{
				return base.OnMeasure(widthConstraint, heightConstraint);
			}

			// Retrieve the intrinsic dimensions of the SVG.
			SKRect svgBounds = this.svg.Picture.CullRect;
			double intrinsicWidth = svgBounds.Width;
			double intrinsicHeight = svgBounds.Height;

			// Check if constraints are unbounded.
			bool unboundedWidth = double.IsInfinity(widthConstraint);
			bool unboundedHeight = double.IsInfinity(heightConstraint);

			// If both dimensions are unbounded, return the intrinsic size.
			if (unboundedWidth && unboundedHeight)
			{
				return new SizeRequest(new Size(intrinsicWidth, intrinsicHeight));
			}
			// If only one dimension is unbounded, calculate it from the other dimension
			// while preserving the aspect ratio of the SVG.
			else if (unboundedWidth)
			{
				double scale = heightConstraint / intrinsicHeight;
				return new SizeRequest(new Size(intrinsicWidth * scale, heightConstraint));
			}
			else if (unboundedHeight)
			{
				double scale = widthConstraint / intrinsicWidth;
				return new SizeRequest(new Size(widthConstraint, intrinsicHeight * scale));
			}
			else
			{
				// If both dimensions are constrained, you can decide whether to honor the constraints
				// (for example, scaling the intrinsic size) or simply return the constraints.
				// Here we preserve the intrinsic aspect ratio.
				double constraintAspect = widthConstraint / heightConstraint;
				double intrinsicAspect = intrinsicWidth / intrinsicHeight;

				if (intrinsicAspect > constraintAspect)
				{
					// Constrained by width.
					double scaledHeight = widthConstraint / intrinsicAspect;
					return new SizeRequest(new Size(widthConstraint, scaledHeight));
				}
				else
				{
					// Constrained by height.
					double scaledWidth = heightConstraint * intrinsicAspect;
					return new SizeRequest(new Size(scaledWidth, heightConstraint));
				}
			}
		}

		#region IDisposable Implementation

		/// <summary>
		/// Releases all resources used by the <see cref="SvgView"/> control.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases resources used by the control.
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					// Unsubscribe from events and dispose managed objects.
					PaintSurface -= this.OnPaintSurface;
					this.svg?.Dispose();
					this.svg = null;
				}
				// Free unmanaged resources, if any.
				this.disposedValue = true;
			}
		}

		/// <summary>
		/// Finalizer for the <see cref="SvgView"/> class.
		/// Ensures that resources are released if Dispose was not called.
		/// </summary>
		~SvgView()
		{
			this.Dispose(disposing: false);
		}

		#endregion
	}
}
