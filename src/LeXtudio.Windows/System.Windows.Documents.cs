namespace System.Windows.Documents
{
    public enum LogicalDirection
    {
        Backward = 0,
        Forward = 1
    }

    /// <summary>Compiler shim for System.Windows.Documents.Inline.</summary>
    public abstract class Inline { }

    /// <summary>Compiler shim for System.Windows.Documents.TextElement.</summary>
    public abstract class TextElement : Inline
    {
        public object Foreground { get; set; }
        public object Background { get; set; }
        public Windows.FontWeight FontWeight { get; set; }
        public Windows.FontStyle FontStyle { get; set; }
    }

    /// <summary>Compiler shim for System.Windows.Documents.Run.</summary>
    public class Run : TextElement
    {
        public string Text { get; set; }
        public Run() { }
        public Run(string text) { Text = text; }
    }
}
