using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.Rendering;

/// <summary>Owns a current production preview and its session-specific loaded callbacks and cleanup.</summary>
public sealed class RenderSession : IAsyncDisposable
{
    private readonly BaseViewModel? viewModel;
    private EventHandler? loadedHandler;
    private bool isDisposed;

    /// <summary>Creates a rendering session without activating the production application's lifecycle.</summary>
    /// <param name="Content">The current production view or control.</param>
    /// <param name="ViewModel">The concrete production ViewModel, when applicable.</param>
    /// <param name="AfterLoaded">Optional initialization requiring an attached handler.</param>
    public RenderSession(View Content, BaseViewModel? ViewModel = null, Action? AfterLoaded = null)
    {
        this.Content = Content;
        this.viewModel = ViewModel;
        if (AfterLoaded is not null)
        {
            this.loadedHandler = (_, _) =>
            {
                this.Content.Loaded -= this.loadedHandler;
                this.loadedHandler = null;
                if (!this.isDisposed)
                {
                    try { AfterLoaded(); }
                    catch (Exception Exception) { this.Failed?.Invoke(Exception); }
                }
            };
            this.Content.Loaded += this.loadedHandler;
        }
    }

    /// <summary>Reports failures in initialization that requires the native view handler.</summary>
    public event Action<Exception>? Failed;

    /// <summary>Gets the view owned by this session.</summary>
    public View Content { get; }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (this.isDisposed)
            return;
        this.isDisposed = true;
        if (this.loadedHandler is not null)
            this.Content.Loaded -= this.loadedHandler;
        this.loadedHandler = null;
        this.Failed = null;
        try
        {
            if (this.viewModel is not null)
                await this.viewModel.OnDisposeAsync();
        }
        finally { this.Content.BindingContext = null; }
    }
}
