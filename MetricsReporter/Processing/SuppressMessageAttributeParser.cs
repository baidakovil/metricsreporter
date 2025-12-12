namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MetricsReporter.Model;

/// <summary>
/// Parses <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/> attributes
/// from Roslyn syntax nodes to extract rule identifiers and justifications.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for identifying and parsing SuppressMessage attributes,
/// reducing coupling in syntax walker classes that need to process suppression attributes.
/// </remarks>
internal static class SuppressMessageAttributeParser
{
  /// <summary>
  /// Attempts to parse a SuppressMessage attribute from a syntax node.
  /// </summary>
  /// <param name="attribute">The attribute syntax node to parse.</param>
  /// <param name="ruleId">The extracted rule identifier (for example, <c>CA1506</c>).</param>
  /// <param name="justification">The extracted justification text, if present.</param>
  /// <returns>
  /// <see langword="true"/> if the attribute is a valid SuppressMessage attribute;
  /// otherwise, <see langword="false"/>.
  /// </returns>
  public static bool TryParse(AttributeSyntax attribute, out string? ruleId, out string? justification)
  {
    ruleId = null;
    justification = null;

    if (!IsSuppressMessageAttribute(attribute) ||
        attribute.ArgumentList is null ||
        attribute.ArgumentList.Arguments.Count < 2)
    {
      return false;
    }

    var args = attribute.ArgumentList.Arguments;

    if (!TryExtractCategory(args[0], out _))
    {
      return false;
    }

    if (!TryExtractRuleId(args[1], out ruleId))
    {
      return false;
    }

    justification = ExtractJustification(args);

    return !string.IsNullOrWhiteSpace(ruleId);
  }

  /// <summary>
  /// Attempts to extract and normalize the target fully qualified name from a SuppressMessage attribute.
  /// </summary>
  /// <param name="attribute">The attribute syntax node to parse.</param>
  /// <param name="fullyQualifiedName">The normalized fully qualified name specified in the Target argument.</param>
  /// <returns>
  /// <see langword="true"/> when the attribute is a SuppressMessage attribute and a valid Target value
  /// could be normalized; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool TryParseTargetFullyQualifiedName(AttributeSyntax attribute, out string? fullyQualifiedName)
  {
    fullyQualifiedName = null;
    if (!IsSuppressMessageAttribute(attribute) || attribute.ArgumentList is null)
    {
      return false;
    }

    foreach (var argument in attribute.ArgumentList.Arguments)
    {
      if (argument.NameEquals is null ||
          !string.Equals(argument.NameEquals.Name.Identifier.Text, "Target", StringComparison.Ordinal))
      {
        continue;
      }

      if (argument.Expression is not LiteralExpressionSyntax literal ||
          !literal.IsKind(SyntaxKind.StringLiteralExpression))
      {
        return false;
      }

      fullyQualifiedName = NormalizeTargetValue(literal.Token.ValueText);
      return !string.IsNullOrWhiteSpace(fullyQualifiedName);
    }

    return false;
  }

  private static string? NormalizeTargetValue(string? target)
  {
    if (string.IsNullOrWhiteSpace(target))
    {
      return null;
    }

    var normalized = target.Trim();
    var prefix = '\0';
    if (normalized.StartsWith("~", StringComparison.Ordinal) && normalized.Length > 2 && normalized[2] == ':')
    {
      prefix = normalized[1];
      normalized = normalized[3..];
    }

    normalized = normalized.Replace('+', '.');
    normalized = RemoveArityMarkers(normalized);

    return prefix switch
    {
      'M' => NormalizeMethodTarget(normalized),
      'T' => NormalizeTypeTarget(normalized),
      'P' or 'F' or 'E' => NormalizeMemberTarget(normalized),
      '\0' => NormalizeMemberTarget(normalized),
      _ => null
    };
  }

  private static string? NormalizeMethodTarget(string raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    var candidate = raw.Contains('(') ? raw : $"{raw}(...)";
    return SymbolNormalizer.NormalizeFullyQualifiedMethodName(candidate);
  }

  private static string? NormalizeTypeTarget(string raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    return SymbolNormalizer.NormalizeTypeName(raw);
  }

  private static string? NormalizeMemberTarget(string raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    var candidate = raw.Contains('(') ? SymbolNormalizer.NormalizeFullyQualifiedMethodName(raw) : raw;
    if (candidate is null)
    {
      return null;
    }

    if (candidate.Contains('('))
    {
      return candidate;
    }

    var lastDot = candidate.LastIndexOf('.');
    if (lastDot <= 0)
    {
      return candidate;
    }

    var typePart = SymbolNormalizer.NormalizeTypeName(candidate[..lastDot]);
    var memberPart = candidate[(lastDot + 1)..];
    return string.IsNullOrWhiteSpace(typePart) || string.IsNullOrWhiteSpace(memberPart)
      ? null
      : $"{typePart}.{memberPart}";
  }

  private static string RemoveArityMarkers(string text)
  {
    return string.IsNullOrWhiteSpace(text)
      ? text
      : Regex.Replace(text, "`[0-9]+", string.Empty);
  }

