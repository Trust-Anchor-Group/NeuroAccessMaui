using CommunityToolkit.Maui;

namespace NeuroAccessMaui.Pages;

public static class PageAppExtension
{
	public static MauiAppBuilder RegisterPagesManager(this MauiAppBuilder Builder)
	{
		Builder.Services.AddTransient(typeof(Registration.RegistrationViewModel));
		return Builder;
	}
}
