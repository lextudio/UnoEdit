using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using UnoEdit.Logging;
using UnoEdit.Skia.Desktop.Controls;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using FontStyleFlags = TextMateSharp.Themes.FontStyle;

namespace ICSharpCode.AvalonEdit.TextMate
{
	/// <summary>
	/// TextMateSharp-backed highlighted-line source for UnoEdit.
	/// </summary>
	public sealed class TextMateLineHighlighter : IHighlightedLineSource, ITextViewAwareHighlightedLineSource, IVisibleRangeWarmableHighlightedLineSource, IVisibleRangeReadyHighlightedLineSource, IModelTokensChangedListener
	{
		static void LogTM(string msg) { HighlightLogger.Log("TM", msg); }

		readonly Registry registry;
		readonly IRegistryOptions registryOptions;
		readonly Action<Exception> exceptionHandler;
		readonly Dictionary<(int Foreground, int Background, FontStyleFlags FontStyle), HighlightingColor> colorCache
			= new Dictionary<(int Foreground, int Background, FontStyleFlags FontStyle), HighlightingColor>();
		readonly Dictionary<int, CachedLineHighlight> lineHighlightCache
			= new Dictionary<int, CachedLineHighlight>();

		TextDocument document;
		TextDocument cachedDocument;
		ITextView textView;
		TextDocumentLineList lineList;
		TMModel model;
		IGrammar grammar;
		Theme theme;
		ReadOnlyDictionary<string, string> themeColorsDictionary;

		sealed class CachedLineHighlight
		{
			public HighlightedLine HighlightedLine { get; init; }
			public bool IsComplete { get; init; }
		}

		public event EventHandler HighlightingInvalidated;
		public event EventHandler<HighlightedLineRangeInvalidatedEventArgs> HighlightingRangeInvalidated;

		public TextMateLineHighlighter(IRegistryOptions registryOptions, Action<Exception> exceptionHandler = null)
		{
			this.registryOptions = registryOptions ?? throw new ArgumentNullException(nameof(registryOptions));
			this.exceptionHandler = exceptionHandler;
			registry = new Registry(registryOptions);
			SetTheme(registryOptions.GetDefaultTheme());
		}

		public IRegistryOptions RegistryOptions => registryOptions;

		public void SetTextView(ITextView textView)
		{
			if (ReferenceEquals(this.textView, textView))
				return;

			this.textView = textView;
			if (document != null && textView != null) {
				RecreateModel();
			} else if (textView == null) {
				DisposeModel();
			}
		}

		public void SetDocument(TextDocument document)
		{
			if (ReferenceEquals(this.document, document))
				return;

			if (this.document != null) {
				this.document.TextChanged -= Document_TextChanged;
			}

			this.document = document;

			if (document == null) {
				DisposeModel();
				RaiseHighlightingInvalidated();
				return;
			}

			if (!ReferenceEquals(cachedDocument, document)) {
				ClearLineCache();
				cachedDocument = document;
			}

			document.TextChanged += Document_TextChanged;
			RecreateModel();
			RaiseHighlightingInvalidated();
		}

		public void SetGrammar(string scopeName)
		{
			if (string.IsNullOrWhiteSpace(scopeName))
				throw new ArgumentException("Scope name must not be empty.", nameof(scopeName));

			SetGrammarInternal(registry.LoadGrammar(scopeName));
		}

		public void SetGrammarFile(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Path must not be empty.", nameof(path));

			SetGrammarInternal(registry.LoadGrammarFromPathSync(path, 0, null));
		}

		public void SetGrammarByExtension(string extension)
		{
			if (string.IsNullOrWhiteSpace(extension))
				throw new ArgumentException("Extension must not be empty.", nameof(extension));

			if (registryOptions is not RegistryOptions builtInRegistryOptions)
				throw new NotSupportedException("SetGrammarByExtension requires TextMateSharp.Grammars.RegistryOptions.");

			string scopeName = builtInRegistryOptions.GetScopeByExtension(extension);
			SetGrammar(scopeName);
		}

