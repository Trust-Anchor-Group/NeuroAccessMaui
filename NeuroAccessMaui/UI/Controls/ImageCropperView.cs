using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Specifies how the image should be cropped.
	/// </summary>
	public enum CropMode
	{
		/// <summary>
		/// The image is cropped to a square.
		/// </summary>
		Square,

		/// <summary>
		/// The image is cropped to a circle.
		/// </summary>
		Circle,

		/// <summary>
		/// The image is cropped to a rectangle using the aspect ratio of OutputMaxResolution.
		/// </summary>
		Aspect
	}

	/// <summary>
	/// Specifies the output format of the cropped image.
	/// </summary>
	public enum CropOutputFormat
	{
		/// <summary>
		/// Encode the image as PNG (lossless).
		/// </summary>
		Png,

		/// <summary>
		/// Encode the image as JPEG (lossy).
		/// </summary>
		Jpeg
	}

	/// <summary>
	/// A custom view that displays an image with pinch-zoom, pan, and rotation support,
	/// then crops it according to CropMode. The crop shape remains centered in the view,
	/// with some optional margin.
	/// </summary>
	public class ImageCropperView : ContentView
	{
		// -----------------------------------------------------------
		// Static Fields
		// -----------------------------------------------------------

		/// <summary>
		/// A shared HttpClient for downloading images.
		/// </summary>
		private static readonly HttpClient SHttpClient = new HttpClient();

		// -----------------------------------------------------------
		// Bindable Properties
		// -----------------------------------------------------------

		/// <summary>
		/// Backing bindable property for OutputMaxResolution.
		/// </summary>
		public static readonly BindableProperty OutputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(OutputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(800, 800));

		/// <summary>
		/// Gets or sets the maximum resolution for the cropped output.
		/// </summary>
		public Size OutputMaxResolution
		{
			get => (Size)this.GetValue(OutputMaxResolutionProperty);
			set => this.SetValue(OutputMaxResolutionProperty, value);
		}

		/// <summary>
		/// Backing bindable property for InputMaxResolution.
		/// </summary>
		public static readonly BindableProperty InputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(InputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(3840, 2160));

		/// <summary>
		/// Gets or sets the maximum resolution for the input image.
		/// </summary>
		public Size InputMaxResolution
		{
			get => (Size)this.GetValue(InputMaxResolutionProperty);
			set => this.SetValue(InputMaxResolutionProperty, value);
		}

		/// <summary>
		/// Backing bindable property for ImageSource.
		/// </summary>
		public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
			 propertyName: nameof(ImageSource),
			 returnType: typeof(ImageSource),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: null,
			 propertyChanged: OnImageSourceChanged);

		/// <summary>
		/// Gets or sets the image source to display and crop.
		/// </summary>
		public ImageSource ImageSource
		{
			get => (ImageSource)this.GetValue(ImageSourceProperty);
			set => this.SetValue(ImageSourceProperty, value);
		}

		/// <summary>
		/// Backing bindable property for CropMode.
		/// </summary>
		public static readonly BindableProperty CropModeProperty = BindableProperty.Create(
			 propertyName: nameof(CropMode),
			 returnType: typeof(CropMode),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropMode.Square);

		/// <summary>
		/// Gets or sets the crop mode.
		/// </summary>
		public CropMode CropMode
		{
			get => (CropMode)this.GetValue(CropModeProperty);
			set => this.SetValue(CropModeProperty, value);
		}

		/// <summary>
		/// Backing bindable property for OutputFormat.
		/// </summary>
		public static readonly BindableProperty OutputFormatProperty = BindableProperty.Create(
			 propertyName: nameof(OutputFormat),
			 returnType: typeof(CropOutputFormat),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropOutputFormat.Png);

		/// <summary>
		/// Gets or sets the output image format.
		/// </summary>
		public CropOutputFormat OutputFormat
		{
			get => (CropOutputFormat)this.GetValue(OutputFormatProperty);
			set => this.SetValue(OutputFormatProperty, value);
		}

		/// <summary>
		/// Backing bindable property for JpegQuality.
		/// </summary>
		public static readonly BindableProperty JpegQualityProperty = BindableProperty.Create(
			 propertyName: nameof(JpegQuality),
			 returnType: typeof(int),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 90);

		/// <summary>
		/// Gets or sets the JPEG quality when encoding as JPEG.
		/// </summary>
		public int JpegQuality
		{
			get => (int)this.GetValue(JpegQualityProperty);
			set => this.SetValue(JpegQualityProperty, value);
		}

		/// <summary>
		/// Backing bindable property for CropShapeFillPortion.
		/// </summary>
		public static readonly BindableProperty CropShapeFillPortionProperty = BindableProperty.Create(
			 propertyName: nameof(CropShapeFillPortion),
			 returnType: typeof(float),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 0.9f);

		/// <summary>
		/// Gets or sets the fraction of the view filled by the crop shape.
		/// </summary>
		public float CropShapeFillPortion
		{
			get => (float)this.GetValue(CropShapeFillPortionProperty);
			set => this.SetValue(CropShapeFillPortionProperty, value);
		}

		/// <summary>
		/// Backing bindable property for RotationAngle.
		/// </summary>
		public static readonly BindableProperty RotationAngleProperty = BindableProperty.Create(
			 propertyName: nameof(RotationAngle),
			 returnType: typeof(double),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 0.0,
			 propertyChanged: OnRotationAngleChanged);

		/// <summary>
		/// Gets or sets the rotation angle (in degrees) for the image.
		/// </summary>
		public double RotationAngle
		{
			get => (double)this.GetValue(RotationAngleProperty);
			set => this.SetValue(RotationAngleProperty, value);
		}

		// -----------------------------------------------------------
		// Internal Fields
		// -----------------------------------------------------------

		/// <summary>
		/// Current scale for the image during pinch gestures.
		/// </summary>
		private float scale = 1f;

		/// <summary>
		/// Current X offset for the image during pan gestures.
		/// </summary>
		private float offsetX;

		/// <summary>
		/// Current Y offset for the image during pan gestures.
		/// </summary>
		private float offsetY;

		/// <summary>
		/// The scale at the start of a pinch gesture.
		/// </summary>
		private float startScale = 1f;

		/// <summary>
		/// The X offset at the start of a pan gesture.
		/// </summary>
		private float startOffsetX;

		/// <summary>
		/// The Y offset at the start of a pan gesture.
		/// </summary>
		private float startOffsetY;

		/// <summary>
		/// The pivot used during pinch gestures (in control-relative coordinates [0..1]).
		/// </summary>
		private Point pinchPivot = new Point(0.5, 0.5);

		/// <summary>
		/// The loaded SKBitmap to be displayed and cropped.
		/// </summary>
		private SKBitmap? bitmap;

		/// <summary>
		/// The SkiaSharp canvas view for rendering the image.
		/// </summary>
		private readonly SKCanvasView canvasView;

		/// <summary>
		/// Whether we've already set the initial position after measuring.
		/// </summary>
		private bool initialPositionSet;

		// -----------------------------------------------------------
		// Constructor
		// -----------------------------------------------------------

		/// <summary>
		/// Initializes a new instance of the <see cref="ImageCropperView"/> class.
		/// </summary>
		public ImageCropperView()
		{
			Grid Root = new Grid
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			this.canvasView = new SKCanvasView
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			// IMPORTANT: Restrict pan gesture to 1 touch point.
			PanGestureRecognizer PanGesture = new PanGestureRecognizer
			{
				TouchPoints = 1
			};
			PanGesture.PanUpdated += this.OnPan;

			// Pinch typically uses 2 touch points.
			PinchGestureRecognizer PinchGesture = new PinchGestureRecognizer();
			PinchGesture.PinchUpdated += this.OnPinch;

			this.canvasView.GestureRecognizers.Add(PanGesture);
			this.canvasView.GestureRecognizers.Add(PinchGesture);

			this.canvasView.PaintSurface += this.OnPaintSurface;
			Root.Add(this.canvasView);

			this.Content = Root;
		}

		// -----------------------------------------------------------
		// Public Methods
		// -----------------------------------------------------------

		/// <summary>
		/// Resets the pan, zoom, and rotation transformations to their default values.
		/// </summary>
		public void ResetTransformations()
		{
			this.scale = 1f;
			this.offsetX = 0f;
			this.offsetY = 0f;
			this.startScale = 1f;
			this.startOffsetX = 0f;
			this.startOffsetY = 0f;
			this.canvasView.InvalidateSurface();
		}

		/// <summary>
		/// Performs the crop, applying pan, zoom, and rotation transformations,
		/// and resizes the output to <see cref="OutputMaxResolution"/>.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation. The task result contains the cropped image as a byte array, or null if an error occurred.</returns>
		public async Task<byte[]?> PerformCropAsync()
		{
			if (this.bitmap == null)
				return null;

			int ViewWidth = (int)this.canvasView.CanvasSize.Width;
			int ViewHeight = (int)this.canvasView.CanvasSize.Height;
			if (ViewWidth <= 0 || ViewHeight <= 0)
				return null;

			// 1) Render the entire image (with transformations) into a temporary surface.
			using SKBitmap TempSurfaceBitmap = new SKBitmap(ViewWidth, ViewHeight, isOpaque: false);
			using SKCanvas TempCanvas = new SKCanvas(TempSurfaceBitmap);
			TempCanvas.Clear(SKColors.Transparent);

			float CenterX = ViewWidth / 2f;
			float CenterY = ViewHeight / 2f;

			TempCanvas.Save();
			TempCanvas.Translate(this.offsetX, this.offsetY);
			TempCanvas.Scale(this.scale);
			// Apply rotation about the center of the view.
			TempCanvas.Translate(CenterX, CenterY);
			TempCanvas.RotateDegrees((float)this.RotationAngle);
			TempCanvas.Translate(-CenterX, -CenterY);

			SKRect DestRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			TempCanvas.DrawBitmap(this.bitmap, DestRect);
			TempCanvas.Restore();

			// 2) Create the crop shape based on the selected CropMode.
			SKRect CropRect = this.ComputeCropShapeRect(ViewWidth, ViewHeight, this.CropShapeFillPortion);
			SKPath CropPath = new SKPath();
			switch (this.CropMode)
			{
				case CropMode.Circle:
					{
						float Radius = CropRect.Width / 2f;
						float Cx = CropRect.MidX;
						float Cy = CropRect.MidY;
						CropPath.AddCircle(Cx, Cy, Radius);
						break;
					}
				case CropMode.Square:
				case CropMode.Aspect:
					CropPath.AddRect(CropRect);
					break;
			}

			// 3) Apply the crop mask.
			using SKBitmap MaskedBitmap = new SKBitmap(ViewWidth, ViewHeight, isOpaque: false);
			using (SKCanvas MaskedCanvas = new SKCanvas(MaskedBitmap))
			{
				MaskedCanvas.Clear(SKColors.Transparent);
				MaskedCanvas.ClipPath(CropPath, SKClipOperation.Intersect, true);

				MaskedCanvas.Save();
				MaskedCanvas.Translate(this.offsetX, this.offsetY);
				MaskedCanvas.Scale(this.scale);
				MaskedCanvas.Translate(CenterX, CenterY);
				MaskedCanvas.RotateDegrees((float)this.RotationAngle);
				MaskedCanvas.Translate(-CenterX, -CenterY);
				MaskedCanvas.DrawBitmap(this.bitmap, DestRect);
				MaskedCanvas.Restore();
			}

			// 4) Extract the bounding box of the crop area.
			SKRectI BoundingBox = SKRectI.Round(CropRect);
			using SKBitmap Cropped = new SKBitmap(BoundingBox.Width, BoundingBox.Height);
			using (SKCanvas CroppedCanvas = new SKCanvas(Cropped))
			{
				SKRect Dst = new SKRect(0, 0, BoundingBox.Width, BoundingBox.Height);
				CroppedCanvas.DrawBitmap(MaskedBitmap, BoundingBox, Dst);
			}

			// 5) Resize if needed, then encode.
			SKBitmap FinalBitmap = ResizeBitmapIfNeeded(Cropped,
				 (int)this.OutputMaxResolution.Width,
				 (int)this.OutputMaxResolution.Height);

			using MemoryStream Ms = new MemoryStream();
			if (this.OutputFormat == CropOutputFormat.Png)
			{
				FinalBitmap.Encode(Ms, SKEncodedImageFormat.Png, 100);
			}
			else
			{
				int Quality = Math.Max(0, Math.Min(100, this.JpegQuality));
				FinalBitmap.Encode(Ms, SKEncodedImageFormat.Jpeg, Quality);
			}

			return Ms.ToArray();
		}

		// -----------------------------------------------------------
		// Gesture Handlers (Pinch + Pan)
		// -----------------------------------------------------------

		/// <summary>
		/// Handles pan gestures (one finger) to update the image offset.
		/// </summary>
		/// <param name="Sender">The gesture recognizer.</param>
		/// <param name="E">The pan gesture event arguments.</param>
		private void OnPan(object? Sender, PanUpdatedEventArgs E)
		{
			switch (E.StatusType)
			{
				case GestureStatus.Started:
					this.startOffsetX = this.offsetX;
					this.startOffsetY = this.offsetY;
					break;

				case GestureStatus.Running:
					this.offsetX = this.startOffsetX + (float)E.TotalX;
					this.offsetY = this.startOffsetY + (float)E.TotalY;
					this.canvasView.InvalidateSurface();
					break;
			}
		}

		/// <summary>
		/// Handles pinch gestures to update the image scale and adjust the translation to keep the pinch stable.
		/// </summary>
		/// <param name="Sender">The gesture recognizer.</param>
		/// <param name="E">The pinch gesture event arguments.</param>
		private void OnPinch(object? Sender, PinchGestureUpdatedEventArgs E)
		{
			switch (E.Status)
			{
				case GestureStatus.Started:
					// Record the current scale and the pinch pivot in [0..1, 0..1].
					this.startScale = this.scale;
					this.pinchPivot = E.ScaleOrigin;
					break;

				case GestureStatus.Running:
					float NewScale = this.startScale * (float)E.Scale;
					// Ensure the scale stays within sensible limits.
					NewScale = MathF.Max(0.05f, MathF.Min(NewScale, 20f));

					float OldScale = this.scale;
					this.scale = NewScale;

					// Calculate the scaling factor.
					float ScaleFactor = OldScale != 0 ? (this.scale / OldScale) : 1f;

					// Convert pivot from [0..1] to actual pixel coordinates.
					float PivotX = (float)(this.pinchPivot.X * this.canvasView.CanvasSize.Width);
					float PivotY = (float)(this.pinchPivot.Y * this.canvasView.CanvasSize.Height);

					// Adjust the offset so the pinch gesture stays centered on the pivot.
					this.offsetX = PivotX + (this.offsetX - PivotX) * ScaleFactor;
					this.offsetY = PivotY + (this.offsetY - PivotY) * ScaleFactor;

					this.canvasView.InvalidateSurface();
					break;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					break;
			}
		}

		// -----------------------------------------------------------
		// Paint & Layout
		// -----------------------------------------------------------

		/// <summary>
		/// Handles the painting of the canvas, applying pan, zoom, and rotation transformations,
		/// then drawing the image, crop overlay, and a white outline for the crop shape.
		/// </summary>
		/// <param name="Sender">The canvas view.</param>
		/// <param name="E">The paint surface event arguments.</param>
		private void OnPaintSurface(object? Sender, SKPaintSurfaceEventArgs E)
		{
			SKCanvas Canvas = E.Surface.Canvas;
			Canvas.Clear(SKColors.Transparent);

			if (this.bitmap == null)
				return;

			float CenterX = E.Info.Width / 2f;
			float CenterY = E.Info.Height / 2f;

			// 1) Draw the transformed image.
			Canvas.Save();
			Canvas.Translate(this.offsetX, this.offsetY);
			Canvas.Scale(this.scale);
			// Apply rotation about the center of the view.
			Canvas.Translate(CenterX, CenterY);
			Canvas.RotateDegrees((float)this.RotationAngle);
			Canvas.Translate(-CenterX, -CenterY);
			SKRect DestRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			Canvas.DrawBitmap(this.bitmap, DestRect);
			Canvas.Restore();

			// 2) Build the crop shape path.
			SKRect ShapeRect = this.ComputeCropShapeRect(E.Info.Width, E.Info.Height, this.CropShapeFillPortion);
			SKPath Path = new SKPath();
			switch (this.CropMode)
			{
				case CropMode.Circle:
					{
						float Radius = ShapeRect.Width / 2f;
						float Cx = ShapeRect.MidX;
						float Cy = ShapeRect.MidY;
						Path.AddCircle(Cx, Cy, Radius);
						break;
					}
				case CropMode.Square:
				case CropMode.Aspect:
					Path.AddRect(ShapeRect);
					break;
			}

			// 3) Draw the semi-transparent overlay (mask) outside the crop shape.
			using (SKPaint OverlayPaint = new SKPaint
			{
				Color = new SKColor(0, 0, 0, 100),
				Style = SKPaintStyle.Fill
			})
			{
				Canvas.Save();
				Canvas.ClipPath(Path, SKClipOperation.Difference, true);
				Canvas.DrawRect(new SKRect(0, 0, E.Info.Width, E.Info.Height), OverlayPaint);
				Canvas.Restore();
			}

			// 4) Draw a white outline along the crop shape.
			using (SKPaint OutlinePaint = new SKPaint
			{
				Color = SKColors.White,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 3, // Adjust the outline thickness as needed.
				IsAntialias = true
			})
			{
				Canvas.DrawPath(Path, OutlinePaint);
			}
		}

		/// <summary>
		/// Overrides the OnSizeAllocated method to set the initial image position when the view is first laid out.
		/// </summary>
		/// <param name="Width">The width allocated to the view.</param>
		/// <param name="Height">The height allocated to the view.</param>
		protected override void OnSizeAllocated(double Width, double Height)
		{
			base.OnSizeAllocated(Width, Height);

			if (!this.initialPositionSet && Width > 0 && Height > 0 && this.bitmap != null)
			{
				this.SetInitialImagePosition(Width, Height);
				this.initialPositionSet = true;
				this.canvasView.InvalidateSurface();
			}
		}

		/// <summary>
		/// Sets the initial image position so that the crop shape is centered.
		/// The image is scaled using MathF.Max so that it at least covers the crop area.
		/// This method converts the view’s allocated size (in DIPs) to pixels using the display density.
		/// </summary>
		/// <param name="width">The width of the view (in DIPs).</param>
		/// <param name="height">The height of the view (in DIPs).</param>
		private void SetInitialImagePosition(double width, double height)
		{
			// Get the display density.
			double density = DeviceDisplay.MainDisplayInfo.Density;

			// Convert the allocated size (in device-independent units) to pixels.
			float viewWidthPx = (float)(width * density);
			float viewHeightPx = (float)(height * density);

			// Compute the crop shape rectangle based on the view size in pixels.
			SKRect cropShapeRect = this.ComputeCropShapeRect(viewWidthPx, viewHeightPx, this.CropShapeFillPortion);

			// Determine the center of the crop shape.
			float cropCenterX = cropShapeRect.MidX;
			float cropCenterY = cropShapeRect.MidY;

			// Compute scaling factors so that the image covers the crop shape.
			float scaleX = cropShapeRect.Width / this.bitmap!.Width;
			float scaleY = cropShapeRect.Height / this.bitmap.Height;
			// Use MathF.Max to ensure the bitmap covers (or exceeds) the crop area.
			float newScale = MathF.Max(scaleX, scaleY);
			this.scale = newScale;

			// Calculate the scaled dimensions of the bitmap.
			float scaledWidth = this.bitmap.Width * newScale;
			float scaledHeight = this.bitmap.Height * newScale;

			// Compute offsets to center the scaled image over the crop shape.
			this.offsetX = cropCenterX - (scaledWidth / 2f);
			this.offsetY = cropCenterY - (scaledHeight / 2f);
		}


		/// <summary>
		/// Computes the rectangle for the crop shape based on the container size and margin factor.
		/// </summary>
		/// <param name="ContainerWidth">The width of the container.</param>
		/// <param name="ContainerHeight">The height of the container.</param>
		/// <param name="MarginFactor">The margin factor (between 0 and 1) for the crop shape.</param>
		/// <returns>A <see cref="SKRect"/> defining the crop shape.</returns>
		private SKRect ComputeCropShapeRect(float ContainerWidth, float ContainerHeight, float MarginFactor)
		{
			MarginFactor = MathF.Max(0.0f, MathF.Min(1.0f, MarginFactor));

			switch (this.CropMode)
			{
				case CropMode.Circle:
				case CropMode.Square:
					{
						float MinDim = MathF.Min(ContainerWidth, ContainerHeight);
						float ShapeSize = MinDim * MarginFactor;

						float Left = (ContainerWidth - ShapeSize) / 2f;
						float Top = (ContainerHeight - ShapeSize) / 2f;
						return new SKRect(Left, Top, Left + ShapeSize, Top + ShapeSize);
					}

				case CropMode.Aspect:
					{
						float TargetRatio = 1.0f;
						if (this.OutputMaxResolution.Height > 0)
						{
							TargetRatio = (float)this.OutputMaxResolution.Width / (float)this.OutputMaxResolution.Height;
						}

						float ShapeWidth = ContainerWidth * MarginFactor;
						float ShapeHeight = ShapeWidth / TargetRatio;
						if (ShapeHeight > ContainerHeight * MarginFactor)
						{
							ShapeHeight = ContainerHeight * MarginFactor;
							ShapeWidth = ShapeHeight * TargetRatio;
						}

						float Left = (ContainerWidth - ShapeWidth) / 2f;
						float Top = (ContainerHeight - ShapeHeight) / 2f;
						return new SKRect(Left, Top, Left + ShapeWidth, Top + ShapeHeight);
					}
			}

			return SKRect.Empty;
		}

		// -----------------------------------------------------------
		// Bitmap Loading & Resizing
		// -----------------------------------------------------------

		/// <summary>
		/// Called when the RotationAngle bindable property changes.
		/// </summary>
		/// <param name="Bindable">The bindable object.</param>
		/// <param name="OldValue">The previous angle</param>
		/// <param name="NewValue">The new angle</param>
		private static void OnRotationAngleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is ImageCropperView Cropper)
			{
				Cropper.canvasView.InvalidateSurface();
			}
		}

		/// <summary>
		/// Called when the ImageSource bindable property changes.
		/// Loads the new image, resizes it if needed, resets transformations,
		/// and automatically fits the image if the canvas size is already available.
		/// </summary>
		/// <param name="Bindable">The bindable object.</param>
		/// <param name="OldValue">The previous image source.</param>
		/// <param name="NewValue">The new image source.</param>
		private static async void OnImageSourceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is ImageCropperView Cropper && NewValue is ImageSource NewSource)
			{
				// Dispose the previous bitmap if any to avoid memory leaks.
				Cropper.bitmap?.Dispose();
				Cropper.bitmap = await LoadAndResizeBitmapAsync(NewSource, Cropper.InputMaxResolution);

				// Reset any previous transformations.
			//Cropper.ResetTransformations();

				// Always reset the InitialPositionSet flag when a new image is loaded.
				Cropper.initialPositionSet = false;

				Console.WriteLine("Width: " + Cropper.canvasView.CanvasSize.Width);
				Console.WriteLine("Height: " + Cropper.canvasView.CanvasSize.Height);
				// If the canvas size is already available, automatically fit the image.
				if (Cropper.canvasView.CanvasSize.Width > 0 && Cropper.canvasView.CanvasSize.Height > 0)
				{
					Cropper.SetInitialImagePosition(Cropper.canvasView.CanvasSize.Width, Cropper.canvasView.CanvasSize.Height);
					Cropper.initialPositionSet = true;
				}
				else
				{
					Cropper.initialPositionSet = false;
				}

				Cropper.canvasView.InvalidateSurface();
			}
		}

		/// <summary>
		/// Loads and resizes the bitmap from the specified image source.
		/// </summary>
		/// <param name="ImageSource">The source of the image.</param>
		/// <param name="MaxResolution">The maximum allowed resolution.</param>
		/// <returns>The loaded and potentially resized <see cref="SKBitmap"/>, or null if loading fails.</returns>
		private static async Task<SKBitmap?> LoadAndResizeBitmapAsync(ImageSource ImageSource, Size MaxResolution)
		{
			try
			{
				StreamImageSource? StreamImageSource = null;
				switch (ImageSource)
				{
					case FileImageSource FileSource:
						if (File.Exists(FileSource.File))
						{
							StreamImageSource = new StreamImageSource
							{
								Stream = _ => Task.FromResult<Stream>(File.OpenRead(FileSource.File))
							};
						}
						break;
					case UriImageSource UriSource:
						{
							HttpResponseMessage Response = await SHttpClient.GetAsync(UriSource.Uri);
							if (Response.IsSuccessStatusCode)
							{
								MemoryStream Ms = new MemoryStream();
								await Response.Content.CopyToAsync(Ms);
								Ms.Position = 0;
								StreamImageSource = new StreamImageSource
								{
									Stream = _ => Task.FromResult<Stream>(Ms)
								};
							}
							break;
						}
					case StreamImageSource SIS:
						StreamImageSource = SIS;
						break;
				}

				if (StreamImageSource == null)
					return null;

				Stream Stream = await StreamImageSource.Stream(CancellationToken.None).ConfigureAwait(false);
				if (Stream == null)
					return null;

				using MemoryStream ManagedStream = new MemoryStream();
				await Stream.CopyToAsync(ManagedStream).ConfigureAwait(false);
				ManagedStream.Position = 0;

				SKBitmap LoadedBitmap = SKBitmap.Decode(ManagedStream);
				if (LoadedBitmap == null)
					return null;

				SKBitmap Resized = ResizeBitmapIfNeeded(LoadedBitmap,
					 (int)MaxResolution.Width,
					 (int)MaxResolution.Height);

				if (Resized != LoadedBitmap)
				{
					LoadedBitmap.Dispose();
				}
				return Resized;
			}
			catch (Exception Ex)
			{
				Console.WriteLine($"Error loading bitmap: {Ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Resizes the provided bitmap if it exceeds the specified maximum dimensions.
		/// </summary>
		/// <param name="SourceBitmap">The source bitmap.</param>
		/// <param name="MaxWidth">The maximum allowed width.</param>
		/// <param name="MaxHeight">The maximum allowed height.</param>
		/// <returns>The original bitmap if no resizing is necessary; otherwise, a new resized bitmap.</returns>
		private static SKBitmap ResizeBitmapIfNeeded(SKBitmap SourceBitmap, int MaxWidth, int MaxHeight)
		{
			if (SourceBitmap is null || MaxWidth <= 0 || MaxHeight <= 0)
				return SourceBitmap;

			int Width = SourceBitmap.Width;
			int Height = SourceBitmap.Height;

			if (Width <= MaxWidth && Height <= MaxHeight)
			{
				return SourceBitmap;
			}

			float WidthRatio = (float)MaxWidth / Width;
			float HeightRatio = (float)MaxHeight / Height;
			float Scale = Math.Min(WidthRatio, HeightRatio);

			int NewWidth = (int)(Width * Scale);
			int NewHeight = (int)(Height * Scale);

			SKBitmap Resized = new SKBitmap(NewWidth, NewHeight, SourceBitmap.ColorType, SourceBitmap.AlphaType);
			using SKCanvas Canvas = new SKCanvas(Resized);

			using SKPaint Paint = new SKPaint
			{
				FilterQuality = SKFilterQuality.High,
				IsAntialias = true
			};

			SKRect SrcRect = new SKRect(0, 0, Width, Height);
			SKRect DestRect = new SKRect(0, 0, NewWidth, NewHeight);

			Canvas.DrawBitmap(SourceBitmap, SrcRect, DestRect, Paint);

			return Resized;
		}
	}
}
