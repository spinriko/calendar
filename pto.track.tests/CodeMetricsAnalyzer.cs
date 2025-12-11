using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace pto.track.tests;

/// <summary>
/// Comprehensive code metrics analyzer including complexity, maintainability, and quality metrics.
/// Usage: Run these tests to check for code quality issues.
/// </summary>
public class CodeMetricsAnalyzer
{
    /// <summary>
    /// Analyzes cyclomatic complexity for all C# projects in the solution.
    /// Reports methods with complexity > 10.
    /// </summary>
    [Fact]
    public async Task AnalyzeProjectComplexity()
    {
        var sourceFiles = GetAllSourceFiles();

        var highComplexityMethods = new List<(string File, string Method, int Complexity)>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var complexity = CalculateCyclomaticComplexity(method);
                    if (complexity > 10)
                    {
                        var methodName = $"{method.Identifier.Text}";
                        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                        var fullName = className != null ? $"{className}.{methodName}" : methodName;

                        highComplexityMethods.Add((
                            GetRelativeProjectPath(file),
                            fullName,
                            complexity
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // Output results
        Console.WriteLine("\n=== Cyclomatic Complexity Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        if (highComplexityMethods.Any())
        {
            Console.WriteLine($"Found {highComplexityMethods.Count} method(s) with complexity > 10:");
            foreach (var (file, method, complexity) in highComplexityMethods.OrderByDescending(x => x.Complexity))
            {
                Console.WriteLine($"  [{complexity}] {method}");
                Console.WriteLine($"      in {file}");
            }
            Console.WriteLine("\nConsider refactoring methods with high complexity to improve maintainability.");
        }
        else
        {
            Console.WriteLine("✓ All methods have acceptable complexity (≤10)");
        }

        // Test always passes - this is informational
        Assert.True(true);
    }

    private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        // Start with complexity of 1
        int complexity = 1;

        var nodes = method.DescendantNodes();

        // Count decision points
        complexity += nodes.OfType<IfStatementSyntax>().Count();
        complexity += nodes.OfType<WhileStatementSyntax>().Count();
        complexity += nodes.OfType<ForStatementSyntax>().Count();
        complexity += nodes.OfType<ForEachStatementSyntax>().Count();
        complexity += nodes.OfType<CaseSwitchLabelSyntax>().Count();
        complexity += nodes.OfType<CatchClauseSyntax>().Count();
        complexity += nodes.OfType<ConditionalExpressionSyntax>().Count();
        complexity += nodes.OfType<DoStatementSyntax>().Count();

        // Count logical operators (&&, ||)
        complexity += nodes.OfType<BinaryExpressionSyntax>()
            .Count(b => b.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalAndExpression) ||
                       b.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalOrExpression));

        return complexity;
    }

    /// <summary>
    /// Analyzes maintainability index for all methods.
    /// Formula: 171 - 5.2 * ln(Volume) - 0.23 * Complexity - 16.2 * ln(Lines)
    /// Scale: 0-100 (higher = more maintainable)
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeMaintainabilityIndex()
    {
        var sourceFiles = GetAllSourceFiles();

        var lowMaintainabilityMethods = new List<(string File, string Method, double Index)>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var complexity = CalculateCyclomaticComplexity(method);
                    var lines = method.GetText().Lines.Count;
                    var volume = CalculateHalsteadVolume(method);

                    // Maintainability Index formula
                    var maintainabilityIndex = 171 - 5.2 * Math.Log(volume) - 0.23 * complexity - 16.2 * Math.Log(lines);
                    maintainabilityIndex = Math.Max(0, Math.Min(100, maintainabilityIndex * 100 / 171)); // Normalize to 0-100

                    if (maintainabilityIndex < 65) // Low maintainability threshold
                    {
                        var methodName = $"{method.Identifier.Text}";
                        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                        var fullName = className != null ? $"{className}.{methodName}" : methodName;

                        lowMaintainabilityMethods.Add((
                            GetRelativeProjectPath(file),
                            fullName,
                            maintainabilityIndex
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\n=== Maintainability Index Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");
        Console.WriteLine("Scale: 0-100 (65+ = Good, 85+ = Excellent)");

        if (lowMaintainabilityMethods.Any())
        {
            Console.WriteLine($"\nFound {lowMaintainabilityMethods.Count} method(s) with low maintainability (<65):");
            foreach (var (file, method, index) in lowMaintainabilityMethods.OrderBy(x => x.Index))
            {
                Console.WriteLine($"  [{index:F1}] {method}");
                Console.WriteLine($"      in {file}");
            }
            Console.WriteLine("\nConsider refactoring these methods to improve maintainability.");
        }
        else
        {
            Console.WriteLine("\n✓ All methods have good maintainability (≥65)");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Analyzes lines of code per method.
    /// Threshold: 50 lines
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeLinesOfCode()
    {
        var sourceFiles = GetAllSourceFiles();

        var longMethods = new List<(string File, string Method, int Lines)>();
        var longClasses = new List<(string File, string Class, int Lines)>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                // Check methods
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var lines = method.GetText().Lines.Count;
                    if (lines > 50)
                    {
                        var methodName = $"{method.Identifier.Text}";
                        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                        var fullName = className != null ? $"{className}.{methodName}" : methodName;

                        longMethods.Add((
                            GetRelativeProjectPath(file),
                            fullName,
                            lines
                        ));
                    }
                }

                // Check classes
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var cls in classes)
                {
                    var lines = cls.GetText().Lines.Count;
                    if (lines > 500)
                    {
                        longClasses.Add((
                            GetRelativeProjectPath(file),
                            cls.Identifier.Text,
                            lines
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\n=== Lines of Code Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        if (longMethods.Any())
        {
            Console.WriteLine($"Found {longMethods.Count} method(s) > 50 lines:");
            foreach (var (file, method, lines) in longMethods.OrderByDescending(x => x.Lines))
            {
                Console.WriteLine($"  [{lines} lines] {method}");
                Console.WriteLine($"      in {file}");
            }
        }
        else
        {
            Console.WriteLine("✓ All methods ≤50 lines");
        }

        if (longClasses.Any())
        {
            Console.WriteLine($"\nFound {longClasses.Count} class(es) > 500 lines:");
            foreach (var (file, className, lines) in longClasses.OrderByDescending(x => x.Lines))
            {
                Console.WriteLine($"  [{lines} lines] {className}");
                Console.WriteLine($"      in {file}");
            }
        }
        else
        {
            Console.WriteLine("\n✓ All classes ≤500 lines");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Analyzes method parameter count.
    /// Threshold: 5 parameters
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeMethodParameters()
    {
        var sourceFiles = GetAllSourceFiles();

        var highParamMethods = new List<(string File, string Method, int ParamCount)>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var paramCount = method.ParameterList.Parameters.Count;
                    if (paramCount > 5)
                    {
                        var methodName = $"{method.Identifier.Text}";
                        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                        var fullName = className != null ? $"{className}.{methodName}" : methodName;

                        highParamMethods.Add((
                            GetRelativeProjectPath(file),
                            fullName,
                            paramCount
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\n=== Method Parameter Count Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        if (highParamMethods.Any())
        {
            Console.WriteLine($"Found {highParamMethods.Count} method(s) with >5 parameters:");
            foreach (var (file, method, count) in highParamMethods.OrderByDescending(x => x.ParamCount))
            {
                Console.WriteLine($"  [{count} params] {method}");
                Console.WriteLine($"      in {file}");
            }
            Console.WriteLine("\nConsider using parameter objects or builder pattern.");
        }
        else
        {
            Console.WriteLine("✓ All methods have ≤5 parameters");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Analyzes nesting depth.
    /// Threshold: 4 levels
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeNestingDepth()
    {
        var sourceFiles = GetAllSourceFiles();

        var deeplyNestedMethods = new List<(string File, string Method, int Depth)>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var maxDepth = CalculateMaxNestingDepth(method);
                    if (maxDepth > 4)
                    {
                        var methodName = $"{method.Identifier.Text}";
                        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                        var fullName = className != null ? $"{className}.{methodName}" : methodName;

                        deeplyNestedMethods.Add((
                            GetRelativeProjectPath(file),
                            fullName,
                            maxDepth
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\n=== Nesting Depth Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        if (deeplyNestedMethods.Any())
        {
            Console.WriteLine($"Found {deeplyNestedMethods.Count} method(s) with nesting depth >4:");
            foreach (var (file, method, depth) in deeplyNestedMethods.OrderByDescending(x => x.Depth))
            {
                Console.WriteLine($"  [depth {depth}] {method}");
                Console.WriteLine($"      in {file}");
            }
            Console.WriteLine("\nConsider extracting nested logic to separate methods.");
        }
        else
        {
            Console.WriteLine("✓ All methods have nesting depth ≤4");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Analyzes class coupling and dependencies.
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeClassCoupling()
    {
        var sourceFiles = GetAllSourceFiles();

        var classDependencies = new Dictionary<string, HashSet<string>>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var cls in classes)
                {
                    var className = cls.Identifier.Text;
                    var dependencies = new HashSet<string>();

                    // Count field/property types (dependencies)
                    var fields = cls.DescendantNodes().OfType<FieldDeclarationSyntax>();
                    foreach (var field in fields)
                    {
                        var typeName = field.Declaration.Type.ToString();
                        if (!IsBuiltInType(typeName))
                        {
                            dependencies.Add(typeName);
                        }
                    }

                    var properties = cls.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                    foreach (var prop in properties)
                    {
                        var typeName = prop.Type.ToString();
                        if (!IsBuiltInType(typeName))
                        {
                            dependencies.Add(typeName);
                        }
                    }

                    // Constructor parameters
                    var constructors = cls.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
                    foreach (var ctor in constructors)
                    {
                        foreach (var param in ctor.ParameterList.Parameters)
                        {
                            var typeName = param.Type?.ToString() ?? "";
                            if (!IsBuiltInType(typeName))
                            {
                                dependencies.Add(typeName);
                            }
                        }
                    }

                    if (dependencies.Count > 0)
                    {
                        classDependencies[className] = dependencies;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not analyze {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\n=== Class Coupling Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        var highCoupling = classDependencies.Where(kvp => kvp.Value.Count > 10).ToList();

        if (highCoupling.Any())
        {
            Console.WriteLine($"Found {highCoupling.Count} class(es) with >10 dependencies:");
            foreach (var (className, deps) in highCoupling.OrderByDescending(x => x.Value.Count))
            {
                Console.WriteLine($"  [{deps.Count} dependencies] {className}");
                Console.WriteLine($"      Dependencies: {string.Join(", ", deps.Take(5))}{(deps.Count > 5 ? "..." : "")}");
            }
            Console.WriteLine("\nHigh coupling may indicate classes that do too much.");
        }
        else
        {
            Console.WriteLine("✓ All classes have ≤10 dependencies");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Analyzes depth of inheritance tree.
    /// Threshold: 4 levels
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task AnalyzeInheritanceDepth()
    {
        var sourceFiles = GetAllSourceFiles();

        var classInheritance = new Dictionary<string, string?>();

        // First pass: build inheritance map
        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var cls in classes)
                {
                    var className = cls.Identifier.Text;
                    var baseType = cls.BaseList?.Types.FirstOrDefault()?.ToString();
                    classInheritance[className] = baseType;
                }
            }
            catch { }
        }

        // Second pass: calculate depth
        var deepInheritance = new List<(string Class, int Depth)>();

        foreach (var className in classInheritance.Keys)
        {
            var depth = CalculateInheritanceDepth(className, classInheritance);
            if (depth > 4)
            {
                deepInheritance.Add((className, depth));
            }
        }

        Console.WriteLine("\n=== Inheritance Depth Report ===");
        Console.WriteLine($"Analyzed {sourceFiles.Count} source files\n");

        if (deepInheritance.Any())
        {
            Console.WriteLine($"Found {deepInheritance.Count} class(es) with inheritance depth >4:");
            foreach (var (className, depth) in deepInheritance.OrderByDescending(x => x.Depth))
            {
                Console.WriteLine($"  [depth {depth}] {className}");
            }
            Console.WriteLine("\nDeep inheritance hierarchies can be difficult to maintain.");
        }
        else
        {
            Console.WriteLine("✓ All classes have inheritance depth ≤4");
        }

        Assert.True(true);
    }

    /// <summary>
    /// Comprehensive summary report combining all metrics.
    /// </summary>
    [Fact(Skip = "bisect: temporary skip")]
    public async Task GenerateComprehensiveSummary()
    {
        var sourceFiles = GetAllSourceFiles();

        int totalMethods = 0;
        int totalClasses = 0;
        int totalLines = 0;
        var complexityScores = new List<int>();
        var maintainabilityScores = new List<double>();
        var parameterCounts = new List<int>();

        foreach (var file in sourceFiles)
        {
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                totalLines += code.Split('\n').Length;
                totalClasses += root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    totalMethods++;
                    var complexity = CalculateCyclomaticComplexity(method);
                    complexityScores.Add(complexity);

                    var lines = method.GetText().Lines.Count;
                    var volume = CalculateHalsteadVolume(method);
                    var mi = 171 - 5.2 * Math.Log(volume) - 0.23 * complexity - 16.2 * Math.Log(lines);
                    mi = Math.Max(0, Math.Min(100, mi * 100 / 171));
                    maintainabilityScores.Add(mi);

                    parameterCounts.Add(method.ParameterList.Parameters.Count);
                }
            }
            catch { }
        }

        Console.WriteLine("\n=== Comprehensive Code Quality Summary ===");
        Console.WriteLine($"Solution: pto.track (all C# projects)\n");

        Console.WriteLine("Code Base Statistics:");
        Console.WriteLine($"  Files:   {sourceFiles.Count}");
        Console.WriteLine($"  Lines:   {totalLines:N0}");
        Console.WriteLine($"  Classes: {totalClasses}");
        Console.WriteLine($"  Methods: {totalMethods}");

        if (complexityScores.Any())
        {
            Console.WriteLine("\nCyclomatic Complexity:");
            Console.WriteLine($"  Average: {complexityScores.Average():F1}");
            Console.WriteLine($"  Median:  {GetMedian(complexityScores.Select(x => (double)x).ToList()):F1}");
            Console.WriteLine($"  Max:     {complexityScores.Max()}");
            Console.WriteLine($"  >10:     {complexityScores.Count(x => x > 10)} methods");
        }

        if (maintainabilityScores.Any())
        {
            Console.WriteLine("\nMaintainability Index (0-100):");
            Console.WriteLine($"  Average: {maintainabilityScores.Average():F1}");
            Console.WriteLine($"  Median:  {GetMedian(maintainabilityScores):F1}");
            Console.WriteLine($"  Min:     {maintainabilityScores.Min():F1}");
            Console.WriteLine($"  <65:     {maintainabilityScores.Count(x => x < 65)} methods");
        }

        if (parameterCounts.Any())
        {
            Console.WriteLine("\nMethod Parameters:");
            Console.WriteLine($"  Average: {parameterCounts.Average():F1}");
            Console.WriteLine($"  Max:     {parameterCounts.Max()}");
            Console.WriteLine($"  >5:      {parameterCounts.Count(x => x > 5)} methods");
        }

        Console.WriteLine("\n✓ Analysis complete");
        // Persist machine-readable metrics for CI and further processing
        try
        {
            var solutionPath = Path.GetFullPath(Path.Combine("..", "..", "..", ".."));
            var artifactsDir = Path.Combine(solutionPath, "artifacts", "metrics");
            Directory.CreateDirectory(artifactsDir);

            // Compute per-project metrics
            var projectSourceMap = GetProjectSourceMap(); // key: projectName, value: list of files
            var projectMetrics = new Dictionary<string, object>();
            var projectSummaries = new Dictionary<string, object>();

            foreach (var kvp in projectSourceMap)
            {
                var projectName = kvp.Key;
                var files = kvp.Value;

                int pTotalLines = 0;
                int pTotalClasses = 0;
                int pTotalMethods = 0;
                var pComplexityScores = new List<int>();
                var pMaintainabilityScores = new List<double>();
                var pParameterCounts = new List<int>();

                foreach (var file in files)
                {
                    try
                    {
                        var code = await File.ReadAllTextAsync(file);
                        var tree = CSharpSyntaxTree.ParseText(code);
                        var root = await tree.GetRootAsync();

                        pTotalLines += code.Split('\n').Length;
                        pTotalClasses += root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();

                        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        foreach (var method in methods)
                        {
                            pTotalMethods++;
                            var complexity = CalculateCyclomaticComplexity(method);
                            pComplexityScores.Add(complexity);

                            var lines = method.GetText().Lines.Count;
                            var volume = CalculateHalsteadVolume(method);
                            var mi = 171 - 5.2 * Math.Log(volume) - 0.23 * complexity - 16.2 * Math.Log(lines);
                            mi = Math.Max(0, Math.Min(100, mi * 100 / 171));
                            pMaintainabilityScores.Add(mi);

                            pParameterCounts.Add(method.ParameterList.Parameters.Count);
                        }
                    }
                    catch { }
                }

                var pMetricsObj = new
                {
                    Project = projectName,
                    Files = files.Count,
                    Lines = pTotalLines,
                    Classes = pTotalClasses,
                    Methods = pTotalMethods,
                    CyclomaticComplexity = new
                    {
                        Average = pComplexityScores.Any() ? pComplexityScores.Average() : 0.0,
                        Median = pComplexityScores.Any() ? GetMedian(pComplexityScores.Select(x => (double)x).ToList()) : 0.0,
                        Max = pComplexityScores.Any() ? pComplexityScores.Max() : 0,
                        OverThreshold = pComplexityScores.Count(x => x > 10)
                    },
                    Maintainability = new
                    {
                        Average = pMaintainabilityScores.Any() ? pMaintainabilityScores.Average() : 0.0,
                        Median = pMaintainabilityScores.Any() ? GetMedian(pMaintainabilityScores) : 0.0,
                        Min = pMaintainabilityScores.Any() ? pMaintainabilityScores.Min() : 0.0,
                        BelowThreshold = pMaintainabilityScores.Count(x => x < 65)
                    },
                    Parameters = new
                    {
                        Average = pParameterCounts.Any() ? pParameterCounts.Average() : 0.0,
                        Max = pParameterCounts.Any() ? pParameterCounts.Max() : 0,
                        OverThreshold = pParameterCounts.Count(x => x > 5)
                    }
                };

                projectMetrics[projectName] = pMetricsObj;

                projectSummaries[projectName] = new
                {
                    Files = pMetricsObj.Files,
                    Lines = pMetricsObj.Lines,
                    Methods = pMetricsObj.Methods,
                    AvgCyclomatic = ((dynamic)pMetricsObj).CyclomaticComplexity.Average,
                    MaxCyclomatic = ((dynamic)pMetricsObj).CyclomaticComplexity.Max,
                    AvgMaintainability = ((dynamic)pMetricsObj).Maintainability.Average,
                    MethodsBelowMaintainabilityThreshold = ((dynamic)pMetricsObj).Maintainability.BelowThreshold
                };
            }

            var metricsObj = new
            {
                Solution = "pto.track",
                Files = sourceFiles.Count,
                Lines = totalLines,
                Classes = totalClasses,
                Methods = totalMethods,
                CyclomaticComplexity = new
                {
                    Average = complexityScores.Any() ? complexityScores.Average() : 0.0,
                    Median = complexityScores.Any() ? GetMedian(complexityScores.Select(x => (double)x).ToList()) : 0.0,
                    Max = complexityScores.Any() ? complexityScores.Max() : 0,
                    OverThreshold = complexityScores.Count(x => x > 10)
                },
                Maintainability = new
                {
                    Average = maintainabilityScores.Any() ? maintainabilityScores.Average() : 0.0,
                    Median = maintainabilityScores.Any() ? GetMedian(maintainabilityScores) : 0.0,
                    Min = maintainabilityScores.Any() ? maintainabilityScores.Min() : 0.0,
                    BelowThreshold = maintainabilityScores.Count(x => x < 65)
                },
                Parameters = new
                {
                    Average = parameterCounts.Any() ? parameterCounts.Average() : 0.0,
                    Max = parameterCounts.Any() ? parameterCounts.Max() : 0,
                    OverThreshold = parameterCounts.Count(x => x > 5)
                },
                Projects = projectMetrics
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var metricsJson = JsonSerializer.Serialize(metricsObj, options);
            var metricsPath = Path.Combine(artifactsDir, "code-metrics.json");
            await File.WriteAllTextAsync(metricsPath, metricsJson);

            // Also write a short summary useful for CI gating (includes per-project summaries)
            var summaryObj = new
            {
                Files = metricsObj.Files,
                Lines = metricsObj.Lines,
                Methods = metricsObj.Methods,
                AvgCyclomatic = metricsObj.CyclomaticComplexity.Average,
                MaxCyclomatic = metricsObj.CyclomaticComplexity.Max,
                AvgMaintainability = metricsObj.Maintainability.Average,
                MethodsBelowMaintainabilityThreshold = metricsObj.Maintainability.BelowThreshold,
                Projects = projectSummaries
            };
            var summaryJson = JsonSerializer.Serialize(summaryObj, options);
            var summaryPath = Path.Combine(artifactsDir, "code-metrics-summary.json");
            await File.WriteAllTextAsync(summaryPath, summaryJson);

            Console.WriteLine($"\nWrote metrics JSON to: {metricsPath}");
            Console.WriteLine($"Wrote metrics summary to: {summaryPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to write metrics artifacts: {ex.Message}");
        }

        Assert.True(true);
    }

    // Helper methods

    private double CalculateHalsteadVolume(MethodDeclarationSyntax method)
    {
        var operators = method.DescendantTokens()
            .Count(t => t.IsKind(SyntaxKind.PlusToken) || t.IsKind(SyntaxKind.MinusToken) ||
                       t.IsKind(SyntaxKind.AsteriskToken) || t.IsKind(SyntaxKind.SlashToken) ||
                       t.IsKind(SyntaxKind.EqualsEqualsToken) || t.IsKind(SyntaxKind.ExclamationEqualsToken) ||
                       t.IsKind(SyntaxKind.LessThanToken) || t.IsKind(SyntaxKind.GreaterThanToken) ||
                       t.IsKind(SyntaxKind.AmpersandAmpersandToken) || t.IsKind(SyntaxKind.BarBarToken));

        var operands = method.DescendantNodes().OfType<IdentifierNameSyntax>().Count();

        var vocabulary = operators + operands;
        var length = vocabulary;

        return vocabulary > 0 ? length * Math.Log(vocabulary, 2) : 1;
    }

    private int CalculateMaxNestingDepth(MethodDeclarationSyntax method)
    {
        return CalculateDepth(method, 0);
    }

    private int CalculateDepth(SyntaxNode node, int currentDepth)
    {
        var maxDepth = currentDepth;

        foreach (var child in node.ChildNodes())
        {
            var childDepth = currentDepth;

            if (child is IfStatementSyntax || child is ForStatementSyntax ||
                child is ForEachStatementSyntax || child is WhileStatementSyntax ||
                child is DoStatementSyntax || child is SwitchStatementSyntax ||
                child is TryStatementSyntax)
            {
                childDepth++;
            }

            maxDepth = Math.Max(maxDepth, CalculateDepth(child, childDepth));
        }

        return maxDepth;
    }

    private int CalculateInheritanceDepth(string className, Dictionary<string, string?> inheritanceMap, HashSet<string>? visited = null)
    {
        visited ??= new HashSet<string>();

        if (!inheritanceMap.ContainsKey(className) || visited.Contains(className))
            return 0;

        visited.Add(className);

        var baseType = inheritanceMap[className];
        if (string.IsNullOrEmpty(baseType))
            return 0;

        // Extract just the class name (remove generics, etc.)
        var cleanBaseType = baseType.Split('<', '(')[0].Trim();

        return 1 + CalculateInheritanceDepth(cleanBaseType, inheritanceMap, visited);
    }

    private bool IsBuiltInType(string typeName)
    {
        var builtInTypes = new HashSet<string>
        {
            "string", "int", "long", "bool", "double", "float", "decimal", "byte", "char",
            "DateTime", "Guid", "object", "void", "var", "dynamic", "Task", "ValueTask",
            "List", "Dictionary", "HashSet", "IEnumerable", "ICollection", "IList",
            "string?", "int?", "long?", "bool?", "double?", "float?", "decimal?", "byte?",
            "DateTime?", "Guid?"
        };

        var cleanName = typeName.Split('<', '?', '[')[0].Trim();
        return builtInTypes.Contains(cleanName);
    }

    private double GetMedian(List<double> values)
    {
        if (!values.Any()) return 0;

        var sorted = values.OrderBy(x => x).ToList();
        int count = sorted.Count;

        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        else
            return sorted[count / 2];
    }

    private List<string> GetAllSourceFiles()
    {
        // Allow skipping heavy code-metrics analysis during CI or long-running test suites.
        // Set environment variable `SKIP_CODE_METRICS=1` to bypass file discovery.
        var skip = Environment.GetEnvironmentVariable("SKIP_CODE_METRICS");
        if (!string.IsNullOrEmpty(skip) && skip == "1")
        {
            return new List<string>();
        }

        var solutionPath = Path.GetFullPath(Path.Combine("..", "..", "..", ".."));

        // Discover all .csproj files under the solution and analyze their source files.
        var csprojFiles = Directory.GetFiles(solutionPath, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        var projectDirs = csprojFiles.Select(p => Path.GetDirectoryName(p)).Where(d => !string.IsNullOrEmpty(d)).Distinct();

        var skipDirs = new[] { ".git", "node_modules", "packages", "artifacts", "test-logs", "bin", "obj" };

        var sourceFiles = new List<string>();

        foreach (var projectDir in projectDirs)
        {
            var files = Directory.GetFiles(projectDir!, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                            && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                            && !f.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar)
                            && !f.EndsWith(".Designer.cs")
                            && !skipDirs.Any(sd => f.IndexOf(Path.DirectorySeparatorChar + sd + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                            && !skipDirs.Any(sd => f.IndexOf(Path.DirectorySeparatorChar + sd + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0));

            // Also remove any files that are located under excluded directories by checking path segments
            files = files.Where(f => !skipDirs.Any(sd => f.Split(Path.DirectorySeparatorChar).Any(seg => string.Equals(seg, sd, StringComparison.OrdinalIgnoreCase))));

            sourceFiles.AddRange(files);
        }

        return sourceFiles.Distinct().ToList();
    }

    private Dictionary<string, List<string>> GetProjectSourceMap()
    {
        var solutionPath = Path.GetFullPath(Path.Combine("..", "..", "..", ".."));
        var csprojFiles = Directory.GetFiles(solutionPath, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        var map = new Dictionary<string, List<string>>();

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

            map[projectName] = files.Distinct().ToList();
        }

        return map;
    }

    private string GetRelativeProjectPath(string filePath)
    {
        var solutionPath = Path.GetFullPath(Path.Combine("..", "..", "..", ".."));
        return Path.GetRelativePath(solutionPath, filePath);
    }
}