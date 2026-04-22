// Type aliases that map WPF names to their Uno/WinUI counterparts.
// These mirror the aliases in UnoEdit's WpfTypeAliases.cs so that types in
// System.Windows.Media.Pen etc. can refer to Brush without qualification.

global using System;
global using Brush             = Microsoft.UI.Xaml.Media.Brush;
global using SolidColorBrush   = Microsoft.UI.Xaml.Media.SolidColorBrush;
global using Rect              = Windows.Foundation.Rect;
global using Size              = Windows.Foundation.Size;
global using Point             = Windows.Foundation.Point;
// WPF → Uno enum aliases used in TextParagraphProperties and related shims
global using FlowDirection     = Microsoft.UI.Xaml.FlowDirection;
global using TextAlignment     = Microsoft.UI.Xaml.TextAlignment;
global using TextWrapping      = Microsoft.UI.Xaml.TextWrapping;
