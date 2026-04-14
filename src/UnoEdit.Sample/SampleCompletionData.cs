using System;
using System.Reflection;
// Avoid ambiguous ImageSource resolves (Microsoft.UI.Xaml.Media vs System.Windows.Media).
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
    /// <summary>
    /// Small sample implementation of <see cref="ICompletionData"/> used by the sample host.
    /// Keep implementation minimal: the sample host performs document insertion itself,
    /// but this class provides a compatible Complete() as a best-effort fallback.
    /// </summary>
    internal sealed class SampleCompletionData : ICompletionData
    {
        public System.Windows.Media.ImageSource? Image => null;

        public string Text { get; }

        public object Content => Text;

        public object Description { get; }

        public double Priority => 0.0;

        public SampleCompletionData(string text, string? description = null)
        {
            Text = text ?? string.Empty;
            Description = description ?? text ?? string.Empty;
        }

        public void Complete(object textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            try
            {
                if (textArea == null || completionSegment == null)
                    return;

                // Try to find a Document property on the provided textArea and replace the segment.
                var docProp = textArea.GetType().GetProperty("Document", BindingFlags.Public | BindingFlags.Instance);
                if (docProp?.GetValue(textArea) is TextDocument doc)
                {
                    int offset = Math.Clamp(completionSegment.Offset, 0, doc.TextLength);
                    int length = Math.Clamp(completionSegment.Length, 0, Math.Max(0, doc.TextLength - offset));
                    doc.Replace(offset, length, Text);
                }
            }
            catch
            {
                // best-effort fallback: swallow exceptions in sample helper
            }
        }

        public override string ToString() => Text;
    }
}
