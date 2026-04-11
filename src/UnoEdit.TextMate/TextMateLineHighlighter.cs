using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
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
	public sealed class TextMateLineHighlighter : IHighlightedLineSource, IModelTokensChangedListener
	{
		readonly Registry registry;
		readonly IRegistryOptions registryOptions;
		readonly Action<Exception> exceptionHandler;
		readonly Dictionary<(int Foreground, int Background, FontStyleFlags FontStyle), HighlightingColor> colorCache
			= new Dictionary<(int Foreground, int Background, FontStyleFlags FontStyle), HighlightingColor>();

		TextDocument document;
		TextDocumentLineList lineList;
		TMModel model;
		IGrammar grammar;
		Theme theme;
		ReadOnlyDictionary<string, string> themeColorsDictionary;

		public event EventHandler HighlightingInvalidated;

		public TextMateLineHighlighter(IRegistryOptions registryOptions, Action<Exception> exceptionHandler = null)
		{
			this.registryOptions = registryOptions ?? throw new ArgumentNullException(nameof(registryOptions));
			this.exceptionHandler = exceptionHandler;
			registry = new Registry(registryOptions);
			SetTheme(registryOptions.GetDefaultTheme());
		}

		public IRegistryOptions RegistryOptions => registryOptions;

		public void SetDocument(TextDocument document)
		{
			if (ReferenceEquals(this.document, document))
				return;

			DisposeModel();
			this.document = document;

			if (document == null) {
				RaiseHighlightingInvalidated();
				return;
			}

			lineList = new TextDocumentLineList(document, exceptionHandler);
			model = new TMModel(lineList);
			if (grammar != null)
				model.SetGrammar(grammar);
			model.AddModelTokensChangedListener(this);
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
			model?.InvalidateLine(0);
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

			int modelLineIndex = lineNumber - 1;
			model.ForceTokenization(modelLineIndex);

			DocumentLine line = document.GetLineByNumber(lineNumber);
			List<TMToken> tokens = model.GetLineTokens(modelLineIndex);
			if (tokens == null || tokens.Count == 0) {
				return null;
			}

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

			if (highlightedLine.Sections.Count == 0) {
				return null;
			}

			return highlightedLine;
		}

		void SetGrammarInternal(IGrammar grammar)
		{
			this.grammar = grammar;
			if (model != null) {
				model.SetGrammar(grammar);
				model.InvalidateLine(0);
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

		void RaiseHighlightingInvalidated()
		{
			HighlightingInvalidated?.Invoke(this, EventArgs.Empty);
		}

		public void ModelTokensChanged(ModelTokensChangedEvent e)
		{
			RaiseHighlightingInvalidated();
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
			DisposeModel();
		}
	}
}
