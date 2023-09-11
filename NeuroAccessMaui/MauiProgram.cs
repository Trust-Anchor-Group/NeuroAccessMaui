using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using NeuroAccessMaui.Pages;

namespace NeuroAccessMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder Builder = MauiApp.CreateBuilder();

		Builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Initialise the toolkit
		Builder.UseMauiCommunityToolkit();
		Builder.UseMauiCommunityToolkitMarkup();

		Builder.RegisterPagesManager();

		Builder.Services.AddLocalization();
		//Builder.Services.AddLogging();

#if DEBUG
		Builder.Logging.AddDebug();
#endif

		return Builder.Build();
	}
}
