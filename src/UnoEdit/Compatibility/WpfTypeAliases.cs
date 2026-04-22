// Maps WPF XAML type names to their Uno/WinUI counterparts via global using aliases.
// This lets AvalonEdit files that reference these names (after a "using System.Windows;"
// directive) compile against Uno without modification, as long as no shim in
// System.Windows.* also defines the same name.
//
// IMPORTANT: Do not add a shim type in System.Windows.* for any name that has an alias
// here — the two would conflict and produce CS0104 (ambiguous reference).
//
// Only add aliases here for types that exist in Uno with a compatible API surface.
// For types that Uno does not provide, use a shim in Compatibility/ instead.

global using DependencyObject                  = Microsoft.UI.Xaml.DependencyObject;
global using DependencyProperty                = Microsoft.UI.Xaml.DependencyProperty;
global using PropertyMetadata                  = Microsoft.UI.Xaml.PropertyMetadata;
global using UIElement                         = Microsoft.UI.Xaml.UIElement;
global using FrameworkElement                  = Microsoft.UI.Xaml.FrameworkElement;
global using DependencyPropertyChangedEventArgs = Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs;
global using PropertyChangedCallback           = Microsoft.UI.Xaml.PropertyChangedCallback;
#if !WINDOWS_APP_SDK
global using FrameworkPropertyMetadataOptions  = Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions;
// Note: FrameworkPropertyMetadata is NOT aliased here because Uno's version is missing
// WPF constructors (1-arg callback, 2-arg default+callback). A shim subclass in
// Compatibility/SystemWindowsCompatibility.cs fills those gaps instead.
#endif

// System.Windows geometry / media types
global using Rect                              = Windows.Foundation.Rect;
global using Size                              = Windows.Foundation.Size;
global using Point                             = Windows.Foundation.Point;
global using Thickness                         = Microsoft.UI.Xaml.Thickness;
global using FlowDirection                     = Microsoft.UI.Xaml.FlowDirection;
global using TextAlignment                     = Microsoft.UI.Xaml.TextAlignment;
global using TextWrapping                      = Microsoft.UI.Xaml.TextWrapping;
global using Brush                             = Microsoft.UI.Xaml.Media.Brush;
global using GeneralTransform                  = Microsoft.UI.Xaml.Media.GeneralTransform;
global using SolidColorBrush                   = Microsoft.UI.Xaml.Media.SolidColorBrush;
global using UserControl                       = Microsoft.UI.Xaml.Controls.UserControl;
global using ScrollBarVisibility               = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
