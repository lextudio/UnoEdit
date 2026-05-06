namespace System.Windows.Media
{
    public class Visual
    {
        public Windows.PresentationSource PresentationSource { get; set; }
    }

    public sealed class CompositionTarget
    {
        public Matrix TransformFromDevice { get; set; } = Matrix.Identity;
        public Matrix TransformToDevice { get; set; } = Matrix.Identity;
    }

    public struct Matrix
    {
        public double M11 { get; set; }
        public double M12 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public static Matrix Identity => new Matrix { M11 = 1, M22 = 1 };
    }

    /// <summary>Portable shim for System.Windows.Media.Color.</summary>
    public readonly struct Color : IEquatable<Color>
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        Color(byte a, byte r, byte g, byte b) { A = a; R = r; G = g; B = b; }

        public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);
        public static Color FromRgb(byte r, byte g, byte b) => new Color(255, r, g, b);

        public bool Equals(Color other) => A == other.A && R == other.R && G == other.G && B == other.B;
        public override bool Equals(object? obj) => obj is Color c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(A, R, G, B);
        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
        public static implicit operator global::Windows.UI.Color(Color color) => global::Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
        public override string ToString() =>
            A == 255
                ? string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B)
                : string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);
    }

    /// <summary>Named color constants — a minimal subset used by typical syntax highlighting themes.</summary>
    public static class Colors
    {
        public static Color Black         => Color.FromRgb(0, 0, 0);
        public static Color White         => Color.FromRgb(255, 255, 255);
        public static Color Red           => Color.FromRgb(255, 0, 0);
        public static Color Green         => Color.FromRgb(0, 128, 0);
        public static Color Blue          => Color.FromRgb(0, 0, 255);
        public static Color Yellow        => Color.FromRgb(255, 255, 0);
        public static Color Cyan          => Color.FromRgb(0, 255, 255);
        public static Color Magenta       => Color.FromRgb(255, 0, 255);
        public static Color Gray          => Color.FromRgb(128, 128, 128);
        public static Color Silver        => Color.FromRgb(192, 192, 192);
        public static Color DarkGray      => Color.FromRgb(169, 169, 169);
        public static Color LightGray     => Color.FromRgb(211, 211, 211);
        public static Color Orange        => Color.FromRgb(255, 165, 0);
        public static Color DarkBlue      => Color.FromRgb(0, 0, 139);
        public static Color DarkRed       => Color.FromRgb(139, 0, 0);
        public static Color DarkGreen     => Color.FromRgb(0, 100, 0);
        public static Color Navy          => Color.FromRgb(0, 0, 128);
        public static Color Teal          => Color.FromRgb(0, 128, 128);
        public static Color Purple        => Color.FromRgb(128, 0, 128);
        public static Color Brown         => Color.FromRgb(165, 42, 42);
        public static Color Pink          => Color.FromRgb(255, 192, 203);
        public static Color Transparent   => Color.FromArgb(0, 0, 0, 0);
        public static Color MidnightBlue  => Color.FromRgb(25, 25, 112);
        public static Color DarkCyan      => Color.FromRgb(0, 139, 139);
        public static Color DarkMagenta   => Color.FromRgb(139, 0, 139);
        public static Color DarkSlateGray => Color.FromRgb(47, 79, 79);
        public static Color DeepPink      => Color.FromRgb(255, 20, 147);
        public static Color Fuchsia       => Color.FromRgb(255, 0, 255);
        public static Color Maroon        => Color.FromRgb(128, 0, 0);
        public static Color Olive         => Color.FromRgb(128, 128, 0);
        public static Color SaddleBrown   => Color.FromRgb(139, 69, 19);
        public static Color Sienna        => Color.FromRgb(160, 82, 45);
        public static Color SlateGray     => Color.FromRgb(112, 128, 144);
    }

    public static class Brushes
    {
        public static Brush Black => new SolidColorBrush(Colors.Black);
        public static Brush White => new SolidColorBrush(Colors.White);
        public static Brush Gray => new SolidColorBrush(Colors.Gray);
        public static Brush DarkGray => new SolidColorBrush(Colors.DarkGray);
        public static Brush LightGray => new SolidColorBrush(Colors.LightGray);
        public static Brush Transparent => new SolidColorBrush(Colors.Transparent);
    }

    public static class FreezableExtensions
    {
        public static void Freeze(this Brush brush)
        {
        }
    }

    public enum PenLineCap
    {
        Flat,
        Square,
        Round,
        Triangle
    }

    /// <summary>
    /// Parses CSS-style color strings (#RGB, #RRGGBB, #AARRGGBB) and a subset of named colors.
    /// Used by the Xshd XSLT loader as a shim for WPF's ColorConverter.
    /// </summary>
    public sealed class ColorConverter
    {
        static readonly Collections.Generic.Dictionary<string, Color> _named =
            new Collections.Generic.Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["Black"]        = Colors.Black,       ["White"]        = Colors.White,
            ["Red"]          = Colors.Red,         ["Green"]        = Colors.Green,
            ["Blue"]         = Colors.Blue,        ["Yellow"]       = Colors.Yellow,
            ["Cyan"]         = Colors.Cyan,        ["Magenta"]      = Colors.Magenta,
            ["Gray"]         = Colors.Gray,        ["Grey"]         = Colors.Gray,
            ["Silver"]       = Colors.Silver,      ["DarkGray"]     = Colors.DarkGray,
            ["LightGray"]    = Colors.LightGray,   ["Orange"]       = Colors.Orange,
            ["DarkBlue"]     = Colors.DarkBlue,    ["DarkRed"]      = Colors.DarkRed,
            ["DarkGreen"]    = Colors.DarkGreen,   ["Navy"]         = Colors.Navy,
            ["Teal"]         = Colors.Teal,        ["Purple"]       = Colors.Purple,
            ["Brown"]        = Colors.Brown,       ["Pink"]         = Colors.Pink,
            ["Transparent"]  = Colors.Transparent,
            ["MidnightBlue"] = Colors.MidnightBlue, ["DarkCyan"]    = Colors.DarkCyan,
            ["DarkMagenta"]  = Colors.DarkMagenta,  ["DarkSlateGray"]= Colors.DarkSlateGray,
            ["DeepPink"]     = Colors.DeepPink,     ["Fuchsia"]     = Colors.Fuchsia,
            ["Maroon"]       = Colors.Maroon,       ["Olive"]       = Colors.Olive,
            ["SaddleBrown"]  = Colors.SaddleBrown,  ["Sienna"]      = Colors.Sienna,
            ["SlateGray"]    = Colors.SlateGray,
        };

        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string v = value.Trim();
            if (v.StartsWith("#", StringComparison.Ordinal))
            {
                string hex = v.Substring(1);
                if (hex.Length == 3)
                {
                    byte r = Convert.ToByte(new string(hex[0], 2), 16);
                    byte g = Convert.ToByte(new string(hex[1], 2), 16);
                    byte b = Convert.ToByte(new string(hex[2], 2), 16);
                    return Color.FromRgb(r, g, b);
                }
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
                if (hex.Length == 8)
                {
                    byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
                throw new FormatException($"Unrecognized color format: '{value}'");
            }
            if (_named.TryGetValue(v, out Color named)) return named;
            throw new FormatException($"Unknown color name: '{value}'");
        }
    }

    /// <summary>Parses font-weight strings. Shim for WPF's TypeConverter.</summary>
    public sealed class FontWeightConverter
    {
        static readonly Collections.Generic.Dictionary<string, Windows.FontWeight> _map =
            new Collections.Generic.Dictionary<string, Windows.FontWeight>(StringComparer.OrdinalIgnoreCase)
        {
            ["Thin"]       = Windows.FontWeight.FromOpenTypeWeight(100),
            ["ExtraLight"] = Windows.FontWeight.FromOpenTypeWeight(200),
            ["UltraLight"] = Windows.FontWeight.FromOpenTypeWeight(200),
            ["Light"]      = Windows.FontWeight.FromOpenTypeWeight(300),
            ["Normal"]     = Windows.FontWeight.FromOpenTypeWeight(400),
            ["Regular"]    = Windows.FontWeight.FromOpenTypeWeight(400),
            ["Medium"]     = Windows.FontWeight.FromOpenTypeWeight(500),
            ["DemiBold"]   = Windows.FontWeight.FromOpenTypeWeight(600),
            ["SemiBold"]   = Windows.FontWeight.FromOpenTypeWeight(600),
            ["Bold"]       = Windows.FontWeight.FromOpenTypeWeight(700),
            ["ExtraBold"]  = Windows.FontWeight.FromOpenTypeWeight(800),
            ["UltraBold"]  = Windows.FontWeight.FromOpenTypeWeight(800),
            ["Black"]      = Windows.FontWeight.FromOpenTypeWeight(900),
            ["Heavy"]      = Windows.FontWeight.FromOpenTypeWeight(900),
        };

        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (_map.TryGetValue(value.Trim(), out Windows.FontWeight fw)) return fw;
            if (int.TryParse(value.Trim(), out int weight))
                return Windows.FontWeight.FromOpenTypeWeight(weight);
            throw new FormatException($"Unknown font weight: '{value}'");
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is Windows.FontWeight fw) return fw.ToString();
            return value?.ToString() ?? "";
        }
    }

    /// <summary>Parses font-style strings. Shim for WPF's TypeConverter.</summary>
    public sealed class FontStyleConverter
    {
        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToLowerInvariant() switch
            {
                "normal"  => Windows.FontStyle.Normal,
                "italic"  => Windows.FontStyle.Italic,
                "oblique" => Windows.FontStyle.Oblique,
                _ => throw new FormatException($"Unknown font style: '{value}'")
            };
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is Windows.FontStyle fs) return fs.ToString();
            return value?.ToString() ?? "";
        }
    }

    /// <summary>Compiler shim for System.Windows.Media.ImageSource.</summary>
    public abstract class ImageSource { }

    /// <summary>
    /// Compiler shim for System.Windows.Media.DrawingContext.
    /// On Uno the actual drawing is performed through Uno's Skia canvas; this compatibility
    /// object records draw operations so shared rendering code keeps a meaningful contract.
    /// </summary>
    public class DrawingContext : IDisposable
    {
        readonly Collections.Generic.List<object> _operations = new Collections.Generic.List<object>();

        public sealed class DrawOperation
        {
            public DrawOperation(string kind, object? payload)
            {
                Kind = kind ?? string.Empty;
                Payload = payload;
            }

            public string Kind { get; }
            public object? Payload { get; }
        }

        public Collections.Generic.IReadOnlyList<object> Operations => _operations;

        public bool IsDisposed { get; private set; }

        public virtual void Record(string kind, object? payload)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(DrawingContext));
            _operations.Add(new DrawOperation(kind, payload));
        }

        public virtual void DrawText(FormattedText formattedText, Point origin)
            => Record("draw-text", new { FormattedText = formattedText, Origin = origin });

        public virtual void DrawRectangle(Brush brush, Pen pen, Rect rect)
            => Record("draw-rectangle", new { Brush = brush, Pen = pen, Rect = rect });

        public virtual void DrawLine(Pen pen, Point point0, Point point1)
            => Record("draw-line", new { Pen = pen, Point0 = point0, Point1 = point1 });

        public virtual void DrawGeometry(Brush brush, Pen pen, object geometry)
            => Record("draw-geometry", new { Brush = brush, Pen = pen, Geometry = geometry });

        public virtual void DrawRoundedRectangle(Brush brush, Pen pen, Rect rect, double radiusX, double radiusY)
            => Record("draw-rounded-rectangle", new { Brush = brush, Pen = pen, Rect = rect, RadiusX = radiusX, RadiusY = radiusY });

        public virtual void PushOpacity(double opacity)
            => Record("push-opacity", opacity);

        public virtual void Pop()
            => Record("pop", null);

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>Portable shim for System.Windows.Media.Pen (brush + thickness).</summary>
    public sealed class Pen
    {
        public Pen() { }
        public Pen(Brush brush, double thickness) { Brush = brush; Thickness = thickness; }
        public Brush Brush { get; set; }
        public double Thickness { get; set; }
    }

    /// <summary>Portable shim for System.Windows.Media.Typeface.</summary>
    public sealed class Typeface
    {
        public Typeface(string familyName)
        {
            FontFamily = new Windows.FontFamily(familyName ?? string.Empty);
            Style = Windows.FontStyle.Normal;
            Weight = Windows.FontWeight.Normal;
            Stretch = Windows.FontStretch.Normal;
        }

        public Typeface(Windows.FontFamily fontFamily, Windows.FontStyle style, Windows.FontWeight weight, Windows.FontStretch stretch)
        {
            FontFamily = fontFamily ?? new Windows.FontFamily(string.Empty);
            Style = style;
            Weight = weight;
            Stretch = stretch;
        }

        public Windows.FontFamily FontFamily { get; }
        public Windows.FontStyle Style { get; }
        public Windows.FontWeight Weight { get; }
        public Windows.FontStretch Stretch { get; }

        public override string ToString() => FontFamily.ToString();
    }

    /// <summary>Portable shim for System.Windows.TextDecoration.</summary>
    public class TextDecoration
    {
        public TextDecorationLocation Location { get; }
        public TextDecoration() { Location = TextDecorationLocation.Underline; }
        public TextDecoration(TextDecorationLocation location) { Location = location; }
    }

    public enum TextDecorationLocation { Underline, Strikethrough, Overline, Baseline }

    /// <summary>Portable shim for System.Windows.TextDecorationCollection.</summary>
    public class TextDecorationCollection : Collections.Generic.IList<TextDecoration>
    {
        readonly Collections.Generic.List<TextDecoration> _items;

        public TextDecorationCollection() { _items = new Collections.Generic.List<TextDecoration>(); }

        public TextDecorationCollection(Collections.Generic.IEnumerable<TextDecoration> items)
        {
            _items = new Collections.Generic.List<TextDecoration>(items ?? Array.Empty<TextDecoration>());
        }

        public bool IsFrozen => true;

        public TextDecorationCollection Clone() => new TextDecorationCollection(_items);

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public TextDecoration this[int index] { get => _items[index]; set => _items[index] = value; }
        public void Add(TextDecoration item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(TextDecoration item) => _items.Contains(item);
        public void CopyTo(TextDecoration[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public Collections.Generic.IEnumerator<TextDecoration> GetEnumerator() => _items.GetEnumerator();
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(TextDecoration item) => _items.IndexOf(item);
        public void Insert(int index, TextDecoration item) => _items.Insert(index, item);
        public bool Remove(TextDecoration item) => _items.Remove(item);
        public void RemoveAt(int index) => _items.RemoveAt(index);
    }

    /// <summary>Named text-decoration collections.</summary>
    public static class TextDecorations
    {
        public static TextDecorationCollection Underline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Underline) });
        public static TextDecorationCollection Strikethrough { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Strikethrough) });
        public static TextDecorationCollection Overline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Overline) });
        public static TextDecorationCollection Baseline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Baseline) });
    }

    /// <summary>Portable shim for System.Windows.Media.TextEffect.</summary>
    public class TextEffect { }

    /// <summary>Portable shim for System.Windows.Media.TextEffectCollection.</summary>
    public class TextEffectCollection : Collections.Generic.IList<TextEffect>
    {
        readonly Collections.Generic.List<TextEffect> _items = new Collections.Generic.List<TextEffect>();

        public bool IsFrozen => true;
        public TextEffectCollection Clone() => new TextEffectCollection();

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public TextEffect this[int index] { get => _items[index]; set => _items[index] = value; }
        public void Add(TextEffect item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(TextEffect item) => _items.Contains(item);
        public void CopyTo(TextEffect[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public Collections.Generic.IEnumerator<TextEffect> GetEnumerator() => _items.GetEnumerator();
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(TextEffect item) => _items.IndexOf(item);
        public void Insert(int index, TextEffect item) => _items.Insert(index, item);
        public bool Remove(TextEffect item) => _items.Remove(item);
        public void RemoveAt(int index) => _items.RemoveAt(index);
    }

    /// <summary>Portable shim for System.Windows.Media.NumberSubstitution.</summary>
    public class NumberSubstitution
    {
        public NumberSubstitution() { }
        public NumberSubstitution(NumberSubstitutionSource source, System.Globalization.CultureInfo overrideCulture, NumberSubstitutionMethod substitution) { }
    }

    public enum NumberSubstitutionSource { ArabicScript, Context, European, Language, Override }
    public enum NumberSubstitutionMethod { ArabicContext, Context, European, NativeNational, Traditional }

    /// <summary>Portable shim for System.Windows.Media.FormattedText.</summary>
    public class FormattedText
    {
        readonly double _emSize;

        public FormattedText(string textToFormat, System.Globalization.CultureInfo culture, FlowDirection flowDirection,
            Typeface typeface, double emSize, Brush foreground)
        {
            _emSize = emSize > 0 ? emSize : 12.0;
            double charCount = (textToFormat ?? string.Empty).Length;
            Width = charCount * _emSize * 0.6;
            WidthIncludingTrailingWhitespace = Width;
            Height = _emSize * 1.4;
            Baseline = _emSize * 1.1;
        }

        public double Width { get; }
        public double Height { get; }
        public double Baseline { get; }
        public double WidthIncludingTrailingWhitespace { get; }
    }
}
