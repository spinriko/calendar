using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Roslyn-based metric walkers used by the metrics runner
class RoslynMetricsWalker : CSharpSyntaxWalker
{
    public int TotalCyclomatic { get; private set; } = 0;
    public int TotalOperators { get; private set; } = 0;
    public int TotalOperands { get; private set; } = 0;
    public HashSet<string> DistinctOperators { get; } = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> DistinctOperands { get; } = new HashSet<string>(StringComparer.Ordinal);

    public RoslynMetricsWalker() : base(SyntaxWalkerDepth.Node)
    {
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // base complexity 1 per method
        int complexity = 1;
        // Count decision points within the method
        var decisionFinder = new DecisionPointWalker();
        decisionFinder.Visit(node.Body ?? (SyntaxNode)node);
        complexity += decisionFinder.DecisionPoints;
        TotalCyclomatic += complexity;

        base.VisitMethodDeclaration(node);
    }

    public override void VisitToken(SyntaxToken token)
    {
        // Classify tokens as operands or operators
        if (token.IsKind(SyntaxKind.IdentifierToken) ||
            token.IsKind(SyntaxKind.StringLiteralToken) ||
            token.IsKind(SyntaxKind.NumericLiteralToken) ||
            token.IsKind(SyntaxKind.CharacterLiteralToken))
        {
            var sval = token.ValueText ?? token.ToString();
            TotalOperands++;
            DistinctOperands.Add(sval);
        }
        else
        {
            // treat many punctuation/operator tokens as operators
            if (token.Kind().ToString().EndsWith("Token") && token.Kind() != SyntaxKind.IdentifierToken)
            {
                var opname = token.Kind().ToString();
                TotalOperators++;
                DistinctOperators.Add(opname);
            }
        }

        base.VisitToken(token);
    }
}

class DecisionPointWalker : CSharpSyntaxWalker
{
    public int DecisionPoints { get; private set; } = 0;

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        DecisionPoints++;
        base.VisitIfStatement(node);
    }
    public override void VisitForStatement(ForStatementSyntax node)
    {
        DecisionPoints++;
        base.VisitForStatement(node);
    }
    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        DecisionPoints++;
        base.VisitForEachStatement(node);
    }
    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        DecisionPoints++;
        base.VisitWhileStatement(node);
    }
    public override void VisitDoStatement(DoStatementSyntax node)
    {
        DecisionPoints++;
        base.VisitDoStatement(node);
    }
    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        DecisionPoints++;
        base.VisitCatchClause(node);
    }
    public override void VisitSwitchSection(SwitchSectionSyntax node)
    {
        // count each case label as a decision
        DecisionPoints += node.Labels.Count;
        base.VisitSwitchSection(node);
    }
    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var kind = node.Kind();
        if (kind == SyntaxKind.LogicalAndExpression || kind == SyntaxKind.LogicalOrExpression)
        {
            DecisionPoints++;
        }
        base.VisitBinaryExpression(node);
    }
    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        DecisionPoints++;
        base.VisitConditionalExpression(node);
    }
}
