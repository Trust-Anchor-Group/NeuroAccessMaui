using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{

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

	public class ImageCropperView : ContentView
	{


		/// <summary>
		/// Sets the max resolution for the cropped image result.
		/// </summary>
		public Size OutputMaxResolution
		{
			get => (Size)this.GetValue(OutputMaxResolutionProperty);
			set => this.SetValue(OutputMaxResolutionProperty, value);
		}
		public static readonly BindableProperty OutputMaxResolutionProperty = BindableProperty.Create(nameof(OutputMaxResolution), typeof(Size), typeof(ImageCropperView), new Size(800, 800));

		/// <summary>
		/// Sets the max resolution transformation for the photo selected by the user.
		/// </summary>
		public Size InputMaxResolution
		{
			get => (Size)this.GetValue(InputMaxResolutionProperty);
			set => this.SetValue(InputMaxResolutionProperty, value);
		}
		public static readonly BindableProperty InputMaxResolutionProperty = BindableProperty.Create(nameof(InputMaxResolutionProperty), typeof(Size), typeof(ImageCropperView), new Size(3840, 2160));

		/// <summary>
		/// The source of the image to be cropped.
		/// </summary>
		public ImageSource ImageSource
		{
			get => (ImageSource)this.GetValue(ImageSourceProperty);
			set => this.SetValue(ImageSourceProperty, value);
		}
		public static readonly BindableProperty ImageSourceProperty =
			 BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(ImageCropperView), null, propertyChanged: OnImageSourceChanged);


		private static async void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ImageCropperView cropper && newValue is ImageSource imageSource)
			{
				cropper.bitmap = await LoadBitmapAsync(imageSource);
				cropper.canvasView.InvalidateSurface();
			}
		}

		private static async Task<SKBitmap?> LoadBitmapAsync(ImageSource imageSource)
		{
			try
			{
				if (imageSource is FileImageSource fileImageSource) // Local file
				{
					var filePath = fileImageSource.File;
					if (File.Exists(filePath))
					{
						using var stream = File.OpenRead(filePath);
						return SKBitmap.Decode(stream);
					}
				}
				else if (imageSource is UriImageSource uriImageSource) // Remote URI
				{
					using var httpClient = new HttpClient();
					var response = await httpClient.GetAsync(uriImageSource.Uri);
					if (response.IsSuccessStatusCode)
					{
						using var stream = await response.Content.ReadAsStreamAsync();
						return SKBitmap.Decode(stream);
					}
				}
				else if (imageSource is StreamImageSource streamImageSource) // Stream
				{
					var stream = await streamImageSource.Stream(CancellationToken.None);
					if (stream != null)
					{
						return SKBitmap.Decode(stream);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading bitmap: {ex.Message}");
			}

			return null;
		}

		/// <summary>
		/// The crop mode (square or circle).
		/// </summary>
		public CropMode CropMode
		{
			get => (CropMode)GetValue(CropModeProperty);
			set => SetValue(CropModeProperty, value);
		}
		public static readonly BindableProperty CropModeProperty =
			 BindableProperty.Create(nameof(CropMode), typeof(CropMode), typeof(ImageCropperView), CropMode.Square);

		private float scale = 1f;
		private float offsetX = 0f;
		private float offsetY = 0f;
		private float startScale = 1f;
		private float startOffsetX = 0f;
		private float startOffsetY = 0f;

		private SKBitmap? bitmap;
		private SKCanvasView canvasView;

		public ImageCropperView()
		{
			Grid root = new Grid
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill,
			};

			this.canvasView = new SKCanvasView
			{
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill,
			};

			PanGestureRecognizer panGesture = new();
			panGesture.PanUpdated += this.OnPan;

			PinchGestureRecognizer pinchGesture = new PinchGestureRecognizer();
			pinchGesture.PinchUpdated += this.OnPinch;

			this.canvasView.PaintSurface += this.OnPaintSurface;

			this.canvasView.GestureRecognizers.Add(pinchGesture);
			this.canvasView.GestureRecognizers.Add(panGesture);

			root.Add(this.canvasView);

			this.Content = root;
		}

		/// <summary>
		/// Executes the image crop and returns the cropped image bytes.
		/// </summary>
		/// <param name="cropRect">If true, crops the image to a rectangle. Defaults to `false` for the full visible area.</param>
		/// <returns>Cropped image bytes.</returns>
		public async Task<byte[]?> PerformCropAsync(bool cropRect = false)
		{
			if (this.bitmap == null)
			{
				return null;
			}

			// Set up the crop region
			int cropWidth = (int)(this.bitmap.Width * (cropRect ? 0.8f : 1f));
			int cropHeight = (int)(this.bitmap.Height * (cropRect ? 0.8f : 1f));
			int cropX = (this.bitmap.Width - cropWidth) / 2;
			int cropY = (this.bitmap.Height - cropHeight) / 2;

			SKRectI cropArea = new SKRectI(cropX, cropY, cropX + cropWidth, cropY + cropHeight);

			// Create a new bitmap for the cropped area
			using SKBitmap croppedBitmap = new SKBitmap(cropArea.Width, cropArea.Height);
			using SKCanvas canvas = new SKCanvas(croppedBitmap);

			// Draw the cropped section of the original bitmap
			SKRect destRect = new SKRect(0, 0, cropArea.Width, cropArea.Height);
			canvas.DrawBitmap(this.bitmap, cropArea, destRect);

			// Encode the cropped image into a byte array
			using MemoryStream memoryStream = new();
			croppedBitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
			memoryStream.Position = 0;

			// Return the byte array of the cropped image
			return memoryStream.ToArray();
		}

		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			SKCanvas canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			if (this.bitmap == null) return;

			canvas.Save();

			// Apply scaling and translation
			canvas.Translate(this.offsetX, this.offsetY);
			canvas.Scale(this.scale);

			// Draw the image
			SKRect destRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
			canvas.DrawBitmap(this.bitmap, destRect);

			// Apply crop mask
			if (this.CropMode == CropMode.Circle)
			{
				float cropRadius = Math.Min(e.Info.Width, e.Info.Height) / 2;
				SKPath clipPath = new SKPath();
				clipPath.AddCircle(e.Info.Width / 2, e.Info.Height / 2, cropRadius);	
				canvas.ClipPath(clipPath, SKClipOperation.Intersect);
			}
			else
			{
				float cropSize = Math.Min(e.Info.Width, e.Info.Height) * 0.8f;
				canvas.ClipRect(new SKRect(
					 (e.Info.Width - cropSize) / 2,
					 (e.Info.Height - cropSize) / 2,
					 (e.Info.Width + cropSize) / 2,
					 (e.Info.Height + cropSize) / 2));
			}

			canvas.Restore();
		}

		private void OnPan(object? sender, PanUpdatedEventArgs e)
		{
			if (e.StatusType == GestureStatus.Started)
			{
				this.startOffsetX = this.offsetX;
				this.startOffsetY = this.offsetY;
			}
			else if (e.StatusType == GestureStatus.Running)
			{
				this.offsetX = this.startOffsetX + (float)e.TotalX;
				this.offsetY = this.startOffsetY + (float)e.TotalY;
				this.canvasView.InvalidateSurface();
			}
		}

		private void OnPinch(object? sender, PinchGestureUpdatedEventArgs e)
		{
			if (e.Status == GestureStatus.Started)
			{
				this.startScale = this.scale;
			}
			else if (e.Status == GestureStatus.Running)
			{
				this.scale = this.startScale * (float)e.Scale;

				this.canvasView.InvalidateSurface();
			}
		}
	}
}
