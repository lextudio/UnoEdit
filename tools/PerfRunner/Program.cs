using System;
using System.Diagnostics;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

class Program
{
    static void Main(string[] args)
    {
        int lines = 100000;
        Console.WriteLine($"Building document with {lines} lines...");
        var sb = new StringBuilder(lines * 10);
        for (int i = 0; i < lines; i++)
        {
            sb.AppendLine($"line {i} {{");
            sb.AppendLine("    int x = 0;");
            sb.AppendLine("}");
        }

        var doc = new TextDocument(sb.ToString());
        var fm = new FoldingManager(doc);
        var strategy = new BraceFoldingStrategy();

        Console.WriteLine("Measuring folding strategy.CreateNewFoldings...");
        var sw = Stopwatch.StartNew();
        var foldings = strategy.CreateNewFoldings(doc);
        sw.Stop();
        Console.WriteLine($"CreateNewFoldings: {sw.ElapsedMilliseconds} ms");

        Console.WriteLine("Measuring FoldingManager.UpdateFoldings...");
        sw.Restart();
        strategy.UpdateFoldings(fm, doc);
        sw.Stop();
        Console.WriteLine($"UpdateFoldings: {sw.ElapsedMilliseconds} ms");
    }
}
