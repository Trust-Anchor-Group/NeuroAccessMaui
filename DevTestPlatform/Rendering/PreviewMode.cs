namespace DevTestPlatform.Rendering;

/// <summary>Controls developer chrome independently of the chosen scenario.</summary>
public enum PreviewMode
{
    /// <summary>Shows the preview together with scenario information and diagnostics.</summary>
    Framed,
    /// <summary>Shows only the production preview and its developer exit action.</summary>
    Fullscreen
}
