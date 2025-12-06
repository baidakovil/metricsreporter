namespace MetricsReporter.Processing;

using System;

/// <summary>
/// Normalizes symbol names (methods, types) from different metric sources to a unified format.
/// </summary>
/// <remarks>
/// This service handles the discrepancy between different metric sources:
/// - AltCover uses fully qualified type names: <c>Method(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)</c>
/// - Roslyn uses short type names with nullable annotations: <c>Method(object? sender, IdlingEventArgs)</c>
/// 
/// Both are normalized to: <c>Method(...)</c> to ensure symbols from different sources are properly merged.
/// </remarks>
public static class SymbolNormalizer
{
  private const string ParameterPlaceholder = "...";

  /// <summary>
  /// Determines whether the given name is a special placeholder name that should not be normalized.
  /// </summary>
  /// <param name="name">The name to check.</param>
  /// <returns>
  /// <see langword="true"/> if the name is a special placeholder (e.g., "&lt;unknown-type&gt;", "&lt;unknown-member&gt;", "&lt;unknown-assembly&gt;", "&lt;global&gt;");
  /// otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// Special placeholder names are used when actual names are missing from metric sources.
  /// These placeholders should be preserved as-is and not treated as generic type parameters.
  /// </remarks>
  private static bool IsSpecialPlaceholderName(string? name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return false;
    }

