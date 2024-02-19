using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Screenshot
{
	[DefaultImplementation(typeof(ScreenshotService))]
	public interface IScreenshotService
	{
		Task<ImageSource?> TakeScreenshotAsync();

		Task<ImageSource?> TakeBlurredScreenshotAsync();

	}
}
