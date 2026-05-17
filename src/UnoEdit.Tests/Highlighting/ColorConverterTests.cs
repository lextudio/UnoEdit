using System;
using System.Windows.Media;
using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
    [TestFixture]
    public class ColorConverterTests
    {
        private readonly ColorConverter _converter = new ColorConverter();

        // ── Named colours ────────────────────────────────────────────────────────────
        [Test]
        [TestCase("Black",       0,   0,   0)]
        [TestCase("White",     255, 255, 255)]
        [TestCase("Red",       255,   0,   0)]
        [TestCase("Blue",        0,   0, 255)]
        [TestCase("MidnightBlue", 25, 25, 112)]
        [TestCase("DarkCyan",     0, 139, 139)]
        [TestCase("DarkMagenta", 139, 0, 139)]
        [TestCase("SlateGray",  112, 128, 144)]
        public void NamedColor_ReturnsCorrectRgb(string name, byte r, byte g, byte b)
        {
            var result = _converter.ConvertFromInvariantString(name);
            Assert.That(result, Is.Not.Null);
            var color = (Color)result;
            Assert.That(color.R, Is.EqualTo(r), "R");
            Assert.That(color.G, Is.EqualTo(g), "G");
            Assert.That(color.B, Is.EqualTo(b), "B");
        }

        [Test]
        public void NamedColor_IsCaseInsensitive()
        {
            var lower = (Color)_converter.ConvertFromInvariantString("midnightblue");
            var upper = (Color)_converter.ConvertFromInvariantString("MIDNIGHTBLUE");
            Assert.That(lower, Is.EqualTo(upper));
        }

        // ── Hex formats ──────────────────────────────────────────────────────────────
        [Test]
        [TestCase("#FF0000", 255,   0,   0)]
        [TestCase("#00FF00",   0, 255,   0)]
        [TestCase("#0000FF",   0,   0, 255)]
        [TestCase("#ABC",    170, 187, 204)]   // 3-digit shorthand
        public void HexColor_Rgb_ReturnsCorrectRgb(string hex, byte r, byte g, byte b)
        {
            var result = _converter.ConvertFromInvariantString(hex);
            Assert.That(result, Is.Not.Null);
            var color = (Color)result;
            Assert.That(color.R, Is.EqualTo(r), "R");
            Assert.That(color.G, Is.EqualTo(g), "G");
            Assert.That(color.B, Is.EqualTo(b), "B");
        }

        [Test]
        public void HexColor_Argb_ReturnsCorrectAlpha()
        {
            // #80FF0000 = semi-transparent red
            var result = _converter.ConvertFromInvariantString("#80FF0000");
            Assert.That(result, Is.Not.Null);
            var color = (Color)result;
            Assert.That(color.A, Is.EqualTo(0x80));
            Assert.That(color.R, Is.EqualTo(0xFF));
            Assert.That(color.G, Is.EqualTo(0x00));
            Assert.That(color.B, Is.EqualTo(0x00));
        }

        // ── Error cases ───────────────────────────────────────────────────────────────
        [Test]
        public void UnknownName_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _converter.ConvertFromInvariantString("NotAColor"));

        [Test]
        public void BadHexLength_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _converter.ConvertFromInvariantString("#12345"));

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NullOrWhitespace_ReturnsNull(string value)
        {
            var result = _converter.ConvertFromInvariantString(value);
            Assert.That(result, Is.Null);
        }
    }
}