  private static bool TryExtractCategory(AttributeArgumentSyntax argument, out string? category)
  {
    category = null;
    var categoryLiteral = argument.Expression as LiteralExpressionSyntax;
    var categoryValue = categoryLiteral?.Token.ValueText;
    var isKnownMetric = Enum.TryParse<MetricIdentifier>(categoryValue, ignoreCase: true, out _);

    if (string.IsNullOrWhiteSpace(categoryValue) ||
        (!categoryValue.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) &&
         !categoryValue.Equals("Style", StringComparison.OrdinalIgnoreCase) &&
         !isKnownMetric))
    {
      return false;
    }

    category = categoryValue;
    return true;
  }

  private static bool TryExtractRuleId(AttributeArgumentSyntax argument, out string? ruleId)
  {
    ruleId = null;
    var checkIdLiteral = argument.Expression as LiteralExpressionSyntax;
    var checkIdValue = checkIdLiteral?.Token.ValueText;
    if (string.IsNullOrWhiteSpace(checkIdValue))
    {
      return false;
    }

    var colonIndex = checkIdValue.IndexOf(':', StringComparison.Ordinal);
    ruleId = colonIndex > 0 ? checkIdValue[..colonIndex] : checkIdValue;
    return true;
  }

  private static string? ExtractJustification(SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
  {
    foreach (var argument in arguments)
    {
      if (argument.NameEquals is null ||
          !string.Equals(argument.NameEquals.Name.Identifier.Text, "Justification", StringComparison.Ordinal))
      {
        continue;
      }

      // WHY: Justification can be a single string literal or a concatenation of multiple
      // string literals (e.g., "string1" + "string2" + "string3"). We need to handle both cases
      // by recursively extracting string literals from binary expressions with the '+' operator.
      return ExtractJustificationText(argument.Expression);
    }

    return null;
  }

  /// <summary>
  /// Extracts justification text from an expression, handling both single string literals
  /// and string concatenation expressions (e.g., "string1" + "string2").
  /// </summary>
  /// <param name="expression">The expression to extract text from.</param>
  /// <returns>
  /// The extracted justification text, or <see langword="null"/> if the expression
  /// does not contain string literals.
  /// </returns>
  private static string? ExtractJustificationText(ExpressionSyntax? expression)
  {
    if (expression is null)
    {
      return null;
    }

    // Single string literal case
    if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      return literal.Token.ValueText;
    }

    // String concatenation case: "string1" + "string2" + ...
    if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
    {
      var parts = new List<string>();

      // WHY: Recursively collect all string literals from the left and right sides
      // of the binary expression. This handles nested concatenations like:
      // "string1" + ("string2" + "string3")
      CollectStringLiterals(binary, parts);

      return parts.Count > 0 ? string.Concat(parts) : null;
    }

    return null;
  }

  /// <summary>
  /// Recursively collects string literals from a binary expression tree.
  /// </summary>
  /// <param name="expression">The expression to traverse.</param>
  /// <param name="parts">The list to collect string literal values into.</param>
  private static void CollectStringLiterals(ExpressionSyntax expression, List<string> parts)
  {
    if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      var text = literal.Token.ValueText;
      if (!string.IsNullOrEmpty(text))
      {
        parts.Add(text);
      }
      return;
    }

    if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
    {
      // Traverse left and right subtrees
      CollectStringLiterals(binary.Left, parts);
      CollectStringLiterals(binary.Right, parts);
    }
  }

  /// <summary>
  /// Determines whether the given attribute is a SuppressMessage attribute.
  /// </summary>
  /// <param name="attribute">The attribute syntax node to check.</param>
  /// <returns>
  /// <see langword="true"/> if the attribute is a SuppressMessage attribute;
  /// otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// Supports both short form (SuppressMessage) and fully qualified form
  /// (System.Diagnostics.CodeAnalysis.SuppressMessage). The attribute name can be parsed
  /// as a simple identifier, qualified name, or alias-qualified name by Roslyn.
  /// We need to handle all cases to ensure suppressions work regardless of using directives.
  /// </remarks>
  private static bool IsSuppressMessageAttribute(AttributeSyntax attribute)
  {
    // Check the simple name (last identifier in the qualified name chain)
    string? simpleName = null;

    if (attribute.Name is SimpleNameSyntax simpleNameSyntax)
    {
      simpleName = simpleNameSyntax.Identifier.Text;
    }
    else if (attribute.Name is QualifiedNameSyntax qualifiedName)
    {
      // WHY: QualifiedNameSyntax has Left (NameSyntax) and Right (SimpleNameSyntax).
      // For "System.Diagnostics.CodeAnalysis.SuppressMessage", it's parsed as:
      // QualifiedName(QualifiedName(QualifiedName(System, Diagnostics), CodeAnalysis), SuppressMessage)
      // The Right property always contains the rightmost SimpleNameSyntax, which is the actual attribute name.
      // No traversal needed - Right is always the simple name we want.
      simpleName = qualifiedName.Right.Identifier.Text;
    }

    if (string.IsNullOrEmpty(simpleName))
    {
      // Fallback to string comparison if structure parsing fails
      var name = attribute.Name.ToString();
      return name.EndsWith("SuppressMessage", StringComparison.Ordinal) ||
             name.EndsWith("SuppressMessageAttribute", StringComparison.Ordinal);
    }

    // Check if the simple name matches (with or without "Attribute" suffix)
    return simpleName.Equals("SuppressMessage", StringComparison.Ordinal) ||
           simpleName.Equals("SuppressMessageAttribute", StringComparison.Ordinal);
  }
}


