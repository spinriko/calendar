using System.Text.Json;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// (Roslyn-based walkers are implemented in RoslynMetrics.cs)

var root = args.Length > 0 ? args[0] : Path.GetFullPath("..\\..");
var solutionPath = Path.GetFullPath(root);
var outDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "metrics");
Directory.CreateDirectory(outDir);

Console.WriteLine($"Solution root: {solutionPath}");

// Discover projects
var csprojFiles = Directory.GetFiles(solutionPath, "*.csproj", SearchOption.AllDirectories)
    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
    .ToList();

var projectMap = new Dictionary<string, List<string>>();
int totalFiles = 0;
long totalLines = 0;
var fileMetrics = new List<object>();

foreach (var csproj in csprojFiles)
{
    var projectDir = Path.GetDirectoryName(csproj)!;
    var projectName = Path.GetFileNameWithoutExtension(csproj);

    var files = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                    && !f.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar)
                    && !f.EndsWith(".Designer.cs"))
        .ToList();

    projectMap[projectName] = files;
    foreach (var f in files)
    {
        try
        {
            var text = File.ReadAllText(f);

            // Parse with Roslyn for more accurate metrics
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text);
            var rootNode = tree.GetRoot();

            // Lines of code (non-empty)
            var nonEmptyLines = text.Split(new[] { '\n' }, StringSplitOptions.None).Count(l => !string.IsNullOrWhiteSpace(l.Trim()));
            totalLines += nonEmptyLines;

            // Walk the syntax tree to compute cyclomatic and Halstead-like counts
            var walker = new RoslynMetricsWalker();
            walker.Visit(rootNode);

            int fileCyclomatic = walker.TotalCyclomatic;
            int N = walker.TotalOperators + walker.TotalOperands;
            int n = walker.DistinctOperators.Count + walker.DistinctOperands.Count;
            double halsteadVolume = 1.0;
            if (n > 0)
            {
                halsteadVolume = N * Math.Log(Math.Max(2, n), 2);
            }

            fileMetrics.Add(new
            {
                path = f,
                files = 1,
                lines = nonEmptyLines,
                halsteadVolume = Math.Round(halsteadVolume, 2),
                cyclomatic = fileCyclomatic
            });
            totalFiles += 1;
        }
        catch
        {
            // ignore read errors
        }
    }
}

// Aggregate complexity and maintainability
int aggregatedCyclomatic = fileMetrics.Cast<dynamic>().Sum(fm => (int)fm.cyclomatic);
double aggregatedHalstead = fileMetrics.Cast<dynamic>().Sum(fm => (double)fm.halsteadVolume);

double mi = 0;
if (totalFiles > 0 && totalLines > 0)
{
    // Use aggregated values to compute an approximate Maintainability Index
    double V = Math.Max(1.0, aggregatedHalstead);
    double G = Math.Max(1.0, aggregatedCyclomatic);
    double LOC = Math.Max(1.0, totalLines);
    double rawMi = 171 - 5.2 * Math.Log(V) - 0.23 * G - 16.2 * Math.Log(LOC);
    mi = Math.Max(0, Math.Min(100, rawMi * 100.0 / 171.0));
}

var report = new
{
    generatedAt = DateTime.UtcNow,
    projectCount = projectMap.Count,
    totalFiles = totalFiles,
    totalLines = totalLines,
    avgLinesPerFile = totalFiles == 0 ? 0 : (double)totalLines / totalFiles,
    projects = projectMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
    aggregatedCyclomatic = aggregatedCyclomatic,
    aggregatedHalstead = Math.Round(aggregatedHalstead, 2),
    maintainabilityIndex = Math.Round(mi, 2),
    files = fileMetrics
};

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
var outFile = Path.Combine(outDir, "metrics.json");
File.WriteAllText(outFile, json);

Console.WriteLine($"Wrote metrics to {outFile}");
Console.WriteLine($"Projects: {report.projectCount}, Files: {report.totalFiles}, Lines: {report.totalLines}");

return 0;