		public void SetTheme(IRawTheme rawTheme)
		{
			if (rawTheme == null)
				throw new ArgumentNullException(nameof(rawTheme));

			registry.SetTheme(rawTheme);
			theme = registry.GetTheme();
			themeColorsDictionary = theme.GetGuiColorDictionary();
			colorCache.Clear();
			ClearLineCache();
			model?.InvalidateLine(0);
			lineList?.InvalidateViewPortLines();
			RaiseHighlightingInvalidated();
		}

		public void SetTheme(ThemeName themeName)
		{
			SetTheme(new RegistryOptions(themeName).GetDefaultTheme());
		}

		public bool TryGetThemeColor(string colorKey, out string colorString)
		{
			if (themeColorsDictionary == null) {
				colorString = null;
				return false;
			}

			return themeColorsDictionary.TryGetValue(colorKey, out colorString);
		}

		public HighlightedLine HighlightLine(int lineNumber)
		{
			if (document == null || model == null || theme == null)
				return null;

			if (lineHighlightCache.TryGetValue(lineNumber, out CachedLineHighlight cachedLine) && cachedLine.IsComplete) {
				LogTM($"HighlightLine lineNumber={lineNumber} cache=hit sections={(cachedLine.HighlightedLine == null ? 0 : cachedLine.HighlightedLine.Sections.Count)}");
				return cachedLine.HighlightedLine;
			}

			int modelLineIndex = lineNumber - 1;
			DocumentLine line = document.GetLineByNumber(lineNumber);
			List<TMToken> tokens = model.GetLineTokens(modelLineIndex);
			if (tokens == null) {
				LogTM($"HighlightLine lineNumber={lineNumber} tokens=null cache=miss retry=warm");
				model.InvalidateLine(modelLineIndex);
				lineList?.WarmLineRange(modelLineIndex, modelLineIndex);
				tokens = model.GetLineTokens(modelLineIndex);
				if (tokens == null) {
					LogTM($"HighlightLine lineNumber={lineNumber} tokens=null cache=miss retry=failed");
					return null;
				}
			}

			if (tokens.Count == 0) {
				LogTM($"HighlightLine lineNumber={lineNumber} tokens=empty");
				return null;
			}

			LogTM($"HighlightLine lineNumber={lineNumber} tokenCount={tokens.Count}");
			var highlightedLine = new HighlightedLine(document, line);
			int lineLength = line.Length;

			for (int i = 0; i < tokens.Count; i++) {
				TMToken token = tokens[i];
				TMToken nextToken = i + 1 < tokens.Count ? tokens[i + 1] : null;

				int startIndex = Clamp(token.StartIndex, 0, lineLength);
				int endIndex = Clamp(nextToken?.StartIndex ?? lineLength, 0, lineLength);
				if (startIndex >= endIndex || token.Scopes == null || token.Scopes.Count == 0)
					continue;

				HighlightingColor color = GetOrCreateColor(token.Scopes);
				if (color == null)
					continue;

				highlightedLine.Sections.Add(new HighlightedSection {
					Offset = line.Offset + startIndex,
					Length = endIndex - startIndex,
					Color = color
				});
			}

			LogTM($"HighlightLine lineNumber={lineNumber} sectionCount={highlightedLine.Sections.Count}");
			if (highlightedLine.Sections.Count == 0) {
				return null;
			}

			lineHighlightCache[lineNumber] = new CachedLineHighlight { HighlightedLine = highlightedLine, IsComplete = true };
			return highlightedLine;
		}

		public void WarmVisibleLineRange(int startLineNumber, int endLineNumber)
		{
			if (document == null || lineList == null || model == null)
				return;

			int startLineIndex = Math.Max(0, startLineNumber - 1);
			int endLineIndex = Math.Max(startLineIndex, endLineNumber - 1);
			LogTM($"WarmVisibleLineRange startLine={startLineNumber} endLine={endLineNumber}");
			lineList.WarmLineRange(startLineIndex, endLineIndex);
		}

