using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Waher.Content;

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
	/// A custom view that displays an image with pinch-zoom and pan support, 
	/// then crops it according to CropMode. The crop shape remains centered in the view, 
	/// with some optional margin.
	/// </summary>
	public class ImageCropperView : ContentView
	{
		// -----------------------------------------------------------
		// Bindable Properties (same as before)...
		// -----------------------------------------------------------

		public static readonly BindableProperty OutputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(OutputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(800, 800));

		public Size OutputMaxResolution
		{
			get => (Size)this.GetValue(OutputMaxResolutionProperty);
			set => this.SetValue(OutputMaxResolutionProperty, value);
		}

		public static readonly BindableProperty InputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(InputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(3840, 2160));

		public Size InputMaxResolution
		{
			get => (Size)this.GetValue(InputMaxResolutionProperty);
			set => this.SetValue(InputMaxResolutionProperty, value);
		}

		public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
			 propertyName: nameof(ImageSource),
			 returnType: typeof(ImageSource),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: null,
			 propertyChanged: OnImageSourceChanged);

		public ImageSource ImageSource
		{
			get => (ImageSource)this.GetValue(ImageSourceProperty);
			set => this.SetValue(ImageSourceProperty, value);
		}

		public static readonly BindableProperty CropModeProperty = BindableProperty.Create(
			 propertyName: nameof(CropMode),
			 returnType: typeof(CropMode),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropMode.Square);

		public CropMode CropMode
		{
			get => (CropMode)this.GetValue(CropModeProperty);
			set => this.SetValue(CropModeProperty, value);
		}

		public static readonly BindableProperty OutputFormatProperty = BindableProperty.Create(
			 propertyName: nameof(OutputFormat),
			 returnType: typeof(CropOutputFormat),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropOutputFormat.Png);

		public CropOutputFormat OutputFormat
		{
			get => (CropOutputFormat)this.GetValue(OutputFormatProperty);
			set => this.SetValue(OutputFormatProperty, value);
		}

		public static readonly BindableProperty JpegQualityProperty = BindableProperty.Create(
			 propertyName: nameof(JpegQuality),
			 returnType: typeof(int),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 90);

		public int JpegQuality
		{
			get => (int)this.GetValue(JpegQualityProperty);
			set => this.SetValue(JpegQualityProperty, value);
		}

		public static readonly BindableProperty CropShapeFillPortionProperty = BindableProperty.Create(
			 propertyName: nameof(CropShapeFillPortion),
			 returnType: typeof(float),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 0.9f);

		public float CropShapeFillPortion
		{
			get => (float)this.GetValue(CropShapeFillPortionProperty);
			set => this.SetValue(CropShapeFillPortionProperty, value);
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

		public ImageCropperView()
		{
			Grid root = new Grid
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			this.canvasView = new SKCanvasView
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			// IMPORTANT: Restrict pan gesture to 1 touch point
			PanGestureRecognizer panGesture = new PanGestureRecognizer
			{
				TouchPoints = 1
			};
			panGesture.PanUpdated += this.OnPan;

			// Pinch typically uses 2 touch points
			PinchGestureRecognizer pinchGesture = new PinchGestureRecognizer
			{
				// In some versions of .NET MAUI, 
				// PinchGesture doesn't have a TouchPoints property, 
				// but if it does, you can set it to 2. 
				// TouchPoints = 2
			};
			pinchGesture.PinchUpdated += this.OnPinch;

			this.canvasView.GestureRecognizers.Add(panGesture);
			this.canvasView.GestureRecognizers.Add(pinchGesture);

			this.canvasView.PaintSurface += this.OnPaintSurface;
			root.Add(this.canvasView);

			this.Content = root;
		}

		// -----------------------------------------------------------
		// Public Methods
		// -----------------------------------------------------------

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
		/// Performs the crop, with the final scaled to OutputMaxResolution, etc.
		/// (Same as before, unchanged except for your existing logic.)
		/// </summary>
		public async Task<byte[]?> PerformCropAsync()
		{
			if (this.bitmap == null)
				return null;

			int viewWidth = (int)this.canvasView.CanvasSize.Width;
			int viewHeight = (int)this.canvasView.CanvasSize.Height;
			if (viewWidth <= 0 || viewHeight <= 0)
				return null;

			// 1) Render the entire image (with transformations) into a temporary surface
			using SKBitmap tempSurfaceBitmap = new SKBitmap(viewWidth, viewHeight, isOpaque: false);
			using SKCanvas tempCanvas = new SKCanvas(tempSurfaceBitmap);
			tempCanvas.Clear(SKColors.Transparent);

			tempCanvas.Save();
			tempCanvas.Translate(this.offsetX, this.offsetY);
			tempCanvas.Scale(this.scale);

			SKRect destRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			tempCanvas.DrawBitmap(this.bitmap, destRect);

			tempCanvas.Restore();

			// 2) Figure out the shape rect, create path, etc. (same as your code)...

			SKRect cropRect = this.ComputeCropShapeRect(viewWidth, viewHeight, this.CropShapeFillPortion);
			SKPath cropPath = new SKPath();
			switch (this.CropMode)
			{
				case CropMode.Circle:
					{
						float radius = cropRect.Width / 2f;
						float cx = cropRect.MidX;
						float cy = cropRect.MidY;
						cropPath.AddCircle(cx, cy, radius);
						break;
					}
				case CropMode.Square:
				case CropMode.Aspect:
					cropPath.AddRect(cropRect);
					break;
			}

			using SKBitmap maskedBitmap = new SKBitmap(viewWidth, viewHeight, isOpaque: false);
			using (SKCanvas maskedCanvas = new SKCanvas(maskedBitmap))
			{
				maskedCanvas.Clear(SKColors.Transparent);
				maskedCanvas.ClipPath(cropPath, SKClipOperation.Intersect, true);

				maskedCanvas.Save();
				maskedCanvas.Translate(this.offsetX, this.offsetY);
				maskedCanvas.Scale(this.scale);
				maskedCanvas.DrawBitmap(this.bitmap, destRect);
				maskedCanvas.Restore();
			}

			SKRectI boundingBox = SKRectI.Round(cropRect);
			using SKBitmap cropped = new SKBitmap(boundingBox.Width, boundingBox.Height);
			using (SKCanvas croppedCanvas = new SKCanvas(cropped))
			{
				SKRect dst = new SKRect(0, 0, boundingBox.Width, boundingBox.Height);
				croppedCanvas.DrawBitmap(maskedBitmap, boundingBox, dst);
			}

			// 5) Resize if needed, then encode
			SKBitmap finalBitmap = ResizeBitmapIfNeeded(cropped,
				 (int)this.OutputMaxResolution.Width,
				 (int)this.OutputMaxResolution.Height);

			using MemoryStream ms = new MemoryStream();
			if (this.OutputFormat == CropOutputFormat.Png)
			{
				finalBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
			}
			else
			{
				int quality = Math.Max(0, Math.Min(100, this.JpegQuality));
				finalBitmap.Encode(ms, SKEncodedImageFormat.Jpeg, quality);
			}

			return ms.ToArray();
		}

		// -----------------------------------------------------------
		// Gesture Handlers (Pinch + Pan)
		// -----------------------------------------------------------

		/// <summary>
		/// Handle pan with one finger.
		/// </summary>
		private void OnPan(object? sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.startOffsetX = this.offsetX;
					this.startOffsetY = this.offsetY;
					break;

				case GestureStatus.Running:
					this.offsetX = this.startOffsetX + (float)e.TotalX;
					this.offsetY = this.startOffsetY + (float)e.TotalY;
					this.canvasView.InvalidateSurface();
					break;
			}
		}

		/// <summary>
		/// Handle pinch with two fingers.
		/// Using the pivot from e.ScaleOrigin to keep the pinch stable and avoid jumpiness.
		/// </summary>
		private void OnPinch(object? sender, PinchGestureUpdatedEventArgs e)
		{
			switch (e.Status)
			{
				case GestureStatus.Started:
					// Record the current scale and the pinch pivot in [0..1, 0..1]
					this.startScale = this.scale;
					this.pinchPivot = e.ScaleOrigin;
					break;

				case GestureStatus.Running:
					// Calculate the new scale
					Console.WriteLine("scale: " + (float)e.Scale);

					float newScale = this.startScale * (float)e.Scale;
					Console.WriteLine("newScale: " + newScale);

					// Ensure scale stays within limits
					newScale = MathF.Max(0.05f, MathF.Min(newScale, 20f));

					// Remember the old scale, then apply the new one
					float oldScale = this.scale;
					this.scale = newScale;

					// Avoid division by zero
					float scaleFactor = oldScale != 0 ? (this.scale / oldScale) : 1f;

					// Convert pivot from [0..1] coords into actual pixel coords
					float pivotX = (float)(this.pinchPivot.X * this.canvasView.CanvasSize.Width);
					float pivotY = (float)(this.pinchPivot.Y * this.canvasView.CanvasSize.Height);

					// Use pivot-based translation to keep the zoom centered under fingers
					this.offsetX = pivotX + (this.offsetX - pivotX) * scaleFactor;
					this.offsetY = pivotY + (this.offsetY - pivotY) * scaleFactor;

					// Redraw
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

		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			SKCanvas canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			if (this.bitmap == null)
				return;

			// 1) Draw the transformed image
			canvas.Save();
			canvas.Translate(this.offsetX, this.offsetY);
			canvas.Scale(this.scale);

			SKRect destRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			canvas.DrawBitmap(this.bitmap, destRect);
			canvas.Restore();

			// 2) Draw the bounding shape overlay (unchanged)
			SKRect shapeRect = this.ComputeCropShapeRect(e.Info.Width, e.Info.Height, this.CropShapeFillPortion);

			SKPath path = new SKPath();
			switch (this.CropMode)
			{
				case CropMode.Circle:
					{
						float radius = shapeRect.Width / 2f;
						float cx = shapeRect.MidX;
						float cy = shapeRect.MidY;
						path.AddCircle(cx, cy, radius);
						break;
					}
				case CropMode.Aspect:
				case CropMode.Square:
					path.AddRect(shapeRect);
					break;
			}

			using SKPaint overlayPaint = new SKPaint
			{
				Color = new SKColor(0, 0, 0, 100),
				Style = SKPaintStyle.Fill
			};

			canvas.Save();
			canvas.ClipPath(path, SKClipOperation.Difference, true);
			canvas.DrawRect(new SKRect(0, 0, e.Info.Width, e.Info.Height), overlayPaint);
			canvas.Restore();
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			if (!this.initialPositionSet && width > 0 && height > 0 && this.bitmap != null)
			{
				this.SetInitialImagePosition(width, height);
				this.initialPositionSet = true;
				this.canvasView.InvalidateSurface();
			}
		}

		private void SetInitialImagePosition(double width, double height)
		{
			SKRect shapeRect = this.ComputeCropShapeRect((float)width, (float)height, this.CropShapeFillPortion);
			float shapeW = shapeRect.Width;
			float shapeH = shapeRect.Height;

			float bmpW = this.bitmap!.Width;
			float bmpH = this.bitmap.Height;

			float scaleX = shapeW / bmpW;
			float scaleY = shapeH / bmpH;
			float newScale = MathF.Max(scaleX, scaleY);

			this.scale = newScale;

			float shapeCX = shapeRect.MidX;
			float shapeCY = shapeRect.MidY;

			float imageHalfW = (bmpW * newScale) / 2f;
			float imageHalfH = (bmpH * newScale) / 2f;

			this.offsetX = shapeCX - imageHalfW;
			this.offsetY = shapeCY - imageHalfH;
		}

		private SKRect ComputeCropShapeRect(float containerWidth, float containerHeight, float marginFactor)
		{
			marginFactor = MathF.Max(0.0f, MathF.Min(1.0f, marginFactor));

			switch (this.CropMode)
			{
				case CropMode.Circle:
				case CropMode.Square:
					{
						float minDim = MathF.Min(containerWidth, containerHeight);
						float shapeSize = minDim * marginFactor;

						float left = (containerWidth - shapeSize) / 2f;
						float top = (containerHeight - shapeSize) / 2f;
						return new SKRect(left, top, left + shapeSize, top + shapeSize);
					}

				case CropMode.Aspect:
					{
						float targetRatio = 1.0f;
						if (this.OutputMaxResolution.Height > 0)
						{
							targetRatio = (float)this.OutputMaxResolution.Width / (float)this.OutputMaxResolution.Height;
						}

						float shapeWidth = containerWidth * marginFactor;
						float shapeHeight = shapeWidth / targetRatio;
						if (shapeHeight > containerHeight * marginFactor)
						{
							shapeHeight = containerHeight * marginFactor;
							shapeWidth = shapeHeight * targetRatio;
						}

						float left = (containerWidth - shapeWidth) / 2f;
						float top = (containerHeight - shapeHeight) / 2f;
						return new SKRect(left, top, left + shapeWidth, top + shapeHeight);
					}
			}

			return SKRect.Empty;
		}

		// -----------------------------------------------------------
		// Bitmap Loading & Resizing
		// -----------------------------------------------------------

		private static async void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ImageCropperView cropper && newValue is ImageSource newSource)
			{
				cropper.bitmap = await LoadAndResizeBitmapAsync(newSource, cropper.InputMaxResolution);
				cropper.initialPositionSet = false;
				cropper.ResetTransformations();
				cropper.canvasView.InvalidateSurface();
			}
		}

		private static async Task<SKBitmap?> LoadAndResizeBitmapAsync(ImageSource imageSource, Size maxResolution)
		{
			try
			{
				StreamImageSource? streamImageSource = null;
				switch (imageSource)
				{
					case FileImageSource fileSource:
						if (File.Exists(fileSource.File))
						{
							streamImageSource = new StreamImageSource
							{
								Stream = _ => Task.FromResult<Stream>(File.OpenRead(fileSource.File))
							};
						}
						break;
					case UriImageSource uriSource:
						{
							using HttpClient httpClient = new HttpClient();
							HttpResponseMessage response = await httpClient.GetAsync(uriSource.Uri);
							if (response.IsSuccessStatusCode)
							{
								MemoryStream ms = new MemoryStream();
								await response.Content.CopyToAsync(ms);
								ms.Position = 0;
								streamImageSource = new StreamImageSource
								{
									Stream = _ => Task.FromResult<Stream>(ms)
								};
							}
							break;
						}
					case StreamImageSource sis:
						streamImageSource = sis;
						break;
				}

				if (streamImageSource == null)
					return null;

				Stream stream = await streamImageSource.Stream(CancellationToken.None).ConfigureAwait(false);
				if (stream == null)
					return null;

				using MemoryStream managedStream = new MemoryStream();
				await stream.CopyToAsync(managedStream).ConfigureAwait(false);
				managedStream.Position = 0;

				SKBitmap loadedBitmap = SKBitmap.Decode(managedStream);
				if (loadedBitmap == null)
					return null;

				SKBitmap resized = ResizeBitmapIfNeeded(loadedBitmap,
					 (int)maxResolution.Width,
					 (int)maxResolution.Height);

				if (resized != loadedBitmap)
				{
					loadedBitmap.Dispose();
				}
				return resized;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading bitmap: {ex.Message}");
				return null;
			}
		}

		private static SKBitmap ResizeBitmapIfNeeded(SKBitmap sourceBitmap, int maxWidth, int maxHeight)
		{
			if (sourceBitmap is null || maxWidth <= 0 || maxHeight <= 0)
				return sourceBitmap;

			int width = sourceBitmap.Width;
			int height = sourceBitmap.Height;

			if (width <= maxWidth && height <= maxHeight)
			{
				return sourceBitmap;
			}

			float widthRatio = (float)maxWidth / width;
			float heightRatio = (float)maxHeight / height;
			float scale = Math.Min(widthRatio, heightRatio);

			int newWidth = (int)(width * scale);
			int newHeight = (int)(height * scale);

			SKBitmap resized = new SKBitmap(newWidth, newHeight, sourceBitmap.ColorType, sourceBitmap.AlphaType);
			using SKCanvas canvas = new SKCanvas(resized);

			SKPaint paint = new SKPaint
			{
				FilterQuality = SKFilterQuality.High,
				IsAntialias = true
			};

			SKRect srcRect = new SKRect(0, 0, width, height);
			SKRect destRect = new SKRect(0, 0, newWidth, newHeight);

			canvas.DrawBitmap(sourceBitmap, srcRect, destRect, paint);

			return resized;
		}
	}
}
