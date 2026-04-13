using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Event args for platform command callbacks (e.g. AppKit <c>doCommandBySelector:</c>).
    /// </summary>
    public sealed class CoreTextCommandReceivedEventArgs : EventArgs
    {
        /// <summary>Initializes a new instance with the given command name.</summary>
        /// <param name="command">The platform command string (e.g. "deleteBackward:", "moveLeft:").</param>
        public CoreTextCommandReceivedEventArgs(string command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        /// <summary>The platform command string.</summary>
        public string Command { get; }

        /// <summary>Set to <c>true</c> if the consumer handled the command.</summary>
        public bool Handled { get; set; }
    }
}
