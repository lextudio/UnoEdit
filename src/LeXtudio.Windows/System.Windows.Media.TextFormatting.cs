using System.Collections.Generic;
using System.Globalization;

namespace System.Windows.Media.TextFormatting
{
    public enum LineBreakCondition { BreakDesired, BreakPossible, BreakRestrained, BreakAlways }
    public enum InvertAxes { None, Horizontal, Vertical, Both }
    public enum TextLineValidity { Unknown, Valid, Overfull, Underfull }
    public enum TextFormattingMode { Ideal, Display }
    public enum FontVariants { Normal, Superscript, Subscript, Ordinal, Inferior, Ruby }
    public enum FontNumeralStyle { Normal, Lining, OldStyle }
    public enum FontNumeralAlignment { Normal, Proportional, Tabular }
    public enum FontFraction { Normal, Slashed, Stacked }
    public enum FontEastAsianWidths { Normal, Proportional, Full, Half, Third, Quarter }
    public enum FontEastAsianLanguage { Normal, HojoKanji, Jis04, Jis78, Jis83, Jis90, NlcKanji, Simplified, Traditional, TraditionalNames }
    public enum FontCapitals { Normal, AllSmallCaps, SmallCaps, AllPetiteCaps, PetiteCaps, Unicase, Titling }
    public enum TextCollapsingStyle { TrailingWord, TrailingCharacter }

    public readonly struct CharacterBufferReference : IEquatable<CharacterBufferReference>
    {
        public static readonly CharacterBufferReference Empty = default;
        readonly string _text;
        readonly int _offset;

        public CharacterBufferReference(string characterString, int offsetToFirstChar)
        {
            _text = characterString ?? string.Empty;
            _offset = offsetToFirstChar;
        }

        public bool Equals(CharacterBufferReference other) => _text == other._text && _offset == other._offset;
        public override bool Equals(object obj) => obj is CharacterBufferReference cb && Equals(cb);
        public override int GetHashCode() => HashCode.Combine(_text, _offset);
        public static bool operator ==(CharacterBufferReference a, CharacterBufferReference b) => a.Equals(b);
        public static bool operator !=(CharacterBufferReference a, CharacterBufferReference b) => !a.Equals(b);
    }

    public readonly struct CharacterBufferRange : IEquatable<CharacterBufferRange>
    {
        public static readonly CharacterBufferRange Empty = default;
        readonly string _text;
        readonly int _offset;
        readonly int _count;

        public CharacterBufferRange(string characterString, int offsetToFirstChar, int count)
        {
            _text = characterString ?? string.Empty;
            _offset = offsetToFirstChar;
            _count = count;
        }

        public int Length => _count;

        public bool Equals(CharacterBufferRange other) => _text == other._text && _offset == other._offset && _count == other._count;
        public override bool Equals(object obj) => obj is CharacterBufferRange cb && Equals(cb);
        public override int GetHashCode() => HashCode.Combine(_text, _offset, _count);
        public static bool operator ==(CharacterBufferRange a, CharacterBufferRange b) => a.Equals(b);
        public static bool operator !=(CharacterBufferRange a, CharacterBufferRange b) => !a.Equals(b);
    }

    public readonly struct CultureSpecificCharacterBufferRange
    {
        public CultureInfo CultureInfo { get; }
        public CharacterBufferRange CharacterBufferRange { get; }

        public CultureSpecificCharacterBufferRange(CultureInfo cultureInfo, CharacterBufferRange characterBufferRange)
        {
            CultureInfo = cultureInfo;
            CharacterBufferRange = characterBufferRange;
        }
    }

    public readonly struct CharacterHit : IEquatable<CharacterHit>
    {
        public int FirstCharacterIndex { get; }
        public int TrailingLength { get; }

        public CharacterHit(int firstCharacterIndex, int trailingLength)
        {
            FirstCharacterIndex = firstCharacterIndex;
            TrailingLength = trailingLength;
        }

        public bool Equals(CharacterHit other) => FirstCharacterIndex == other.FirstCharacterIndex && TrailingLength == other.TrailingLength;
        public override bool Equals(object obj) => obj is CharacterHit ch && Equals(ch);
        public override int GetHashCode() => HashCode.Combine(FirstCharacterIndex, TrailingLength);
        public static bool operator ==(CharacterHit a, CharacterHit b) => a.Equals(b);
        public static bool operator !=(CharacterHit a, CharacterHit b) => !a.Equals(b);
    }

    public readonly struct TextEmbeddedObjectMetrics
    {
        public double Width { get; }
        public double Height { get; }
        public double Baseline { get; }

        public TextEmbeddedObjectMetrics(double width, double height, double baseline)
        {
            Width = width;
            Height = height;
            Baseline = baseline;
        }
    }