    // Special placeholder names are enclosed in angle brackets and start with '<'
    // Examples: "<unknown-type>", "<unknown-member>", "<unknown-assembly>", "<global>"
    return name.StartsWith('<') && name.EndsWith('>');
  }

  /// <summary>
  /// Normalizes a method signature by removing parameter details and replacing them with a placeholder.
  /// </summary>
  /// <param name="methodSignature">The method signature to normalize (e.g., "Method(System.Object, System.String)" or "Method(object? sender, string name)").</param>
  /// <returns>
  /// Normalized method signature with parameters replaced by <c>...</c> (e.g., "Method(...)").
  /// If the input is <see langword="null"/> or whitespace, returns the input unchanged.
  /// </returns>
  /// <remarks>
  /// This method handles various signature formats:
  /// - AltCover format: <c>Namespace.Type.Method(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)</c>
  /// - Roslyn format: <c>Namespace.Type.Method(object? sender, IdlingEventArgs e)</c>
  /// - Both are normalized to: <c>Namespace.Type.Method(...)</c>
  /// 
  /// The normalization process:
  /// 1. Finds the opening parenthesis of the parameter list
  /// 2. Replaces everything from the opening parenthesis to the matching closing parenthesis with "..."
  /// 3. Handles nested parentheses in generic types (e.g., <c>Method(List&lt;string&gt; items)</c>)
  /// </remarks>
  public static string? NormalizeMethodSignature(string? methodSignature)
  {
    if (string.IsNullOrWhiteSpace(methodSignature))
    {
      return methodSignature;
    }

    var paramStart = methodSignature.IndexOf('(');
    if (paramStart < 0)
    {
      // No parameters, return as-is
      return methodSignature;
    }

    // Find the matching closing parenthesis, handling nested parentheses
    var paramEnd = FindMatchingClosingParenthesis(methodSignature, paramStart);
    if (paramEnd < 0)
    {
      // Malformed signature, return as-is
      return methodSignature;
    }

    // Replace parameters with placeholder
    return methodSignature[..(paramStart + 1)] + ParameterPlaceholder + methodSignature[paramEnd..];
  }

  /// <summary>
  /// Extracts the method name without parameters from a full method signature.
  /// </summary>
  /// <param name="methodSignature">The method signature (e.g., "Method(System.Object)" or "Method(object? sender)").</param>
  /// <returns>
  /// The method name without parameters (e.g., "Method").
  /// If the input is <see langword="null"/> or whitespace, returns the input unchanged.
  /// </returns>
  /// <remarks>
  /// This method extracts just the method name part, removing:
  /// - Return type prefix (e.g., "void Method(...)")
  /// - Parameter list (e.g., "Method(System.Object)")
  /// - Generic type parameters (e.g., "Method&lt;T&gt;(...)")
  /// </remarks>
  public static string? ExtractMethodName(string? methodSignature)
  {
    if (string.IsNullOrWhiteSpace(methodSignature))
    {
      return methodSignature;
    }

    var nameStart = FindMethodNameStart(methodSignature);
    var methodNameEnd = FindMethodNameEnd(methodSignature, nameStart);
    var methodNameWithoutGenerics = ExtractMethodNameWithoutGenerics(methodSignature, nameStart, methodNameEnd);
    var extractedName = ExtractNameAfterLastDot(methodNameWithoutGenerics);
    return NormalizeConstructorName(extractedName, methodNameWithoutGenerics);
  }

  private static int FindMethodNameStart(string methodSignature)
  {
    var spaceIndex = methodSignature.IndexOf(' ');
    return spaceIndex >= 0 ? spaceIndex + 1 : 0;
  }

  private static int FindMethodNameEnd(string methodSignature, int nameStart)
  {
    var paramStart = methodSignature.IndexOf('(', nameStart);
    var whereIndex = methodSignature.IndexOf(" where ", StringComparison.Ordinal);

    var methodNameEnd = methodSignature.Length;
    if (paramStart >= nameStart)
    {
      methodNameEnd = paramStart;
    }
    if (whereIndex >= nameStart && whereIndex < methodNameEnd)
    {
      methodNameEnd = whereIndex;
    }

    return methodNameEnd;
  }

  private static string ExtractMethodNameWithoutGenerics(string methodSignature, int nameStart, int methodNameEnd)
  {
    var genericStartInSignature = methodSignature.IndexOf('<', nameStart);
    if (genericStartInSignature < 0 || genericStartInSignature >= methodNameEnd)
    {
      return methodSignature[nameStart..methodNameEnd].Trim();
    }

    return ExtractMethodNameHandlingGenerics(methodSignature, nameStart, methodNameEnd, genericStartInSignature);
  }

  private static string ExtractMethodNameHandlingGenerics(
    string methodSignature,
    int nameStart,
    int methodNameEnd,
    int genericStartInSignature)
  {
    var genericEnd = FindMatchingClosingAngleBracket(methodSignature, genericStartInSignature);
    if (genericEnd < 0 || genericEnd >= methodNameEnd)
    {
      return methodSignature[nameStart..methodNameEnd].Trim();
    }

    if (IsGenericParameterList(methodSignature, genericEnd, methodNameEnd))
    {
      return methodSignature[nameStart..genericStartInSignature].Trim();
    }

    return methodSignature[nameStart..methodNameEnd].Trim();
  }

  private static bool IsGenericParameterList(string methodSignature, int genericEnd, int methodNameEnd)
  {
    var afterGeneric = genericEnd + 1;
    return afterGeneric >= methodNameEnd ||
           methodSignature[afterGeneric] == ' ' ||
           methodSignature[afterGeneric] == '(' ||
           methodSignature[afterGeneric] == ')';
  }

  private static string ExtractNameAfterLastDot(string methodNameWithoutGenerics)
  {
    var lastDot = methodNameWithoutGenerics.LastIndexOf('.');
    return lastDot >= 0 ? methodNameWithoutGenerics[(lastDot + 1)..].Trim() : methodNameWithoutGenerics.Trim();
  }

  private static string NormalizeConstructorName(string extractedName, string methodNameWithoutGenerics)
  {
    if (extractedName != "ctor" && extractedName != "cctor")
    {
      return extractedName;
    }

    var lastDot = methodNameWithoutGenerics.LastIndexOf('.');
    var beforeLastDot = lastDot > 0 ? methodNameWithoutGenerics[..lastDot] : string.Empty;
    if (beforeLastDot.EndsWith('.'))
    {
      return "." + extractedName;
    }

    return extractedName;
  }

  /// <summary>
  /// Normalizes a fully qualified method name by normalizing the method signature part.
  /// </summary>
  /// <param name="fullyQualifiedMethodName">The fully qualified method name (e.g., "Namespace.Type.Method(System.Object)").</param>
  /// <returns>
  /// Normalized fully qualified method name (e.g., "Namespace.Type.Method(...)").
  /// If the input is <see langword="null"/> or whitespace, returns the input unchanged.
  /// </returns>
  /// <remarks>
  /// This method preserves the namespace and type parts while normalizing only the method signature.
  /// It handles both AltCover and Roslyn formats by applying signature normalization to the method part.
  /// 
  /// The method works by:
  /// 1. Normalizing generic type parameters in the type part (e.g., "Type&lt;T&gt;.Method(...)" -> "Type.Method(...)")
  /// 2. Finding the parameter list (opening parenthesis)
  /// 3. Finding the matching closing parenthesis (handling nested parentheses in generic types)
  /// 4. Replacing the entire parameter list with "..."
  /// 5. Removing generic parameters from the method name (e.g., "Method&lt;T&gt;(...)" -> "Method(...)")
  /// 
  /// This approach ensures that methods from generic types are properly matched with suppressions
  /// regardless of whether the type's generic parameters are included in the FQN.
  /// </remarks>
  public static string? NormalizeFullyQualifiedMethodName(string? fullyQualifiedMethodName)
  {
    if (string.IsNullOrWhiteSpace(fullyQualifiedMethodName))
    {
      return fullyQualifiedMethodName;
    }

    // First, normalize generic type parameters in the type part of the FQN
    // This handles cases like "Type<T>.Method(...)" -> "Type.Method(...)"
    // We need to find the last dot before the method name to separate type and method parts
    var paramStart = fullyQualifiedMethodName.IndexOf('(');
    var searchEnd = paramStart >= 0 ? paramStart : fullyQualifiedMethodName.Length;

    // Find the last dot before the method parameter list (separates type from method)
    var lastDotBeforeMethod = fullyQualifiedMethodName.LastIndexOf('.', searchEnd - 1);
    if (lastDotBeforeMethod >= 0)
    {
      // Extract the type part (everything before the last dot)
      var typePart = fullyQualifiedMethodName[..lastDotBeforeMethod];
      // Extract the method part (everything from the last dot onwards)
      var methodPart = fullyQualifiedMethodName[lastDotBeforeMethod..];

      // Normalize generic parameters in the type part
      var normalizedTypePart = NormalizeTypeName(typePart);
      if (normalizedTypePart != typePart)
      {
        // Reconstruct FQN with normalized type part
        fullyQualifiedMethodName = normalizedTypePart + methodPart;
        // Update search end since the string length may have changed
        paramStart = fullyQualifiedMethodName.IndexOf('(');
        searchEnd = paramStart >= 0 ? paramStart : fullyQualifiedMethodName.Length;
      }
    }

    // Then, remove generic type parameters from the method name (e.g., "Process<T>" -> "Process")
    // This ensures methods with different generic parameters are treated as the same method for aggregation
    // Note: We need to distinguish between actual generic parameters (Method<T>) and
    // method names that contain angle brackets (like <Clone>$)
    var genericStart = fullyQualifiedMethodName.IndexOf('<');
    if (genericStart >= 0 && genericStart < searchEnd)
    {
      var genericEnd = FindMatchingClosingAngleBracket(fullyQualifiedMethodName, genericStart);
      if (genericEnd >= 0 && genericEnd < searchEnd)
      {
        // Check if after '>' there's a space, '(', ')' or end of method name part
        // This indicates it's a generic parameter list (Method<T>(...) or Method<T> where ...)
        var afterGeneric = genericEnd + 1;
        if (afterGeneric >= searchEnd ||
            fullyQualifiedMethodName[afterGeneric] == ' ' ||
            fullyQualifiedMethodName[afterGeneric] == '(' ||
            fullyQualifiedMethodName[afterGeneric] == ')')
        {
          // It's a generic parameter list - remove it: "Method<T>(...)" -> "Method(...)"
          fullyQualifiedMethodName = fullyQualifiedMethodName[..genericStart] + fullyQualifiedMethodName[(genericEnd + 1)..];
        }
        // Otherwise, '<' is part of the method name (like <Clone>$), so don't remove it
      }
    }

    // Finally, apply method signature normalization which will find and replace parameters
    return NormalizeMethodSignature(fullyQualifiedMethodName);
  }

  /// <summary>
  /// Normalizes a type name by removing generic type parameters.
  /// </summary>
  /// <param name="typeName">The type name to normalize (e.g., "List&lt;string&gt;" or "Dictionary&lt;string, int&gt;").</param>
  /// <returns>
  /// Normalized type name without generic parameters (e.g., "List").
  /// If the input is <see langword="null"/> or whitespace, returns the input unchanged.
  /// Special placeholder names like "&lt;unknown-type&gt;" are returned unchanged.
  /// </returns>
  /// <remarks>
  /// This method removes generic type parameters to ensure types with different generic arguments
  /// are treated as the same base type for aggregation purposes.
  /// Special placeholder names (enclosed in angle brackets) are preserved as-is to maintain their semantic meaning.
  /// </remarks>
  public static string? NormalizeTypeName(string? typeName)
  {
    if (string.IsNullOrWhiteSpace(typeName))
    {
      return typeName;
    }

    // Preserve special placeholder names (e.g., "<unknown-type>", "<unknown-member>", "<unknown-assembly>", "<global>")
    // These are not generic type parameters and should be returned unchanged
    if (IsSpecialPlaceholderName(typeName))
    {
      return typeName;
    }

    var genericStart = typeName.IndexOf('<');
    if (genericStart < 0)
    {
      return typeName;
    }

    return typeName[..genericStart].Trim();
  }

  private static int FindMatchingClosingParenthesis(string text, int openIndex)
  {
    var depth = 0;
    for (var i = openIndex; i < text.Length; i++)
    {
      var ch = text[i];
      if (ch == '(')
      {
        depth++;
      }
      else if (ch == ')')
      {
        depth--;
        if (depth == 0)
        {
          return i;
        }
      }
    }

    return -1;
  }

  private static int FindMatchingClosingAngleBracket(string text, int openIndex)
  {
    var depth = 0;
    for (var i = openIndex; i < text.Length; i++)
    {
      var ch = text[i];
      if (ch == '<')
      {
        depth++;
      }
      else if (ch == '>')
      {
        depth--;
        if (depth == 0)
        {
          return i;
        }
      }
    }

    return -1;
  }
}


