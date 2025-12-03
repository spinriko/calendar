using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Xunit;

namespace pto.track.tests;

/// <summary>
/// Utility class for analyzing cyclomatic complexity.
/// Usage: Run this test to check for overly complex methods.
/// </summary>
public class CodeMetricsAnalyzer
{
    /// <summary>
    /// Analyzes cyclomatic complexity for all source files in the main project.
    /// Reports methods with complexity > 10.
    /// </summary>
    [Fact]
    public async Task AnalyzeProjectComplexity()
    {
        var projectPath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "pto.track"));
        var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") &&
                       !f.Contains("\\Migrations\\") && !f.Contains(".Designer.cs"))
            .ToList();

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
                            Path.GetRelativePath(projectPath, file),
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
}