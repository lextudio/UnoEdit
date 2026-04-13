using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>Event arguments for a text-updating operation.</summary>
    public class CoreTextTextUpdatingEventArgs : EventArgs
    {
        /// <summary>Creates a new instance with the proposed new text.</summary>
        public CoreTextTextUpdatingEventArgs(string newText) => NewText = newText;

        /// <summary>The new text being applied; handlers may modify this value.</summary>
        public string NewText { get; set; }
    }
}
