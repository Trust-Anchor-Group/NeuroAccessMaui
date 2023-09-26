using System.Diagnostics.CodeAnalysis;
using NeuroAccessMaui.Pages;
using NeuroAccessMaui.Resources.Languages;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Navigation;

[Singleton]
internal sealed partial class NavigationService : LoadableService, INavigationService
{
	private bool isNavigating = false;
	private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new();

	public NavigationService()
	{
	}

	// Navigation service uses Shell and its routing system, which are only available after the application reached the main page.
	// Before that (during on-boarding and the loading page), we need to use the usual Xamarin Forms navigation.
	private bool CanUseNavigationService => App.IsOnboarded;

	/// <inheritdoc/>
	public Page CurrentPage => Shell.Current?.CurrentPage;

	///<inheritdoc/>
	public override Task Load(bool isResuming, CancellationToken cancellationToken)
	{
		if (this.BeginLoad(cancellationToken))
		{
			try
			{
				Application Application = Application.Current;
				Application.PropertyChanging += this.OnApplicationPropertyChanging;
				Application.PropertyChanged += this.OnApplicationPropertyChanged;
				this.SubscribeToShellNavigatingIfNecessary(Application);

				this.EndLoad(true);
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
				this.EndLoad(false);
			}
		}

		return Task.CompletedTask;
	}

	///<inheritdoc/>
	public override Task Unload()
	{
		if (this.BeginUnload())
		{
			try
			{
				Application Application = Application.Current;
				this.UnsubscribeFromShellNavigatingIfNecessary(Application);
				Application.PropertyChanged -= this.OnApplicationPropertyChanged;
				Application.PropertyChanging -= this.OnApplicationPropertyChanging;
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}

			this.EndUnload();
		}

		return Task.CompletedTask;
	}


	/// <inheritdoc/>
	public Task GoToAsync(string Route, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null)
	{
		// No args navigation will create a default navigation arguments
		return this.GoToAsync<NavigationArgs>(Route, null, BackMethod, UniqueId);
	}

	/// <inheritdoc/>
	public async Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null) where TArgs : NavigationArgs, new()
	{
		if (!this.CanUseNavigationService)
		{
			return;
		}

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			// Get the parent's navigation arguments
			NavigationArgs ParentArgs = this.GetCurrentNavigationArgs();

			// Create a default navigation arguments if Args are null
			NavigationArgs NavigationArgs = Args ?? new();

			NavigationArgs.SetBackArguments(ParentArgs, BackMethod, UniqueId);
			this.PushArgs(Route, NavigationArgs);

			if (!string.IsNullOrEmpty(UniqueId))
			{
				Route += "?UniqueId=" + UniqueId;
			}

			try
			{
				this.isNavigating = true;
				await Shell.Current.GoToAsync(Route, true);
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				ServiceRef.LogService.LogException(e);
				string ExtraInfo = Environment.NewLine + e.Message;

				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToNavigateToPage)], Route, ExtraInfo);
			}
			finally
			{
				this.isNavigating = false;
			}
		});
	}

	///<inheritdoc/>
	public async Task GoBackAsync(bool Animate = true)
	{
		if (!this.CanUseNavigationService)
		{
			return;
		}

		try
		{
			NavigationArgs NavigationArgs = this.GetCurrentNavigationArgs();

			if (NavigationArgs is not null) // the main view?
			{
				string BackRoute = NavigationArgs.GetBackRoute();

				this.isNavigating = true;
				await Shell.Current.GoToAsync(BackRoute, Animate);
			}
		}
		catch (Exception e)
		{
			ServiceRef.LogService.LogException(e);

			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[nameof(AppResources.FailedToClosePage)]);
		}
		finally
		{
			this.isNavigating = false;
		}
	}

	///<inheritdoc/>
	public bool TryGetArgs<TArgs>([NotNullWhen(true)] out TArgs? Args, string? UniqueId = null) where TArgs : NavigationArgs, new()
	{
		NavigationArgs? NavigationArgs = null;

		if (this.CanUseNavigationService && (this.CurrentPage is Page Page))
		{
			NavigationArgs = this.TryGetArgs(Page.GetType().Name, UniqueId);
			string Route = Routing.GetRoute(Page);
			NavigationArgs ??= this.TryGetArgs(Route, UniqueId);

			if ((NavigationArgs is null) && (UniqueId is null) &&
				(Page is BaseContentPage BasePage) && (BasePage.UniqueId is not null))
			{
				return this.TryGetArgs(out Args, BasePage.UniqueId);
			}
		}

		if (NavigationArgs is TArgs TArgsArgs)
		{
			Args = TArgsArgs;
		}
		else
		{
			Args = null;
		}

		return (Args is not null);
	}

	private NavigationArgs GetCurrentNavigationArgs()
	{
		this.TryGetArgs(out NavigationArgs Args);
		return Args;
	}

	private void OnApplicationPropertyChanged(object Sender, System.ComponentModel.PropertyChangedEventArgs Args)
	{
		if (Args.PropertyName == nameof(Application.MainPage))
		{
			this.SubscribeToShellNavigatingIfNecessary((Application)Sender);
		}
	}

	private void OnApplicationPropertyChanging(object Sender, PropertyChangingEventArgs Args)
	{
		if (Args.PropertyName == nameof(Application.MainPage))
		{
			this.UnsubscribeFromShellNavigatingIfNecessary((Application)Sender);
		}
	}

	private void SubscribeToShellNavigatingIfNecessary(Application Application)
	{
		if (Application.MainPage is Shell Shell)
		{
			Shell.Navigating += this.Shell_Navigating;
		}
	}

	private void UnsubscribeFromShellNavigatingIfNecessary(Application Application)
	{
		if (Application.MainPage is Shell Shell)
		{
			Shell.Navigating -= this.Shell_Navigating;
		}
	}

	private void Shell_Navigating(object Sender, ShellNavigatingEventArgs e)
	{
		try
		{
			if ((e.Source == ShellNavigationSource.Pop) &&
				e.CanCancel && !this.isNavigating)
			{
				e.Cancel();

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await this.GoBackAsync();
				});
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private static bool TryGetPageName(string Route, [NotNullWhen(true)] out string? PageName)
	{
		PageName = null;

		if (!string.IsNullOrWhiteSpace(Route))
		{
			PageName = Route.TrimStart('.', '/');
			return !string.IsNullOrWhiteSpace(PageName);
		}

		return false;
	}

	private void PushArgs(string Route, NavigationArgs Args)
	{
		if (TryGetPageName(Route, out string? PageName))
		{
			if (Args is not null)
			{
				string? UniqueId = Args.GetUniqueId();

				if (!string.IsNullOrEmpty(UniqueId))
				{
					PageName += UniqueId;
				}

				this.navigationArgsMap[PageName] = Args;
			}
			else
			{
				this.navigationArgsMap.Remove(PageName);
			}
		}
	}

	private NavigationArgs? TryGetArgs(string Route, string? UniqueId)
	{
		if (!string.IsNullOrEmpty(UniqueId))
		{
			Route += UniqueId;
		}

		if (TryGetPageName(Route, out string? PageName) &&
			this.navigationArgsMap.TryGetValue(PageName, out NavigationArgs? Args))
		{
			return Args;
		}

		return null;
	}
}
