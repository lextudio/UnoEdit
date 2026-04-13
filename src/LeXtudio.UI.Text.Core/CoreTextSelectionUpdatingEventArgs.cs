using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>Event arguments used when updating the current selection.</summary>
    public class CoreTextSelectionUpdatingEventArgs : EventArgs
    {
        /// <summary>The proposed new start offset for the selection.</summary>
        public int NewStart { get; set; }

        /// <summary>The proposed new length for the selection.</summary>
        public int NewLength { get; set; }
    }
}
