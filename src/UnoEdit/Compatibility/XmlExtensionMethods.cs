// XML reader extension retained separately to avoid expanding the main shim file with every overload.
using System.Xml;

namespace ICSharpCode.AvalonEdit.Utils
{
	static partial class ExtensionMethods
	{
		/// <summary>Gets the value of the attribute as boolean, or null if the attribute does not exist.</summary>
		public static bool? GetBoolAttribute(this XmlReader reader, string attributeName)
		{
			string v = reader.GetAttribute(attributeName);
			return v != null ? (bool?)XmlConvert.ToBoolean(v) : null;
		}
	}
}
