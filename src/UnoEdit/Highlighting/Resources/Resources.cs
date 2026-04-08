// Forked from AvalonEdit for UnoEdit — loads resources from UnoEdit assembly, registers built-in highlightings.
// Original: ICSharpCode.AvalonEdit/Highlighting/Resources/Resources.cs

using System.IO;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	static class Resources
	{
		static readonly string Prefix = typeof(Resources).FullName + ".";

		public static Stream OpenStream(string name)
		{
			Stream s = typeof(Resources).Assembly.GetManifestResourceStream(Prefix + name);
			if (s == null)
				throw new FileNotFoundException("The resource file '" + name + "' was not found.");
			return s;
		}

		internal static void RegisterBuiltInHighlightings(HighlightingManager.DefaultHighlightingManager hlm)
		{
			hlm.RegisterHighlighting("XmlDoc", null, "XmlDoc.xshd");
			hlm.RegisterHighlighting("C#", new[] { ".cs" }, "CSharp-Mode.xshd");
			hlm.RegisterHighlighting("JavaScript", new[] { ".js" }, "JavaScript-Mode.xshd");
			hlm.RegisterHighlighting("HTML", new[] { ".htm", ".html" }, "HTML-Mode.xshd");
			hlm.RegisterHighlighting("CSS", new[] { ".css" }, "CSS-Mode.xshd");
			hlm.RegisterHighlighting("C++", new[] { ".c", ".h", ".cc", ".cpp", ".hpp" }, "CPP-Mode.xshd");
			hlm.RegisterHighlighting("Java", new[] { ".java" }, "Java-Mode.xshd");
			hlm.RegisterHighlighting("Python", new[] { ".py", ".pyw" }, "Python-Mode.xshd");
			hlm.RegisterHighlighting("XML", (".xml;.xsl;.xslt;.xsd;.manifest;.config;.addin;" +
											 ".xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.ilproj;" +
											 ".booproj;.build;.xfrm;.targets;.xaml;.xpt;" +
											 ".xft;.map;.wsdl;.disco;.ps1xml;.nuspec").Split(';'),
									 "XML-Mode.xshd");
			hlm.RegisterHighlighting("TSQL", new[] { ".sql" }, "TSQL-Mode.xshd");
			hlm.RegisterHighlighting("VB", new[] { ".vb" }, "VB-Mode.xshd");
		}
	}
}