		public bool IsVisibleLineRangeReady(int startLineNumber, int endLineNumber)
		{
			if (document == null || model == null)
				return false;

			startLineNumber = Math.Max(1, startLineNumber);
			endLineNumber = Math.Max(startLineNumber, endLineNumber);
			for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++) {
				if (model.GetLineTokens(lineNumber - 1) == null) {
					return false;
				}
			}

			return true;
		}

		void SetGrammarInternal(IGrammar grammar)
		{
			this.grammar = grammar;
			ClearLineCache();
			if (model != null) {
				model.SetGrammar(grammar);
				model.InvalidateLine(0);
				lineList?.InvalidateViewPortLines();
			}
			RaiseHighlightingInvalidated();
		}

		HighlightingColor GetOrCreateColor(IList<string> scopes)
		{
			int foreground = 0;
			int background = 0;
			FontStyleFlags fontStyle = FontStyleFlags.NotSet;

			foreach (ThemeTrieElementRule rule in theme.Match(scopes)) {
				if (foreground == 0 && rule.foreground > 0)
					foreground = rule.foreground;
				if (background == 0 && rule.background > 0)
					background = rule.background;
				if (fontStyle == FontStyleFlags.NotSet && rule.fontStyle != FontStyleFlags.NotSet)
					fontStyle = rule.fontStyle;
			}

			if (foreground == 0 && background == 0 && fontStyle == FontStyleFlags.NotSet)
				return null;

			var key = (foreground, background, fontStyle);
			if (colorCache.TryGetValue(key, out HighlightingColor existing))
				return existing;

			var color = new HighlightingColor();
			if (foreground > 0 && TryGetThemeColorById(foreground, out System.Windows.Media.Color fgColor))
				color.Foreground = new SimpleHighlightingBrush(fgColor);
			if (background > 0 && TryGetThemeColorById(background, out System.Windows.Media.Color bgColor))
				color.Background = new SimpleHighlightingBrush(bgColor);
			if ((fontStyle & FontStyleFlags.Italic) != 0)
				color.FontStyle = System.Windows.FontStyles.Italic;
			if ((fontStyle & FontStyleFlags.Bold) != 0)
				color.FontWeight = System.Windows.FontWeights.Bold;
			if ((fontStyle & FontStyleFlags.Underline) != 0)
				color.Underline = true;
			color.Freeze();

			colorCache[key] = color;
			return color;
		}

		bool TryGetThemeColorById(int colorId, out System.Windows.Media.Color color)
		{
			color = default(System.Windows.Media.Color);
			if (theme == null)
				return false;

			string[] colorMap = theme.GetColorMap().ToArray();
			if (colorId < 0 || colorId >= colorMap.Length)
				return false;

			string themeColor = colorMap[colorId];
			if (string.IsNullOrEmpty(themeColor))
				return false;

			color = ParseColor(themeColor);
			return true;
		}

		static System.Windows.Media.Color ParseColor(string color)
		{
			string normalizedColor = NormalizeColor(color);
			if (normalizedColor.Length == 7) {
				return System.Windows.Media.Color.FromRgb(
					Convert.ToByte(normalizedColor.Substring(1, 2), 16),
					Convert.ToByte(normalizedColor.Substring(3, 2), 16),
					Convert.ToByte(normalizedColor.Substring(5, 2), 16));
			}

			if (normalizedColor.Length == 9) {
				return System.Windows.Media.Color.FromArgb(
					Convert.ToByte(normalizedColor.Substring(1, 2), 16),
					Convert.ToByte(normalizedColor.Substring(3, 2), 16),
					Convert.ToByte(normalizedColor.Substring(5, 2), 16),
					Convert.ToByte(normalizedColor.Substring(7, 2), 16));
			}

			throw new InvalidOperationException("Unsupported TextMate color format: " + color);
		}

		static string NormalizeColor(string color)
		{
			if (color != null && color.Length == 9) {
				return "#" + color[7] + color[8] + color[1] + color[2] + color[3] + color[4] + color[5] + color[6];
			}

			return color;
		}

