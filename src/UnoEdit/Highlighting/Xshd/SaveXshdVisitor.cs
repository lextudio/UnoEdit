// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — functional serializer implementation.

using System;
using System.Linq;
using System.Xml;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// Visitor that serializes an XSHD definition to XML.
	/// </summary>
	public sealed class SaveXshdVisitor : IXshdVisitor
	{
		/// <summary>The XSHD namespace URI.</summary>
		public const string Namespace = V2Loader.Namespace;

		readonly XmlWriter writer;

		/// <summary>Creates a new SaveXshdVisitor.</summary>
		public SaveXshdVisitor(XmlWriter writer)
		{
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
		}

		/// <summary>Writes the syntax definition.</summary>
		public void WriteDefinition(XshdSyntaxDefinition definition)
		{
			if (definition == null)
				throw new ArgumentNullException(nameof(definition));

			writer.WriteStartElement("SyntaxDefinition", Namespace);
			if (definition.Name != null)
				writer.WriteAttributeString("name", definition.Name);
			if (definition.Extensions != null)
				writer.WriteAttributeString("extensions", string.Join(";", definition.Extensions.ToArray()));

			definition.AcceptElements(this);
			writer.WriteEndElement();
		}

		object IXshdVisitor.VisitRuleSet(XshdRuleSet ruleSet)
		{
			writer.WriteStartElement("RuleSet", Namespace);
			if (ruleSet.Name != null)
				writer.WriteAttributeString("name", ruleSet.Name);
			WriteBoolAttribute("ignoreCase", ruleSet.IgnoreCase);
			ruleSet.AcceptElements(this);
			writer.WriteEndElement();
			return null;
		}

		void WriteBoolAttribute(string attributeName, bool? value)
		{
			if (value != null)
				writer.WriteAttributeString(attributeName, value.Value ? "true" : "false");
		}

		void WriteRuleSetReference(XshdReference<XshdRuleSet> ruleSetReference)
		{
			if (ruleSetReference.ReferencedElement != null) {
				if (ruleSetReference.ReferencedDefinition != null)
					writer.WriteAttributeString("ruleSet", ruleSetReference.ReferencedDefinition + "/" + ruleSetReference.ReferencedElement);
				else
					writer.WriteAttributeString("ruleSet", ruleSetReference.ReferencedElement);
			}
		}

		void WriteColorReference(XshdReference<XshdColor> color)
		{
			if (color.InlineElement != null)
				WriteColorAttributes(color.InlineElement);
			else if (color.ReferencedElement != null) {
				if (color.ReferencedDefinition != null)
					writer.WriteAttributeString("color", color.ReferencedDefinition + "/" + color.ReferencedElement);
				else
					writer.WriteAttributeString("color", color.ReferencedElement);
			}
		}

		void WriteColorAttributes(XshdColor color)
		{
			if (color.Foreground != null)
				writer.WriteAttributeString("foreground", color.Foreground.ToString());
			if (color.Background != null)
				writer.WriteAttributeString("background", color.Background.ToString());
			if (color.FontWeight != null)
				writer.WriteAttributeString("fontWeight", V2Loader.FontWeightConverter.ConvertToInvariantString(color.FontWeight.Value).ToLowerInvariant());
			if (color.FontStyle != null)
				writer.WriteAttributeString("fontStyle", V2Loader.FontStyleConverter.ConvertToInvariantString(color.FontStyle.Value).ToLowerInvariant());
		}

		object IXshdVisitor.VisitColor(XshdColor color)
		{
			writer.WriteStartElement("Color", Namespace);
			if (color.Name != null)
				writer.WriteAttributeString("name", color.Name);
			WriteColorAttributes(color);
			if (color.ExampleText != null)
				writer.WriteAttributeString("exampleText", color.ExampleText);
			writer.WriteEndElement();
			return null;
		}

		object IXshdVisitor.VisitImport(XshdImport import)
		{
			writer.WriteStartElement("Import", Namespace);
			WriteRuleSetReference(import.RuleSetReference);
			writer.WriteEndElement();
			return null;
		}

		object IXshdVisitor.VisitKeywords(XshdKeywords keywords)
		{
			writer.WriteStartElement("Keywords", Namespace);
			WriteColorReference(keywords.ColorReference);
			foreach (var word in keywords.Words)
				writer.WriteElementString("Word", Namespace, word);
			writer.WriteEndElement();
			return null;
		}

		object IXshdVisitor.VisitRule(XshdRule rule)
		{
			writer.WriteStartElement("Rule", Namespace);
			WriteColorReference(rule.ColorReference);
			writer.WriteString(rule.Regex);
			writer.WriteEndElement();
			return null;
		}

		object IXshdVisitor.VisitSpan(XshdSpan span)
		{
			writer.WriteStartElement("Span", Namespace);
			WriteColorReference(span.SpanColorReference);
			if (span.BeginRegexType == XshdRegexType.Default && span.BeginRegex != null)
				writer.WriteAttributeString("begin", span.BeginRegex);
			if (span.EndRegexType == XshdRegexType.Default && span.EndRegex != null)
				writer.WriteAttributeString("end", span.EndRegex);
			WriteRuleSetReference(span.RuleSetReference);
			if (span.Multiline)
				writer.WriteAttributeString("multiline", "true");

			if (span.BeginRegexType == XshdRegexType.IgnorePatternWhitespace)
				WriteBeginEndElement("Begin", span.BeginRegex, span.BeginColorReference);
			if (span.EndRegexType == XshdRegexType.IgnorePatternWhitespace)
				WriteBeginEndElement("End", span.EndRegex, span.EndColorReference);

			if (span.RuleSetReference.InlineElement != null)
				span.RuleSetReference.InlineElement.AcceptVisitor(this);

			writer.WriteEndElement();
			return null;
		}

		void WriteBeginEndElement(string elementName, string regex, XshdReference<XshdColor> colorReference)
		{
			if (regex == null)
				return;
			writer.WriteStartElement(elementName, Namespace);
			WriteColorReference(colorReference);
			writer.WriteString(regex);
			writer.WriteEndElement();
		}
	}
}
