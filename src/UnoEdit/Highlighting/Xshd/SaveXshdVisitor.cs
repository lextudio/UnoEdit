// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — minimal stub for API parity.

using System;
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

		/// <summary>Creates a new SaveXshdVisitor.</summary>
		public SaveXshdVisitor(XmlWriter writer) { }

		/// <summary>Writes the syntax definition.</summary>
		public void WriteDefinition(XshdSyntaxDefinition definition) { }

		object IXshdVisitor.VisitColor(XshdColor color) => null;
		object IXshdVisitor.VisitImport(XshdImport import) => null;
		object IXshdVisitor.VisitKeywords(XshdKeywords keywords) => null;
		object IXshdVisitor.VisitRule(XshdRule rule) => null;
		object IXshdVisitor.VisitRuleSet(XshdRuleSet ruleSet) => null;
		object IXshdVisitor.VisitSpan(XshdSpan span) => null;
	}
}
