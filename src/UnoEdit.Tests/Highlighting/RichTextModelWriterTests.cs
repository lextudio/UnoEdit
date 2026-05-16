using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
	[TestFixture]
	public class RichTextModelWriterTests
	{
		[Test]
		public void Writer_InsertsTextAndForegroundHighlighting()
		{
			var document = new TextDocument("seed");
			var model = new RichTextModel();
			var writer = new RichTextModelWriter(model, document, 0);

			writer.BeginSpan(Color.FromArgb(255, 0x12, 0x34, 0x56));
			writer.Write("hi");
			writer.EndSpan();

			Assert.That(document.Text, Is.EqualTo("hiseed"));
			var sections = model.GetHighlightedSections(0, 2);
			var first = System.Linq.Enumerable.First(sections);
			Assert.That(first.Offset, Is.EqualTo(0));
			Assert.That(first.Length, Is.EqualTo(2));
			Assert.That(first.Color.Foreground, Is.Not.Null);
			Assert.That(first.Color.Foreground.GetColor(), Is.EqualTo(Color.FromArgb(255, 0x12, 0x34, 0x56)));
		}

		[Test]
		public void Writer_TracksInsertionOffsetAndNestedSpans()
		{
			var document = new TextDocument("ab");
			var model = new RichTextModel();
			var writer = new RichTextModelWriter(model, document, 1);

			writer.BeginSpan(new HighlightingColor { FontWeight = System.Windows.FontWeights.Bold });
			writer.Write("X");
			writer.BeginSpan(new HighlightingColor { FontStyle = System.Windows.FontStyles.Italic });
			writer.Write("Y");
			writer.EndSpan();
			writer.EndSpan();

			Assert.That(document.Text, Is.EqualTo("aXYb"));
			Assert.That(writer.InsertionOffset, Is.EqualTo(3));

			var sections = System.Linq.Enumerable.ToArray(model.GetHighlightedSections(1, 2));
			Assert.That(sections.Length, Is.EqualTo(2));
			Assert.That(sections[0].Length, Is.EqualTo(1));
			Assert.That(sections[0].Color.FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));
			Assert.That(sections[0].Color.FontStyle, Is.Null);
			Assert.That(sections[1].Length, Is.EqualTo(1));
			Assert.That(sections[1].Color.FontWeight, Is.EqualTo(System.Windows.FontWeights.Bold));
			Assert.That(sections[1].Color.FontStyle, Is.EqualTo(System.Windows.FontStyles.Italic));
		}
	}
}
