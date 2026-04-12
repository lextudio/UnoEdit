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

    [Test]
    public void TypographyProperties_CanCarryConfiguredFeatureState()
    {
        var typography = new DefaultTextRunTypographyProperties(
            annotationAlternates: 2,
            capitals: DefaultTextRunTypographyProperties.TypographyKind.Normal,
            capitalSpacing: true,
            caseSensitiveForms: true,
            contextualAlternates: false,
            contextualLigatures: false,
            contextualSwashes: 3,
            discretionaryLigatures: true,
            eastAsianExpertForms: true,
            eastAsianLanguage: DefaultTextRunTypographyProperties.TypographyKind.Proportional,
            eastAsianWidths: DefaultTextRunTypographyProperties.TypographyKind.Default,
            fraction: DefaultTextRunTypographyProperties.TypographyKind.Normal,
            historicalForms: true,
            historicalLigatures: true,
            kerning: false,
            mathematicalGreek: true,
            numeralAlignment: DefaultTextRunTypographyProperties.TypographyKind.Default,
            numeralStyle: DefaultTextRunTypographyProperties.TypographyKind.Proportional,
            slashedZero: true,
            standardLigatures: false,
            standardSwashes: 4,
            stylisticAlternates: 5,
            variants: DefaultTextRunTypographyProperties.TypographyKind.Proportional,
            stylisticSets: new[] { true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false });

        Assert.That(typography.AnnotationAlternates, Is.EqualTo(2));
        Assert.That(typography.CapitalSpacing, Is.True);
        Assert.That(typography.CaseSensitiveForms, Is.True);
        Assert.That(typography.ContextualAlternates, Is.False);
        Assert.That(typography.ContextualLigatures, Is.False);
        Assert.That(typography.ContextualSwashes, Is.EqualTo(3));
        Assert.That(typography.DiscretionaryLigatures, Is.True);
        Assert.That(typography.EastAsianExpertForms, Is.True);
        Assert.That(typography.HistoricalForms, Is.True);
        Assert.That(typography.HistoricalLigatures, Is.True);
        Assert.That(typography.Kerning, Is.False);
        Assert.That(typography.MathematicalGreek, Is.True);
        Assert.That(typography.SlashedZero, Is.True);
        Assert.That(typography.StandardLigatures, Is.False);
        Assert.That(typography.StandardSwashes, Is.EqualTo(4));
        Assert.That(typography.StylisticAlternates, Is.EqualTo(5));
        Assert.That(typography.StylisticSet1, Is.True);
        Assert.That(typography.StylisticSet2, Is.False);
        Assert.That(typography.StylisticSet19, Is.True);
        Assert.That(typography.StylisticSet20, Is.False);
    }
}
