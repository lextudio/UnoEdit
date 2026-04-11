// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — stub for API parity.

using System;
using Microsoft.UI.Xaml.Automation.Peers;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Automation peer for the TextEditor control.
	/// </summary>
	public class TextEditorAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>Creates a new TextEditorAutomationPeer.</summary>
		public TextEditorAutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner) : base(owner) { }

		/// <summary>Gets a pattern object for the specified pattern interface.</summary>
		public new object GetPattern(PatternInterface patternInterface)
		{
			return base.GetPattern(patternInterface);
		}

		/// <inheritdoc/>
		protected override string GetClassNameCore() => "TextEditor";

		/// <inheritdoc/>
		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
	}
}
