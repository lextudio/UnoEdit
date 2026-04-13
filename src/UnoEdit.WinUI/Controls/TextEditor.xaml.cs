using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UnoEdit.Skia.Desktop.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace UnoEdit.WinUI.Controls;

/// <summary>
/// WinUI 3 TextEditor control providing the same public API as the Uno TextEditor.
/// Backed by a native WinUI <see cref="TextBox"/> for editing; all document-level
/// operations (undo, folding, highlighting, search, load/save) are routed through
/// the <see cref="TextDocument"/> model.
/// </summary>
public sealed partial class TextEditor : UserControl
{
    // Dependency properties
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextEditor),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(TextEditorTheme), typeof(TextEditor),
            new PropertyMetadata(TextEditorTheme.Dark, OnThemeChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(true));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty SyntaxHighlightingProperty =
        DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition),
            typeof(TextEditor), new PropertyMetadata(null));

    public static readonly DependencyProperty CurrentOffsetProperty =
        DependencyProperty.Register(nameof(CurrentOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0, OnCurrentOffsetChanged));

    public static readonly DependencyProperty SelectionStartOffsetProperty =
        DependencyProperty.Register(nameof(SelectionStartOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0));

    public static readonly DependencyProperty SelectionEndOffsetProperty =
        DependencyProperty.Register(nameof(SelectionEndOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0));

    public static readonly DependencyProperty EncodingProperty =
        DependencyProperty.Register(nameof(Encoding), typeof(Encoding), typeof(TextEditor),
            new PropertyMetadata(Encoding.UTF8));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(nameof(Options), typeof(TextEditorOptions), typeof(TextEditor),
            new PropertyMetadata(new TextEditorOptions(), OnOptionsChanged));

    public TextEditor()
    {
        this.InitializeComponent();
        _textArea = new TextAreaProxy();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme(Theme ?? TextEditorTheme.Dark);
        if (Document == null)
            Document = new TextDocument();
    }

    // Public properties

    public TextDocument Document
    {
        get => (TextDocument)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public TextEditorTheme Theme
    {
        get => (TextEditorTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    public bool ShowLineNumbers
    {
        get => (bool)GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    public bool WordWrap
    {
        get => (bool)GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    public IHighlightingDefinition SyntaxHighlighting
    {
        get => (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty);
        set => SetValue(SyntaxHighlightingProperty, value);
    }

    public int CurrentOffset
    {
        get => (int)GetValue(CurrentOffsetProperty);
        set => SetValue(CurrentOffsetProperty, value);
    }

    public int SelectionStartOffset
    {
        get => (int)GetValue(SelectionStartOffsetProperty);
        set => SetValue(SelectionStartOffsetProperty, value);
    }

    public int SelectionEndOffset
    {
        get => (int)GetValue(SelectionEndOffsetProperty);
        set => SetValue(SelectionEndOffsetProperty, value);
    }

    public Encoding Encoding
    {
        get => (Encoding)GetValue(EncodingProperty);
        set => SetValue(EncodingProperty, value);
    }

    public TextEditorOptions Options
    {
        get => (TextEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public string Text
    {
        get => Document?.Text ?? string.Empty;
        set
        {
            var doc = EnsureDocument();
            doc.Text = value ?? string.Empty;
            CurrentOffset = 0;
            SelectionStartOffset = 0;
            SelectionEndOffset = 0;
            doc.UndoStack.ClearAll();
        }
    }

    public int CaretOffset
    {
        get => CurrentOffset;
        set => CurrentOffset = value;
    }

    public int SelectionStart
    {
        get => SelectionLength == 0 ? CurrentOffset : Math.Min(SelectionStartOffset, SelectionEndOffset);
        set => Select(value, SelectionLength);
    }

    public int SelectionLength
    {
        get => Math.Abs(SelectionEndOffset - SelectionStartOffset);
        set => Select(SelectionStart, value);
    }

    public string SelectedText
    {
        get
        {
            if (Document == null || SelectionLength == 0) return string.Empty;
            int start = Math.Min(SelectionStartOffset, SelectionEndOffset);
            return Document.GetText(start, SelectionLength);
        }
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var doc = EnsureDocument();
            int start = SelectionStart;
            doc.Replace(start, SelectionLength, value);
            Select(start, value.Length);
        }
    }

    public int LineCount => Document?.LineCount ?? 1;
    public bool CanUndo   => Document?.UndoStack.CanUndo ?? false;
    public bool CanRedo   => Document?.UndoStack.CanRedo ?? false;
    public bool IsSearchPanelOpen => PART_SearchBar?.Visibility == Visibility.Visible;

    /// <summary>Text-input event proxy (TextEntered / TextEntering) matching the Uno TextArea API.</summary>
    public TextAreaProxy TextArea => _textArea;

    /// <summary>Folding manager — operations apply to the <see cref="Document"/>.</summary>
    public FoldingManager FoldingManager { get; set; }

    /// <summary>Per-line syntax-highlighting source (stored; applied by consuming code).</summary>
    public IHighlightedLineSource HighlightedLineSource { get; set; }

    // Events
    public event EventHandler DocumentChanged;
    public event EventHandler TextChanged;
    public event PropertyChangedEventHandler OptionChanged;

    // Operations

    public void Select(int start, int length)
    {
        var doc = Document;
        int docLen = doc?.TextLength ?? 0;
        if (start < 0 || start > docLen) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || start + length > docLen) throw new ArgumentOutOfRangeException(nameof(length));
        SetSelection(start, start + length);
    }

    public void SetSelection(int startOffset, int endOffset)
    {
        if (Document == null) return;
        int len = Document.TextLength;
        SelectionStartOffset = Math.Clamp(Math.Min(startOffset, endOffset), 0, len);
        SelectionEndOffset   = Math.Clamp(Math.Max(startOffset, endOffset), 0, len);
        CurrentOffset        = SelectionEndOffset;
        if (PART_EditBox != null && !_updatingEditBox)
        {
            _updatingEditBox = true;
            try
            {
                PART_EditBox.SelectionStart  = SelectionStartOffset;
                PART_EditBox.SelectionLength = SelectionEndOffset - SelectionStartOffset;
            }
            finally { _updatingEditBox = false; }
        }
    }

    public void ScrollToLine(int lineNumber)
    {
        if (Document == null || lineNumber < 1 || lineNumber > Document.LineCount) return;
        var line = Document.GetLineByNumber(lineNumber);
        ScrollToOffset(line.Offset);
    }

    public void ScrollToOffset(int offset)
    {
        if (Document == null) return;
        CurrentOffset = Math.Clamp(offset, 0, Document.TextLength);
    }

    public void AppendText(string text)
    {
        var doc = EnsureDocument();
        doc.Insert(doc.TextLength, text ?? string.Empty);
    }

    public void Clear() => Text = string.Empty;

    public void BeginChange()          => EnsureDocument().BeginUpdate();
    public IDisposable DeclareChangeBlock() => EnsureDocument().RunUpdate();
    public void EndChange()            => EnsureDocument().EndUpdate();

    public void Undo()   { if (CanUndo) Document.UndoStack.Undo(); }
    public void Redo()   { if (CanRedo) Document.UndoStack.Redo(); }
    public void SelectAll() => Select(0, Document?.TextLength ?? 0);

    public void Copy()
    {
        if (SelectionLength == 0) return;
        var pkg = new DataPackage();
        pkg.SetText(SelectedText);
        Clipboard.SetContent(pkg);
        Clipboard.Flush();
    }

    public void Cut()
    {
        if (IsReadOnly || SelectionLength == 0) return;
        Copy();
        DeleteSelection();
    }

    public void Delete()
    {
        if (IsReadOnly) return;
        if (SelectionLength > 0) { DeleteSelection(); return; }
        var doc = Document;
        if (doc == null) return;
        int offset = Math.Clamp(CurrentOffset, 0, doc.TextLength);
        if (offset < doc.TextLength) { doc.Remove(offset, 1); Select(offset, 0); }
    }

    public async void Paste()
    {
        if (IsReadOnly) return;
        var view = Clipboard.GetContent();
        if (view.Contains(StandardDataFormats.Text))
        {
            string text = await view.GetTextAsync();
            if (text == null) return;
            var doc = EnsureDocument();
            if (SelectionLength > 0)
                doc.Replace(SelectionStart, SelectionLength, text);
            else
                doc.Insert(CurrentOffset, text);
        }
    }

    public void Load(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        using var reader = new StreamReader(stream, Encoding ?? Encoding.UTF8, true, 1024, leaveOpen: true);
        Text = reader.ReadToEnd();
        Encoding = reader.CurrentEncoding;
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Load(string fileName)
    {
        if (fileName == null) throw new ArgumentNullException(nameof(fileName));
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        Load(fs);
    }

    public void Save(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        using var writer = new StreamWriter(stream, Encoding ?? Encoding.UTF8, 1024, leaveOpen: true);
        writer.Write(Document?.Text ?? string.Empty);
        writer.Flush();
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Save(string fileName)
    {
        if (fileName == null) throw new ArgumentNullException(nameof(fileName));
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        Save(fs);
    }

    // Search

    public void OpenSearchPanel(string initialText = null)
    {
        PART_SearchBar.Visibility = Visibility.Visible;
        if (initialText != null)
            PART_SearchBox.Text = initialText;
        PART_SearchBox.Focus(FocusState.Programmatic);
        PART_SearchBox.SelectAll();
        RunSearch();
    }

    public void CloseSearchPanel()
    {
        PART_SearchBar.Visibility = Visibility.Collapsed;
        PART_EditBox.Focus(FocusState.Programmatic);
        _searchMatches.Clear();
        _searchIndex = -1;
        PART_SearchStatus.Text = string.Empty;
    }

    public void FindNext()
    {
        if (!IsSearchPanelOpen) { OpenSearchPanel(); return; }
        if (_searchMatches.Count == 0) return;
        _searchIndex = (_searchIndex + 1) % _searchMatches.Count;
        MoveToMatch(_searchIndex);
    }

    public void FindPrevious()
    {
        if (!IsSearchPanelOpen) { OpenSearchPanel(); return; }
        if (_searchMatches.Count == 0) return;
        _searchIndex = (_searchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;
        MoveToMatch(_searchIndex);
    }

    // Private

    private readonly TextAreaProxy _textArea;
    private bool _updatingEditBox;
    private bool _updatingDocument;
    private string _previousEditBoxText = string.Empty;
    private readonly List<int> _searchMatches = new List<int>();
    private int _searchIndex = -1;

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (e.OldValue is TextDocument old)
        {
            old.Changed -= editor.OnDocumentTextChanged;
            old.UndoStack.PropertyChanged -= editor.OnUndoStackChanged;
        }
        if (e.NewValue is TextDocument newDoc)
        {
            newDoc.Changed += editor.OnDocumentTextChanged;
            newDoc.UndoStack.PropertyChanged += editor.OnUndoStackChanged;
            editor.SyncEditBoxFromDocument();
        }
        editor.DocumentChanged?.Invoke(editor, EventArgs.Empty);
        editor.TextChanged?.Invoke(editor, EventArgs.Empty);
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).ApplyTheme(e.NewValue as TextEditorTheme ?? TextEditorTheme.Dark);

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (editor.PART_EditBox != null)
            editor.PART_EditBox.IsReadOnly = (bool)e.NewValue;
    }

    private static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (editor.PART_EditBox != null)
            editor.PART_EditBox.TextWrapping = (bool)e.NewValue ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private static void OnCurrentOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (editor.PART_EditBox != null && !editor._updatingEditBox)
        {
            editor._updatingEditBox = true;
            try
            {
                int offset = Math.Clamp((int)e.NewValue, 0, editor.PART_EditBox.Text.Length);
                editor.PART_EditBox.SelectionStart  = offset;
                editor.PART_EditBox.SelectionLength = 0;
            }
            finally { editor._updatingEditBox = false; }
        }
    }

    private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).OptionChanged?.Invoke(d, new PropertyChangedEventArgs(null));

    private void OnDocumentTextChanged(object sender, EventArgs e)
    {
        SyncEditBoxFromDocument();
        SyncIsModified();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnUndoStackChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(ICSharpCode.AvalonEdit.Document.UndoStack.IsOriginalFile))
            SyncIsModified();
    }

    private void SyncEditBoxFromDocument()
    {
        if (_updatingDocument || PART_EditBox == null || Document == null) return;
        _updatingEditBox = true;
        try
        {
            string newText = Document.Text;
            if (PART_EditBox.Text != newText)
            {
                int sel = PART_EditBox.SelectionStart;
                PART_EditBox.Text = newText;
                PART_EditBox.SelectionStart = Math.Clamp(sel, 0, newText.Length);
            }
            _previousEditBoxText = newText;
        }
        finally { _updatingEditBox = false; }
    }

    private void SyncIsModified()
    {
        bool modified = Document != null && !Document.UndoStack.IsOriginalFile;
        if ((bool)GetValue(IsModifiedProperty) != modified)
            SetValue(IsModifiedProperty, modified);
    }

    private void OnEditBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingEditBox || Document == null) return;
        string newText = PART_EditBox.Text;
        string oldText = _previousEditBoxText;

        // Detect single-char insertion and fire TextArea proxy events.
        if (newText.Length == oldText.Length + 1)
        {
            int pos = PART_EditBox.SelectionStart;
            if (pos > 0)
            {
                string ch = newText[pos - 1].ToString();
                _textArea.NotifyTextEntering(ch);
                _textArea.NotifyTextEntered(ch);
            }
        }
        _previousEditBoxText = newText;

        _updatingDocument = true;
        try { Document.Text = newText; }
        finally { _updatingDocument = false; }

        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditBoxSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingEditBox) return;
        int start  = PART_EditBox.SelectionStart;
        int length = PART_EditBox.SelectionLength;
        _updatingEditBox = true;
        try
        {
            CurrentOffset        = start + length;
            SelectionStartOffset = start;
            SelectionEndOffset   = start + length;
        }
        finally { _updatingEditBox = false; }
    }

    private void OnEditBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool ctrl  = IsKeyDown(VirtualKey.Control);
        bool shift = IsKeyDown(VirtualKey.Shift);

        if (ctrl && e.Key == VirtualKey.F)
        {
            string sel = SelectedText;
            OpenSearchPanel(sel.Contains('\n') ? null : (sel.Length > 0 ? sel : null));
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.F3)
        {
            if (shift) FindPrevious(); else FindNext();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape && IsSearchPanelOpen)
        {
            CloseSearchPanel();
            e.Handled = true;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => RunSearch();

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)   { FindNext();      e.Handled = true; }
        else if (e.Key == VirtualKey.Escape) { CloseSearchPanel(); e.Handled = true; }
    }

    private void OnFindNextClick(object sender, RoutedEventArgs e)  => FindNext();
    private void OnFindPrevClick(object sender, RoutedEventArgs e) => FindPrevious();
    private void OnCloseSearchClick(object sender, RoutedEventArgs e) => CloseSearchPanel();

    private void RunSearch()
    {
        _searchMatches.Clear();
        _searchIndex = -1;
        string query = PART_SearchBox.Text;
        string text  = Document?.Text ?? string.Empty;

        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(text))
        {
            PART_SearchStatus.Text = string.Empty;
            return;
        }

        int pos = 0;
        while ((pos = text.IndexOf(query, pos, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            _searchMatches.Add(pos);
            pos += query.Length;
        }

        if (_searchMatches.Count == 0)
            PART_SearchStatus.Text = "No results";
        else
        {
            _searchIndex = 0;
            MoveToMatch(0);
        }
    }

    private void MoveToMatch(int index)
    {
        if (index < 0 || index >= _searchMatches.Count) return;
        int start = _searchMatches[index];
        string query = PART_SearchBox.Text;
        _updatingEditBox = true;
        try
        {
            PART_EditBox.SelectionStart  = start;
            PART_EditBox.SelectionLength = query.Length;
            PART_EditBox.Focus(FocusState.Programmatic);
        }
        finally { _updatingEditBox = false; }
        PART_SearchStatus.Text = $"{index + 1} / {_searchMatches.Count}";
    }

    private void ApplyTheme(TextEditorTheme t)
    {
        if (PART_EditBox == null || EditorBorder == null) return;
        var editorBg = new SolidColorBrush(t.EditorBackground);
        var fg       = new SolidColorBrush(t.DefaultForeground);
        var border   = new SolidColorBrush(t.BorderColor);
        PART_EditBox.Background = editorBg;
        PART_EditBox.Foreground = fg;
        EditorBorder.Background  = editorBg;
        EditorBorder.BorderBrush = border;
        PART_SearchBar.Background  = new SolidColorBrush(t.GutterBackground);
        PART_SearchBar.BorderBrush = border;
    }

    private void DeleteSelection()
    {
        var doc = Document;
        if (doc == null) return;
        int start = Math.Min(SelectionStartOffset, SelectionEndOffset);
        doc.Remove(start, SelectionLength);
        Select(start, 0);
    }

    private TextDocument EnsureDocument()
    {
        if (Document == null) Document = new TextDocument();
        return Document;
    }

    private static bool IsKeyDown(VirtualKey key)
        => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
