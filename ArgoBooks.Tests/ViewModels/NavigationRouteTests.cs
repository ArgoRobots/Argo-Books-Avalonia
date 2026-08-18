using System.Text.RegularExpressions;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Every page a command navigates to is a page that exists.
///
/// NavigateTo takes a string, so a name that was never registered compiles, runs, and silently
/// does nothing: the sidebar highlight moves, a modal opens over whatever page was already
/// showing, and the app looks like it half-worked. The dashboard's stock adjustment action
/// asked for "Adjustments" against a page registered as "StockAdjustments".
///
/// Read from source rather than exercised, because registering the pages builds real Avalonia
/// controls and cannot run headless. Comparing the two sets of string literals is what a
/// compiler would do if these were not strings.
/// </summary>
public class NavigationRouteTests
{
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;

        while (dir != null && !File.Exists(Path.Combine(dir, "ArgoBooks.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new DirectoryNotFoundException("ArgoBooks.sln not found above the test output directory");
    }

    private static HashSet<string> Registered()
    {
        string app = File.ReadAllText(Path.Combine(RepoRoot(), "ArgoBooks", "App.axaml.cs"));

        return [.. Regex.Matches(app, @"RegisterPage\(\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)];
    }

    /// <summary>Every NavigateTo("...") across the view models, with the file it sits in.</summary>
    private static List<(string File, string Route)> Requested()
    {
        string viewModels = Path.Combine(RepoRoot(), "ArgoBooks", "ViewModels");

        return
        [
            .. Directory.EnumerateFiles(viewModels, "*.cs", SearchOption.AllDirectories)
                .SelectMany(file => Regex
                    .Matches(File.ReadAllText(file), @"NavigateTo\(\s*""([^""]+)""")
                    .Select(m => (Path.GetFileName(file), m.Groups[1].Value)))
        ];
    }

    [Fact]
    public void ThePagesAreRegisteredAtAll()
    {
        // Guards the test itself: a regex that stopped matching would pass everything below.
        HashSet<string> registered = Registered();

        Assert.Contains("Dashboard", registered);
        Assert.Contains("StockAdjustments", registered);
        Assert.True(registered.Count > 20, $"only found {registered.Count} registered pages");
    }

    [Fact]
    public void EveryPageAViewModelNavigatesTo_IsRegistered()
    {
        HashSet<string> registered = Registered();
        List<(string File, string Route)> requested = Requested();

        Assert.NotEmpty(requested);

        List<string> unknown =
        [
            .. requested
                .Where(r => !registered.Contains(r.Route))
                .Select(r => $"{r.File} navigates to \"{r.Route}\"")
                .Distinct()
                .Order()
        ];

        Assert.True(unknown.Count == 0, string.Join("\n", unknown));
    }
}
