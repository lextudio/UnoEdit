using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
    [TestFixture]
    public class HtmlOptionsTests
    {
        [Test]
        public void Constructor_FromTextEditorOptions_UsesIndentationSizeAsTabSize()
        {
            var editorOptions = new TextEditorOptions
            {
                IndentationSize = 8
            };

            var htmlOptions = new HtmlOptions(editorOptions);

            Assert.That(htmlOptions.TabSize, Is.EqualTo(8));
        }

        [Test]
        public void WriteStyleAttributeForColor_WritesEscapedCss()
        {
            var options = new HtmlOptions();
            var color = new HighlightingColor
            {
                Foreground = new SimpleHighlightingBrush(System.Windows.Media.Colors.Red),
                FontWeight = System.Windows.FontWeights.Bold
            };

            using var writer = new StringWriter();
            options.WriteStyleAttributeForColor(writer, color);

            Assert.That(writer.ToString(), Is.EqualTo(" style=\"color: #ff0000; font-weight: bold; \""));
        }

        [Test]
        public void ColorNeedsSpanForStyling_ReturnsFalseForEmptyColor()
        {
            var options = new HtmlOptions();

            Assert.That(options.ColorNeedsSpanForStyling(new HighlightingColor()), Is.False);
        }
    }
}
