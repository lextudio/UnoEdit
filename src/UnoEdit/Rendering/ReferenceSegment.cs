namespace ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// A text range that should be rendered as a clickable hyperlink / reference.
/// Hosts (e.g. ILSpy) attach a list of these to the editor to enable Ctrl+Click navigation.
/// </summary>
public sealed class ReferenceSegment
{
    /// <summary>Start offset in the document (inclusive).</summary>
    public int StartOffset { get; init; }

    /// <summary>End offset in the document (exclusive).</summary>
    public int EndOffset { get; init; }

    /// <summary>
    /// Arbitrary payload — typically the member reference, definition offset, or URI
    /// to navigate to when the segment is Ctrl+Clicked.
    /// </summary>
    public object? Reference { get; init; }

    /// <summary>True when this segment points to its own definition (self-reference).</summary>
    public bool IsLocal { get; init; }

    public int Length => EndOffset - StartOffset;

    public bool Contains(int offset) => offset >= StartOffset && offset < EndOffset;
}
