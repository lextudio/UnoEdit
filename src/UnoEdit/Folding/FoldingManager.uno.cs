// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — platform installation uses reflection to avoid an app-layer dependency.

using System;
using System.Collections.Generic;
using System.Reflection;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Folding
{
	public partial class FoldingManager
	{
		static readonly Dictionary<FoldingManager, WeakReference<object>> installedManagers = new Dictionary<FoldingManager, WeakReference<object>>();

		/// <summary>Creates a new FoldingManager and attaches it to the given text area.</summary>
		public static FoldingManager Install(object textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException(nameof(textArea));

			var fmProperty = textArea.GetType().GetProperty("FoldingManager", BindingFlags.Public | BindingFlags.Instance);
			if (fmProperty == null)
				throw new ArgumentException("textArea must expose a FoldingManager property", nameof(textArea));

			if (fmProperty.GetValue(textArea) is FoldingManager existing)
				return existing;

			var document = textArea.GetType().GetProperty("Document", BindingFlags.Public | BindingFlags.Instance)?.GetValue(textArea) as TextDocument;
			if (document == null)
				throw new ArgumentException("textArea must expose a Document property of type TextDocument", nameof(textArea));

			var manager = new FoldingManager(document);
			fmProperty.SetValue(textArea, manager);
			lock (installedManagers) {
				installedManagers[manager] = new WeakReference<object>(textArea);
			}
			return manager;
		}

		/// <summary>Uninstalls a FoldingManager.</summary>
		public static void Uninstall(FoldingManager foldingManager)
		{
			if (foldingManager == null)
				return;

			object textArea = null;
			lock (installedManagers) {
				if (installedManagers.TryGetValue(foldingManager, out var weak)) {
					weak.TryGetTarget(out textArea);
					installedManagers.Remove(foldingManager);
				}
			}

			if (textArea != null) {
				var fmProperty = textArea.GetType().GetProperty("FoldingManager", BindingFlags.Public | BindingFlags.Instance);
				if (fmProperty?.GetValue(textArea) is FoldingManager installed && ReferenceEquals(installed, foldingManager))
					fmProperty.SetValue(textArea, null);
			}

			foldingManager.Clear();
		}
	}
}
