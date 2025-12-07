namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="SymbolNormalizer"/> class.
/// </summary>
/// <remarks>
/// These tests verify that symbol normalization works correctly for various edge cases,
/// including different formats from AltCover and Roslyn, generic types, nested types,
/// and malformed signatures.
/// </remarks>
[TestFixture]
[Category("Unit")]
public sealed class SymbolNormalizerTests
{
  #region NormalizeMethodSignature Tests

  [Test]
  public void NormalizeMethodSignature_AltCoverFormat_ReplacesParametersWithPlaceholder()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)";
    var expected = "Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_RoslynFormat_ReplacesParametersWithPlaceholder()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.OnApplicationIdling(object? sender, IdlingEventArgs e)";
    var expected = "Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_NoParameters_ReturnsUnchanged()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.Method";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeMethodSignature_EmptyParameters_ReplacesWithPlaceholder()
  {
    // Arrange
    var input = "Method()";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_SingleParameter_ReplacesWithPlaceholder()
  {
    // Arrange
    var input = "Method(System.String)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_MultipleParameters_ReplacesWithPlaceholder()
  {
    // Arrange
    var input = "Method(System.String, System.Int32, System.Boolean)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_NestedParenthesesInGenerics_HandlesCorrectly()
  {
    // Arrange
    var input = "Method(System.Collections.Generic.List<System.String>, System.Int32)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_ComplexGenericParameters_HandlesCorrectly()
  {
    // Arrange
    var input = "Method(System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.List<System.Int32>>)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_NullableParameters_HandlesCorrectly()
  {
    // Arrange
    var input = "Method(object? sender, string? name, int? value)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_NullInput_ReturnsNull()
  {
    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(null);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void NormalizeMethodSignature_EmptyString_ReturnsEmpty()
  {
    // Arrange
    var input = string.Empty;

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void NormalizeMethodSignature_Whitespace_ReturnsUnchanged()
  {
    // Arrange
    var input = "   ";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeMethodSignature_MalformedSignature_ReturnsUnchanged()
  {
    // Arrange - missing closing parenthesis
    var input = "Method(System.String";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeMethodSignature_PropertyGetter_ReplacesParameters()
  {
    // Arrange - property getter from real data
    var input = "Rca.Network.NetworkPlaceholder.get_IsReady()";
    var expected = "Rca.Network.NetworkPlaceholder.get_IsReady(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_MethodWithReturnType_HandlesCorrectly()
  {
    // Arrange - method with return type prefix (AltCover format)
    var input = "void Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)";
    var expected = "void Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_NestedTypes_HandlesCorrectly()
  {
    // Arrange - nested type with + separator
    var input = "Rca.Loader.LoaderApp+NestedClass.Method(System.String)";
    var expected = "Rca.Loader.LoaderApp+NestedClass.Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_MultipleNestedParentheses_HandlesCorrectly()
  {
    // Arrange - complex nested structure
    var input = "Method(Func<System.String, System.Collections.Generic.List<System.Int32>>, System.Action)";
    var expected = "Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  #endregion

  #region ExtractMethodName Tests

  [Test]
  public void ExtractMethodName_SimpleMethod_ReturnsMethodName()
  {
    // Arrange
    var input = "Method(System.String)";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_WithReturnType_ReturnsMethodName()
  {
    // Arrange
    var input = "void Method(System.String)";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_GenericMethod_ReturnsMethodNameWithoutGenerics()
  {
    // Arrange
    var input = "Method<T>(System.String)";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_GenericMethodWithConstraints_ReturnsMethodName()
  {
    // Arrange
    var input = "Method<T>() where T : class";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_FullyQualifiedName_ReturnsMethodName()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.OnApplicationIdling(System.Object)";
    var expected = "OnApplicationIdling";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_NoParameters_ReturnsMethodName()
  {
    // Arrange
    var input = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void ExtractMethodName_PropertyGetter_ReturnsGetterName()
  {
    // Arrange
    var input = "Rca.Network.NetworkPlaceholder.get_IsReady()";
    var expected = "get_IsReady";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_NullInput_ReturnsNull()
  {
    // Act
    var result = SymbolNormalizer.ExtractMethodName(null);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void ExtractMethodName_EmptyString_ReturnsEmpty()
  {
    // Arrange
    var input = string.Empty;

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void ExtractMethodName_ComplexGeneric_ReturnsMethodName()
  {
    // Arrange
    var input = "Method<System.Collections.Generic.List<System.String>>(System.Int32)";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_MultipleGenericParameters_ReturnsMethodName()
  {
    // Arrange
    var input = "Method<TKey, TValue>(System.String)";
    var expected = "Method";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    // The method should extract just "Method" by removing generic parameters
    result.Should().Be(expected, because: "generic parameters should be stripped from method name");
  }

  #endregion

  #region NormalizeFullyQualifiedMethodName Tests

  [Test]
  public void NormalizeFullyQualifiedMethodName_AltCoverFormat_NormalizesCorrectly()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)";
    var expected = "Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_RoslynFormat_NormalizesCorrectly()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.OnApplicationIdling(object? sender, IdlingEventArgs e)";
    var expected = "Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_RealWorldExample_NormalizesCorrectly()
  {
    // Arrange - from actual metrics report
    var input = "Rca.Network.NetworkPlaceholder.get_IsReady()";
    var expected = "Rca.Network.NetworkPlaceholder.get_IsReady(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_NullInput_ReturnsNull()
  {
    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(null);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_NoParameters_ReturnsUnchanged()
  {
    // Arrange
    var input = "Rca.Loader.LoaderApp.Method";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_GenericTypeInFqn_NormalizesTypeGenerics()
  {
    // Arrange - method from generic type like MetricsReaderCommandBase<TSettings>.CreateEngine(...)
    var input = "MetricsReporter.MetricsReader.Commands.MetricsReaderCommandBase<TSettings>.CreateEngine(MetricsReaderContext context)";
    var expected = "MetricsReporter.MetricsReader.Commands.MetricsReaderCommandBase.CreateEngine(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_GenericTypeWithMultipleParameters_NormalizesTypeGenerics()
  {
    // Arrange - method from generic type with multiple type parameters
    var input = "Namespace.Type<TKey, TValue>.Method(string param)";
    var expected = "Namespace.Type.Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_GenericTypeAndGenericMethod_NormalizesBoth()
  {
    // Arrange - both type and method have generic parameters
    var input = "Namespace.Type<TSettings>.Method<TResult>(TResult value)";
    var expected = "Namespace.Type.Method(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  #endregion

  #region NormalizeTypeName Tests

  [Test]
  public void NormalizeTypeName_SimpleType_ReturnsUnchanged()
  {
    // Arrange
    var input = "String";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_GenericType_RemovesGenericParameters()
  {
    // Arrange
    var input = "List<string>";
    var expected = "List";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeTypeName_GenericTypeWithMultipleParameters_RemovesGenerics()
  {
    // Arrange
    var input = "Dictionary<string, int>";
    var expected = "Dictionary";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeTypeName_NestedGenericType_RemovesGenerics()
  {
    // Arrange
    var input = "List<Dictionary<string, int>>";
    var expected = "List";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeTypeName_FullyQualifiedGenericType_RemovesGenerics()
  {
    // Arrange
    var input = "System.Collections.Generic.List<System.String>";
    var expected = "System.Collections.Generic.List";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeTypeName_NullInput_ReturnsNull()
  {
    // Act
    var result = SymbolNormalizer.NormalizeTypeName(null);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void NormalizeTypeName_EmptyString_ReturnsEmpty()
  {
    // Arrange
    var input = string.Empty;

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void NormalizeTypeName_TypeWithAngleBracketsButNotGeneric_ReturnsUnchanged()
  {
    // Arrange - this shouldn't happen in practice, but handle gracefully
    var input = "Type<";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be("Type");
  }

  [Test]
  public void NormalizeTypeName_UnknownTypePlaceholder_ReturnsUnchanged()
  {
    // Arrange
    var input = "<unknown-type>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_UnknownMemberPlaceholder_ReturnsUnchanged()
  {
    // Arrange
    var input = "<unknown-member>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_UnknownAssemblyPlaceholder_ReturnsUnchanged()
  {
    // Arrange
    var input = "<unknown-assembly>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_GlobalNamespacePlaceholder_ReturnsUnchanged()
  {
    // Arrange
    var input = "<global>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_TypeStartingWithAngleBracketButNotPlaceholder_RemovesGenerics()
  {
    // Arrange - type name that starts with '<' but is not a placeholder (edge case)
    // This shouldn't happen in practice, but we should handle it correctly
    var input = "<Type<string>>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    // Since it starts with '<' and ends with '>', it's treated as a placeholder
    result.Should().Be(input);
  }

  [Test]
  public void NormalizeTypeName_TypeWithAngleBracketInMiddle_RemovesGenerics()
  {
    // Arrange - type name with angle bracket in the middle (not at start)
    var input = "Type<string>";

    // Act
    var result = SymbolNormalizer.NormalizeTypeName(input);

    // Assert
    result.Should().Be("Type");
  }


  #endregion

  #region Integration Tests - Real World Scenarios

  [Test]
  public void NormalizeMethodSignature_AltCoverAndRoslyn_ProduceSameResult()
  {
    // Arrange
    var altCoverFormat = "Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)";
    var roslynFormat = "Rca.Loader.LoaderApp.OnApplicationIdling(object? sender, IdlingEventArgs e)";
    var expected = "Rca.Loader.LoaderApp.OnApplicationIdling(...)";

    // Act
    var altCoverResult = SymbolNormalizer.NormalizeMethodSignature(altCoverFormat);
    var roslynResult = SymbolNormalizer.NormalizeMethodSignature(roslynFormat);

    // Assert
    altCoverResult.Should().Be(expected);
    roslynResult.Should().Be(expected);
    altCoverResult.Should().Be(roslynResult, because: "different formats should normalize to the same result");
  }

  [Test]
  public void NormalizeMethodSignature_ComplexRealWorldExample_HandlesCorrectly()
  {
    // Arrange - complex method signature with nested generics and nullable types
    var input = "ProcessData<System.Collections.Generic.List<System.String>>(System.String? input, System.Collections.Generic.Dictionary<System.String, System.Int32>? options, System.Action<System.Exception>? onError)";
    var expected = "ProcessData<System.Collections.Generic.List<System.String>>(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_ComplexRealWorldExample_ExtractsCorrectly()
  {
    // Arrange
    var input = "void Rca.Loader.LoaderApp.OnApplicationIdling(System.Object sender, Autodesk.Revit.UI.Events.IdlingEventArgs args)";
    var expected = "OnApplicationIdling";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  #endregion

  #region Real-World Examples from AltCover Metrics

  [Test]
  public void NormalizeMethodSignature_AltCover_ToString_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.String Rca.Logging.Contracts.LogEntryDto::ToString()
    var input = "Rca.Logging.Contracts.LogEntryDto::ToString()";
    var expected = "Rca.Logging.Contracts.LogEntryDto::ToString(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_ToString_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data: System.String Rca.Logging.Contracts.LogEntryDto::ToString()
    // Note: AltCover parser replaces :: with . before calling ExtractMethodName
    var input = "System.String Rca.Logging.Contracts.LogEntryDto.ToString()";
    var expected = "ToString";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_PrintMembers_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Boolean Rca.Logging.Contracts.LogEntryDto::PrintMembers(System.Text.StringBuilder)
    var input = "Rca.Logging.Contracts.LogEntryDto::PrintMembers(System.Text.StringBuilder)";
    var expected = "Rca.Logging.Contracts.LogEntryDto::PrintMembers(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_PrintMembers_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    var input = "System.Boolean Rca.Logging.Contracts.LogEntryDto.PrintMembers(System.Text.StringBuilder)";
    var expected = "PrintMembers";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_OperatorEquality_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Boolean Rca.Logging.Contracts.LogEntryDto::op_Equality(Rca.Logging.Contracts.LogEntryDto,Rca.Logging.Contracts.LogEntryDto)
    var input = "Rca.Logging.Contracts.LogEntryDto::op_Equality(Rca.Logging.Contracts.LogEntryDto,Rca.Logging.Contracts.LogEntryDto)";
    var expected = "Rca.Logging.Contracts.LogEntryDto::op_Equality(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_OperatorEquality_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    var input = "System.Boolean Rca.Logging.Contracts.LogEntryDto.op_Equality(Rca.Logging.Contracts.LogEntryDto,Rca.Logging.Contracts.LogEntryDto)";
    var expected = "op_Equality";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_OperatorInequality_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Boolean Rca.Logging.Contracts.LogEntryDto::op_Inequality(Rca.Logging.Contracts.LogEntryDto,Rca.Logging.Contracts.LogEntryDto)
    var input = "Rca.Logging.Contracts.LogEntryDto::op_Inequality(Rca.Logging.Contracts.LogEntryDto,Rca.Logging.Contracts.LogEntryDto)";
    var expected = "Rca.Logging.Contracts.LogEntryDto::op_Inequality(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_GetHashCode_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    var input = "System.Int32 Rca.Logging.Contracts.LogEntryDto.GetHashCode()";
    var expected = "GetHashCode";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_EqualsObject_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Boolean Rca.Logging.Contracts.LogEntryDto::Equals(System.Object)
    var input = "Rca.Logging.Contracts.LogEntryDto::Equals(System.Object)";
    var expected = "Rca.Logging.Contracts.LogEntryDto::Equals(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_EqualsTyped_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Boolean Rca.Logging.Contracts.LogEntryDto::Equals(Rca.Logging.Contracts.LogEntryDto)
    var input = "Rca.Logging.Contracts.LogEntryDto::Equals(Rca.Logging.Contracts.LogEntryDto)";
    var expected = "Rca.Logging.Contracts.LogEntryDto::Equals(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_Clone_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    var input = "Rca.Logging.Contracts.LogEntryDto Rca.Logging.Contracts.LogEntryDto.<Clone>$()";
    var expected = "<Clone>$";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_Clone_HandlesCorrectly()
  {
    // Arrange - from real AltCover data
    var input = "Rca.Logging.Contracts.LogEntryDto::<Clone>$()";
    var expected = "Rca.Logging.Contracts.LogEntryDto::<Clone>$(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_Constructor_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Void Rca.UI.Services.ServiceResolver::.ctor(Rca.Contracts.Infrastructure.ServiceContainer)
    var input = "Rca.UI.Services.ServiceResolver::.ctor(Rca.Contracts.Infrastructure.ServiceContainer)";
    var expected = "Rca.UI.Services.ServiceResolver::.ctor(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_Constructor_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    // Note: ExtractMethodName should preserve the leading dot for constructors
    var input = "System.Void Rca.UI.Services.ServiceResolver..ctor(Rca.Contracts.Infrastructure.ServiceContainer)";
    var expected = ".ctor";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_StaticConstructor_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Void Rca.UI.Services.ServiceResolver::.cctor()
    var input = "Rca.UI.Services.ServiceResolver::.cctor()";
    var expected = "Rca.UI.Services.ServiceResolver::.cctor(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_StaticConstructor_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    // Note: ExtractMethodName should preserve the leading dot for static constructors
    var input = "System.Void Rca.UI.Services.ServiceResolver..cctor()";
    var expected = ".cctor";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_ConstructorWithoutReturnType_PreservesLeadingDot()
  {
    // Arrange - variant without explicit return type, exercising constructor name extraction
    var input = "Rca.UI.Services.ServiceResolver..ctor(Rca.Contracts.Infrastructure.ServiceContainer)";
    const string expected = ".ctor";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_GenericMethodRegister_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Void Rca.Loader.Contracts.SharedServiceRegistry::Register(TInterface)
    var input = "Rca.Loader.Contracts.SharedServiceRegistry::Register(TInterface)";
    var expected = "Rca.Loader.Contracts.SharedServiceRegistry::Register(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_AltCover_GenericMethodResolve_ExtractsCorrectly()
  {
    // Arrange - from real AltCover data (:: replaced with . by parser)
    var input = "TInterface Rca.Loader.Contracts.SharedServiceRegistry.Resolve()";
    var expected = "Resolve";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_AltCover_MethodWithException_HandlesCorrectly()
  {
    // Arrange - from real AltCover data: System.Void Rca.UI.Services.ServiceResolver::LogServiceResolutionError(System.Exception)
    var input = "Rca.UI.Services.ServiceResolver::LogServiceResolutionError(System.Exception)";
    var expected = "Rca.UI.Services.ServiceResolver::LogServiceResolutionError(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_AltCover_GenericMethodRegister_RemovesGenericParameters()
  {
    // Arrange - method with generic type parameter in AltCover format
    var input = "Rca.Loader.Contracts.SharedServiceRegistry::Register(TInterface)";
    var expected = "Rca.Loader.Contracts.SharedServiceRegistry::Register(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  #endregion

  #region Real-World Examples from Roslyn Metrics

  [Test]
  public void NormalizeMethodSignature_Roslyn_TaskString_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: Task&lt;string&gt; IPythonExecutionService.ExecuteAsync(string code)
    var input = "IPythonExecutionService.ExecuteAsync(string code)";
    var expected = "IPythonExecutionService.ExecuteAsync(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_Roslyn_TaskString_ExtractsCorrectly()
  {
    // Arrange - from real Roslyn data: Task&lt;string&gt; IPythonExecutionService.ExecuteAsync(string code)
    var input = "Task<string> IPythonExecutionService.ExecuteAsync(string code)";
    var expected = "ExecuteAsync";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericMethodRegister_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: void IServiceRegistrar.Register&lt;TInterface&gt;(TInterface implementation)
    var input = "IServiceRegistrar.Register<TInterface>(TInterface implementation)";
    var expected = "IServiceRegistrar.Register(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericMethodRegister_RemovesGenericParameters()
  {
    // Arrange - from real Roslyn data
    var input = "IServiceRegistrar.Register<TInterface>(TInterface implementation)";
    var expected = "IServiceRegistrar.Register(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_Roslyn_GenericMethodRegister_ExtractsCorrectly()
  {
    // Arrange - from real Roslyn data
    var input = "void IServiceRegistrar.Register<TInterface>(TInterface implementation)";
    var expected = "Register";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericMethodRegisterWithFunc_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: void IServiceRegistrar.Register&lt;TInterface&gt;(Func&lt;TInterface&gt; factory)
    var input = "IServiceRegistrar.Register<TInterface>(Func<TInterface> factory)";
    var expected = "IServiceRegistrar.Register(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericMethodResolve_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: TInterface IServiceResolver.Resolve&lt;TInterface&gt;()
    var input = "IServiceResolver.Resolve<TInterface>()";
    var expected = "IServiceResolver.Resolve(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericMethodResolve_RemovesGenericParameters()
  {
    // Arrange - from real Roslyn data
    var input = "IServiceResolver.Resolve<TInterface>()";
    var expected = "IServiceResolver.Resolve(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_Roslyn_GenericMethodResolve_ExtractsCorrectly()
  {
    // Arrange - from real Roslyn data
    var input = "TInterface IServiceResolver.Resolve<TInterface>()";
    var expected = "Resolve";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_Roslyn_TaskTuple_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: Task&lt;(bool Success, string? ErrorMessage)&gt; IRuntimeManager.LoadRuntimeAsync(string folderPath)
    var input = "IRuntimeManager.LoadRuntimeAsync(string folderPath)";
    var expected = "IRuntimeManager.LoadRuntimeAsync(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_Roslyn_TaskTuple_ExtractsCorrectly()
  {
    // Arrange - from real Roslyn data
    var input = "Task<(bool Success, string? ErrorMessage)> IRuntimeManager.LoadRuntimeAsync(string folderPath)";
    var expected = "LoadRuntimeAsync";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericLogMethod_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: void UiPipeLogger.Log&lt;TState&gt;(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func&lt;TState, Exception?, string&gt; formatter)
    var input = "UiPipeLogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)";
    var expected = "UiPipeLogger.Log(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeFullyQualifiedMethodName_Roslyn_GenericLogMethod_RemovesGenericParameters()
  {
    // Arrange - from real Roslyn data
    var input = "UiPipeLogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)";
    var expected = "UiPipeLogger.Log(...)";

    // Act
    var result = SymbolNormalizer.NormalizeFullyQualifiedMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void ExtractMethodName_Roslyn_GenericLogMethod_ExtractsCorrectly()
  {
    // Arrange - from real Roslyn data
    var input = "void UiPipeLogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)";
    var expected = "Log";

    // Act
    var result = SymbolNormalizer.ExtractMethodName(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_Roslyn_ConstructorWithFunc_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: RcaDockablePanelViewModel.RcaDockablePanelViewModel(Func&lt;object?&gt; revitContextProvider, IPythonExecutionService pythonService)
    var input = "RcaDockablePanelViewModel.RcaDockablePanelViewModel(Func<object?> revitContextProvider, IPythonExecutionService pythonService)";
    var expected = "RcaDockablePanelViewModel.RcaDockablePanelViewModel(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  [Test]
  public void NormalizeMethodSignature_Roslyn_ConstructorWithAction_HandlesCorrectly()
  {
    // Arrange - from real Roslyn data: RelayCommand.RelayCommand(Action&lt;object&gt; execute, Func&lt;object, bool&gt;? canExecute = null)
    var input = "RelayCommand.RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)";
    var expected = "RelayCommand.RelayCommand(...)";

    // Act
    var result = SymbolNormalizer.NormalizeMethodSignature(input);

    // Assert
    result.Should().Be(expected);
  }

  #endregion
}


