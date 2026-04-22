// All System.Windows.*, System.Windows.Media.*, and System.Windows.Documents.* shim types
// have been moved to the LeXtudio.Windows assembly (src/LeXtudio.Windows/).
//
// Only FrameworkPropertyMetadata remains here because it subclasses
// Microsoft.UI.Xaml.FrameworkPropertyMetadata (a Uno/WinUI XAML type) and must match
// the WINDOWS_APP_SDK conditional that UnoEdit's net9.0-windows10.0.19041.0 TFM defines.

namespace System.Windows
{
#if WINDOWS_APP_SDK
	public class FrameworkPropertyMetadata : PropertyMetadata
	{
		public FrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback)
			: base(null, propertyChangedCallback) { }

		public FrameworkPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, propertyChangedCallback) { }

		public FrameworkPropertyMetadata(object defaultValue)
			: base(defaultValue) { }
	}
#else
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
#endif
}
