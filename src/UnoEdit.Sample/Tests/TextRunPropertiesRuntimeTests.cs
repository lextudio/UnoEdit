using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class TextRunPropertiesRuntimeTests
{
    [TestMethod]
    public void Constructor_InitializesCurrentShimDefaults()
    {
        var properties = new VisualLineElementTextRunProperties();

        Assert.IsNotNull(properties.Typeface);
        Assert.AreEqual("Default", properties.Typeface.FontFamily.ToString());
        Assert.IsNull(properties.TextDecorations);
        Assert.IsNull(properties.TextEffects);
        Assert.IsNotNull(properties.CultureInfo);
        Assert.IsNull(properties.NumberSubstitution);
        Assert.IsNull(properties.TypographyProperties);
        Assert.AreEqual(BaselineAlignment.Baseline, properties.BaselineAlignment);
        Assert.AreEqual(12d, properties.FontRenderingEmSize);
        Assert.AreEqual(12d, properties.FontHintingEmSize);
    }

    [TestMethod]
    public void SettersAndClone_PreserveConfiguredState()
    {
        var properties = new VisualLineElementTextRunProperties();
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        var typeface = new Typeface(new FontFamily("Cascadia Mono"), FontStyles.Italic, FontWeights.Bold, FontStretches.Expanded);
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

        Assert.AreSame(typeface, clone.Typeface);
        Assert.AreSame(foreground, clone.ForegroundBrush);
        Assert.AreSame(background, clone.BackgroundBrush);
        Assert.AreEqual(culture, clone.CultureInfo);
        Assert.AreSame(numberSubstitution, clone.NumberSubstitution);
        Assert.AreSame(typography, clone.TypographyProperties);
        Assert.AreEqual(BaselineAlignment.Superscript, clone.BaselineAlignment);
        Assert.AreEqual(14, clone.FontRenderingEmSize);
        Assert.AreEqual(13, clone.FontHintingEmSize);
        Assert.AreSame(textDecorations, clone.TextDecorations);
        Assert.AreSame(textEffects, clone.TextEffects);
    }

    [TestMethod]
    public void TypographyProperties_ExposeLinkedAvalonEditDefaults()
    {
        var typography = new DefaultTextRunTypographyProperties();

        Assert.AreEqual(0, typography.AnnotationAlternates);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontCapitals.Normal, typography.Capitals);
        Assert.IsFalse(typography.CapitalSpacing);
        Assert.IsFalse(typography.CaseSensitiveForms);
        Assert.IsTrue(typography.ContextualAlternates);
        Assert.IsTrue(typography.ContextualLigatures);
        Assert.AreEqual(0, typography.ContextualSwashes);
        Assert.IsFalse(typography.DiscretionaryLigatures);
        Assert.IsFalse(typography.EastAsianExpertForms);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontEastAsianLanguage.Normal, typography.EastAsianLanguage);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontEastAsianWidths.Normal, typography.EastAsianWidths);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontFraction.Normal, typography.Fraction);
        Assert.IsFalse(typography.HistoricalForms);
        Assert.IsFalse(typography.HistoricalLigatures);
        Assert.IsTrue(typography.Kerning);
        Assert.IsFalse(typography.MathematicalGreek);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontNumeralAlignment.Normal, typography.NumeralAlignment);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontNumeralStyle.Normal, typography.NumeralStyle);
        Assert.IsFalse(typography.SlashedZero);
        Assert.IsTrue(typography.StandardLigatures);
        Assert.AreEqual(0, typography.StandardSwashes);
        Assert.AreEqual(0, typography.StylisticAlternates);
        Assert.AreEqual(System.Windows.Media.TextFormatting.FontVariants.Normal, typography.Variants);
        Assert.IsFalse(typography.StylisticSet1);
        Assert.IsFalse(typography.StylisticSet20);
    }
}
