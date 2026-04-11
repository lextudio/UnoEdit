// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Provides HTML clipboard format for highlighted text.
	/// </summary>
	public static class HtmlClipboard
	{
		/// <summary>Sets the HTML content on a DataObject.</summary>
		public static void SetHtml(object dataObject, string htmlFragment)
		{
			if (dataObject == null)
				throw new ArgumentNullException(nameof(dataObject));
			if (htmlFragment == null)
				throw new ArgumentNullException(nameof(htmlFragment));

			if (dataObject is DataObject dobj) {
				dobj.SetData(DataFormats.Html, htmlFragment);
				return;
			}

			// Best effort for WinUI DataPackage-like objects without taking a direct dependency.
			var type = dataObject.GetType();
			var setHtmlFormat = type.GetMethod("SetHtmlFormat", BindingFlags.Public | BindingFlags.Instance);
			if (setHtmlFormat != null) {
				setHtmlFormat.Invoke(dataObject, new object[] { htmlFragment });
				return;
			}

			var setData = type.GetMethod("SetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(object) }, null);
			if (setData != null)
				setData.Invoke(dataObject, new object[] { DataFormats.Html, htmlFragment });
		}

		/// <summary>Creates an HTML fragment for the specified document range.</summary>
		public static string CreateHtmlFragment(IDocument document, IHighlighter highlighter, ISegment segment, HtmlOptions options)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			if (segment == null)
				throw new ArgumentNullException(nameof(segment));

			var start = Math.Clamp(segment.Offset, 0, document.TextLength);
			var end = Math.Clamp(segment.EndOffset, start, document.TextLength);

			var richText = DocumentPrinter.ConvertTextDocumentToRichText(document, highlighter);
			var htmlBody = richText.ToHtml(start, end - start, options ?? new HtmlOptions());
			var fragment = "<!--StartFragment-->" + htmlBody + "<!--EndFragment-->";
			var html = "<html><body>" + fragment + "</body></html>";

			const string markerStartHtml = "StartHTML:";
			const string markerEndHtml = "EndHTML:";
			const string markerStartFragment = "StartFragment:";
			const string markerEndFragment = "EndFragment:";
			const string markerStartSelection = "StartSelection:";
			const string markerEndSelection = "EndSelection:";

			var headerTemplate =
				"Version:1.0\r\n" +
				markerStartHtml + "{0}\r\n" +
				markerEndHtml + "{1}\r\n" +
				markerStartFragment + "{2}\r\n" +
				markerEndFragment + "{3}\r\n" +
				markerStartSelection + "{2}\r\n" +
				markerEndSelection + "{3}\r\n";

			var headerLength = string.Format(CultureInfo.InvariantCulture, headerTemplate, 0, 0, 0, 0).Length;
			var startHtml = headerLength;
			var startFragment = startHtml + html.IndexOf("<!--StartFragment-->", StringComparison.Ordinal) + "<!--StartFragment-->".Length;
			var endFragment = startHtml + html.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);
			var endHtml = startHtml + html.Length;

			var header = string.Format(
				CultureInfo.InvariantCulture,
				headerTemplate,
				startHtml.ToString("D10", CultureInfo.InvariantCulture),
				endHtml.ToString("D10", CultureInfo.InvariantCulture),
				startFragment.ToString("D10", CultureInfo.InvariantCulture),
				endFragment.ToString("D10", CultureInfo.InvariantCulture));

			return header + html;
		}
	}
}
