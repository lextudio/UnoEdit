using System;

namespace UnoEdit.WinUI.Controls;

/// <summary>
/// Lightweight proxy that mirrors the Uno TextArea event API surface needed by
/// code-completion and IME scenarios.  The WinUI TextEditor fires these events
/// from its TextBox when text commitments are detected.
/// </summary>
public sealed class TextAreaProxy
{
    /// <summary>Raised after one or more characters have been committed to the document.</summary>
    public event EventHandler<TextAreaTextInputEventArgs> TextEntered;

    /// <summary>Raised just before characters are committed (handlers may set Handled = true to suppress).</summary>
    public event EventHandler<TextAreaTextInputEventArgs> TextEntering;

    internal void NotifyTextEntering(string text)
        => TextEntering?.Invoke(this, new TextAreaTextInputEventArgs(text));

    internal void NotifyTextEntered(string text)
        => TextEntered?.Invoke(this, new TextAreaTextInputEventArgs(text));
}

/// <summary>Event arguments for <see cref="TextAreaProxy"/> text-input events.</summary>
public sealed class TextAreaTextInputEventArgs : EventArgs
{
    /// <summary>The text that was (or is about to be) inserted.</summary>
    public string Text { get; }

    /// <summary>Set to <see langword="true"/> by a handler to suppress the default insertion.</summary>
    public bool Handled { get; set; }

    public TextAreaTextInputEventArgs(string text) => Text = text ?? string.Empty;
}
