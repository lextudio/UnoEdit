namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Lightweight deferral used to signal asynchronous completion of a request.
    /// </summary>
    public class CoreTextDeferral
    {
        private bool _completed;

        /// <summary>Marks the deferral as complete.</summary>
        public void Complete() => _completed = true;

        /// <summary>Gets whether <see cref="Complete"/> has been called.</summary>
        public bool IsCompleted => _completed;
    }
}
