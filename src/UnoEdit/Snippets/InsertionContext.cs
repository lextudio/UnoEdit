// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using System.Reflection;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>Tracks the context of a snippet insertion.</summary>
	public class InsertionContext
	{
		private readonly TextDocument document;
		private readonly Dictionary<SnippetElement, IActiveElement> elementMap = new Dictionary<SnippetElement, IActiveElement>();
		private readonly List<IActiveElement> registeredElements = new List<IActiveElement>();

		/// <summary>Creates a new InsertionContext.</summary>
		public InsertionContext(object textArea, int insertionPosition)
		{
			TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
			document = GetValue<TextDocument>(textArea, "Document")
				?? throw new ArgumentException("textArea must expose a Document property.", nameof(textArea));

			SelectedText = ReadSelectedText(textArea, document);

			var startLine = document.GetLineByOffset(Math.Clamp(insertionPosition, 0, document.TextLength));
			var indentationSegment = TextUtilities.GetWhitespaceAfter(document, startLine.Offset);
			var indentLength = Math.Min(indentationSegment.EndOffset, insertionPosition) - indentationSegment.Offset;
			if (indentLength > 0)
				Indentation = document.GetText(indentationSegment.Offset, indentLength);

			var options = GetValue<object>(textArea, "Options");
			Tab = GetValue<string>(options, "IndentationString") ?? "\t";
			LineTerminator = TextUtilities.GetNewLineFromDocument(document, startLine.LineNumber);

			InsertionPosition = insertionPosition;
			StartPosition = insertionPosition;
		}

		/// <summary>Gets the text area.</summary>
		public object TextArea { get; private set; }

		/// <summary>Gets the document.</summary>
				public TextDocument Document => document;

		/// <summary>Gets the selected text at insertion time.</summary>
		public string SelectedText { get; private set; } = string.Empty;

		/// <summary>Gets the indentation string.</summary>
		public string Indentation { get; private set; } = string.Empty;

		/// <summary>Gets the tab string.</summary>
		public string Tab { get; private set; } = "\t";

		/// <summary>Gets the line terminator string.</summary>
		public string LineTerminator { get; private set; } = "\n";

		/// <summary>Gets/sets the current insertion position.</summary>
		public int InsertionPosition { get; set; }

		/// <summary>Gets the start position of the snippet.</summary>
		public int StartPosition { get; private set; }

		/// <summary>Inserts text at the current position.</summary>
		public void InsertText(string text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			text = text.Replace("\t", Tab);
			using (Document.RunUpdate()) {
				int textOffset = 0;
				SimpleSegment segment;
				while ((segment = NewLineFinder.NextNewLine(text, textOffset)) != SimpleSegment.Invalid) {
					var insert = text.Substring(textOffset, segment.Offset - textOffset) + LineTerminator + Indentation;
					Document.Insert(InsertionPosition, insert);
					InsertionPosition += insert.Length;
					textOffset = segment.EndOffset;
				}

				var remaining = text.Substring(textOffset);
				Document.Insert(InsertionPosition, remaining);
				InsertionPosition += remaining.Length;
			}
		}

		/// <summary>Registers an active element.</summary>
		public void RegisterActiveElement(SnippetElement owner, IActiveElement element)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			elementMap[owner] = element;
			registeredElements.Add(element);
		}

		/// <summary>Gets the active element for the given owner.</summary>
		public IActiveElement GetActiveElement(SnippetElement owner)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			return elementMap.TryGetValue(owner, out var value) ? value : null;
		}

		/// <summary>Gets all active elements.</summary>
				public IEnumerable<IActiveElement> ActiveElements => registeredElements;

		/// <summary>Raises the <see cref="InsertionCompleted"/> event.</summary>
		public void RaiseInsertionCompleted(EventArgs e) { InsertionCompleted?.Invoke(this, e); }

		/// <summary>Raised when insertion is completed.</summary>
		public event EventHandler InsertionCompleted;

		/// <summary>Deactivates the insertion context.</summary>
		public void Deactivate(SnippetEventArgs e)
		{
			var args = e ?? new SnippetEventArgs(DeactivateReason.Unknown);
			foreach (var element in registeredElements)
				element.Deactivate(args);
			Deactivated?.Invoke(this, args);
		}

		/// <summary>Raised when the context is deactivated.</summary>
		public event EventHandler<SnippetEventArgs> Deactivated;

		/// <summary>Links a main element to bound elements.</summary>
		public void Link(ISegment mainElement, ISegment[] boundElements)
		{
			if (mainElement == null)
				throw new ArgumentNullException(nameof(mainElement));
			boundElements ??= Array.Empty<ISegment>();

			var text = Document.GetText(mainElement.Offset, mainElement.Length);
			var mainSnippetElement = new SnippetReplaceableTextElement { Text = text };
			RegisterActiveElement(mainSnippetElement, new ReplaceableSegmentElement(mainElement, text));

			foreach (var segment in boundElements) {
				if (segment == null)
					continue;
				RegisterActiveElement(new SnippetBoundElement { TargetElement = mainSnippetElement }, new ReplaceableSegmentElement(segment, Document.GetText(segment.Offset, segment.Length)));
			}
		}

		private static T GetValue<T>(object instance, string propertyName) where T : class
		{
			if (instance == null)
				return null;
			var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if (property == null)
				return null;
			return property.GetValue(instance) as T;
		}

		private static string ReadSelectedText(object textArea, TextDocument document)
		{
			var explicitSelectedText = GetValue<string>(textArea, "SelectedText");
			if (!string.IsNullOrEmpty(explicitSelectedText))
				return explicitSelectedText;

			var start = GetIntValue(textArea, "SelectionStartOffset");
			var end = GetIntValue(textArea, "SelectionEndOffset");
			if (start < 0 || end < 0)
				return string.Empty;

			if (end < start) {
				var tmp = start;
				start = end;
				end = tmp;
			}
			start = Math.Clamp(start, 0, document.TextLength);
			end = Math.Clamp(end, start, document.TextLength);
			return end > start ? document.GetText(start, end - start) : string.Empty;
		}

		private static int GetIntValue(object instance, string propertyName)
		{
			if (instance == null)
				return -1;
			var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if (property == null)
				return -1;
			var value = property.GetValue(instance);
			return value is int i ? i : -1;
		}

		private sealed class ReplaceableSegmentElement : IReplaceableActiveElement
		{
			private readonly string text;

			public ReplaceableSegmentElement(ISegment segment, string text)
			{
				Segment = segment;
				this.text = text ?? string.Empty;
			}

			public string Text => text;
			public bool IsEditable => true;
			public ISegment Segment { get; }
			public event EventHandler TextChanged;
			public void OnInsertionCompleted() { }
			public void Deactivate(SnippetEventArgs e) { }
		}
	}
}
