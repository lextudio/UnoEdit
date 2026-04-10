namespace System.Windows
{
	public interface IWeakEventListener
	{
		bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e);
	}

	/// <summary>Portable shim for System.Windows.FontFamily.</summary>
	public sealed class FontFamily
	{
		readonly string _name;
		public FontFamily(string name) { _name = name ?? ""; }
		public System.Collections.Generic.IReadOnlyList<string> FamilyNames => new[] { _name };
		public override string ToString() => _name;
	}

	/// <summary>Portable shim for System.Windows.FontWeight.</summary>
	public readonly struct FontWeight : IEquatable<FontWeight>
	{
		readonly int _weight;
		FontWeight(int weight) { _weight = weight; }
		public static FontWeight FromOpenTypeWeight(int weight) => new FontWeight(weight);
		public int ToOpenTypeWeight() => _weight;
		public bool Equals(FontWeight other) => _weight == other._weight;
		public override bool Equals(object? obj) => obj is FontWeight fw && Equals(fw);
		public override int GetHashCode() => _weight;
		public static bool operator ==(FontWeight a, FontWeight b) => a._weight == b._weight;
		public static bool operator !=(FontWeight a, FontWeight b) => a._weight != b._weight;
		public override string ToString() => _weight switch {
			100 => "Thin", 200 => "ExtraLight", 300 => "Light",
			400 => "Normal", 500 => "Medium", 600 => "SemiBold",
			700 => "Bold", 800 => "ExtraBold", 900 => "Black", _ => _weight.ToString()
		};

		public static readonly FontWeight Normal = new FontWeight(400);
		public static readonly FontWeight Bold = new FontWeight(700);
	}

	/// <summary>Portable shim for System.Windows.FontStyle.</summary>
	public readonly struct FontStyle : IEquatable<FontStyle>
	{
		readonly string _name;
		FontStyle(string name) { _name = name; }
		public bool Equals(FontStyle other) => string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object? obj) => obj is FontStyle fs && Equals(fs);
		public override int GetHashCode() => (_name ?? "").GetHashCode(StringComparison.OrdinalIgnoreCase);
		public static bool operator ==(FontStyle a, FontStyle b) => a.Equals(b);
		public static bool operator !=(FontStyle a, FontStyle b) => !a.Equals(b);
		public override string ToString() => _name ?? "Normal";

		public static readonly FontStyle Normal = new FontStyle("Normal");
		public static readonly FontStyle Italic = new FontStyle("Italic");
		public static readonly FontStyle Oblique = new FontStyle("Oblique");
	}

	/// <summary>Portable shim for System.Windows.FontStyles.</summary>
	public static class FontStyles
	{
		public static FontStyle Normal => FontStyle.Normal;
		public static FontStyle Italic => FontStyle.Italic;
		public static FontStyle Oblique => FontStyle.Oblique;
	}

	/// <summary>Portable shim for System.Windows.FontWeights (WPF static class).</summary>
	public static class FontWeights
	{
		public static FontWeight Thin       => FontWeight.FromOpenTypeWeight(100);
		public static FontWeight ExtraLight => FontWeight.FromOpenTypeWeight(200);
		public static FontWeight Light      => FontWeight.FromOpenTypeWeight(300);
		public static FontWeight Normal     => FontWeight.FromOpenTypeWeight(400);
		public static FontWeight Medium     => FontWeight.FromOpenTypeWeight(500);
		public static FontWeight SemiBold   => FontWeight.FromOpenTypeWeight(600);
		public static FontWeight Bold       => FontWeight.FromOpenTypeWeight(700);
		public static FontWeight ExtraBold  => FontWeight.FromOpenTypeWeight(800);
		public static FontWeight Black      => FontWeight.FromOpenTypeWeight(900);
	}
}

namespace System.Windows.Documents
{
	public enum LogicalDirection
	{
		Backward = 0,
		Forward = 1
	}

	/// <summary>Compiler shim for System.Windows.Documents.Inline.</summary>
	public abstract class Inline { }

	/// <summary>Compiler shim for System.Windows.Documents.Run.</summary>
	public class Run : Inline
	{
		public string Text { get; set; }
		public Run() { }
		public Run(string text) { Text = text; }
	}
}

namespace System.Windows
{
	/// <summary>Compiler shim for DataObject clipboard transfer object.</summary>
	public class DataObject
	{
		readonly System.Collections.Generic.Dictionary<string, object> _data =
			new System.Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		public DataObject() { }
		public DataObject(string format, object data) { _data[format] = data; }

		public void SetData(string format, object data) => _data[format] = data;
		public object GetData(string format) =>
			_data.TryGetValue(format, out object v) ? v : null;
		public bool GetDataPresent(string format) => _data.ContainsKey(format);
	}

	/// <summary>Compiler shim for DataFormats clipboard format constants.</summary>
	public static class DataFormats
	{
		public const string Text        = "Text";
		public const string UnicodeText = "UnicodeText";
		public const string OemText     = "OemText";
		public const string Rtf         = "Rtf";
		public const string Html        = "Html";
	}

	/// <summary>
	/// Extends Uno's <see cref="Microsoft.UI.Xaml.FrameworkPropertyMetadata"/> with the WPF
	/// constructors that Uno omits (1-arg callback, 2-arg default+callback).
	/// Uno's type is not sealed, so inheritance works cleanly.
	/// </summary>
	public class FrameworkPropertyMetadata : Microsoft.UI.Xaml.FrameworkPropertyMetadata
	{
		/// <summary>WPF-compat: creates metadata with only a property-changed callback (no default value).</summary>
		public FrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback)
			: base(null, Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.None, propertyChangedCallback) { }

