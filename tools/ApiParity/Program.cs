using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static int Main(string[] args)
    {
        string toolDirectory = AppContext.BaseDirectory;
        string repoRoot = FindRepoRoot(toolDirectory);
        string defaultJustifications = Path.Combine(repoRoot, "tools", "ApiParity", "api-parity.justifications.json");

        var options = ParseOptions(args, repoRoot, defaultJustifications);

        var avalonSymbols = CollectSymbols(options.AvalonRoots, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var unoLinkedFiles = options.UnoProjectFiles
            .SelectMany(GetLinkedFilesFromCsproj)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unoSymbols = CollectSymbols(options.UnoRoots, unoLinkedFiles);
        var justifications = LoadJustifications(options.JustificationsPath);

        var rawMissingTypes = avalonSymbols.Types.Except(unoSymbols.Types, StringComparer.Ordinal).OrderBy(static x => x).ToList();
        var rawMissingMembers = avalonSymbols.Members.Except(unoSymbols.Members, StringComparer.Ordinal).OrderBy(static x => x).ToList();

        var justifiedTypeNames = new HashSet<string>(justifications.IgnoredTypes.Select(static x => x.Name), StringComparer.Ordinal);
        var justifiedMemberNames = new HashSet<string>(justifications.IgnoredMembers.Select(static x => x.Name), StringComparer.Ordinal);
        foreach (MappedMember mapped in justifications.MappedMembers)
        {
            if (unoSymbols.Members.Contains(mapped.Target))
            {
                justifiedMemberNames.Add(mapped.Source);
            }
        }

        var adjustedMissingTypes = rawMissingTypes.Where(name => !justifiedTypeNames.Contains(name)).ToList();
        var adjustedMissingMembers = rawMissingMembers.Where(name => !justifiedMemberNames.Contains(name)).ToList();

        var report = new ParityReport(
            new CoverageSummary(avalonSymbols.Types.Count, unoSymbols.Types.Count, rawMissingTypes.Count, adjustedMissingTypes.Count),
            new CoverageSummary(avalonSymbols.Members.Count, unoSymbols.Members.Count, rawMissingMembers.Count, adjustedMissingMembers.Count),
            rawMissingTypes.Take(options.ListLimit).ToList(),
            rawMissingMembers.Take(options.ListLimit).ToList(),
            adjustedMissingTypes.Take(options.ListLimit).ToList(),
            adjustedMissingMembers.Take(options.ListLimit).ToList(),
            justifications);

        PrintReport(report, options.JustificationsPath);

        if (!string.IsNullOrWhiteSpace(options.OutputJsonPath))
        {
            string outputPath = Path.GetFullPath(options.OutputJsonPath, Directory.GetCurrentDirectory());
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
            Console.WriteLine();
            Console.WriteLine($"JSON report written to {outputPath}");
        }

        return 0;
    }

    private static Options ParseOptions(string[] args, string repoRoot, string defaultJustifications)
    {
        var avalonRoots = new List<string>
        {
            Path.Combine(repoRoot, "avalonedit", "ICSharpCode.AvalonEdit")
        };
        var unoRoots = new List<string>
        {
            Path.Combine(repoRoot, "src", "UnoEdit"),
            Path.Combine(repoRoot, "src", "UnoEdit.TextMate")
        };
        var unoProjectFiles = new List<string>
        {
            Path.Combine(repoRoot, "src", "UnoEdit", "UnoEdit.csproj")
        };

        string justificationsPath = defaultJustifications;
        string? outputJsonPath = null;
        int listLimit = 25;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--avalon-root":
                    avalonRoots.Add(RequireValue(args, ref i, "--avalon-root"));
                    break;
                case "--uno-root":
                    unoRoots.Add(RequireValue(args, ref i, "--uno-root"));
                    break;
                case "--justifications":
                    justificationsPath = RequireValue(args, ref i, "--justifications");
                    break;
                case "--output-json":
                    outputJsonPath = RequireValue(args, ref i, "--output-json");
                    break;
                case "--list-limit":
                    listLimit = int.Parse(RequireValue(args, ref i, "--list-limit"));
                    break;
                case "--replace-avalon-roots":
                    avalonRoots.Clear();
                    break;
                case "--replace-uno-roots":
                    unoRoots.Clear();
                    break;
                case "--uno-project":
                    unoProjectFiles.Add(RequireValue(args, ref i, "--uno-project"));
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        return new Options(
            avalonRoots.Select(Path.GetFullPath).ToArray(),
            unoRoots.Select(Path.GetFullPath).ToArray(),
            unoProjectFiles.Where(File.Exists).Select(Path.GetFullPath).ToArray(),
            Path.GetFullPath(justificationsPath),
            outputJsonPath,
            listLimit);
    }

    private static string RequireValue(string[] args, ref int i, string optionName)
    {
        i++;
        if (i >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        return args[i];
    }

    private static string FindRepoRoot(string startDirectory)
    {
        DirectoryInfo? current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "UnoEdit.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root containing UnoEdit.slnx.");
    }

    private static SourceSymbols CollectSymbols(IEnumerable<string> roots, HashSet<string> additionalFiles)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        var members = new HashSet<string>(StringComparer.Ordinal);
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots.Where(Directory.Exists))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                processedFiles.Add(file);
                ScanFile(file, types, members);
            }
        }

        foreach (string file in additionalFiles)
        {
            if (!processedFiles.Contains(file) && File.Exists(file))
            {
                processedFiles.Add(file);
                ScanFile(file, types, members);
            }
        }

        return new SourceSymbols(types, members);
    }

    private static void ScanFile(string file, HashSet<string> types, HashSet<string> members)
    {
        string text = File.ReadAllText(file);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
        SyntaxNode rootNode = tree.GetRoot();
        var walker = new PublicApiWalker(types, members);
        walker.Visit(rootNode);
    }

    private static IEnumerable<string> GetLinkedFilesFromCsproj(string csprojPath)
    {
        string csprojDir = Path.GetDirectoryName(csprojPath)!;
        XDocument xdoc = XDocument.Load(csprojPath);

        foreach (XElement compile in xdoc.Descendants().Where(e => e.Name.LocalName == "Compile"))
        {
            string? include = compile.Attribute("Include")?.Value;
            if (include == null)
                continue;

            // Normalize path separators
            include = include.Replace('\\', Path.DirectorySeparatorChar);

            // Only process paths that point outside src/ (i.e., linked from avalonedit/)
            if (!include.StartsWith("..", StringComparison.Ordinal))
                continue;

            if (include.Contains("**"))
            {
                // MSBuild recursive glob: split at **
                int starIdx = include.IndexOf("**", StringComparison.Ordinal);
                string baseRelative = include.Substring(0, starIdx);
                string baseDir = Path.GetFullPath(Path.Combine(csprojDir, baseRelative));
                if (Directory.Exists(baseDir))
                {
                    foreach (string file in Directory.EnumerateFiles(baseDir, "*.cs", SearchOption.AllDirectories))
                    {
                        if (!file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                        {
                            yield return file;
                        }
                    }
                }
            }
            else
            {
                string fullPath = Path.GetFullPath(Path.Combine(csprojDir, include));
                if (File.Exists(fullPath))
                    yield return fullPath;
            }
        }
    }

    private static Justifications LoadJustifications(string path)
    {
        if (!File.Exists(path))
        {
            return new Justifications();
        }

        return JsonSerializer.Deserialize<Justifications>(File.ReadAllText(path), JsonOptions) ?? new Justifications();
    }

    private static void PrintReport(ParityReport report, string justificationsPath)
    {
        Console.WriteLine("API parity report");
        Console.WriteLine();
        Console.WriteLine($"Justifications: {justificationsPath}");
        Console.WriteLine();

        PrintCoverage("Types", report.Types);
        PrintCoverage("Members", report.Members);

        PrintList("Raw missing types", report.RawMissingTypesSample);
        PrintList("Raw missing members", report.RawMissingMembersSample);
        PrintList("Adjusted missing types", report.AdjustedMissingTypesSample);
        PrintList("Adjusted missing members", report.AdjustedMissingMembersSample);
    }

    private static void PrintCoverage(string label, CoverageSummary summary)
    {
        Console.WriteLine($"{label}:");
        Console.WriteLine($"  source total: {summary.SourceTotal}");
        Console.WriteLine($"  uno total: {summary.TargetTotal}");
        Console.WriteLine($"  raw missing: {summary.RawMissing}");
        Console.WriteLine($"  adjusted missing: {summary.AdjustedMissing}");
        Console.WriteLine($"  raw parity: {FormatPercent(summary.SourceTotal - summary.RawMissing, summary.SourceTotal)}");
        Console.WriteLine($"  adjusted parity: {FormatPercent(summary.SourceTotal - summary.AdjustedMissing, summary.SourceTotal)}");
        Console.WriteLine();
    }

    private static string FormatPercent(int matched, int total)
    {
        if (total <= 0)
        {
            return "n/a";
        }

        double percent = (double)matched / total * 100d;
        return $"{matched}/{total} ({percent:F1}%)";
    }

    private static void PrintList(string title, IReadOnlyList<string> values)
    {
        Console.WriteLine($"{title}:");
        if (values.Count == 0)
        {
            Console.WriteLine("  <none>");
        }
        else
        {
            foreach (string value in values)
            {
                Console.WriteLine($"  - {value}");
            }
        }

        Console.WriteLine();
    }

    private sealed class PublicApiWalker(HashSet<string> types, HashSet<string> members) : CSharpSyntaxWalker(SyntaxWalkerDepth.Node)
    {
        private readonly Stack<string> typeStack = new();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitType(node, node.Identifier.ValueText, static (walker, current) => walker.VisitClassDeclarationCore(current));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            VisitType(node, node.Identifier.ValueText, static (walker, current) => walker.VisitStructDeclarationCore(current));
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            VisitType(node, node.Identifier.ValueText, static (walker, current) => walker.VisitInterfaceDeclarationCore(current));
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            VisitType(node, node.Identifier.ValueText, static (walker, current) => walker.VisitEnumDeclarationCore(current));
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            VisitType(node, node.Identifier.ValueText, static (walker, current) => walker.VisitRecordDeclarationCore(current));
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers))
            {
                types.Add(node.Identifier.ValueText);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            MaybeAddMember(node.Modifiers, node.Identifier.ValueText, node.Parent);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            MaybeAddMember(node.Modifiers, node.Identifier.ValueText, node.Parent);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            MaybeAddMember(node.Modifiers, "this[]", node.Parent);
            base.VisitIndexerDeclaration(node);
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            MaybeAddMember(node.Modifiers, node.Identifier.ValueText, node.Parent);
            base.VisitEventDeclaration(node);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && TryGetCurrentType(out string? typeName))
            {
                foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
                {
                    members.Add($"{typeName}.{variable.Identifier.ValueText}");
                }
            }

            base.VisitEventFieldDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (IsPublic(node.Modifiers) && TryGetCurrentType(out string? typeName))
            {
                foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
                {
                    members.Add($"{typeName}.{variable.Identifier.ValueText}");
                }
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            MaybeAddMember(node.Modifiers, ".ctor", node.Parent);
            base.VisitConstructorDeclaration(node);
        }

        private void VisitType<TNode>(TNode node, string typeName, Action<PublicApiWalker, TNode> baseVisit)
            where TNode : MemberDeclarationSyntax
        {
            if (!IsPublic(node))
            {
                return;
            }

            types.Add(typeName);
            typeStack.Push(typeName);
            try
            {
                baseVisit(this, node);
            }
            finally
            {
                typeStack.Pop();
            }
        }

        private void VisitClassDeclarationCore(ClassDeclarationSyntax node) => base.VisitClassDeclaration(node);
        private void VisitStructDeclarationCore(StructDeclarationSyntax node) => base.VisitStructDeclaration(node);
        private void VisitInterfaceDeclarationCore(InterfaceDeclarationSyntax node) => base.VisitInterfaceDeclaration(node);
        private void VisitEnumDeclarationCore(EnumDeclarationSyntax node) => base.VisitEnumDeclaration(node);
        private void VisitRecordDeclarationCore(RecordDeclarationSyntax node) => base.VisitRecordDeclaration(node);

        private void MaybeAddMember(SyntaxTokenList modifiers, string memberName, SyntaxNode? parent)
        {
            if (!TryGetCurrentType(out string? typeName))
            {
                return;
            }

            bool isPublic = IsPublic(modifiers) || IsInterfaceMember(parent, modifiers);
            if (isPublic)
            {
                members.Add($"{typeName}.{memberName}");
            }
        }

        private bool TryGetCurrentType(out string? typeName)
        {
            if (typeStack.Count > 0)
            {
                typeName = typeStack.Peek();
                return true;
            }

            typeName = null;
            return false;
        }

        private static bool IsInterfaceMember(SyntaxNode? parent, SyntaxTokenList modifiers)
        {
            if (parent is InterfaceDeclarationSyntax)
            {
                return !modifiers.Any(token =>
                    token.IsKind(SyntaxKind.PrivateKeyword)
                    || token.IsKind(SyntaxKind.InternalKeyword)
                    || token.IsKind(SyntaxKind.ProtectedKeyword));
            }

            return false;
        }

        private static bool IsPublic(MemberDeclarationSyntax node)
        {
            return IsPublic(node.GetModifiers()) || node.Parent is InterfaceDeclarationSyntax;
        }

        private static bool IsPublic(SyntaxTokenList modifiers)
        {
            return modifiers.Any(static token => token.IsKind(SyntaxKind.PublicKeyword));
        }
    }

    private sealed record Options(
        string[] AvalonRoots,
        string[] UnoRoots,
        string[] UnoProjectFiles,
        string JustificationsPath,
        string? OutputJsonPath,
        int ListLimit);

    private sealed record SourceSymbols(
        HashSet<string> Types,
        HashSet<string> Members);

    private sealed record CoverageSummary(
        int SourceTotal,
        int TargetTotal,
        int RawMissing,
        int AdjustedMissing);

    private sealed record ParityReport(
        CoverageSummary Types,
        CoverageSummary Members,
        IReadOnlyList<string> RawMissingTypesSample,
        IReadOnlyList<string> RawMissingMembersSample,
        IReadOnlyList<string> AdjustedMissingTypesSample,
        IReadOnlyList<string> AdjustedMissingMembersSample,
        Justifications Justifications);

    private sealed class Justifications
    {
        public string? Notes { get; init; }
        public List<NamedJustification> IgnoredTypes { get; init; } = [];
        public List<NamedJustification> IgnoredMembers { get; init; } = [];
        public List<MappedMember> MappedMembers { get; init; } = [];
    }

    private sealed class NamedJustification
    {
        public string Name { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }

    private sealed class MappedMember
    {
        public string Source { get; init; } = string.Empty;
        public string Target { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }
}

internal static class RoslynExtensions
{
    public static SyntaxTokenList GetModifiers(this MemberDeclarationSyntax node)
    {
        return node switch
        {
            BaseTypeDeclarationSyntax type => type.Modifiers,
            DelegateDeclarationSyntax @delegate => @delegate.Modifiers,
            MethodDeclarationSyntax method => method.Modifiers,
            PropertyDeclarationSyntax property => property.Modifiers,
            IndexerDeclarationSyntax indexer => indexer.Modifiers,
            EventDeclarationSyntax @event => @event.Modifiers,
            EventFieldDeclarationSyntax eventField => eventField.Modifiers,
            FieldDeclarationSyntax field => field.Modifiers,
            ConstructorDeclarationSyntax ctor => ctor.Modifiers,
            _ => default
        };
    }
}