    public readonly struct MinMaxParagraphWidth : IEquatable<MinMaxParagraphWidth>
    {
        public double MinWidth { get; }
        public double MaxWidth { get; }

        public MinMaxParagraphWidth(double minWidth, double maxWidth)
        {
            MinWidth = minWidth;
            MaxWidth = maxWidth;
        }

        public bool Equals(MinMaxParagraphWidth other) => MinWidth.Equals(other.MinWidth) && MaxWidth.Equals(other.MaxWidth);
        public override bool Equals(object obj) => obj is MinMaxParagraphWidth m && Equals(m);
        public override int GetHashCode() => HashCode.Combine(MinWidth, MaxWidth);
        public static bool operator ==(MinMaxParagraphWidth a, MinMaxParagraphWidth b) => a.Equals(b);
        public static bool operator !=(MinMaxParagraphWidth a, MinMaxParagraphWidth b) => !a.Equals(b);
    }

    public readonly struct TextBounds
    {
        public Rect Rectangle { get; }
        public TextBounds(Rect rect) { Rectangle = rect; }
    }

    public class TextSpan<T>
    {
        public int Length { get; }
        public T Value { get; }
        public TextSpan(int length, T value) { Length = length; Value = value; }
    }

    public class TextLineBreak { }

    public class TextRunCache
    {
        public void Change(int textSourceCharacterIndex, int addition, bool modificationOnly) { }
    }

    public sealed class IndexedGlyphRun { }

    public abstract class TextCollapsingProperties
    {
        public abstract double Width { get; }
        public abstract TextRun Symbol { get; }
        public abstract TextCollapsingStyle Style { get; }
    }

    public abstract class TextMarkerProperties
    {
        public abstract double Offset { get; }
        public abstract TextSource TextSource { get; }
    }

    public abstract class TextRunProperties
    {
        public abstract Brush ForegroundBrush { get; }
        public abstract Brush BackgroundBrush { get; }
        public abstract System.Windows.Media.Typeface Typeface { get; }
        public abstract double FontRenderingEmSize { get; }
        public abstract double FontHintingEmSize { get; }
        public abstract System.Windows.Media.TextDecorationCollection TextDecorations { get; }
        public abstract System.Windows.Media.TextEffectCollection TextEffects { get; }
        public abstract CultureInfo CultureInfo { get; }
        public virtual System.Windows.Media.NumberSubstitution NumberSubstitution => null;
        public virtual TextRunTypographyProperties TypographyProperties => null;
        public virtual System.Windows.BaselineAlignment BaselineAlignment => System.Windows.BaselineAlignment.Baseline;
    }

    public abstract class TextRunTypographyProperties
    {
        public abstract FontVariants Variants { get; }
        public abstract int AnnotationAlternates { get; }
        public abstract FontCapitals Capitals { get; }
        public abstract bool CapitalSpacing { get; }
        public abstract bool CaseSensitiveForms { get; }
        public abstract bool ContextualAlternates { get; }
        public abstract bool ContextualLigatures { get; }
        public abstract int ContextualSwashes { get; }
        public abstract bool DiscretionaryLigatures { get; }
        public abstract bool EastAsianExpertForms { get; }
        public abstract FontEastAsianLanguage EastAsianLanguage { get; }
        public abstract FontEastAsianWidths EastAsianWidths { get; }
        public abstract FontFraction Fraction { get; }
        public abstract bool HistoricalForms { get; }
        public abstract bool HistoricalLigatures { get; }
        public abstract bool Kerning { get; }
        public abstract bool MathematicalGreek { get; }
        public abstract FontNumeralAlignment NumeralAlignment { get; }
        public abstract FontNumeralStyle NumeralStyle { get; }
        public abstract bool SlashedZero { get; }
        public abstract bool StandardLigatures { get; }
        public abstract int StandardSwashes { get; }
        public abstract int StylisticAlternates { get; }
        public abstract bool StylisticSet1 { get; }
        public abstract bool StylisticSet2 { get; }
        public abstract bool StylisticSet3 { get; }
        public abstract bool StylisticSet4 { get; }
        public abstract bool StylisticSet5 { get; }
        public abstract bool StylisticSet6 { get; }
        public abstract bool StylisticSet7 { get; }
        public abstract bool StylisticSet8 { get; }
        public abstract bool StylisticSet9 { get; }
        public abstract bool StylisticSet10 { get; }
        public abstract bool StylisticSet11 { get; }
        public abstract bool StylisticSet12 { get; }
        public abstract bool StylisticSet13 { get; }
        public abstract bool StylisticSet14 { get; }
        public abstract bool StylisticSet15 { get; }
        public abstract bool StylisticSet16 { get; }
        public abstract bool StylisticSet17 { get; }
        public abstract bool StylisticSet18 { get; }
        public abstract bool StylisticSet19 { get; }
        public abstract bool StylisticSet20 { get; }
    }

