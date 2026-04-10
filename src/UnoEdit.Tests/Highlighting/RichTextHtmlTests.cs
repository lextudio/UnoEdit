using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
	[TestFixture]
	public class RichTextHtmlTests
	{
		[Test]
		public void HighlightedLine_ToHtml_EncodesAndWrapsHighlightedSpan()
		{
			var document = new TextDocument("ab<cd>");
			var line = document.GetLineByNumber(1);
			var highlighted = new HighlightedLine(document, line);
			highlighted.Sections.Add(new HighlightedSection {
				Offset = 2,
				Length = 4,
				Color = new HighlightingColor {
					Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(255, 0x11, 0x22, 0x33))
				}
			});

			string html = highlighted.ToHtml();

			Assert.That(html, Is.EqualTo("ab<span style=\"color: #112233; \">&lt;cd&gt;</span>"));
		}

		[Test]
		public void RichText_ToHtml_Substring_PreservesHighlighting()
		{
			var model = new RichTextModel();
			model.SetForeground(1, 3, new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(255, 0xaa, 0xbb, 0xcc)));
			var richText = new RichText("hello", model);

			string html = richText.Substring(1, 3).ToHtml();

			Assert.That(html, Is.EqualTo("<span style=\"color: #aabbcc; \">ell</span>"));
		}

		[Test]
		public void RichText_Concat_PreservesSectionsAcrossBoundary()
		{
			var leftModel = new RichTextModel();
			leftModel.SetForeground(0, 1, new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(255, 0xff, 0, 0)));
			var rightModel = new RichTextModel();
			rightModel.SetForeground(0, 1, new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0xff)));

			var richText = RichText.Concat(new RichText("A", leftModel), new RichText("B", rightModel));

			Assert.That(richText.ToHtml(), Is.EqualTo("<span style=\"color: #ff0000; \">A</span><span style=\"color: #0000ff; \">B</span>"));
		}
	}
}
