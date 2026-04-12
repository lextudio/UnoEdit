using System.Globalization;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextRunPropertiesTests
{
    [Test]
    public void Constructor_InitializesConcreteDefaults()
    {
        var properties = new VisualLineElementTextRunProperties();

        Assert.That(properties.Typeface, Is.Not.Null);
        Assert.That(properties.TextDecorations, Is.Not.Null);
        Assert.That(properties.TextEffects, Is.Not.Null);
        Assert.That(properties.CultureInfo, Is.Not.Null);
        Assert.That(properties.NumberSubstitution, Is.Not.Null);
        Assert.That(properties.TypographyProperties, Is.SameAs(DefaultTextRunTypographyProperties.Default));
        Assert.That(properties.BaselineAlignment, Is.EqualTo("Baseline"));
    }

    [Test]
    public void SettersAndClone_PreserveConfiguredState()
    {
        var properties = new VisualLineElementTextRunProperties();
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        var typeface = new VisualLineElementTextRunProperties.TypefaceDescriptor("Cascadia Mono", "Italic", "Bold", "Expanded");
        var numberSubstitution = new VisualLineElementTextRunProperties.NumberSubstitutionDescriptor("Override", "European");

        properties.SetTypeface(typeface);
        properties.SetTextDecorations("Underline");
        properties.SetTextEffects(new object[] { "Shadow" });
        properties.SetCultureInfo(culture);
        properties.SetNumberSubstitution(numberSubstitution);
        properties.SetTypographyProperties("CustomTypography");
        properties.SetBaselineAlignment("Superscript");
        properties.SetFontRenderingEmSize(14);
        properties.SetFontHintingEmSize(13);

        var clone = properties.Clone();

        Assert.That(clone.Typeface, Is.EqualTo(typeface));
        Assert.That(clone.CultureInfo, Is.EqualTo(culture));
        Assert.That(clone.NumberSubstitution, Is.EqualTo(numberSubstitution));
        Assert.That(clone.TypographyProperties, Is.EqualTo("CustomTypography"));
        Assert.That(clone.BaselineAlignment, Is.EqualTo("Superscript"));
        Assert.That(clone.FontRenderingEmSize, Is.EqualTo(14));
        Assert.That(clone.FontHintingEmSize, Is.EqualTo(13));
        Assert.That(clone.TextDecorations, Is.Not.SameAs(properties.TextDecorations));
        Assert.That(clone.TextEffects, Is.Not.SameAs(properties.TextEffects));
    }
}
