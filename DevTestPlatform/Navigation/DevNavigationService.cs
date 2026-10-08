using DevTestPlatform.Catalog;
using DevTestPlatform.UI.Pages;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.Navigation;

/// <summary>Navigates the developer catalog through current CustomShell without activating production workflows.</summary>
public sealed class DevNavigationService : INavigationService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly SemaphoreSlim navigationLock = new SemaphoreSlim(1, 1);
    private readonly Stack<BaseContentPage> pages = new Stack<BaseContentPage>();
    private DevShell? shell;

    /// <summary>Creates developer navigation with the isolated application provider.</summary>
    /// <param name="ServiceProvider">The developer provider.</param>
    public DevNavigationService(IServiceProvider ServiceProvider) => this.serviceProvider = ServiceProvider;

    /// <inheritdoc/>
    public BaseContentPage? CurrentPage => this.pages.TryPeek(out BaseContentPage? Page) ? Page : null;

    /// <summary>Attaches the current production shell to developer navigation.</summary>
    /// <param name="Shell">The shell displaying developer pages.</param>
    public void Attach(DevShell Shell)
    {
        this.shell = Shell;
        this.pages.Push(this.serviceProvider.GetRequiredService<CatalogPage>());
    }

    /// <summary>Opens a controlled production preview without production lifecycle initialization.</summary>
    /// <param name="Screen">The production subject.</param>
    /// <param name="Scenario">The controlled visual state.</param>
    /// <returns>The preview navigation operation.</returns>
    public async Task OpenPreviewAsync(ScreenDefinition Screen, ScenarioDefinition Scenario)
    {
        PreviewPage Page = this.serviceProvider.GetRequiredService<PreviewPage>();
        try
        {
            await Page.LoadAsync(Screen, Scenario);
            await this.GoToAsync(Page);
        }
        catch
        {
            await Page.OnDisposeAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public Task GoToAsync(BaseContentPage Page) => this.NavigateAsync(async () =>
    {
        await this.GetShell().ShowScreen(Page, TransitionType.None);
        this.pages.Push(Page);
    });

    /// <inheritdoc/>
    public Task GoBackAsync() => this.NavigateAsync(async () =>
    {
        if (this.pages.Count <= 1)
            return;
        BaseContentPage Previous = this.pages.Skip(1).First();
        await this.GetShell().ShowScreen(Previous, TransitionType.None);
        BaseContentPage Removed = this.pages.Pop();
        await Removed.OnDisposeAsync();
    });

    /// <inheritdoc/>
    public async Task PopToRootAsync()
    {
        while (this.pages.Count > 1)
            await this.GoBackAsync();
    }

    /// <inheritdoc/>
    public Task SetRootAsync(BaseContentPage Page) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        await this.PopToRootAsync();
        if (this.pages.TryPop(out BaseContentPage? Previous))
            await Previous.OnDisposeAsync();
        this.pages.Push(Page);
        await this.GetShell().ShowScreen(Page, TransitionType.None);
    });

    /// <inheritdoc/>
    public Task GoToAsync(string Route) => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public Task GoToAsync(string Route, BackMethod BackMethod, string? UniqueId = null) => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public Task GoToAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new() => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod, string? UniqueId = null) where TArgs : NavigationArgs, new() => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public Task SetRootAsync(string Route) => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public Task SetRootAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new() => throw BlockedRoute(Route);
    /// <inheritdoc/>
    public TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new() => null;

    /// <summary>Releases the application-scoped navigation synchronization resource.</summary>
    public void Dispose()
    {
        this.navigationLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task NavigateAsync(Func<Task> Operation)
    {
        await this.navigationLock.WaitAsync();
        try { await MainThread.InvokeOnMainThreadAsync(Operation); }
        finally { this.navigationLock.Release(); }
    }

    private DevShell GetShell() => this.shell ?? throw new InvalidOperationException("The developer shell is not attached.");
    private static InvalidOperationException BlockedRoute(string Route) => new InvalidOperationException($"Production route '{Route}' is unavailable in visual previews.");
}
