using IdApp.Cv;
using IdApp.Cv.Channels;
using IdApp.Cv.ColorModels;
using Microsoft.Maui.Animations;
using SkiaSharp;
using Waher.Runtime.Inventory;
namespace NeuroAccessMaui.Services.Screenshot
{
	[Singleton]
	public class ScreenshotService : IScreenshotService
	{
		public async Task<ImageSource?> TakeBlurredScreenshotAsync()
		{
			try
			{

				IScreenshotResult? result = await Microsoft.Maui.Media.Screenshot.CaptureAsync();
				if (result is null)
					return null;
				//Read screenshot
				using (Stream pngStream = await result.OpenReadAsync(ScreenshotFormat.Png, 20))
				{
					// Original SKBitmap from PNG stream
					SKBitmap originalBitmap = SKBitmap.FromImage(SKImage.FromEncodedData(pngStream));

					// Desired width and height for the downscaled image
					int desiredWidth = originalBitmap.Width / 4;   //Reduce the width by a quarter
					int desiredHeight = originalBitmap.Height / 4; //Reduce the height by a quarter

					// Create an SKImageInfo with the desired width, height, and color type of the original
					SKImageInfo resizedInfo = new SKImageInfo(desiredWidth, desiredHeight, SKColorType.Gray8);

					// Create a new SKBitmap for the downscaled image
					SKBitmap resizedBitmap = originalBitmap.Resize(resizedInfo, SKFilterQuality.Medium);

					//Blur image
					IMatrix rezisedMatrix = IdApp.Cv.Bitmaps.FromBitmap(resizedBitmap);
					IMatrix greyChannelMatrix = rezisedMatrix.GrayScale(); 
					IMatrix newMatrix = IdApp.Cv.Transformations.Convolutions.ConvolutionOperations.GaussianBlur(greyChannelMatrix, 12, 3.5f);

					// Continue with the blurring and encoding to PNG as before
					byte[] blurred = IdApp.Cv.Bitmaps.EncodeAsPng(newMatrix);
					return ImageSource.FromStream(() => new MemoryStream(blurred));
				}
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
				return null;
			}
		}

		public async Task<ImageSource?> TakeScreenshotAsync()
		{
			IScreenshotResult? result = await Microsoft.Maui.Media.Screenshot.CaptureAsync();
			if (result is not null)
			{
				// Read the stream into a memory stream or byte array
				using (Stream stream = await result.OpenReadAsync())
				{
					MemoryStream memoryStream = new MemoryStream();
					await stream.CopyToAsync(memoryStream);
					byte[] bytes = memoryStream.ToArray();

					// Return a new MemoryStream based on the byte array for each invocation
					return ImageSource.FromStream(() => new MemoryStream(bytes));
				}
			}
			return null;
		}
	}
}

