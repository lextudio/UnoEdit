using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Implemented by the host (e.g. ILSpy view model) to supply reference segments
/// that the editor should render as hyperlinks.
/// </summary>
public interface IReferenceSegmentSource
{
    /// <summary>
    /// Return all <see cref="ReferenceSegment"/> instances that overlap
    /// the closed interval [<paramref name="startOffset"/>, <paramref name="endOffset"/>].
    /// </summary>
    IReadOnlyList<ReferenceSegment> GetSegments(int startOffset, int endOffset);
}
