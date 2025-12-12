namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MetricsReporter.Model;

/// <summary>
/// Performs a lightweight Roslyn-based scan of the solution source tree to locate
/// <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/> usages.
/// </summary>
/// <remarks>
/// The analyzer works purely on syntax trees and simple path heuristics:
/// <list type="number">
/// <item>
/// <description>
/// Enumerates <c>.cs</c> files only under the specified source code folders
/// (relative to the solution directory).
/// </description>
/// </item>
/// <item>
/// <description>
/// Derives a logical assembly name by finding the longest matching source code folder
/// prefix in the file path and taking the next segment as the assembly name.
/// For example, with <c>SourceCodeFolders = ["src", "src/Tools"]</c> and file
/// <c>src/Tools/MetricsReporter/File.cs</c>, the analyzer matches <c>src/Tools</c>
/// (longest prefix) and derives assembly name <c>MetricsReporter</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Applies <see cref="AssemblyFilter"/> rules to skip excluded assemblies before processing files.
/// </description>
/// </item>
/// <item>
/// <description>
/// Walks syntax trees to find suppression attributes, maps rule identifiers
/// (for example, <c>CA1506</c>) to <see cref="MetricIdentifier"/> values via
/// <see cref="SuppressedRuleMetricMapper"/>, and emits normalized fully qualified
/// names so that results can be correlated with metrics nodes.
/// </description>
/// </item>
/// </list>
/// This approach avoids the complexity of loading full Roslyn compilations while
/// still providing stable identifiers for HTML and downstream tooling.
/// </remarks>
internal static class SuppressedSymbolsAnalyzer
{
  /// <summary>
  /// Executes suppressed symbol analysis for the specified solution directory.
  /// </summary>
  /// <param name="solutionDirectory">Root directory of the solution source tree.</param>
  /// <param name="sourceCodeFolders">
  /// Collection of source code folder paths (relative to <paramref name="solutionDirectory"/>)
  /// that contain assembly projects. Only files under these folders are scanned.
  /// </param>
  /// <param name="excludedAssemblyNames">
  /// Comma- or semicolon-separated list of assembly patterns to exclude.
  /// </param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>A report containing all discovered suppressed symbols.</returns>
  public static SuppressedSymbolsReport Analyze(
      string solutionDirectory,
      IReadOnlyCollection<string> sourceCodeFolders,
      string? excludedAssemblyNames,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(solutionDirectory))
    {
      throw new ArgumentException("Solution directory must be provided for suppressed symbol analysis.", nameof(solutionDirectory));
    }

    var suppressedSymbols = new List<SuppressedSymbolInfo>();
    var context = CreateAnalysisContext(solutionDirectory, sourceCodeFolders, excludedAssemblyNames, suppressedSymbols, cancellationToken);

    SuppressedSymbolFileProcessor.ProcessFiles(context, AnalyzeSingleFile);

