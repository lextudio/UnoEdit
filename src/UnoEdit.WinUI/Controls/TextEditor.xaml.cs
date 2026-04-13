using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoEdit.WinUI.Controls;

/// <summary>
/// WinUI 3 TextEditor control backed by the UnoEdit document model.
/// Editing is handled by a native <see cref="TextBox"/>; the <see cref="Document"/> property
/// exposes the full <see cref="TextDocument"/> API for search, replace, folding, etc.
/// </summary>
public sealed partial class TextEditor : UserControl
{
    // ── Dependency properties ────────────────────────────────────────────────
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextEditor),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty SyntaxHighlightingProperty =
        DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition), typeof(TextEditor),
            new PropertyMetadata(null));

    // ── Constructor ─────────────────────────────────────────────────────────
    public TextEditor()
    {
        this.InitializeComponent();
        Document = new TextDocument();
    }

    // ── Public API ──────────────────────────────────────────────────────────
    public TextDocument Document
    {
        get => (TextDocument)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public IHighlightingDefinition SyntaxHighlighting
    {
        get => (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty);
        set => SetValue(SyntaxHighlightingProperty, value);
    }

    /// <summary>Gets or sets the editor text, syncing with the backing <see cref="Document"/>.</summary>
    public string Text
    {
        get => Document?.Text ?? string.Empty;
        set { if (Document != null) Document.Text = value; }
    }

    // ── Event: text changed from model ───────────────────────────────────────
    public event EventHandler TextChanged;

    // ── Private helpers ──────────────────────────────────────────────────────
    private bool _updatingEditBox;
    private bool _updatingDocument;

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (e.OldValue is TextDocument old)
            old.Changed -= editor.OnDocumentTextChanged;
        if (e.NewValue is TextDocument newDoc)
        {
            newDoc.Changed += editor.OnDocumentTextChanged;
            editor.SyncEditBoxFromDocument();
        }
    }

    private void OnDocumentTextChanged(object sender, DocumentChangeEventArgs e)
    {
        if (_updatingDocument) return;
        SyncEditBoxFromDocument();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncEditBoxFromDocument()
    {
        if (_updatingEditBox) return;
        _updatingEditBox = true;
        try
        {
            if (PART_EditBox != null && Document != null)
                PART_EditBox.Text = Document.Text;
        }
        finally { _updatingEditBox = false; }
    }

    private void OnEditBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingEditBox || Document == null) return;
        _updatingDocument = true;
        try { Document.Text = PART_EditBox.Text; }
        finally { _updatingDocument = false; }
    }
}
