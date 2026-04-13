using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>Event arguments for a layout request.</summary>
    public class CoreTextLayoutRequestedEventArgs : EventArgs
    {
        /// <summary>Creates a new instance wrapping the specified layout request.</summary>
        public CoreTextLayoutRequestedEventArgs(CoreTextLayoutRequest request) => Request = request;

        /// <summary>The <see cref="CoreTextLayoutRequest"/> associated with the event.</summary>
        public CoreTextLayoutRequest Request { get; }
    }
}
