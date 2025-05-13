using Waher.Runtime.Cache;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Service for loading and applying themes/branding in the app.
	/// </summary>
	[Singleton]
	public class ThemeService : IThemeService
	{
		private static readonly string providerFlagKey = "IsServerThemeDictionary";


		public async Task ApplyProviderTheme()
		{
			// 1) Check cache

			// 2) If not in cache or needs refresh, load from xmpp

			// 3) Put it in the cache



			using Stream Stream = await FileSystem.OpenAppPackageFileAsync("Test.xaml");

			using StreamReader Reader = new StreamReader(Stream);
			string XamlContent = await Reader.ReadToEndAsync();

			// 4) Parse XAML into a ResourceDictionary
			ResourceDictionary Dict = new ResourceDictionary().LoadFromXaml(XamlContent);


			// 5) Merge it into the Application Resources
			ResourceDictionary? AppResources = Application.Current?.Resources;
			if (AppResources is null)
			{
				ServiceRef.LogService.LogWarning("Resources Could not be found");
				return;
			}

			// Remove any previously loaded “server theme” dictionaries
			ResourceDictionary? Existing = AppResources.MergedDictionaries
				.FirstOrDefault(d => d.ContainsKey(providerFlagKey));
			if (Existing is not null)
				AppResources.MergedDictionaries.Remove(Existing);

			// Mark this so we can find it later
			Dict.Add(providerFlagKey, true);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				// 6) Add the new ResourceDictionary to the Application Resources
				AppResources.MergedDictionaries.Add(Dict);
			});
		}

		public Task<AppTheme> GetTheme()
		{
			return Task.FromResult(App.Current?.UserAppTheme ?? AppTheme.Unspecified);
		}

		public Task SetTheme(AppTheme Type)
		{
			App? AppInstance = App.Current;
			if (AppInstance is not null)
				AppInstance.UserAppTheme = Type;
			return Task.CompletedTask;
		}
	}
}
