using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>Event arguments for a text request.</summary>
    public class CoreTextTextRequestedEventArgs : EventArgs
    {
        /// <summary>Creates a new instance wrapping the specified request.</summary>
        public CoreTextTextRequestedEventArgs(CoreTextTextRequest request) => Request = request;

        /// <summary>The <see cref="CoreTextTextRequest"/> associated with the event.</summary>
        public CoreTextTextRequest Request { get; }
    }
}