		/// <summary>WPF-compat: creates metadata with a default value and a property-changed callback.</summary>
		public FrameworkPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.None, propertyChangedCallback) { }

		/// <summary>Forwards all other WPF constructors — pass through to Uno base.</summary>
		public FrameworkPropertyMetadata(object defaultValue)
			: base(defaultValue) { }

		public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options)
			: base(defaultValue, options) { }

		public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options,
		                                  PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, options, propertyChangedCallback) { }
	}

	public class PresentationSource
	{
		public System.Windows.Media.CompositionTarget CompositionTarget { get; set; }

		public static PresentationSource FromVisual(System.Windows.Media.Visual visual)
		{
			return visual?.PresentationSource;
		}
	}
}

namespace System.Windows.Media
{
	public class Visual
	{
		public System.Windows.PresentationSource PresentationSource { get; set; }
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

		public static Matrix Identity => new Matrix
		{
			M11 = 1,
			M22 = 1
		};
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
		public override string ToString() =>
			A == 255
				? string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B)
				: string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);
	}

	/// <summary>Named color constants — a minimal subset used by typical syntax highlighting themes.</summary>
	public static class Colors
	{
		public static Color Black       => Color.FromRgb(0, 0, 0);
		public static Color White       => Color.FromRgb(255, 255, 255);
		public static Color Red         => Color.FromRgb(255, 0, 0);
		public static Color Green       => Color.FromRgb(0, 128, 0);
		public static Color Blue        => Color.FromRgb(0, 0, 255);
		public static Color Yellow      => Color.FromRgb(255, 255, 0);
		public static Color Cyan        => Color.FromRgb(0, 255, 255);
		public static Color Magenta     => Color.FromRgb(255, 0, 255);
		public static Color Gray        => Color.FromRgb(128, 128, 128);
		public static Color Silver      => Color.FromRgb(192, 192, 192);
		public static Color DarkGray    => Color.FromRgb(169, 169, 169);
		public static Color LightGray   => Color.FromRgb(211, 211, 211);
		public static Color Orange      => Color.FromRgb(255, 165, 0);
		public static Color DarkBlue    => Color.FromRgb(0, 0, 139);
		public static Color DarkRed     => Color.FromRgb(139, 0, 0);
		public static Color DarkGreen   => Color.FromRgb(0, 100, 0);
		public static Color Navy        => Color.FromRgb(0, 0, 128);
		public static Color Teal        => Color.FromRgb(0, 128, 128);
		public static Color Purple      => Color.FromRgb(128, 0, 128);
		public static Color Brown       => Color.FromRgb(165, 42, 42);
		public static Color Pink        => Color.FromRgb(255, 192, 203);
		public static Color Transparent => Color.FromArgb(0, 0, 0, 0);
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

	/// <summary>
	/// Parses CSS-style color strings (#RGB, #RRGGBB, #AARRGGBB) and a subset of named colors.
	/// Used by the Xshd XSLT loader as a shim for WPF's ColorConverter.
	/// </summary>
	public sealed class ColorConverter
	{
		static readonly System.Collections.Generic.Dictionary<string, Color> _named =
			new System.Collections.Generic.Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
		{
			["Black"]      = Colors.Black,     ["White"]    = Colors.White,
			["Red"]        = Colors.Red,       ["Green"]    = Colors.Green,
			["Blue"]       = Colors.Blue,      ["Yellow"]   = Colors.Yellow,
			["Cyan"]       = Colors.Cyan,      ["Magenta"]  = Colors.Magenta,
			["Gray"]       = Colors.Gray,      ["Grey"]     = Colors.Gray,
			["Silver"]     = Colors.Silver,    ["DarkGray"] = Colors.DarkGray,
			["LightGray"]  = Colors.LightGray, ["Orange"]   = Colors.Orange,
			["DarkBlue"]   = Colors.DarkBlue,  ["DarkRed"]  = Colors.DarkRed,
			["DarkGreen"]  = Colors.DarkGreen, ["Navy"]     = Colors.Navy,
			["Teal"]       = Colors.Teal,      ["Purple"]   = Colors.Purple,
			["Brown"]      = Colors.Brown,     ["Pink"]        = Colors.Pink,
			["Transparent"]= Colors.Transparent,
			["MidnightBlue"] = Colors.MidnightBlue,  ["DarkCyan"]    = Colors.DarkCyan,
			["DarkMagenta"]  = Colors.DarkMagenta,   ["DarkSlateGray"]= Colors.DarkSlateGray,
			["DeepPink"]     = Colors.DeepPink,      ["Fuchsia"]     = Colors.Fuchsia,
			["Maroon"]       = Colors.Maroon,        ["Olive"]       = Colors.Olive,
			["SaddleBrown"]  = Colors.SaddleBrown,   ["Sienna"]      = Colors.Sienna,
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
		static readonly System.Collections.Generic.Dictionary<string, Windows.FontWeight> _map =
			new System.Collections.Generic.Dictionary<string, Windows.FontWeight>(StringComparer.OrdinalIgnoreCase)
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

	/// <summary>Compiler shim for System.Windows.Media.ImageSource.</summary>
	public abstract class ImageSource { }

	/// <summary>
	/// Compiler shim for System.Windows.Media.DrawingContext.
	/// On Uno the actual drawing is performed through Uno's Skia canvas; this stub
	/// exists only so that <see cref="ICSharpCode.AvalonEdit.Rendering.IBackgroundRenderer"/>
	/// can keep the same interface signature as the WPF version.
	/// </summary>
	public abstract class DrawingContext : System.IDisposable
	{
		public virtual void Dispose() { }
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
}