    return CreateReport(suppressedSymbols);
  }

  private static SuppressedSymbolAnalysisContext CreateAnalysisContext(
      string solutionDirectory,
      IReadOnlyCollection<string> sourceCodeFolders,
      string? excludedAssemblyNames,
      ICollection<SuppressedSymbolInfo> suppressedSymbols,
      CancellationToken cancellationToken)
  {
    var normalizedRoot = Path.GetFullPath(solutionDirectory);
    var assemblyFilter = AssemblyFilter.FromString(excludedAssemblyNames);
    var normalizedFolders = SourceCodeFolderProcessor.NormalizeAndSortFolders(sourceCodeFolders);

    return new SuppressedSymbolAnalysisContext(
        normalizedRoot,
        normalizedFolders,
        assemblyFilter,
        suppressedSymbols,
        cancellationToken);
  }


  private static SuppressedSymbolsReport CreateReport(ICollection<SuppressedSymbolInfo> suppressedSymbols)
  {
    return new SuppressedSymbolsReport
    {
      GeneratedAtUtc = DateTime.UtcNow,
      SuppressedSymbols = suppressedSymbols is List<SuppressedSymbolInfo> list ? list : [.. suppressedSymbols]
    };
  }

  private static void AnalyzeSingleFile(
      string filePath,
      string relativePath,
      ICollection<SuppressedSymbolInfo> output,
      CancellationToken cancellationToken)
  {
    var text = File.ReadAllText(filePath);
    var syntaxTree = CSharpSyntaxTree.ParseText(text, cancellationToken: cancellationToken);
    var root = syntaxTree.GetRoot(cancellationToken);

    var walker = new SuppressMessageWalker(relativePath, output);
    walker.Visit(root);
  }

  private sealed class SuppressMessageWalker : CSharpSyntaxWalker
  {
    private readonly string _relativePath;
    private readonly ICollection<SuppressedSymbolInfo> _output;
    private readonly FullyQualifiedNameBuilder _fqnBuilder = new();

    public SuppressMessageWalker(string relativePath, ICollection<SuppressedSymbolInfo> output)
      : base(SyntaxWalkerDepth.StructuredTrivia)
    {
      _relativePath = relativePath;
      _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushNamespace(node.Name.ToString());
      base.VisitNamespaceDeclaration(node);
      _fqnBuilder.PopNamespace();
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushNamespace(node.Name.ToString());
      base.VisitFileScopedNamespaceDeclaration(node);
      _fqnBuilder.PopNamespace();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushType(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, _fqnBuilder.BuildTypeFqn());
      base.VisitClassDeclaration(node);
      _fqnBuilder.PopType();
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushType(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, _fqnBuilder.BuildTypeFqn());
      base.VisitStructDeclaration(node);
      _fqnBuilder.PopType();
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushType(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, _fqnBuilder.BuildTypeFqn());
      base.VisitRecordDeclaration(node);
      _fqnBuilder.PopType();
    }

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
      if (node is null)
      {
        return;
      }

      TryRecordSuppression(node.AttributeLists, null, allowTargetResolution: true);
      base.VisitCompilationUnit(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      _fqnBuilder.PushType(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, _fqnBuilder.BuildTypeFqn());
      base.VisitInterfaceDeclaration(node);
      _fqnBuilder.PopType();
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      var memberFqn = _fqnBuilder.BuildMemberFqn(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, memberFqn);
      base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      // Constructors are filtered out of the metrics report, but we still record
      // suppressions in case future consumers need them.
      var memberFqn = _fqnBuilder.BuildMemberFqn(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, memberFqn);
      base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      if (node is null)
      {
        return;
      }

      var propertyFqn = _fqnBuilder.BuildPropertyFqn(node.Identifier.Text);
      TryRecordSuppression(node.AttributeLists, propertyFqn);
      base.VisitPropertyDeclaration(node);
    }

    private void TryRecordSuppression(
        SyntaxList<AttributeListSyntax> attributeLists,
        string? fullyQualifiedName,
        bool allowTargetResolution = false)
    {
      if (string.IsNullOrWhiteSpace(fullyQualifiedName))
      {
        fullyQualifiedName = null;
      }

      foreach (var attributeList in attributeLists)
      {
        foreach (var attribute in attributeList.Attributes)
        {
          if (!SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification))
          {
            continue;
          }

          var symbolName = fullyQualifiedName;
          if (allowTargetResolution &&
              SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var targetFqn) &&
              !string.IsNullOrWhiteSpace(targetFqn))
          {
            symbolName = targetFqn;
          }

          if (string.IsNullOrWhiteSpace(symbolName))
          {
            continue;
          }

          SuppressedRuleMetricMapper.TryGetMetricName(ruleId, out var metricName);
          metricName ??= ruleId;
          if (string.IsNullOrWhiteSpace(metricName))
          {
            continue;
          }

          _output.Add(new SuppressedSymbolInfo
          {
            FilePath = _relativePath,
            FullyQualifiedName = symbolName,
            RuleId = ruleId ?? string.Empty,
            Metric = metricName,
            Justification = justification
          });
        }
      }
    }
  }
}



