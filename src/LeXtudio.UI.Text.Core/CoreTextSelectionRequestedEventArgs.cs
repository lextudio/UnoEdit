using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>Event arguments for a selection request.</summary>
    public class CoreTextSelectionRequestedEventArgs : EventArgs
    {
        /// <summary>Creates a new instance wrapping the specified selection request.</summary>
        public CoreTextSelectionRequestedEventArgs(CoreTextSelectionRequest request) => Request = request;

        /// <summary>The <see cref="CoreTextSelectionRequest"/> associated with the event.</summary>
        public CoreTextSelectionRequest Request { get; }
    }
}
