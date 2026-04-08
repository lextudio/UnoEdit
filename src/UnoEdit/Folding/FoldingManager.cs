// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team (original AvalonEdit)
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// Stores a list of foldings for a specific <see cref="TextDocument"/>.
	/// Changes in the document automatically update the folding offsets.
	/// </summary>
	public class FoldingManager : IWeakEventListener
	{
		internal readonly TextDocument document;
		readonly TextSegmentCollection<FoldingSection> foldings;
		bool isFirstUpdate = true;

		/// <summary>Raised whenever the set of foldings or the IsFolded state changes.</summary>
		public event EventHandler FoldingsChanged;

		/// <summary>Gets the document whose foldings are tracked.</summary>
		public TextDocument Document => document;

		#region Constructor
		/// <summary>Creates a new <see cref="FoldingManager"/> for the given document.</summary>
		public FoldingManager(TextDocument document)
		{
			if (document == null) throw new ArgumentNullException("document");
			this.document = document;
			this.foldings = new TextSegmentCollection<FoldingSection>();
			document.VerifyAccess();
			TextDocumentWeakEventManager.Changed.AddListener(document, this);
		}
		#endregion

		#region IWeakEventListener
		/// <inheritdoc/>
		protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextDocumentWeakEventManager.Changed)) {
				OnDocumentChanged((DocumentChangeEventArgs)e);
				return true;
			}
			return false;
		}

		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}

		void OnDocumentChanged(DocumentChangeEventArgs e)
		{
			foldings.UpdateOffsets(e);
			int newEndOffset = e.Offset + e.InsertionLength;
			var endLine = document.GetLineByOffset(newEndOffset);
			newEndOffset = endLine.Offset + endLine.TotalLength;
			foreach (var affectedFolding in foldings.FindOverlappingSegments(e.Offset, newEndOffset - e.Offset).ToList()) {
				if (affectedFolding.Length == 0)
					RemoveFolding(affectedFolding);
			}
			RaiseFoldingsChanged();
		}
		#endregion

		#region Create / Remove / Clear
		/// <summary>Creates a folding from <paramref name="startOffset"/> to <paramref name="endOffset"/>.</summary>
		public FoldingSection CreateFolding(int startOffset, int endOffset)
		{
			if (startOffset >= endOffset)
				throw new ArgumentException("startOffset must be less than endOffset");
			if (startOffset < 0 || endOffset > document.TextLength)
				throw new ArgumentException("Folding must be within document boundary");
			var fs = new FoldingSection(this, startOffset, endOffset);
			foldings.Add(fs);
			RaiseFoldingsChanged();
			return fs;
		}

		/// <summary>Removes a folding section from this manager.</summary>
		public void RemoveFolding(FoldingSection fs)
		{
			if (fs == null) throw new ArgumentNullException("fs");
			fs.IsFolded = false;
			foldings.Remove(fs);
			RaiseFoldingsChanged();
		}

		/// <summary>Removes all folding sections.</summary>
		public void Clear()
		{
			document.VerifyAccess();
			foreach (FoldingSection s in foldings)
				s.IsFolded = false;
			foldings.Clear();
			RaiseFoldingsChanged();
		}
		#endregion

		#region Get…Folding
		/// <summary>Gets all foldings sorted by start offset.</summary>
		public IEnumerable<FoldingSection> AllFoldings => foldings;

		/// <summary>
		/// Gets the first offset ≥ <paramref name="startOffset"/> where a folded section starts.
		/// Returns -1 if there are none.
		/// </summary>
		public int GetNextFoldedFoldingStart(int startOffset)
		{
			var fs = foldings.FindFirstSegmentWithStartAfter(startOffset);
			while (fs != null && !fs.IsFolded)
				fs = foldings.GetNextSegment(fs);
			return fs != null ? fs.StartOffset : -1;
		}

		/// <summary>Gets all foldings whose start offset equals <paramref name="startOffset"/>.</summary>
		public ReadOnlyCollection<FoldingSection> GetFoldingsAt(int startOffset)
		{
			var result = new List<FoldingSection>();
			var fs = foldings.FindFirstSegmentWithStartAfter(startOffset);
			while (fs != null && fs.StartOffset == startOffset) {
				result.Add(fs);
				fs = foldings.GetNextSegment(fs);
			}
			return result.AsReadOnly();
		}

		/// <summary>Gets all foldings that contain <paramref name="offset"/>.</summary>
		public ReadOnlyCollection<FoldingSection> GetFoldingsContaining(int offset)
		{
			return foldings.FindSegmentsContaining(offset);
		}
		#endregion

		#region UpdateFoldings
		/// <summary>
		/// Replaces the current set of foldings with <paramref name="newFoldings"/>, preserving
		/// the <see cref="FoldingSection.IsFolded"/> state of sections that remain.
		/// <paramref name="newFoldings"/> must be sorted by <see cref="NewFolding.StartOffset"/>.
		/// </summary>
		public void UpdateFoldings(IEnumerable<NewFolding> newFoldings, int firstErrorOffset)
		{
			if (newFoldings == null) throw new ArgumentNullException("newFoldings");
			if (firstErrorOffset < 0) firstErrorOffset = int.MaxValue;

			var oldFoldings = this.AllFoldings.ToArray();
			int oldFoldingIndex = 0;
			int previousStartOffset = 0;

			foreach (NewFolding newFolding in newFoldings) {
				if (newFolding.StartOffset < previousStartOffset)
					throw new ArgumentException("newFoldings must be sorted by start offset");
				previousStartOffset = newFolding.StartOffset;

				int startOffset = Math.Clamp(newFolding.StartOffset, 0, document.TextLength);
				int endOffset   = Math.Clamp(newFolding.EndOffset,   0, document.TextLength);
				if (startOffset == endOffset) continue;

				// Discard old foldings that were skipped (start before this new one).
				while (oldFoldingIndex < oldFoldings.Length && newFolding.StartOffset > oldFoldings[oldFoldingIndex].StartOffset) {
					this.RemoveFolding(oldFoldings[oldFoldingIndex++]);
				}

				FoldingSection section;
				if (oldFoldingIndex < oldFoldings.Length && newFolding.StartOffset == oldFoldings[oldFoldingIndex].StartOffset) {
					// Reuse existing section.
					section = oldFoldings[oldFoldingIndex++];
					section.Length = endOffset - startOffset;
				} else {
					// No matching section — create a new one.
					section = this.CreateFolding(startOffset, endOffset);
					if (isFirstUpdate)
						section.IsFolded = newFolding.DefaultClosed;
					section.Tag = newFolding;
				}
				section.Title = newFolding.Name;
			}

			isFirstUpdate = false;

			// Remove any remaining old foldings.
			while (oldFoldingIndex < oldFoldings.Length) {
				if (oldFoldings[oldFoldingIndex].StartOffset >= firstErrorOffset) break;
				this.RemoveFolding(oldFoldings[oldFoldingIndex++]);
			}
		}
		#endregion

		internal void RaiseFoldingsChanged() => FoldingsChanged?.Invoke(this, EventArgs.Empty);
	}
}
