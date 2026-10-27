using Android.App;
using Android.Runtime;

namespace DevTestPlatform;

/// <summary>Provides the Android entry point for the independent developer application.</summary>
[Application]
public sealed class MainApplication : MauiApplication
{
    /// <summary>Initializes the Android application wrapper.</summary>
    /// <param name="Handle">The native application handle.</param>
    /// <param name="Ownership">The native handle ownership.</param>
    public MainApplication(IntPtr Handle, JniHandleOwnership Ownership) : base(Handle, Ownership) { }
    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
