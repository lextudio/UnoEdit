using System;
using System.Reflection;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;

namespace UnoEdit.WinUI.Sample;

/// <summary>
/// Minimal <see cref="ICompletionData"/> implementation for the WinUI sample host.
/// </summary>
internal sealed class SampleCompletionData : ICompletionData
{
    public System.Windows.Media.ImageSource Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => 0.0;

    public SampleCompletionData(string text, string? description = null)
    {
        Text = text ?? string.Empty;
        Description = (object?)description ?? text ?? string.Empty;
    }

    public void Complete(object textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        try
        {
            if (textArea is null || completionSegment is null) return;
            var docProp = textArea.GetType().GetProperty("Document", BindingFlags.Public | BindingFlags.Instance);
            if (docProp?.GetValue(textArea) is TextDocument doc)
            {
                int offset = Math.Clamp(completionSegment.Offset, 0, doc.TextLength);
                int length = Math.Clamp(completionSegment.Length, 0, Math.Max(0, doc.TextLength - offset));
                doc.Replace(offset, length, Text);
            }
        }
        catch { /* best-effort */ }
    }

    public override string ToString() => Text;
}