		static int Clamp(int value, int min, int max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		void Document_TextChanged(object sender, EventArgs e)
		{
			ClearLineCache();
			cachedDocument = document;
		}

		void ClearLineCache()
		{
			lineHighlightCache.Clear();
		}

		void InvalidateCachedLineRange(int startLineNumber, int endLineNumber)
		{
			if (lineHighlightCache.Count == 0)
				return;

			startLineNumber = Math.Max(1, startLineNumber);
			endLineNumber = Math.Max(startLineNumber, endLineNumber);
			for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++) {
				lineHighlightCache.Remove(lineNumber);
			}
		}

		void RaiseHighlightingInvalidated()
		{
			HighlightingInvalidated?.Invoke(this, EventArgs.Empty);
		}

		public void ModelTokensChanged(ModelTokensChangedEvent e)
		{
			var t = Thread.CurrentThread;
			LogTM($"ModelTokensChanged thread={t.ManagedThreadId} name={t.Name} isBackground={t.IsBackground}");
			if (e?.Ranges == null || model == null || model.IsStopped) {
				return;
			}

			if (textView is null) {
				RaiseHighlightingInvalidated();
				return;
			}

			int firstChangedLine = int.MaxValue;
			int lastChangedLine = -1;
			foreach (var range in e.Ranges) {
				firstChangedLine = Math.Min(firstChangedLine, range.FromLineNumber);
				lastChangedLine = Math.Max(lastChangedLine, range.ToLineNumber);
			}

			if (firstChangedLine == int.MaxValue || lastChangedLine < 0) {
				return;
			}

			InvalidateCachedLineRange(firstChangedLine, lastChangedLine);

			int firstVisibleLine = textView.FirstVisibleLineNumber;
			int lastVisibleLine = textView.LastVisibleLineNumber;
			if (firstVisibleLine > 0 && lastVisibleLine > 0
				&& (lastChangedLine < firstVisibleLine || firstChangedLine > lastVisibleLine)) {
				LogTM($"ModelTokensChanged skipped changed={firstChangedLine}-{lastChangedLine} visible={firstVisibleLine}-{lastVisibleLine}");
				return;
			}

			int intersectedStart = firstVisibleLine > 0 ? Math.Max(firstChangedLine, firstVisibleLine) : firstChangedLine;
			int intersectedEnd = lastVisibleLine > 0 ? Math.Min(lastChangedLine, lastVisibleLine) : lastChangedLine;
			if (intersectedEnd < intersectedStart) {
				return;
			}

			void RaiseOnUiThread()
			{
				LogTM($"ModelTokensChanged -> HighlightingRangeInvalidated changed={firstChangedLine}-{lastChangedLine} visible={firstVisibleLine}-{lastVisibleLine} redraw={intersectedStart}-{intersectedEnd}");
				HighlightingRangeInvalidated?.Invoke(this, new HighlightedLineRangeInvalidatedEventArgs(intersectedStart, intersectedEnd));
			}

			var dispatcherQueue = textView.DispatcherQueue;
			if (dispatcherQueue is not null && dispatcherQueue.HasThreadAccess) {
				RaiseOnUiThread();
				return;
			}

			if (dispatcherQueue is null || !dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, RaiseOnUiThread)) {
				RaiseOnUiThread();
			}
		}

		void RecreateModel()
		{
			DisposeModel();
			if (document == null || textView == null)
				return;

			lineList = new TextDocumentLineList(textView, document, exceptionHandler);
			model = new TMModel(lineList);
			if (grammar != null)
				model.SetGrammar(grammar);
			model.AddModelTokensChangedListener(this);
		}

		void DisposeModel()
		{
			if (model != null)
				model.RemoveModelTokensChangedListener(this);
			model?.Dispose();
			model = null;
			lineList?.Dispose();
			lineList = null;
		}

		public void Dispose()
		{
			if (document != null) {
				document.TextChanged -= Document_TextChanged;
			}
			DisposeModel();
		}
	}
}
