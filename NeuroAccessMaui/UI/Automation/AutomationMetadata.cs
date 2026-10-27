using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using System.Threading;

namespace NeuroAccessMaui.UI.Automation;

/// <summary>Configures the same native automation metadata for production and developer hosts.</summary>
public static class AutomationMetadata
{
#if ANDROID
    private static int configured;
#endif

    /// <summary>Registers Android metadata updates once without replacing spoken accessibility descriptions.</summary>
    public static void Configure()
    {
#if ANDROID
        if (Interlocked.Exchange(ref configured, 1) != 0)
            return;

        ViewHandler.ViewMapper.AppendToMapping(nameof(IView.AutomationId), (Handler, View) =>
        {
            if (!string.IsNullOrWhiteSpace(View.AutomationId))
                ViewHandler.MapSemantics(Handler, View);
        });
#endif
    }
}
