using System.Text.Json;

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
    totalFiles += files.Count;

    foreach (var f in files)
    {
        try
        {
            var lines = File.ReadAllLines(f).Length;
            totalLines += lines;
        }
        catch
        {
            // ignore read errors
        }
    }
}

var report = new
{
    generatedAt = DateTime.UtcNow,
    projectCount = projectMap.Count,
    totalFiles = totalFiles,
    totalLines = totalLines,
    avgLinesPerFile = totalFiles == 0 ? 0 : (double)totalLines / totalFiles,
    projects = projectMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
};

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
var outFile = Path.Combine(outDir, "metrics.json");
File.WriteAllText(outFile, json);

Console.WriteLine($"Wrote metrics to {outFile}");
Console.WriteLine($"Projects: {report.projectCount}, Files: {report.totalFiles}, Lines: {report.totalLines}");

return 0;
