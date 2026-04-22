using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Xaml.Media;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextRunPropertiesTests
{
    [Test]
    public void Constructor_InitializesCurrentShimDefaults()
    {
        var properties = new VisualLineElementTextRunProperties();

        Assert.That(properties.Typeface, Is.Not.Null);
        Assert.That(properties.Typeface.FontFamily.ToString(), Is.EqualTo("Default"));
        Assert.That(properties.TextDecorations, Is.Null);
        Assert.That(properties.TextEffects, Is.Null);
        Assert.That(properties.CultureInfo, Is.Not.Null);
        Assert.That(properties.NumberSubstitution, Is.Null);
        Assert.That(properties.TypographyProperties, Is.Null);
        Assert.That(properties.BaselineAlignment, Is.EqualTo(BaselineAlignment.Baseline));
        Assert.That(properties.FontRenderingEmSize, Is.EqualTo(12d));
        Assert.That(properties.FontHintingEmSize, Is.EqualTo(12d));
    }

    [Test]
    public void SettersAndClone_PreserveConfiguredState()
    {
        var properties = new VisualLineElementTextRunProperties();
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        var typeface = new Typeface(new System.Windows.FontFamily("Cascadia Mono"), FontStyles.Italic, FontWeights.Bold, FontStretches.Expanded);
        var foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
        var background = new SolidColorBrush(Microsoft.UI.Colors.Blue);
        var textDecorations = new TextDecorationCollection(TextDecorations.Underline);
        var textEffects = new TextEffectCollection { new TextEffect() };
        var numberSubstitution = new NumberSubstitution(NumberSubstitutionSource.Override, culture, NumberSubstitutionMethod.European);
        var typography = new DefaultTextRunTypographyProperties();

        properties.SetTypeface(typeface);
        properties.SetForegroundBrush(foreground);
        properties.SetBackgroundBrush(background);
        properties.SetTextDecorations(textDecorations);
        properties.SetTextEffects(textEffects);
        properties.SetCultureInfo(culture);
        properties.SetNumberSubstitution(numberSubstitution);
        properties.SetTypographyProperties(typography);
        properties.SetBaselineAlignment(BaselineAlignment.Superscript);
        properties.SetFontRenderingEmSize(14);
        properties.SetFontHintingEmSize(13);

        var clone = properties.Clone();

        Assert.That(clone.Typeface, Is.SameAs(typeface));
        Assert.That(clone.ForegroundBrush, Is.SameAs(foreground));
        Assert.That(clone.BackgroundBrush, Is.SameAs(background));
        Assert.That(clone.CultureInfo, Is.EqualTo(culture));
        Assert.That(clone.NumberSubstitution, Is.SameAs(numberSubstitution));
        Assert.That(clone.TypographyProperties, Is.SameAs(typography));
        Assert.That(clone.BaselineAlignment, Is.EqualTo(BaselineAlignment.Superscript));
        Assert.That(clone.FontRenderingEmSize, Is.EqualTo(14));
        Assert.That(clone.FontHintingEmSize, Is.EqualTo(13));
        Assert.That(clone.TextDecorations, Is.SameAs(textDecorations));
        Assert.That(clone.TextEffects, Is.SameAs(textEffects));
    }

    [Test]
    public void TypographyProperties_ExposeLinkedAvalonEditDefaults()
    {
        var typography = new DefaultTextRunTypographyProperties();

        Assert.That(typography.AnnotationAlternates, Is.EqualTo(0));
        Assert.That(typography.Capitals, Is.EqualTo(System.Windows.Media.TextFormatting.FontCapitals.Normal));
        Assert.That(typography.CapitalSpacing, Is.False);
        Assert.That(typography.CaseSensitiveForms, Is.False);
        Assert.That(typography.ContextualAlternates, Is.True);
        Assert.That(typography.ContextualLigatures, Is.True);
        Assert.That(typography.ContextualSwashes, Is.EqualTo(0));
        Assert.That(typography.DiscretionaryLigatures, Is.False);
        Assert.That(typography.EastAsianExpertForms, Is.False);
        Assert.That(typography.EastAsianLanguage, Is.EqualTo(System.Windows.Media.TextFormatting.FontEastAsianLanguage.Normal));
        Assert.That(typography.EastAsianWidths, Is.EqualTo(System.Windows.Media.TextFormatting.FontEastAsianWidths.Normal));
        Assert.That(typography.Fraction, Is.EqualTo(System.Windows.Media.TextFormatting.FontFraction.Normal));
        Assert.That(typography.HistoricalForms, Is.False);
        Assert.That(typography.HistoricalLigatures, Is.False);
        Assert.That(typography.Kerning, Is.True);
        Assert.That(typography.MathematicalGreek, Is.False);
        Assert.That(typography.NumeralAlignment, Is.EqualTo(System.Windows.Media.TextFormatting.FontNumeralAlignment.Normal));
        Assert.That(typography.NumeralStyle, Is.EqualTo(System.Windows.Media.TextFormatting.FontNumeralStyle.Normal));
        Assert.That(typography.SlashedZero, Is.False);
        Assert.That(typography.StandardLigatures, Is.True);
        Assert.That(typography.StandardSwashes, Is.EqualTo(0));
        Assert.That(typography.StylisticAlternates, Is.EqualTo(0));
        Assert.That(typography.Variants, Is.EqualTo(System.Windows.Media.TextFormatting.FontVariants.Normal));
        Assert.That(typography.StylisticSet1, Is.False);
        Assert.That(typography.StylisticSet20, Is.False);
    }
}
