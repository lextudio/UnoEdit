namespace UnoEdit.Skia.Desktop.Controls;

/// <summary>A self-contained color scheme for the editor UI.</summary>
public sealed class TextEditorTheme
{
    // ------------------------------------------------------------------ names
    public string Name { get; init; } = "";

    // ------------------------------------------------------------------ editor chrome
    public Windows.UI.Color EditorBackground    { get; init; }
    public Windows.UI.Color GutterBackground    { get; init; }
    public Windows.UI.Color GutterForeground    { get; init; }
    public Windows.UI.Color LineHighlight       { get; init; }
    public Windows.UI.Color DefaultForeground   { get; init; }
    public Windows.UI.Color CaretColor          { get; init; }
    public Windows.UI.Color SelectionColor      { get; init; }
    public Windows.UI.Color BorderColor         { get; init; }
    public Windows.UI.Color TitleBarBackground  { get; init; }
    public Windows.UI.Color TitleBarForeground  { get; init; }

    // ------------------------------------------------------------------ highlighting
    /// <summary>
    /// When true the syntax colors from the xshd definition are used directly.
    /// When false (dark theme) they are used directly too — the xshd files already
    /// define colors suitable for dark backgrounds (e.g. C#.xshd uses steelblue, violet, etc.).
    /// No inversion is performed; both themes just use the xshd colors plus the chrome palette.
    /// </summary>
    /// 
    // ------------------------------------------------------------------ built-in themes
    public static readonly TextEditorTheme Dark = new()
    {
        Name                = "Dark",
        EditorBackground    = Color(0xFF_0F172A),
        GutterBackground    = Color(0xFF_0F172A),
        GutterForeground    = Color(0xFF_64748B),
        LineHighlight       = Color(0xFF_162033),
        DefaultForeground   = Color(0xFF_E2E8F0),
        CaretColor          = Color(0xFF_F8FAFC),
        SelectionColor      = Color(0xFF_2563EB),
        BorderColor         = Color(0xFF_334155),
        TitleBarBackground  = Color(0xFF_0F172A),
        TitleBarForeground  = Color(0xFF_E5E7EB),
    };

    public static readonly TextEditorTheme Light = new()
    {
        Name                = "Light",
        EditorBackground    = Color(0xFF_FFFFFF),
        GutterBackground    = Color(0xFF_F3F4F6),
        GutterForeground    = Color(0xFF_6E7681),
        LineHighlight       = Color(0xFF_EFF6FF),
        DefaultForeground   = Color(0xFF_1F2328),
        CaretColor          = Color(0xFF_1F2328),
        SelectionColor      = Color(0xFF_0969DA),
        BorderColor         = Color(0xFF_D1D5DB),
        TitleBarBackground  = Color(0xFF_F9FAFB),
        TitleBarForeground  = Color(0xFF_111827),
    };

    // ---------------------------------------------------- helper
    private static Windows.UI.Color Color(uint argb) =>
        Windows.UI.Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte) argb);
}
