// Stub: HighlightingDefinitionTypeConverter is WPF-specific.
// Kept as a no-op so IHighlightingDefinition.cs can be linked without modification.
using System;
using System.ComponentModel;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>Stub TypeConverter for portable builds. Not functional.</summary>
	sealed class HighlightingDefinitionTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => false;
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => false;
	}
}