    public abstract class TextParagraphProperties
    {
        public abstract FlowDirection FlowDirection { get; }
        public abstract TextAlignment TextAlignment { get; }
        public abstract double LineHeight { get; }
        public abstract bool FirstLineInParagraph { get; }
        public abstract TextRunProperties DefaultTextRunProperties { get; }
        public abstract TextWrapping TextWrapping { get; }
        public abstract TextMarkerProperties TextMarkerProperties { get; }
        public abstract double Indent { get; }
        public virtual double DefaultIncrementalTab => 48.0;
    }

    public abstract class TextSource
    {
        public abstract TextRun GetTextRun(int textSourceCharacterIndex);
        public abstract TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit);
        public abstract int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex);
    }

    public abstract class TextRun
    {
        public virtual LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;
        public virtual LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;
        public abstract CharacterBufferReference CharacterBufferReference { get; }
        public abstract int Length { get; }
        public abstract TextRunProperties Properties { get; }
    }

    public abstract class TextEmbeddedObject : TextRun
    {
        public abstract bool HasFixedSize { get; }
        public abstract TextEmbeddedObjectMetrics Format(double remainingParagraphWidth);
        public abstract Rect ComputeBoundingBox(bool rightToLeft, bool sideways);
        public abstract void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways);
    }

    public abstract class TextLine : IDisposable
    {
        public abstract void Draw(DrawingContext drawingContext, Point origin, InvertAxes invert);
        public virtual TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList) => this;
        public virtual CharacterHit GetCharacterHitFromDistance(double distance) => new CharacterHit(0, 0);
        public virtual double GetDistanceFromCharacterHit(CharacterHit characterHit) => 0;
        public virtual CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit) => characterHit;
        public virtual CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit) => characterHit;
        public virtual CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit) => characterHit;
        public virtual IList<TextBounds> GetTextBounds(int firstTextSourceCharacterIndex, int textLength) => Array.Empty<TextBounds>();
        public virtual IList<TextSpan<TextRun>> GetTextRunSpans() => Array.Empty<TextSpan<TextRun>>();
        public virtual IEnumerable<IndexedGlyphRun> GetIndexedGlyphRuns() => Array.Empty<IndexedGlyphRun>();
        public virtual TextLineBreak GetTextLineBreak() => null;
        public virtual bool HasOverflowed => false;
        public virtual bool HasCollapsed => false;
        public virtual bool IsTruncated => false;
        public virtual bool HasEllipsis => false;
        public virtual TextLineValidity TextLineValidity => TextLineValidity.Valid;
        public abstract int Start { get; }
        public abstract double Width { get; }
        public abstract double WidthIncludingTrailingWhitespace { get; }
        public abstract double Height { get; }
        public virtual double TextHeight => Height;
        public virtual double Extent => Height;
        public virtual double TextExtent => Extent;
        public abstract double Baseline { get; }
        public virtual double TextBaseline => Baseline;
        public virtual double MarkerBaseline => Baseline;
        public virtual double MarkerHeight => 0;
        public virtual double OverhangLeading => 0;
        public virtual double OverhangAfter => 0;
        public virtual double OverhangAfterAdjustment => 0;
        public abstract int Length { get; }
        public virtual int DependentLength => 0;
        public virtual int TrailingWhitespaceLength => 0;
        public virtual int NewlineLength => 0;
        public virtual void Dispose() { }
    }

    public abstract class TextFormatter : IDisposable
    {
        public static TextFormatter Create() => new DefaultTextFormatter();
        public static TextFormatter Create(TextFormattingMode textFormattingMode) => new DefaultTextFormatter();

        public abstract TextLine FormatLine(
            TextSource textSource,
            int firstCharIndex,
            double paragraphWidth,
            TextParagraphProperties paragraphProperties,
            TextLineBreak previousLineBreak);

        public virtual TextLine FormatLine(
            TextSource textSource,
            int firstCharIndex,
            double paragraphWidth,
            TextParagraphProperties paragraphProperties,
            TextLineBreak previousLineBreak,
            TextRunCache textRunCache)
            => FormatLine(textSource, firstCharIndex, paragraphWidth, paragraphProperties, previousLineBreak);

        public virtual MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource textSource,
            int firstCharIndex,
            TextParagraphProperties paragraphProperties)
            => new MinMaxParagraphWidth(0, double.MaxValue);

        public virtual MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource textSource,
            int firstCharIndex,
            TextParagraphProperties paragraphProperties,
            TextRunCache textRunCache)
            => FormatMinMaxParagraphWidth(textSource, firstCharIndex, paragraphProperties);

        public virtual void Dispose() { }
    }

    public class TextCharacters : TextRun
    {
        readonly string _text;
        readonly int _offset;
        readonly int _count;
        readonly TextRunProperties _properties;

        public TextCharacters(string characterString, TextRunProperties textRunProperties)
            : this(characterString, 0, (characterString ?? string.Empty).Length, textRunProperties) { }

        public TextCharacters(string characterString, int offsetToFirstChar, int count, TextRunProperties textRunProperties)
        {
            _text = characterString ?? string.Empty;
            _offset = offsetToFirstChar;
            _count = count;
            _properties = textRunProperties;
        }

        public override CharacterBufferReference CharacterBufferReference => new CharacterBufferReference(_text, _offset);
        public override int Length => _count;
        public override TextRunProperties Properties => _properties;

        internal string GetText() => _text.Substring(_offset, _count);
    }

    public class TextEndOfParagraph : TextRun
    {
        readonly int _length;
        public TextEndOfParagraph(int length) { _length = length; }
        public override CharacterBufferReference CharacterBufferReference => CharacterBufferReference.Empty;
        public override int Length => _length;
        public override TextRunProperties Properties => null;
    }

    public class TextEndOfLine : TextRun
    {
        readonly int _length;
        readonly TextRunProperties _properties;
        public TextEndOfLine(int length, TextRunProperties properties = null) { _length = length; _properties = properties; }
        public override CharacterBufferReference CharacterBufferReference => CharacterBufferReference.Empty;
        public override int Length => _length;
        public override TextRunProperties Properties => _properties;
    }

    public class TextEndOfSegment : TextRun
    {
        readonly int _length;
        public TextEndOfSegment(int length) { _length = length; }
        public override CharacterBufferReference CharacterBufferReference => CharacterBufferReference.Empty;
        public override int Length => _length;
        public override TextRunProperties Properties => null;
    }

    internal sealed class DefaultTextFormatter : TextFormatter
    {
        public override TextLine FormatLine(
            TextSource textSource,
            int firstCharIndex,
            double paragraphWidth,
            TextParagraphProperties paragraphProperties,
            TextLineBreak previousLineBreak)
        {
            var runs = new List<TextRun>();
            int charIndex = firstCharIndex;
            double totalWidth = 0;
            double emSize = paragraphProperties?.DefaultTextRunProperties?.FontRenderingEmSize ?? 12.0;

            while (true)
            {
                var run = textSource.GetTextRun(charIndex);
                if (run == null || run.Length <= 0) break;
                runs.Add(run);
                charIndex += run.Length;
                if (run is TextEndOfParagraph || run is TextEndOfLine || run is TextEndOfSegment) break;
                if (run is TextCharacters tc)
                {
                    double runEm = run.Properties?.FontRenderingEmSize ?? emSize;
                    totalWidth += tc.GetText().Length * runEm * 0.6;
                }
            }

            double height = emSize * 1.4;
            double baseline = emSize * 1.1;
            int length = charIndex - firstCharIndex;
            double width = paragraphWidth > 0 ? Math.Min(totalWidth, paragraphWidth) : totalWidth;

            return new DefaultTextLine(firstCharIndex, length, width, totalWidth, height, baseline, runs);
        }
    }

    internal sealed class DefaultTextLine : TextLine
    {
        readonly int _start;
        readonly int _length;
        readonly double _width;
        readonly double _widthWithTrailing;
        readonly double _height;
        readonly double _baseline;
        readonly List<TextRun> _runs;

        internal DefaultTextLine(int start, int length, double width, double widthWithTrailing, double height, double baseline, List<TextRun> runs)
        {
            _start = start;
            _length = length;
            _width = width;
            _widthWithTrailing = widthWithTrailing;
            _height = height;
            _baseline = baseline;
            _runs = runs;
        }

        public override void Draw(DrawingContext drawingContext, Point origin, InvertAxes invert)
        {
            drawingContext?.Record("text-line", new { Origin = origin, Runs = _runs, Width = _width, Height = _height, Baseline = _baseline });
        }

        public override int Start => _start;
        public override double Width => _width;
        public override double WidthIncludingTrailingWhitespace => _widthWithTrailing;
        public override double Height => _height;
        public override double Baseline => _baseline;
        public override int Length => _length;

        public override IList<TextSpan<TextRun>> GetTextRunSpans()
        {
            var result = new List<TextSpan<TextRun>>();
            foreach (var run in _runs)
                result.Add(new TextSpan<TextRun>(run.Length, run));
            return result;
        }
    }
}
