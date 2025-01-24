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
		Circle
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
	/// A custom view that displays an image with pinch-zoom and pan support, then crops it according to <see cref="CropMode"/>.
	/// </summary>
	public class ImageCropperView : ContentView
	{
		/// <summary>
		/// Backing store for <see cref="OutputMaxResolution"/>.
		/// </summary>
		public static readonly BindableProperty OutputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(OutputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(800, 800));

		/// <summary>
		/// Sets the maximum resolution (width x height) for the *final* cropped image.
		/// </summary>
		public Size OutputMaxResolution
		{
			get => (Size)GetValue(OutputMaxResolutionProperty);
			set => SetValue(OutputMaxResolutionProperty, value);
		}

		/// <summary>
		/// Backing store for <see cref="InputMaxResolution"/>.
		/// </summary>
		public static readonly BindableProperty InputMaxResolutionProperty = BindableProperty.Create(
			 propertyName: nameof(InputMaxResolution),
			 returnType: typeof(Size),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: new Size(3840, 2160));

		/// <summary>
		/// Sets a maximum resolution to which the *input* image is down-scaled when loaded.
		/// </summary>
		public Size InputMaxResolution
		{
			get => (Size)GetValue(InputMaxResolutionProperty);
			set => SetValue(InputMaxResolutionProperty, value);
		}

		/// <summary>
		/// Backing store for <see cref="ImageSource"/>.
		/// </summary>
		public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
			 propertyName: nameof(ImageSource),
			 returnType: typeof(ImageSource),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: null,
			 propertyChanged: OnImageSourceChanged);

		/// <summary>
		/// The source of the image to be cropped.
		/// </summary>
		public ImageSource ImageSource
		{
			get => (ImageSource)GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		/// <summary>
		/// Backing store for <see cref="CropMode"/>.
		/// </summary>
		public static readonly BindableProperty CropModeProperty = BindableProperty.Create(
			 propertyName: nameof(CropMode),
			 returnType: typeof(CropMode),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropMode.Square);

		/// <summary>
		/// The crop mode (square or circle).
		/// </summary>
		public CropMode CropMode
		{
			get => (CropMode)GetValue(CropModeProperty);
			set => SetValue(CropModeProperty, value);
		}

		/// <summary>
		/// Backing store for <see cref="OutputFormat"/>.
		/// </summary>
		public static readonly BindableProperty OutputFormatProperty = BindableProperty.Create(
			 propertyName: nameof(OutputFormat),
			 returnType: typeof(CropOutputFormat),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: CropOutputFormat.Png);

		/// <summary>
		/// Specifies the output format (PNG or JPEG) for the cropped image.
		/// </summary>
		public CropOutputFormat OutputFormat
		{
			get => (CropOutputFormat)GetValue(OutputFormatProperty);
			set => SetValue(OutputFormatProperty, value);
		}

		/// <summary>
		/// Backing store for <see cref="JpegQuality"/>.
		/// </summary>
		public static readonly BindableProperty JpegQualityProperty = BindableProperty.Create(
			 propertyName: nameof(JpegQuality),
			 returnType: typeof(int),
			 declaringType: typeof(ImageCropperView),
			 defaultValue: 90);

		/// <summary>
		/// JPEG quality (0–100). This property is only used if <see cref="OutputFormat"/> is set to <see cref="CropOutputFormat.Jpeg"/>.
		/// </summary>
		public int JpegQuality
		{
			get => (int)GetValue(JpegQualityProperty);
			set => SetValue(JpegQualityProperty, value);
		}

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
		/// The loaded SKBitmap to be displayed and cropped.
		/// </summary>
		private SKBitmap? bitmap;

		/// <summary>
		/// The SkiaSharp canvas view for rendering the image.
		/// </summary>
		private readonly SKCanvasView canvasView;

		/// <summary>
		/// Initializes a new instance of the <see cref="ImageCropperView"/> class.
		/// </summary>
		public ImageCropperView()
		{
			// Create the root layout
			var root = new Grid
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			// Create the SkiaSharp canvas view
			this.canvasView = new SKCanvasView
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill
			};

			// Attach gesture recognizers
			var panGesture = new PanGestureRecognizer();
			panGesture.PanUpdated += OnPan;

			var pinchGesture = new PinchGestureRecognizer();
			pinchGesture.PinchUpdated += OnPinch;

			this.canvasView.GestureRecognizers.Add(panGesture);
			this.canvasView.GestureRecognizers.Add(pinchGesture);

			// Attach the paint event
			this.canvasView.PaintSurface += OnPaintSurface;

			// Add the canvas to the layout
			root.Add(this.canvasView);

			// Set the Content of this ContentView
			this.Content = root;
		}

		/// <summary>
		/// Resets the transformations (scale and offsets) back to default.
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
		/// Asynchronously performs the crop, respecting the user's current pan/zoom transformations
		/// and the selected <see cref="CropMode"/>. The final image is scaled to <see cref="OutputMaxResolution"/>
		/// and encoded in the chosen <see cref="OutputFormat"/>.
		/// </summary>
		/// <returns>The cropped image as a byte array, or null if no bitmap is loaded.</returns>
		public async Task<byte[]?> PerformCropAsync()
		{
			if (this.bitmap == null)
			{
				return null;
			}

			// Get control size in device pixels (CanvasSize is already in device pixels)
			var viewWidth = (int)this.canvasView.CanvasSize.Width;
			var viewHeight = (int)this.canvasView.CanvasSize.Height;

			if (viewWidth <= 0 || viewHeight <= 0)
			{
				// Control might not be laid out yet.
				return null;
			}

			// Create a temporary bitmap/surface to render the final crop
			using var tempSurfaceBitmap = new SKBitmap(viewWidth, viewHeight, isOpaque: false);
			using var tempCanvas = new SKCanvas(tempSurfaceBitmap);

			tempCanvas.Clear(SKColors.Transparent);

			// Apply transformations
			tempCanvas.Save();
			tempCanvas.Translate(this.offsetX, this.offsetY);
			tempCanvas.Scale(this.scale);

			// Draw the full bitmap
			var destRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			tempCanvas.DrawBitmap(this.bitmap, destRect);

			tempCanvas.Restore();

			// Create a path (circle or square) for masking
			var cropPath = new SKPath();
			if (this.CropMode == CropMode.Circle)
			{
				float circleRadius = Math.Min(viewWidth, viewHeight) / 2f;
				cropPath.AddCircle(viewWidth / 2f, viewHeight / 2f, circleRadius);
			}
			else
			{
				float squareSize = Math.Min(viewWidth, viewHeight);
				float left = (viewWidth - squareSize) / 2f;
				float top = (viewHeight - squareSize) / 2f;
				cropPath.AddRect(new SKRect(left, top, left + squareSize, top + squareSize));
			}

			// "Mask out" anything outside the path by drawing again into a new offscreen
			using var maskedBitmap = new SKBitmap(viewWidth, viewHeight, isOpaque: false);
			using (var maskedCanvas = new SKCanvas(maskedBitmap))
			{
				maskedCanvas.Clear(SKColors.Transparent);
				maskedCanvas.ClipPath(cropPath, SKClipOperation.Intersect, true);

				// Re-apply the same transformations
				maskedCanvas.Save();
				maskedCanvas.Translate(this.offsetX, this.offsetY);
				maskedCanvas.Scale(this.scale);
				maskedCanvas.DrawBitmap(this.bitmap, destRect);
				maskedCanvas.Restore();
			}

			// Determine bounding box
			SKRectI boundingBox;
			if (this.CropMode == CropMode.Circle)
			{
				float circleSize = Math.Min(viewWidth, viewHeight);
				float left = (viewWidth - circleSize) / 2f;
				float top = (viewHeight - circleSize) / 2f;
				boundingBox = new SKRectI((int)left, (int)top, (int)(left + circleSize), (int)(top + circleSize));
			}
			else
			{
				float squareSize = Math.Min(viewWidth, viewHeight);
				float left = (viewWidth - squareSize) / 2f;
				float top = (viewHeight - squareSize) / 2f;
				boundingBox = new SKRectI((int)left, (int)top, (int)(left + squareSize), (int)(top + squareSize));
			}

			// Crop to bounding box
			using var cropped = new SKBitmap(boundingBox.Width, boundingBox.Height);
			using (var croppedCanvas = new SKCanvas(cropped))
			{
				var dest = new SKRect(0, 0, boundingBox.Width, boundingBox.Height);
				croppedCanvas.DrawBitmap(maskedBitmap, boundingBox, dest);
			}

			// Resize to OutputMaxResolution if needed
			var finalBitmap = ResizeBitmapIfNeeded(cropped, (int)this.OutputMaxResolution.Width, (int)this.OutputMaxResolution.Height);

			// Encode
			using var ms = new MemoryStream();

			if (this.OutputFormat == CropOutputFormat.Png)
			{
				finalBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
			}
			else
			{
				// JPEG Quality can be from 0 to 100
				int quality = Math.Max(0, Math.Min(100, this.JpegQuality));
				finalBitmap.Encode(ms, SKEncodedImageFormat.Jpeg, quality);
			}

			return ms.ToArray();
		}

		/// <summary>
		/// Event handler for painting the canvas. Renders the image with the current transformations
		/// and visually clips to the selected <see cref="CropMode"/> (circle or square).
		/// </summary>
		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			if (this.bitmap == null)
			{
				return;
			}

			// Draw the transformed image
			canvas.Save();
			canvas.Translate(this.offsetX, this.offsetY);
			canvas.Scale(this.scale);
			var destRect = new SKRect(0, 0, this.bitmap.Width, this.bitmap.Height);
			canvas.DrawBitmap(this.bitmap, destRect);
			canvas.Restore();

			// Draw the bounding shape overlay (so the user knows the crop region)
			var path = new SKPath();
			if (this.CropMode == CropMode.Circle)
			{
				float radius = Math.Min(e.Info.Width, e.Info.Height) / 2f;
				path.AddCircle(e.Info.Width / 2f, e.Info.Height / 2f, radius);
			}
			else
			{
				float size = Math.Min(e.Info.Width, e.Info.Height);
				float left = (e.Info.Width - size) / 2f;
				float top = (e.Info.Height - size) / 2f;
				path.AddRect(new SKRect(left, top, left + size, top + size));
			}

			using var overlayPaint = new SKPaint
			{
				Color = new SKColor(0, 0, 0, 100), // A dark translucent overlay
				Style = SKPaintStyle.Fill
			};

			canvas.Save();
			canvas.ClipPath(path, SKClipOperation.Difference, true);
			canvas.DrawRect(new SKRect(0, 0, e.Info.Width, e.Info.Height), overlayPaint);
			canvas.Restore();
		}

		/// <summary>
		/// Handles pan gestures to move the image (offsetX, offsetY).
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
		/// Handles pinch gestures to zoom the image (scale).
		/// </summary>
		private void OnPinch(object? sender, PinchGestureUpdatedEventArgs e)
		{
			switch (e.Status)
			{
				case GestureStatus.Started:
					this.startScale = this.scale;
					break;
				case GestureStatus.Running:
					this.scale = this.startScale * (float)e.Scale;
					this.canvasView.InvalidateSurface();
					break;
			}
		}

		/// <summary>
		/// Triggered when the <see cref="ImageSource"/> property changes. Loads the bitmap asynchronously.
		/// </summary>
		private static async void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ImageCropperView cropper && newValue is ImageSource newSource)
			{
				cropper.bitmap = await LoadAndResizeBitmapAsync(newSource, cropper.InputMaxResolution);
				cropper.ResetTransformations();
			}
		}

		/// <summary>
		/// Loads a bitmap from an <see cref="ImageSource"/> and scales it down if it exceeds the specified maximum resolution.
		/// </summary>
		private static async Task<SKBitmap?> LoadAndResizeBitmapAsync(ImageSource imageSource, Size maxResolution)
		{
			try
			{
				// Convert ImageSource to a Stream
				StreamImageSource? streamImageSource = null;

				switch (imageSource)
				{
					case FileImageSource fileSource:
						// Local file
						if (File.Exists(fileSource.File))
						{
							streamImageSource = new StreamImageSource
							{
								Stream = token => Task.FromResult<Stream>(File.OpenRead(fileSource.File))
							};
						}
						break;

					case UriImageSource uriSource:
						// Remote URI
						using (var httpClient = new HttpClient())
						{
							var response = await httpClient.GetAsync(uriSource.Uri);
							if (response.IsSuccessStatusCode)
							{
								var ms = new MemoryStream();
								await response.Content.CopyToAsync(ms);
								ms.Position = 0;
								streamImageSource = new StreamImageSource
								{
									Stream = token => Task.FromResult<Stream>(ms)
								};
							}
						}
						break;

					case StreamImageSource sis:
						streamImageSource = sis;
						break;
				}

				if (streamImageSource == null)
				{
					return null;
				}

				var stream = await streamImageSource.Stream(CancellationToken.None).ConfigureAwait(false);
				if (stream == null)
				{
					return null;
				}

				using var managedStream = new MemoryStream();
				await stream.CopyToAsync(managedStream).ConfigureAwait(false);
				managedStream.Position = 0;

				// Decode bitmap
				var loadedBitmap = SKBitmap.Decode(managedStream);
				if (loadedBitmap == null)
				{
					return null;
				}

				// Scale down if needed
				var resized = ResizeBitmapIfNeeded(loadedBitmap, (int)maxResolution.Width, (int)maxResolution.Height);

				// Dispose original if we resized
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

		/// <summary>
		/// Resizes the given <paramref name="sourceBitmap"/> so that it does not exceed <paramref name="maxWidth"/>
		/// or <paramref name="maxHeight"/>. Returns a new <see cref="SKBitmap"/> if a resize is performed;
		/// otherwise, returns the original.
		/// </summary>
		private static SKBitmap ResizeBitmapIfNeeded(SKBitmap sourceBitmap, int maxWidth, int maxHeight)
		{
			if (sourceBitmap is null || maxWidth <= 0 || maxHeight <= 0)
			{
				return sourceBitmap;
			}

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			if (width <= maxWidth && height <= maxHeight)
			{
				// No resizing needed
				return sourceBitmap;
			}

			// Compute scale factor
			float widthRatio = (float)maxWidth / width;
			float heightRatio = (float)maxHeight / height;
			float scale = Math.Min(widthRatio, heightRatio);

			int newWidth = (int)(width * scale);
			int newHeight = (int)(height * scale);

			var resized = new SKBitmap(newWidth, newHeight, sourceBitmap.ColorType, sourceBitmap.AlphaType);
			using var canvas = new SKCanvas(resized);

			var paint = new SKPaint
			{
				FilterQuality = SKFilterQuality.High,
				IsAntialias = true
			};

			var srcRect = new SKRect(0, 0, width, height);
			var destRect = new SKRect(0, 0, newWidth, newHeight);

			canvas.DrawBitmap(sourceBitmap, srcRect, destRect, paint);

			return resized;
		}
	}
}
