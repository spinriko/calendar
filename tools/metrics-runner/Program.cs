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
            // remove block comments
            text = System.Text.RegularExpressions.Regex.Replace(text, "/\\*.*?\\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
            // remove line comments
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None)
                .Select(l =>
                {
                    var idx = l.IndexOf("//");
                    return idx >= 0 ? l.Substring(0, idx) : l;
                })
                .Select(l => l.TrimEnd('\r'))
                .ToList();

            var nonEmptyLines = lines.Count(l => !string.IsNullOrWhiteSpace(l));
            totalLines += nonEmptyLines;

            // Halstead token approximation
            var tokens = System.Text.RegularExpressions.Regex.Split(text, "\\W+")
                .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            int N = tokens.Count;
            int n = tokens.Distinct(StringComparer.Ordinal).Count();
            double halsteadVolume = 1.0;
            if (n > 0)
            {
                halsteadVolume = N * Math.Log(Math.Max(2, n), 2);
            }

            // Cyclomatic complexity estimation per file by scanning methods
            int fileCyclomatic = 0;
            var methodRegex = new System.Text.RegularExpressions.Regex(@"\b(?:public|private|protected|internal|static|async|sealed|virtual|override|extern)\s+[\w<>,\[\]\s]+\s+\w+\s*\([^)]*\)\s*\{", System.Text.RegularExpressions.RegexOptions.Compiled);
            var matches = methodRegex.Matches(text);
            var complexityKeywords = new[] { "if", "for", "foreach", "while", "case", "catch", "&&", "||", "?" };

            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                int start = m.Index + m.Length - 1; // position at '{'
                int braceDepth = 0;
                int i = start;
                for (; i < text.Length; i++)
                {
                    if (text[i] == '{') braceDepth++;
                    else if (text[i] == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0) { i++; break; }
                    }
                }
                var methodBody = text.Substring(start, Math.Min(text.Length - start, i - start));
                int methodComplexity = 1;
                foreach (var kw in complexityKeywords)
                {
                    methodComplexity += System.Text.RegularExpressions.Regex.Matches(methodBody, System.Text.RegularExpressions.Regex.Escape(kw)).Count;
                }
                fileCyclomatic += methodComplexity;
            }

            // Fallback: if no methods found, approximate complexity by counting keywords in file
            if (matches.Count == 0)
            {
                int approx = 1;
                foreach (var kw in complexityKeywords)
                {
                    approx += System.Text.RegularExpressions.Regex.Matches(text, System.Text.RegularExpressions.Regex.Escape(kw)).Count;
                }
                fileCyclomatic = approx;
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
