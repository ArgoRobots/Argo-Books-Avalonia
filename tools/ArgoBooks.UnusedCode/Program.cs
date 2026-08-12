using System.Text.RegularExpressions;

namespace ArgoBooks.UnusedCode;

/// <summary>
/// Reports public and internal members that nothing in the solution references.
///
/// A member is reported when its name appears exactly once across every .cs and .axaml
/// file in the repository: the declaration itself. Counting whole words rather than
/// resolving symbols keeps this dependency free, and it errs the safe way, because a
/// name mentioned in a string literal, an attribute or a XAML binding still counts as
/// a use.
///
/// WHY THE FILTERS MATTER
///
/// Without them this reports around 2300 members, of which roughly 97% are alive.
/// Three framework patterns call a member without ever naming it again in source:
///
///   1. xUnit [Fact] and [Theory] methods, invoked by the runner through reflection.
///   2. [RelayCommand] methods. CommunityToolkit generates a Name + "Command" property
///      and XAML binds to that. It also drops a trailing "Async", so SaveFooAsync is
///      reached as SaveFooCommand and a plain name match never connects the two.
///   3. Avalonia attached property accessors (GetX/SetX next to RegisterAttached),
///      which the XAML compiler calls.
///
/// Test files are excluded as a source of declarations but kept as a source of usage,
/// so something exercised only by a test counts as used, not as dead.
///
/// WHAT IT CANNOT SEE
///
/// Anything reached by reflection over a computed name, by dependency injection, or
/// from outside the repository. Read each hit before deleting it. Overrides and
/// interface implementations are safe by construction: the base or interface
/// declaration is a second occurrence, so they never reach the report.
/// </summary>
internal static partial class Program
{
    private static readonly string[] ScanExtensions = [".cs", ".axaml"];

    private static readonly string[] SkipDirectories = ["obj", "bin", ".git", ".vs", "node_modules"];

    /// <summary>
    /// Words the method pattern can pick up from control flow that happens to look like
    /// a declaration, for example a line starting with "return Foo(".
    /// </summary>
    private static readonly HashSet<string> NotMethodNames =
    [
        "if", "for", "foreach", "while", "switch", "return", "lock",
        "using", "catch", "get", "set", "new", "await", "yield",
    ];

    // Both declaration patterns are anchored at the start of a line so a call sitting
    // inside a method body is never mistaken for a declaration.
    [GeneratedRegex(@"^[ \t]*(?:public|internal)\s+(?:static\s+|async\s+|virtual\s+|sealed\s+|partial\s+|new\s+)*[\w<>?\[\],\. ]+?\s+(\w+)\s*\(", RegexOptions.Multiline)]
    private static partial Regex MethodDeclaration();

    [GeneratedRegex(@"^[ \t]*(?:public|internal)\s+const\s+\w+\s+(\w+)\s*=", RegexOptions.Multiline)]
    private static partial Regex ConstDeclaration();

    [GeneratedRegex(@"\w+")]
    private static partial Regex Word();

    [GeneratedRegex(@"^(Get|Set)[A-Z]")]
    private static partial Regex AttachedAccessor();

