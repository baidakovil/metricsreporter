namespace MetricsReporter.Processing;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds fully qualified names (FQNs) from namespace and type stacks
/// for use in symbol suppression analysis.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for constructing FQNs from hierarchical
/// namespace and type information, reducing coupling in syntax walker classes.
/// </remarks>
internal sealed class FullyQualifiedNameBuilder
{
  private readonly Stack<string> _namespaceStack = new();
  private readonly Stack<string> _typeStack = new();

  /// <summary>
  /// Pushes a namespace name onto the namespace stack.
  /// </summary>
  /// <param name="namespaceName">The namespace name to push.</param>
  public void PushNamespace(string namespaceName)
  {
    _namespaceStack.Push(namespaceName);
  }

  /// <summary>
  /// Pops a namespace name from the namespace stack.
  /// </summary>
  public void PopNamespace()
  {
    _namespaceStack.Pop();
  }

  /// <summary>
  /// Pushes a type name onto the type stack.
  /// </summary>
  /// <param name="typeName">The type name to push.</param>
  public void PushType(string typeName)
  {
    _typeStack.Push(typeName);
  }

  /// <summary>
  /// Pops a type name from the type stack.
  /// </summary>
  public void PopType()
  {
    _typeStack.Pop();
  }

  /// <summary>
  /// Builds a fully qualified name for the current type.
  /// </summary>
  /// <returns>
  /// The FQN of the current type (for example, <c>Namespace.Type</c>),
  /// or <see langword="null"/> if no type is on the stack.
  /// </returns>
  public string? BuildTypeFqn()
  {
    if (_typeStack.Count == 0)
    {
      return null;
    }

    var typeName = string.Join(".", _typeStack.Reverse());
    var ns = _namespaceStack.Count == 0 ? null : string.Join(".", _namespaceStack.Reverse());
    return string.IsNullOrWhiteSpace(ns) ? typeName : ns + "." + typeName;
  }

  /// <summary>
  /// Builds a fully qualified name for a member (method, property, etc.) of the current type.
  /// </summary>
  /// <param name="memberIdentifier">The identifier of the member (method name, property name, etc.).</param>
  /// <returns>
  /// The normalized FQN of the member (for example, <c>Namespace.Type.Method(...)</c>),
  /// or <see langword="null"/> if no type is on the stack.
  /// </returns>
  /// <remarks>
  /// Parameter details are not required because the normalizer will collapse
  /// them to "(...)" and only preserve the namespace/type/method name chain.
  /// </remarks>
  public string? BuildMemberFqn(string memberIdentifier)
  {
    var typeFqn = BuildTypeFqn();
    if (string.IsNullOrWhiteSpace(typeFqn))
    {
      return null;
    }

    // Parameter details are not required because the normalizer will collapse
    // them to "(...)" and only preserve the namespace/type/method name chain.
    var raw = $"{typeFqn}.{memberIdentifier}()";
    return SymbolNormalizer.NormalizeFullyQualifiedMethodName(raw);
  }

  /// <summary>
  /// Builds a fully qualified name for a property of the current type.
  /// </summary>
  /// <param name="propertyIdentifier">The identifier of the property.</param>
  /// <returns>
  /// The FQN of the property (for example, <c>Namespace.Type.Property</c>),
  /// or <see langword="null"/> if no type is on the stack.
  /// </returns>
  public string? BuildPropertyFqn(string propertyIdentifier)
  {
    var typeFqn = BuildTypeFqn();
    if (string.IsNullOrWhiteSpace(typeFqn))
    {
      return null;
    }

    return $"{typeFqn}.{propertyIdentifier}";
  }
}


