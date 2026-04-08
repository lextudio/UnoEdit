// Forked from AvalonEdit for UnoEdit — V1Loader removed (only V2 xshd format supported).
// Original: ICSharpCode.AvalonEdit/Highlighting/Xshd/HighlightingLoader.cs

using System;
using System.Xml;
using System.Xml.Schema;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// Static class with helper methods to load XSHD highlighting files.
	/// </summary>
	public static class HighlightingLoader
	{
		#region XSHD loading
		/// <summary>
		/// Loads a syntax definition from the xml reader.
		/// </summary>
		public static XshdSyntaxDefinition LoadXshd(XmlReader reader)
		{
			return LoadXshd(reader, false);
		}

		internal static XshdSyntaxDefinition LoadXshd(XmlReader reader, bool skipValidation)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			try {
				reader.MoveToContent();
				if (reader.NamespaceURI == V2Loader.Namespace) {
					return V2Loader.LoadDefinition(reader, skipValidation);
				} else {
					throw new NotSupportedException("Only XSHD version 2 (namespace '" + V2Loader.Namespace + "') is supported in this build.");
				}
			} catch (XmlSchemaException ex) {
				throw WrapException(ex, ex.LineNumber, ex.LinePosition);
			} catch (XmlException ex) {
				throw WrapException(ex, ex.LineNumber, ex.LinePosition);
			}
		}

		static Exception WrapException(Exception ex, int lineNumber, int linePosition)
		{
			return new HighlightingDefinitionInvalidException(FormatExceptionMessage(ex.Message, lineNumber, linePosition), ex);
		}

		internal static string FormatExceptionMessage(string message, int lineNumber, int linePosition)
		{
			if (lineNumber <= 0)
				return message;
			else
				return "Error at position (line " + lineNumber + ", column " + linePosition + "):\n" + message;
		}

		internal static XmlReader GetValidatingReader(XmlReader input, bool ignoreWhitespace, XmlSchemaSet schemaSet)
		{
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.CloseInput = true;
			settings.IgnoreComments = true;
			settings.IgnoreWhitespace = ignoreWhitespace;
			if (schemaSet != null) {
				settings.Schemas = schemaSet;
				settings.ValidationType = ValidationType.Schema;
			}
			return XmlReader.Create(input, settings);
		}

		internal static XmlSchemaSet LoadSchemaSet(XmlReader schemaInput)
		{
			XmlSchemaSet schemaSet = new XmlSchemaSet();
			schemaSet.Add(null, schemaInput);
			schemaSet.ValidationEventHandler += ValidationHandler;
			schemaSet.Compile();
			return schemaSet;
		}

		static void ValidationHandler(object sender, ValidationEventArgs args)
		{
			throw new HighlightingDefinitionInvalidException(args.Message);
		}
		#endregion

		/// <summary>
		/// Loads a highlighting definition from the syntax definition.
		/// </summary>
		public static IHighlightingDefinition Load(XshdSyntaxDefinition syntaxDefinition, IHighlightingDefinitionReferenceResolver resolver)
		{
			if (syntaxDefinition == null) throw new ArgumentNullException("syntaxDefinition");
			return new XmlHighlightingDefinition(syntaxDefinition, resolver);
		}
	}
}