    private static int Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("""
                Usage: argo-unused-code [options]

                  --methods   report methods only
                  --consts    report constants only
                  --raw       disable the filters, to inspect what they are hiding

                Reports public and internal members whose name appears only once in the
                whole repository, which means nothing references them.
                """);
            return 0;
        }

        bool raw = args.Contains("--raw");
        bool onlyMethods = args.Contains("--methods");
        bool onlyConsts = args.Contains("--consts");
        bool wantMethods = onlyMethods || !onlyConsts;
        bool wantConsts = onlyConsts || !onlyMethods;

        string root = RepositoryRoot();
        List<string> files = SourceFiles(root);
        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No source files found under {root}");
            return 1;
        }

        // One pass to read every file and count every word in the repository.
        var contents = new Dictionary<string, string>(files.Count);
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string path in files)
        {
            string text = File.ReadAllText(path);
            contents[path] = text;
            foreach (Match word in Word().Matches(text))
            {
                string value = word.Value;
                occurrences[value] = occurrences.GetValueOrDefault(value) + 1;
            }
        }

        var findings = new Dictionary<string, List<(int Line, string Name)>>(StringComparer.Ordinal);
        int scanned = 0;

        foreach ((string path, string text) in contents)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            string relative = Path.GetRelativePath(root, path);
            if (!raw && IsTestFile(relative))
                continue;

            // GetX/SetX pairs are only framework called in a file that registers an
            // attached property, so the exemption is scoped to those files.
            bool hasAttachedProperty = text.Contains("RegisterAttached", StringComparison.Ordinal);

            if (wantMethods)
            {
                foreach (Match match in MethodDeclaration().Matches(text))
                {
                    string name = match.Groups[1].Value;
                    if (NotMethodNames.Contains(name))
                        continue;

                    // An override is reachable through its base declaration, which is
                    // itself a second occurrence, so it could never be reported anyway.
                    if (match.Value.Contains("override", StringComparison.Ordinal))
                        continue;

                    scanned++;

                    if (occurrences.GetValueOrDefault(name) > 1)
                        continue;

                    if (!raw)
                    {
                        if (PrecededByRelayCommand(text, match.Index))
                        {
                            string stem = name.EndsWith("Async", StringComparison.Ordinal)
                                ? name[..^5]
                                : name;
                            if (occurrences.ContainsKey(stem + "Command") ||
                                occurrences.ContainsKey(name + "Command"))
                            {
                                continue;
                            }
                        }

                        if (hasAttachedProperty && AttachedAccessor().IsMatch(name))
                            continue;
                    }

                    Record(findings, relative, LineOf(text, match.Index), name);
                }
            }

            if (wantConsts)
            {
                foreach (Match match in ConstDeclaration().Matches(text))
                {
                    string name = match.Groups[1].Value;
                    scanned++;

                    if (occurrences.GetValueOrDefault(name) > 1)
                        continue;

                    Record(findings, relative, LineOf(text, match.Index), name);
                }
            }
        }

        int total = findings.Values.Sum(v => v.Count);
        Console.WriteLine($"Scanned {files.Count} files, {scanned} declarations ({(raw ? "unfiltered" : "filtered")}).");
        Console.WriteLine($"Referenced nowhere else: {total}");
        Console.WriteLine();

        foreach ((string relative, List<(int Line, string Name)> entries) in
                 findings.OrderByDescending(f => f.Value.Count).ThenBy(f => f.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"{relative}  ({entries.Count})");
            foreach ((int line, string name) in entries.OrderBy(e => e.Line))
            {
                Console.WriteLine($"    {relative}:{line}  {name}");
            }
            Console.WriteLine();
        }

        // Always succeeds: this is a report to read, not a gate to fail a build on.
        return 0;
    }

    private static void Record(
        Dictionary<string, List<(int Line, string Name)>> findings,
        string relative,
        int line,
        string name)
    {
        if (!findings.TryGetValue(relative, out List<(int, string)>? entries))
        {
            entries = [];
            findings[relative] = entries;
        }
        entries.Add((line, name));
    }

    /// <summary>
    /// Walks up from the executable to the directory holding the .sln, so the tool works
    /// from any working directory and from inside bin/.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
                return directory.FullName;
            directory = directory.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static List<string> SourceFiles(string root)
    {
        var found = new List<string>();
        Walk(root);
        found.Sort(StringComparer.Ordinal);
        return found;

        void Walk(string directory)
        {
            foreach (string sub in Directory.EnumerateDirectories(directory))
            {
                if (!SkipDirectories.Contains(Path.GetFileName(sub)))
                    Walk(sub);
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (ScanExtensions.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    found.Add(file);
            }
        }
    }

    /// <summary>The first path segment names the project, so a test project is visible from it.</summary>
    private static bool IsTestFile(string relative)
    {
        int separator = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        string project = separator < 0 ? relative : relative[..separator];
        return project.Contains("Tests", StringComparison.Ordinal);
    }

    /// <summary>True when [RelayCommand] sits on the member immediately above this one.</summary>
    private static bool PrecededByRelayCommand(string text, int start)
    {
        int from = Math.Max(0, start - 300);
        string window = text[from..start];
        int tail = Math.Max(0, window.Length - 200);
        return window[tail..].Contains("[RelayCommand", StringComparison.Ordinal);
    }

    private static int LineOf(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;
}
